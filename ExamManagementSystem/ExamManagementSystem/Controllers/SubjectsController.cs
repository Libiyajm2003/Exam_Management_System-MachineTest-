using ExamManagementSystem.Models;
using ExamManagementSystem.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ExamManagementSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubjectsController : ControllerBase
{
    private readonly ISubjectRepository _repository;

    public SubjectsController(ISubjectRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<SubjectMst>>> GetSubjects()
    {
        var subjects = await _repository.GetSubjectsAsync();
        return Ok(subjects);
    }
}

