using DotnetBoilerplate.Models.Helpers;

namespace DotnetBoilerplate.Models.Requests;


public class AddDiaryItemsRequest
{
    public int CusId { get; set; }
    public List<DiaryItemDto> Items { get; set; } = new();
}

public class DiaryItemDto
{
    public decimal FineSilver { get; set; }
    public decimal LbrBalance { get; set; }
    public string? Comment { get; set; }
    public List<string>? Images { get; set; }
}