using DotnetBoilerplate.Data;
using DotnetBoilerplate.Models.Entities;
using DotnetBoilerplate.Models.Helpers;
using DotnetBoilerplate.Models.Requests;
using DotnetBoilerplate.Models.Responses;
using DotnetBoilerplate.Services;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DotnetBoilerplate.Services;

public interface IInvoiceItemsService { 
    Task<(bool isOldInvoice, string invoiceId, decimal silverRate)> AddItemsAsync(int cusId, List<Models.Requests.ItemDto> items, string? invoiceNo, decimal silverRate, char? cusType, string? customerName, bool? sendWhatsAppMsg);
    Task<bool> DeleteItemAsync(int itemId);
    //Task<int> DeleteItemsByInvoiceAsync(int invoiceId); // 👈 ADD
    Task<List<CustomerItemResponse>> GetItemsAsync(int cusId); 
    Task<(decimal rate, string rateType, List<PolyDetail>? polythenes, char? labourType, int labourRate, int labourAmount)?> FetchLatestRateAsync(string itemName, char cusType); 
    Task<List<string>> SearchItemNamesAsync(string query, char cusType); 
}

public class InvoiceItemsService : IInvoiceItemsService
{
    private readonly ApplicationDbContext _db;
    private readonly IWhatsAppService _whatsApp;
    private readonly string? _whatsAppSendNo;

    public InvoiceItemsService(ApplicationDbContext db, IWhatsAppService whatsApp, IConfiguration config)
    {
        _db = db;
        _whatsApp = whatsApp;
        _whatsAppSendNo = config["WhatsApp:PhoneNumber"] ?? null;
    }

    // 🔹 STEP 2: JSON → XML helper (APPLIED HERE)
    //private static string? BuildPolythenesXml(Dictionary<int, decimal>? polyData)
    //{
    //    if (polyData == null || polyData.Count == 0)
    //        return null;

    //    var list = polyData.Select(x => new PolyDetail
    //    {
    //        NoOfPPs = x.Key,
    //        WeightOfOpp = x.Value
    //    }).ToList();

    //    return PolyDetailXmlHelper.ToXml(list);
    //}
    private static string? BuildPolythenesXml(List<PolyDetailDto>? polyData)
    {
        if (polyData == null || polyData.Count == 0)
            return null;

        var list = polyData.Select(x => new PolyDetail
        {
            NoOfPPs = x.NoOfPPs,
            WeightOfOpp = x.Weight
        }).ToList();

        return PolyDetailXmlHelper.ToXml(list);
    }


    public async Task<(bool isOldInvoice, string invoiceId, decimal silverRate)> AddItemsAsync(
    int cusId,
    List<ItemDto> items,
    string? invoiceNo,
    decimal silverRate,
    char? cusType,
    string? customerName,
    bool? sendWhatsAppMsg
    )
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        int invoiceId;
        bool isOldInvoice = false;

        try
        {

            if (string.IsNullOrWhiteSpace(invoiceNo))
            {
                var invoice = new CustomerInvoice
                {
                    CustomerId = cusId,
                    SilverRate = silverRate
                };

                _db.CustomerInvoices.Add(invoice);
                await _db.SaveChangesAsync();

                invoiceId = invoice.Id;
            }
            else
            {
                invoiceId = int.Parse(invoiceNo);

                var invoice = await _db.CustomerInvoices
                   .FirstOrDefaultAsync(x => x.Id == invoiceId);

                if (invoice == null)
                    throw new Exception("Invoice not found");

                // ✅ Update SilverRate
                invoice.SilverRate = silverRate;

                // 🔥 DELETE OLD ITEMS OF THIS INVOICE
                var oldItems = _db.InvoiceItems
                    .Where(x => x.InvoiceId == invoiceId);

                _db.InvoiceItems.RemoveRange(oldItems);
                await _db.SaveChangesAsync();
                isOldInvoice = true;
                //Console.WriteLine("Old Invoice..."+ invoiceId);
                //Console.WriteLine("Deletion is done...");
            }

            // 🔁 INSERT NEW ITEMS
            foreach (var item in items)
            {
                char rateType;
                decimal rateValue;

                if (cusType == 'B') {
                    rateType = 'B';
                    rateValue = 0;
                }
                else if (item.RateGm.HasValue && item.RateGm.Value > 0)
                {
                    rateType = 'G';
                    rateValue = item.RateGm.Value;
                }
                else if (item.RateKg.HasValue && item.RateKg.Value > 0) { 
                    rateType = 'K';
                    rateValue = item.RateKg.Value;
                }
                else if (item.RatePer.HasValue && item.RatePer.Value > 0)  // ⭐ ADD CONDITION & > 0 CHECK
                {
                    rateType = 'P';
                    rateValue = item.RatePer.Value;
                }
                else
                {
                    rateType = 'P';
                    rateValue = 0;
                }

                var polyXml = BuildPolythenesXml(item.Polythenes);

                _db.InvoiceItems.Add(new InvoiceItems
                {
                    InvoiceId = invoiceId,
                    ItemName = item.ItemName.ToLower(),
                    ItemType = item.ItemType,
                    MeasurementValue = rateType,
                    Rate = rateValue,
                    GrossWeight = item.GrossWeight,
                    NetWeight = item.NetWeight,
                    Polythenes = polyXml,
                    LabourType = item.LabourType,
                    LabourRate = item.LabourRate,
                    LabourAmount = item.LabourAmount,
                    Amount = item.Amount,
                    Comment = item.Comment
                });
            }

            await _db.SaveChangesAsync();
            //Console.WriteLine("New is inserted...");

            await tx.CommitAsync();

            //if (cusType == 'B')
            //{
            //    Console.WriteLine("whatsapp function below...");
            //    string message = BuildWhatsAppMessage(items);
            //    string phone = "9870663306"; // ✅ 10 digits only, service adds 91+
            //    await _whatsApp.SendInvoiceMessage(phone, message);
            //}

            //return (isOldInvoice, invoiceId.ToString(), silverRate);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        if (cusType == 'B' && sendWhatsAppMsg == true)
        {
            try // ✅ separate try so WhatsApp failure doesn't affect invoice success
            {
                Console.WriteLine("Sending WhatsApp message...");
                string message = BuildWhatsAppMessage(items, customerName);
                string phone = _whatsAppSendNo;
                if (string.IsNullOrEmpty(phone)) {
                    return (isOldInvoice, invoiceId.ToString(), silverRate);
                }
                await _whatsApp.SendInvoiceMessage(phone, message);
                Console.WriteLine("WhatsApp sent successfully.");
            }
            catch (Exception ex)
            {
                // ⚠️ Log but don't crash — invoice is already saved
                Console.WriteLine($"WhatsApp failed (non-fatal): {ex.Message}");
            }
        }

        return (isOldInvoice, invoiceId.ToString(), silverRate);

    }



