using System.ComponentModel.DataAnnotations.Schema;
namespace DotnetBoilerplate.Models.Entities;

public class DiaryItems
{
    public int Id { get; set; }

    public int CusId { get; set; }

    public decimal FineSilver { get; set; }

    public decimal LabourBalance { get; set; }

    public string? Comment { get; set; }

    // CHANGE FROM STRING TO JSON ARRAY
    [Column(TypeName = "nvarchar(max)")]
    public string? CommentMediaLinks { get; set; }  // Stores JSON array: ["url1", "url2"]

}