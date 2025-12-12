using ExamManagementSystem.Models;
using ExamManagementSystem.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ExamManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly IStudentRepository _repository;

    public StudentsController(IStudentRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<StudentMst>>> GetStudents()
    {
        try
        {
            var students = await _repository.GetStudentsAsync();
            return Ok(students);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while retrieving students: {ex.Message}");
        }
    }

    [HttpPost]
    public async Task<ActionResult<StudentMst>> CreateStudent([FromBody] StudentMst student)
    {
        if (student == null)
        {
            return BadRequest("Student data is required.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        // Validate student name length
        if (string.IsNullOrWhiteSpace(student.StudentName) || student.StudentName.Length < 5 || student.StudentName.Length > 250)
        {
            return BadRequest("Student name must be between 5 and 250 characters.");
        }

        // Validate email
        if (string.IsNullOrWhiteSpace(student.Mail) || !student.Mail.Contains("@"))
        {
            return BadRequest("Valid email address is required.");
        }

        try
        {
            var created = await _repository.CreateStudentAsync(student);
            return Created($"/api/students/{created.StudentID}", created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (SqlException ex) when (ex.Number == 50000)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while creating student: {ex.Message}");
        }
    }
}

