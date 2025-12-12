using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ExamManagementSystem.Models;

public class StudentMst
{
    [Key]
    public int StudentID { get; set; }

    [Required, MinLength(5), MaxLength(250)]
    public string StudentName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Mail { get; set; } = string.Empty;

    [JsonIgnore]
    public ICollection<ExamMaster> Exams { get; set; } = new List<ExamMaster>();
}



