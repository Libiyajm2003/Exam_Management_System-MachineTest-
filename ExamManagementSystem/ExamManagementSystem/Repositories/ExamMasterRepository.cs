using System.Data;
using ExamManagementSystem.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ExamManagementSystem.Repositories;

public class ExamMasterRepository : IExamMasterRepository
{
    private readonly string _connectionString;

    public ExamMasterRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    private SqlConnection CreateConnection() => new(_connectionString);

    public async Task<List<ExamMaster>> GetExamMastersAsync()
    {
        const string storedProc = "Exams_WithDetails_GetAll";
        var masters = new List<ExamMaster>();

        await using var connection = CreateConnection();
        await using var command = new SqlCommand(storedProc, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        var masterDict = new Dictionary<int, ExamMaster>();

        // First result set: masters + student info
        while (await reader.ReadAsync())
        {
            var masterId = reader.GetInt32(reader.GetOrdinal("MasterID"));
            var master = new ExamMaster
            {
                MasterID = masterId,
                StudentID = reader.GetInt32(reader.GetOrdinal("StudentID")),
                ExamYear = reader.GetInt32(reader.GetOrdinal("ExamYear")),
                TotalMark = reader.GetDecimal(reader.GetOrdinal("TotalMark")),
                PassOrFail = reader.GetString(reader.GetOrdinal("PassOrFail")),
                CreateTime = reader.GetDateTime(reader.GetOrdinal("CreateTime")),
                Student = new StudentMst
                {
                    StudentID = reader.GetInt32(reader.GetOrdinal("StudentID")),
                    StudentName = reader.GetString(reader.GetOrdinal("StudentName")),
                    Mail = reader.GetString(reader.GetOrdinal("Mail"))
                }
            };

            masterDict[masterId] = master;
            masters.Add(master);
        }

        // Move to second result set: detail rows
        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                var masterId = reader.GetInt32(reader.GetOrdinal("MasterID"));
                if (!masterDict.TryGetValue(masterId, out var master))
                {
                    continue;
                }

                var detail = new ExamDtls
                {
                    DtlsID = reader.GetInt32(reader.GetOrdinal("DtlsID")),
                    MasterID = masterId,
                    SubjectID = reader.GetInt32(reader.GetOrdinal("SubjectID")),
                    Marks = reader.GetDecimal(reader.GetOrdinal("Marks")),
                    Subject = new SubjectMst
                    {
                        SubjectID = reader.GetInt32(reader.GetOrdinal("SubjectID")),
                        SubjectName = reader.GetString(reader.GetOrdinal("SubjectName"))
                    }
                };

                master.Details.Add(detail);
            }
        }

