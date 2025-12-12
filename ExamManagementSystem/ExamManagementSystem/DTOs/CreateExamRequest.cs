using System.ComponentModel.DataAnnotations;
using ExamManagementSystem.Models;

namespace ExamManagementSystem.DTOs;

public class CreateExamRequest
{
    [Required]
    public int StudentID { get; set; }

    [Required]
    [Range(2000, 2100)]
    public int ExamYear { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one subject mark is required.")]
    public List<ExamDetailDto> Details { get; set; } = new();
}

public class ExamDetailDto
{
    [Required]
    public int SubjectID { get; set; }

    [Required]
    [Range(0, 100, ErrorMessage = "Marks must be between 0 and 100.")]
    public decimal Marks { get; set; }
}

