using DotnetBoilerplate.Models.Helpers;

namespace DotnetBoilerplate.Models.Responses;

public class CustomerItemResponse
{
    public int Id { get; set; }
    public char ItemType { get; set; }
    public string ItemName { get; set; } = null!;
    public decimal? Rate { get; set; }
    public char? MeasurementValue { get; set; }
    public decimal? GrossWeight { get; set; }
    public decimal? NetWeight { get; set; }
    public char? LabourType { get; set; }
    public int? LabourRate {get; set;}
    public int? LabourAmount { get; set; }
    public int? Amount { get; set; }
    public string? Comment { get; set; }

    // JSON version of XML
    public List<PolyDetail>? Polythenes { get; set; }
}
