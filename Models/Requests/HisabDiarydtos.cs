namespace DotnetBoilerplate.Models.Requests;

public class AddHisabDiaryCustomerRequest
{
    public string Name { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? FirmName { get; set; }
}

public class AddHisabDiaryTransactionRequest
{
    public int CusId { get; set; }
    public string TransactionType { get; set; } // N --> Naam, J --> Jama
    public decimal? SilverInGram { get; set; }
    public decimal? Cash { get; set; }
    public string? Comment { get; set; }
    public string? MediaUrls { get; set; }
    public DateTime? Date { get; set; }
}