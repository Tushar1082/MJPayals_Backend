namespace DotnetBoilerplate.Models.Entities;

public class HisabDiaryCustomer
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? FirmName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

}