    public async Task<bool> DeleteItemAsync(int itemId)
    {
        var item = await _db.InvoiceItems
            .FirstOrDefaultAsync(x =>
                x.Id == itemId);

        if (item == null)
            return false;

        _db.InvoiceItems.Remove(item);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<List<CustomerItemResponse>> GetItemsAsync(int cusId)
    {
        // 1️⃣ Fetch raw entities ONLY
        var items = await _db.InvoiceItems
            .Where(ci =>
                _db.CustomerInvoices.Any(i =>
                    i.Id == ci.InvoiceId && i.CustomerId == cusId))
            .OrderByDescending(x => x.Id)
            .ToListAsync();

        // 2️⃣ Map in memory (SAFE)
        return items.Select(ci => new CustomerItemResponse
        {
            Id = ci.Id,
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

            // ✅ Defensive + safe
            Polythenes = string.IsNullOrWhiteSpace(ci.Polythenes)
                ? null
                : PolyDetailXmlHelper.FromXml(ci.Polythenes)

        }).ToList();
    }


    public async Task<(decimal rate, string rateType, List<PolyDetail>? polythenes, char? labourType, int labourRate, int labourAmount)?> FetchLatestRateAsync(string itemName, char cusType)
    {
        //cusType = cusType == 'B' ? 'W' : cusType;

        var result = await (
            from ii in _db.InvoiceItems
            join ci in _db.CustomerInvoices on ii.InvoiceId equals ci.Id
            join c in _db.Customers on ci.CustomerId equals c.Id
            where (cusType == 'B' ? (c.Type == 'B' || c.Type == 'W') : c.Type == cusType) 
            && ii.ItemType != 'P'
            && ii.ItemName.ToLower() == itemName.ToLower()
            //where c.Type == cusType
            //      && ii.ItemName.ToLower() == itemName.ToLower()
            orderby ii.Id descending
            select new
            {
                ii.Rate,
                ii.MeasurementValue,
                ii.Polythenes,
                ii.LabourType,
                ii.LabourRate,
                ii.LabourAmount
            }
        ).FirstOrDefaultAsync();

        if (result == null) return null;

        var polyList = string.IsNullOrWhiteSpace(result.Polythenes)
                ? null
                : PolyDetailXmlHelper.FromXml(result.Polythenes);

        return (
            result.Rate ?? 0,
            result.MeasurementValue switch
            {
                'G' => "gram",
                'K' => "kilogram",
                _ => "percentage"
            }, 
            polyList,
            result.LabourType,
            result.LabourRate ?? 0,
            result.LabourAmount ?? 0
           );
    }

    public async Task<List<string>> SearchItemNamesAsync(string query, char cusType)
    {
        query = query.ToLower();
        //cusType = cusType == 'B' ? 'W' : cusType;

        return await (
            from ii in _db.InvoiceItems
            join ci in _db.CustomerInvoices on ii.InvoiceId equals ci.Id
            join c in _db.Customers on ci.CustomerId equals c.Id
            where (cusType == 'B' ? (c.Type == 'B' || c.Type == 'W') : c.Type == cusType) 
            && ii.ItemType != 'P'
            && ii.ItemName.ToLower().Contains(query)
            //where c.Type == cusType &&
            //      ii.ItemName.ToLower().Contains(query)
            select ii.ItemName
        )
        .Distinct()
        .OrderBy(x => x)
        .ToListAsync();
    }

    private string BuildWhatsAppMessage(List<ItemDto> items, string? customerName)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(customerName)) { 
            sb.AppendLine("Dear "+ customerName);
            sb.AppendLine();
        }
        sb.AppendLine($"Date: {DateTime.Now:dd MMM yyyy}");
        sb.AppendLine($"Time: {DateTime.Now:hh:mm tt}");
        sb.AppendLine();
        sb.AppendLine("Items:");

        int i = 1;

        foreach (var item in items)
        {
            sb.AppendLine($"{i}. {item.ItemName} - {item.GrossWeight}g ({(item.ItemType == 'S' ? "Sell" : "Purchase")})");
            i++;
        }

        return sb.ToString();
    }

}
