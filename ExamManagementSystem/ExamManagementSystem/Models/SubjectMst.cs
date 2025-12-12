using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ExamManagementSystem.Models;

public class SubjectMst
{
    [Key]
    public int SubjectID { get; set; }

    [Required, MaxLength(200)]
    public string SubjectName { get; set; } = string.Empty;

    [JsonIgnore]
    public ICollection<ExamDtls> ExamDetails { get; set; } = new List<ExamDtls>();
}



