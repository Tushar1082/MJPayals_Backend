using DotnetBoilerplate.Models.Helpers;

namespace DotnetBoilerplate.Models.Requests;


public class AddItemsRequest
{
    public int CusId { get; set; }
    public string? InvoiceNo { get; set; }
    public List<ItemDto> Items { get; set; } = new();
    public decimal SilverRate { get; set; }
    public char? CusType { get; set; }
    public string? CustomerName { get; set; }
    public bool? SendWhatsAppMsg { get; set; }
}

public class ItemDto
{
    public string ItemName { get; set; } = null!;
    public char ItemType { get; set; }
    public decimal? RateGm { get; set; }
    public decimal? RateKg { get; set; }
    public decimal? RatePer { get; set; }
    public decimal GrossWeight { get; set; }
    public decimal NetWeight { get; set; }

    /*
     frontend sends: -
     "polythenes": [
          { "noOfPPs": 2, "weight": 0.2 },
          { "noOfPPs": 3, "weight": 0.5 }
        ]
     */
    public List<PolyDetailDto>? Polythenes { get; set; }
    public char? LabourType { get; set; }
    public int? LabourRate { get; set; }
    public int? LabourAmount { get; set; }
    public int Amount { get; set; }
    public string? Comment { get; set; }
}

public class PolyDetailDto
{
    public decimal NoOfPPs { get; set; }
    public decimal Weight { get; set; }
}