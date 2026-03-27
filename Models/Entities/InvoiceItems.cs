using System.ComponentModel.DataAnnotations.Schema;
namespace DotnetBoilerplate.Models.Entities;

[Table("InvoiceItems")]
public class InvoiceItems
{
    public int Id { get; set; }

    public int? InvoiceId { get; set; }

    public string ItemName { get; set; } = null!;

    public char ItemType { get; set; }

    public char? MeasurementValue { get; set; }

    public decimal? Rate { get; set; }

    public decimal GrossWeight { get; set; }

    [Column(TypeName = "xml")] // optional but recommended
    public string? Polythenes { get; set; }

    public decimal? NetWeight { get; set; }
    public char ?LabourType { get; set; }
    public int ?LabourRate { get; set; }
    public int ?LabourAmount { get; set; }

    public int Amount { get; set; }
    public string? Comment { get; set; }
}