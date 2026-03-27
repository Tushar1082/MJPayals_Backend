using System.ComponentModel.DataAnnotations.Schema;

namespace DotnetBoilerplate.Models.Entities;

[Table("CustomerInvoice")]
public class CustomerInvoice
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal SilverRate { get; set; }
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
}
