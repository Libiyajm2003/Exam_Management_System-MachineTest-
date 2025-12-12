using System.Data;
using ExamManagementSystem.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ExamManagementSystem.Repositories;

public class SubjectRepository : ISubjectRepository
{
    private readonly string _connectionString;

    public SubjectRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    private SqlConnection CreateConnection() => new(_connectionString);

    public async Task<List<SubjectMst>> GetSubjectsAsync()
    {
        const string storedProc = "Subjects_GetAll";
        var subjects = new List<SubjectMst>();

        await using var connection = CreateConnection();
        await using var command = new SqlCommand(storedProc, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            subjects.Add(new SubjectMst
            {
                SubjectID = reader.GetInt32(reader.GetOrdinal("SubjectID")),
                SubjectName = reader.GetString(reader.GetOrdinal("SubjectName"))
            });
        }

        return subjects;
    }

    public async Task<SubjectMst?> GetSubjectByIdAsync(int id)
    {
        const string sql = @"SELECT SubjectID, SubjectName FROM dbo.SubjectMst WHERE SubjectID = @SubjectID";

        await using var connection = CreateConnection();
        await using var command = new SqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };
        command.Parameters.AddWithValue("@SubjectID", id);

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new SubjectMst
            {
                SubjectID = reader.GetInt32(reader.GetOrdinal("SubjectID")),
                SubjectName = reader.GetString(reader.GetOrdinal("SubjectName"))
            };
        }

        return null;
    }
}

