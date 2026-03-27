namespace DotnetBoilerplate.Models.Entities;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public char Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
