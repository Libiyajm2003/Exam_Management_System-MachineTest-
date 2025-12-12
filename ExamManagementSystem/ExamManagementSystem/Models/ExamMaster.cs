using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamManagementSystem.Models;

public class ExamMaster
{
    [Key]
    public int MasterID { get; set; }

    [Required]
    public int StudentID { get; set; }

    public StudentMst? Student { get; set; }

    [Required, Range(2000, 2100)]
    public int ExamYear { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalMark { get; set; }

    [Required, MaxLength(10)]
    public string PassOrFail { get; set; } = "FAIL";

    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    public ICollection<ExamDtls> Details { get; set; } = new List<ExamDtls>();
}