        return masters;
    }

    public async Task<ExamMaster?> GetExamByIdAsync(int masterId)
    {
        var masters = await GetExamMastersByIdsAsync(new[] { masterId });
        return masters.FirstOrDefault();
    }

    public async Task<ExamMaster> AddExamAsync(ExamMaster masterInput)
    {
        if (masterInput.Details == null || masterInput.Details.Count == 0)
        {
            throw new ArgumentException("At least one subject mark is required.");
        }

        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await using var transaction = connection.BeginTransaction();

        try
        {
            // Check student exists
            await EnsureStudentExists(masterInput.StudentID, connection, transaction);

            // Check unique StudentID + ExamYear
            var exists = await CheckExamExists(masterInput.StudentID, masterInput.ExamYear, connection, transaction);
            if (exists)
            {
                throw new InvalidOperationException("An exam for this student and year already exists.");
            }

            // Validate subject IDs
            await ValidateSubjectIdsAsync(masterInput.Details.Select(d => d.SubjectID).ToList(), connection, transaction);

            var masterId = await InsertExamMasterAsync(masterInput, connection, transaction);

            foreach (var detail in masterInput.Details)
            {
                await InsertExamDetailAsync(masterId, detail, connection, transaction);
            }

            await FinalizeExamAsync(masterId, connection, transaction);

            await transaction.CommitAsync();

            // Return the newly inserted record with details
            var masters = await GetExamMastersByIdsAsync(new[] { masterId });
            return masters.First();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task EnsureStudentExists(int studentId, SqlConnection connection, SqlTransaction transaction)
    {
        const string storedProc = "Student_GetById";
        await using var command = new SqlCommand(storedProc, connection, transaction)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@StudentID", studentId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            throw new KeyNotFoundException("Student not found.");
        }
    }

    private async Task<bool> CheckExamExists(int studentId, int year, SqlConnection connection, SqlTransaction transaction)
    {
        const string storedProc = "Exam_CheckExists";
        await using var command = new SqlCommand(storedProc, connection, transaction)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@StudentID", studentId);
        command.Parameters.AddWithValue("@ExamYear", year);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    private async Task ValidateSubjectIdsAsync(List<int> subjectIds, SqlConnection connection, SqlTransaction transaction)
    {
        var distinctIds = subjectIds.Distinct().ToList();
        if (!distinctIds.Any())
        {
            throw new ArgumentException("One or more subjects are invalid.");
        }

        var parameters = distinctIds.Select((id, idx) => new SqlParameter($"@p{idx}", id)).ToArray();
        var inClause = string.Join(",", parameters.Select(p => p.ParameterName));
        var cmdText = $"SELECT COUNT(*) FROM SubjectMst WHERE SubjectID IN ({inClause})";

        await using var command = new SqlCommand(cmdText, connection, transaction)
        {
            CommandType = CommandType.Text
        };
        command.Parameters.AddRange(parameters);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        if (count != distinctIds.Count)
        {
            throw new ArgumentException("One or more subjects are invalid.");
        }
    }

    private async Task<int> InsertExamMasterAsync(ExamMaster masterInput, SqlConnection connection, SqlTransaction transaction)
    {
        const string storedProc = "Exam_Insert";
        await using var command = new SqlCommand(storedProc, connection, transaction)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@StudentID", masterInput.StudentID);
        command.Parameters.AddWithValue("@ExamYear", masterInput.ExamYear);

        var outputId = new SqlParameter("@NewMasterID", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(outputId);

        await command.ExecuteNonQueryAsync();
        return (int)outputId.Value;
    }

    private async Task InsertExamDetailAsync(int masterId, ExamDtls detail, SqlConnection connection, SqlTransaction transaction)
    {
        const string storedProc = "ExamDetail_Insert";
        await using var command = new SqlCommand(storedProc, connection, transaction)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@MasterID", masterId);
        command.Parameters.AddWithValue("@SubjectID", detail.SubjectID);
        command.Parameters.AddWithValue("@Marks", detail.Marks);

        await command.ExecuteNonQueryAsync();
    }

    private async Task FinalizeExamAsync(int masterId, SqlConnection connection, SqlTransaction transaction)
    {
        const string storedProc = "FinalizeExamResult";
        await using var command = new SqlCommand(storedProc, connection, transaction)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@MasterID", masterId);

        await command.ExecuteNonQueryAsync();
    }

    private async Task<List<ExamMaster>> GetExamMastersByIdsAsync(IEnumerable<int> masterIds)
    {
        var ids = masterIds.ToList();
        if (!ids.Any())
        {
            return new List<ExamMaster>();
        }

        const string storedProc = "Exams_GetByMasterId";
        var masters = new List<ExamMaster>();
        var masterDict = new Dictionary<int, ExamMaster>();

        await using var connection = CreateConnection();
        await using var command = new SqlCommand(storedProc, connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@MasterID", ids.First());

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var masterId = reader.GetInt32(reader.GetOrdinal("MasterID"));
            var master = new ExamMaster
            {
                MasterID = masterId,
                StudentID = reader.GetInt32(reader.GetOrdinal("StudentID")),
                ExamYear = reader.GetInt32(reader.GetOrdinal("ExamYear")),
                TotalMark = reader.GetDecimal(reader.GetOrdinal("TotalMark")),
                PassOrFail = reader.GetString(reader.GetOrdinal("PassOrFail")),
                CreateTime = reader.GetDateTime(reader.GetOrdinal("CreateTime")),
                Student = new StudentMst
                {
                    StudentID = reader.GetInt32(reader.GetOrdinal("StudentID")),
                    StudentName = reader.GetString(reader.GetOrdinal("StudentName")),
                    Mail = reader.GetString(reader.GetOrdinal("Mail"))
                }
            };

            masterDict[masterId] = master;
            masters.Add(master);
        }

        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                var masterId = reader.GetInt32(reader.GetOrdinal("MasterID"));
                if (!masterDict.TryGetValue(masterId, out var master))
                {
                    continue;
                }

                master.Details.Add(new ExamDtls
                {
                    DtlsID = reader.GetInt32(reader.GetOrdinal("DtlsID")),
                    MasterID = masterId,
                    SubjectID = reader.GetInt32(reader.GetOrdinal("SubjectID")),
                    Marks = reader.GetDecimal(reader.GetOrdinal("Marks")),
                    Subject = new SubjectMst
                    {
                        SubjectID = reader.GetInt32(reader.GetOrdinal("SubjectID")),
                        SubjectName = reader.GetString(reader.GetOrdinal("SubjectName"))
                    }
                });
            }
        }

        return masters;
    }
}

