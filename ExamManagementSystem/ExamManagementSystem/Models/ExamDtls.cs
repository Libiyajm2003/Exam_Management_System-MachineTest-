using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ExamManagementSystem.Models;

public class ExamDtls
{
    [Key]
    public int DtlsID { get; set; }

    [Required]
    public int MasterID { get; set; }

    [JsonIgnore]
    public ExamMaster? Master { get; set; }

    [Required]
    public int SubjectID { get; set; }

    public SubjectMst? Subject { get; set; }

    [Required, Range(0, 100)]
    [Column(TypeName = "decimal(5,2)")]
    public decimal Marks { get; set; }
}



