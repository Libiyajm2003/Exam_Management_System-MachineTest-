using System.Data;
using ExamManagementSystem.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ExamManagementSystem.Repositories;

public class ExamDetailRepository : IExamDetailRepository
{
    private readonly string _connectionString;

    public ExamDetailRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    private SqlConnection CreateConnection() => new(_connectionString);

    public async Task<List<ExamDtls>> GetDetailsByMasterIdAsync(int masterId)
    {
        const string storedProc = "Exams_GetByMasterId";
        var details = new List<ExamDtls>();

        await using var connection = CreateConnection();
        await using var command = new SqlCommand(storedProc, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@MasterID", masterId);

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        // Skip master result set
        while (await reader.ReadAsync())
        {
            // just advance
        }

        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                details.Add(new ExamDtls
                {
                    DtlsID = reader.GetInt32(reader.GetOrdinal("DtlsID")),
                    MasterID = reader.GetInt32(reader.GetOrdinal("MasterID")),
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

        return details;
    }
}

