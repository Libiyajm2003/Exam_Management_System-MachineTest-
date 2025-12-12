using ExamManagementSystem.DTOs;
using ExamManagementSystem.Models;
using ExamManagementSystem.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ExamManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExamsController : ControllerBase
{
    private readonly IExamMasterRepository _masterRepository;
    private readonly IExamDetailRepository _detailRepository;

    public ExamsController(IExamMasterRepository masterRepository, IExamDetailRepository detailRepository)
    {
        _masterRepository = masterRepository;
        _detailRepository = detailRepository;
    }

    [HttpGet]
    public async Task<ActionResult<List<ExamMaster>>> GetAll()
    {
        try
        {
            var exams = await _masterRepository.GetExamMastersAsync();
            return Ok(exams);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while retrieving exams: {ex.Message}");
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExamMaster>> GetById(int id)
    {
        try
        {
            var exam = await _masterRepository.GetExamByIdAsync(id);
            if (exam == null)
            {
                return NotFound("Exam not found.");
            }

            return Ok(exam);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while retrieving exam: {ex.Message}");
        }
    }

    [HttpGet("{id:int}/details")]
    public async Task<ActionResult<List<ExamDtls>>> GetDetails(int id)
    {
        try
        {
            var details = await _detailRepository.GetDetailsByMasterIdAsync(id);
            if (details.Count == 0)
            {
                return NotFound("No details found for this exam.");
            }

            return Ok(details);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while retrieving exam details: {ex.Message}");
        }
    }

    [HttpPost]
    public async Task<ActionResult<ExamMaster>> Create([FromBody] CreateExamRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.Details == null || request.Details.Count == 0)
        {
            return BadRequest("At least one subject mark is required.");
        }

        // Check for duplicate subjects
        var subjectIds = request.Details.Select(d => d.SubjectID).ToList();
        if (subjectIds.Count != subjectIds.Distinct().Count())
        {
            return BadRequest("Duplicate subjects are not allowed. Each subject can only be added once.");
        }

        try
        {
            // Convert DTO to model
            var examMaster = new ExamMaster
            {
                StudentID = request.StudentID,
                ExamYear = request.ExamYear,
                Details = request.Details.Select(d => new ExamDtls
                {
                    SubjectID = d.SubjectID,
                    Marks = d.Marks
                }).ToList()
            };

            var created = await _masterRepository.AddExamAsync(examMaster);
            return CreatedAtAction(nameof(GetById), new { id = created.MasterID }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred while creating exam: {ex.Message}");
        }
    }
}

