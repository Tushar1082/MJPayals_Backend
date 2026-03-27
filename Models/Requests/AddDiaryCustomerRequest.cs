namespace DotnetBoilerplate.Models.Requests;

public class AddDiaryCustomerRequest
{
    public string Name { get; set; } = null!;
    public string? Phone { get; set; }
    public int? CurrentCusId { get; set; }
}