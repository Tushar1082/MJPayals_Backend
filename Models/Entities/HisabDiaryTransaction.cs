namespace DotnetBoilerplate.Models.Entities;


public class HisabDiaryTransaction
{
    public int Id { get; set; }

    public int CusId { get; set; }

    public string TransactionType { get; set; } = null!;

    public decimal? SilverInGram { get; set; }

    public decimal? Cash { get; set; }

    public string? Comment { get; set; }

    public string? MediaUrls { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.Now;

}