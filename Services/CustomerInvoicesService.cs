using DotnetBoilerplate.Data;
using DotnetBoilerplate.Models.Responses;
using DotnetBoilerplate.Models.Helpers;
using Microsoft.EntityFrameworkCore;

namespace DotnetBoilerplate.Services;

public interface ICustomerInvoicesService
{
    Task<List<CustomerItemResponse>> GetItemsByInvoiceIdAsync(int invoiceId);
    Task<bool> DeleteInvoiceAndItemsAsync(int invoiceId);
    Task<bool> UpdateSilverRateAsync(int invoiceId, decimal silverRate);
}

public class CustomerInvoicesService : ICustomerInvoicesService
{
    private readonly ApplicationDbContext _db;

    public CustomerInvoicesService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<CustomerItemResponse>> GetItemsByInvoiceIdAsync(int invoiceId)
    {
        var items = await _db.InvoiceItems
            .Where(x => x.InvoiceId == invoiceId)
            .OrderBy(x => x.Id)
            .ToListAsync();

        return items.Select(ci => new CustomerItemResponse
        {
            Id = ci.Id,
            ItemType = ci.ItemType,
            ItemName = ci.ItemName,
            Rate = ci.Rate,
            MeasurementValue = ci.MeasurementValue,
            GrossWeight = ci.GrossWeight,
            NetWeight = ci.NetWeight,
            LabourType = ci.LabourType,
            LabourRate = ci.LabourRate,
            LabourAmount = ci.LabourAmount,
            Amount = ci.Amount,
            Comment = ci.Comment,
            Polythenes = string.IsNullOrWhiteSpace(ci.Polythenes)
                ? null
                : PolyDetailXmlHelper.FromXml(ci.Polythenes)
        }).ToList();
    }

    public async Task<bool> UpdateSilverRateAsync(int invoiceId, decimal silverRate)
    {
        var invoice = await _db.CustomerInvoices
            .FirstOrDefaultAsync(x => x.Id == invoiceId);

        if (invoice == null)
        {
            return false;
        }

        invoice.SilverRate = silverRate;

        // Note: If you have a TotalAmount column on the CustomerInvoices table, 
        // you might need to recalculate it here based on the new silver rate!

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteInvoiceAndItemsAsync(int invoiceId)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            // 1️⃣ Delete items first
            var items = await _db.InvoiceItems
                .Where(x => x.InvoiceId == invoiceId)
                .ToListAsync();

            if (items.Any())
            {
                _db.InvoiceItems.RemoveRange(items);
                await _db.SaveChangesAsync();
            }

            // 2️⃣ Then delete invoice
            var invoice = await _db.CustomerInvoices
                .FirstOrDefaultAsync(x => x.Id == invoiceId);

            if (invoice == null)
            {
                await transaction.RollbackAsync();
                return false;
            }

            _db.CustomerInvoices.Remove(invoice);
            await _db.SaveChangesAsync();

            // 3️⃣ Commit transaction
            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw; // Let middleware handle the error
        }
    }

}
