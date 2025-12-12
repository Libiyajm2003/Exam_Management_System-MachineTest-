using System.Data;
using System.Data.Common;
using ExamManagementSystem.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ExamManagementSystem.Repositories;

public class StudentRepository : IStudentRepository
{
    private readonly string _connectionString;

    public StudentRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    private SqlConnection CreateConnection() => new(_connectionString);

    public async Task<List<StudentMst>> GetStudentsAsync()
    {
        const string storedProc = "Students_GetAll";
        var students = new List<StudentMst>();

        await using var connection = CreateConnection();
        await using var command = new SqlCommand(storedProc, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            students.Add(new StudentMst
            {
                StudentID = reader.GetInt32(reader.GetOrdinal("StudentID")),
                StudentName = reader.GetString(reader.GetOrdinal("StudentName")),
                Mail = reader.GetString(reader.GetOrdinal("Mail"))
            });
        }

        return students;
    }

    public async Task<StudentMst?> GetStudentByIdAsync(int id)
    {
        const string sql = @"SELECT StudentID, StudentName, Mail FROM dbo.StudentMst WHERE StudentID = @StudentID";

        await using var connection = CreateConnection();
        await using var command = new SqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };
        command.Parameters.AddWithValue("@StudentID", id);

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new StudentMst
            {
                StudentID = reader.GetInt32(reader.GetOrdinal("StudentID")),
                StudentName = reader.GetString(reader.GetOrdinal("StudentName")),
                Mail = reader.GetString(reader.GetOrdinal("Mail"))
            };
        }

        return null;
    }

    public async Task<StudentMst> CreateStudentAsync(StudentMst student)
    {
        const string storedProc = "Student_Insert";
        
        await using var connection = CreateConnection();
        await using var command = new SqlCommand(storedProc, connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        
        command.Parameters.AddWithValue("@StudentName", student.StudentName);
        command.Parameters.AddWithValue("@Mail", student.Mail);
        
        var outputId = new SqlParameter("@NewStudentID", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(outputId);

        await connection.OpenAsync();
        
        try
        {
            await command.ExecuteNonQueryAsync();
            var newId = (int)outputId.Value;
            
            // Return the created student
            return new StudentMst
            {
                StudentID = newId,
                StudentName = student.StudentName,
                Mail = student.Mail
            };
        }
        catch (SqlException ex) when (ex.Number == 50000) // Custom error from stored procedure
        {
            throw new InvalidOperationException(ex.Message);
        }
    }
}

