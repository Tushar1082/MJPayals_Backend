namespace DotnetBoilerplate.Models.Requests;

public class AddCustomerRequest
{
    public string Name { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public char CusType { get; set; }
    public int? CurrentCusId { get; set; }
}