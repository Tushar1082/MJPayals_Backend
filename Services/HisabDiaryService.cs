using DotnetBoilerplate.Data;
using DotnetBoilerplate.Models.Entities;
using DotnetBoilerplate.Models.Requests;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace DotnetBoilerplate.Services;

public interface IHisabDiaryService
{
    // Customer Methods
    Task<object> AddHisabDiaryCustomerAsync(string name, string phone, string? address, string? city, string? firmName);
    Task<object> GetAllHisabDiaryCustomersAsync(int page = 1, int pageSize = 10, string? search = null);
    Task<object?> GetHisabDiaryCustomerByIdAsync(int id);
    Task<object> SearchHisabDiaryCustomersAsync(string term);
    Task<object> DeleteHisabDiaryCustomerAsync(int id);

    // Transaction Methods
    Task<object> AddHisabDiaryTransactionAsync(int cusId, string transactionType, decimal? silverInGram, decimal? cash, string? comment, string? mediaUrls, DateTime transactionDate);
    Task<object> GetAllHisabDiaryTransactionsAsync();
    Task<object?> GetHisabDiaryTransactionByIdAsync(int id);
    Task<object> GetTransactionsByCustomerIdAsync(int cusId, int page = 1, int pageSize = 20);
    Task<object> UpdateHisabDiaryTransactionAsync(int id, string transactionType, decimal? silverInGram, decimal? cash, string? comment, DateTime transactionDate);
    Task<object> DeleteHisabDiaryTransactionAsync(int id);
    Task<object> SendWhatsAppReportAsync(int cusId);
}

public class HisabDiaryService : IHisabDiaryService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWhatsAppService _whatsAppService;

    public HisabDiaryService(ApplicationDbContext db, IConfiguration config, IWhatsAppService whatsAppService)
    {
        _db = db;
        _config = config;
        _whatsAppService = whatsAppService;
    }

    //=================================================== Hisab Diary Customer Service Methods ===================================

    public async Task<object> AddHisabDiaryCustomerAsync(
        string name,
        string phone,
        string? address,
        string? city,
        string? firmName
    )
    {
        var cusName = name.Trim();
        var cusPhone = phone.Trim();

        // CREATE NEW CUSTOMER
        var customer = new HisabDiaryCustomer
        {
            Name = cusName,
            Phone = cusPhone,
            Address = address,
            City = city,
            FirmName = firmName
        };

        _db.HisabDiaryCustomer.Add(customer);
        await _db.SaveChangesAsync();

        return new
        {
            status = "success",
            message = "Customer Added Successfully!"
        };
    }

    public async Task<object> GetAllHisabDiaryCustomersAsync(int page = 1, int pageSize = 10, string? search = null)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var baseQuery = _db.HisabDiaryCustomer.AsQueryable();

        // 🔴 SEARCH FILTER LOGIC ADDED HERE
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            baseQuery = baseQuery.Where(c =>
                c.Name.Contains(search) ||
                c.Phone.Contains(search) ||
                (c.FirmName != null && c.FirmName.Contains(search))
            );
        }

        var query = baseQuery
        .Select(c => new
        {
            c.Id,
            c.Name,
            c.Phone,
            c.Address,
            c.City,
            c.FirmName,
            c.CreatedAt,

            // Naam (Dr) Totals
            TotalNaamSilver = _db.HisabDiaryTransaction
            .Where(t => t.CusId == c.Id && t.TransactionType == "N")
            .Sum(t => (decimal?)t.SilverInGram) ?? 0,

            TotalNaamCash = _db.HisabDiaryTransaction
            .Where(t => t.CusId == c.Id && t.TransactionType == "N")
            .Sum(t => (decimal?)t.Cash) ?? 0,

            // Jama (Cr) Totals
            TotalJamaSilver = _db.HisabDiaryTransaction
            .Where(t => t.CusId == c.Id && t.TransactionType == "J")
            .Sum(t => (decimal?)t.SilverInGram) ?? 0,

            TotalJamaCash = _db.HisabDiaryTransaction
            .Where(t => t.CusId == c.Id && t.TransactionType == "J")
            .Sum(t => (decimal?)t.Cash) ?? 0
        })
        .OrderByDescending(x => x.Id);

        var totalRecords = await query.CountAsync();

        var customers = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new
        {
            status = "success",
            data = customers,
            pagination = new
            {
                currentPage = page,
                pageSize = pageSize,
                totalRecords = totalRecords,
                totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize)
            }
        };
    }

    public async Task<object?> GetHisabDiaryCustomerByIdAsync(int id)
    {
        var customer = await _db.HisabDiaryCustomer
            .FirstOrDefaultAsync(x => x.Id == id);

        if (customer == null)
            return null;

        return new
        {
            status = "success",
            data = customer
        };
    }

    public async Task<object> SearchHisabDiaryCustomersAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return new
            {
                status = "success",
                data = new List<object>()
            };
        }

        term = term.Trim();

        var customers = await _db.HisabDiaryCustomer
            .Where(x => x.Name.Contains(term))
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name
            })
            .Take(10) // limit results for performance
            .ToListAsync();

        return new
        {
            status = "success",
            data = customers
        };
    }

    public async Task<object> DeleteHisabDiaryCustomerAsync(int id)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            var customer = await _db.HisabDiaryCustomer.FirstOrDefaultAsync(x => x.Id == id);

            if (customer == null)
            {
                return new
                {
                    status = "error",
                    message = "Customer not found"
                };
            }

            // 1. Find related transactions
            var relatedTransactions = await _db.HisabDiaryTransaction
                                               .Where(t => t.CusId == id)
                                               .ToListAsync();

            // 2. Remove transactions (if any) and FORCE execution immediately
            if (relatedTransactions.Any())
            {
                _db.HisabDiaryTransaction.RemoveRange(relatedTransactions);

                // FIX: Ye line force karegi ki pehle child records database se udey
                await _db.SaveChangesAsync();
            }

            // 3. Remove customer now that child records are safely gone
            _db.HisabDiaryCustomer.Remove(customer);
            await _db.SaveChangesAsync(); // Ab customer araam se delete ho jayega

            // 4. Sab kuch successful hone ke baad permanent commit
            await transaction.CommitAsync();

            return new
            {
                status = "success",
                message = "Customer and all related transactions deleted successfully"
            };
        }
        catch (Exception ex)
        {
            // ERROR HANDLING (Atomicity in action)
            await transaction.RollbackAsync();

            return new
            {
                status = "error",
                message = "An error occurred during deletion. No data was deleted. (Rolled back)"
            };
        }
    }

    //=================================================== Hisab Diary Transaction Service Methods ===================================


    public async Task<object> AddHisabDiaryTransactionAsync(
        int cusId,
        string transactionType,
        decimal? silverInGram,
        decimal? cash,
        string? comment,
        string? mediaUrls,
        DateTime transactionDate
    )
    {
        var transaction = new HisabDiaryTransaction
        {
            CusId = cusId,
            TransactionType = transactionType,
            SilverInGram = silverInGram,
            Cash = cash,
            Comment = comment,
            MediaUrls = mediaUrls,
            TransactionDate = transactionDate
        };

        _db.HisabDiaryTransaction.Add(transaction);
        await _db.SaveChangesAsync();

        return new
        {
            status = "success",
            message = "Transaction added successfully",
            data = transaction
        };
    }

    public async Task<object> GetAllHisabDiaryTransactionsAsync()
    {
        var transactions = await _db.HisabDiaryTransaction
            .OrderByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.CusId,
                x.TransactionType,
                x.SilverInGram,
                x.Cash,
                x.Comment,
                x.MediaUrls,
                x.TransactionDate
            })
            .ToListAsync();

        return new
        {
            status = "success",
            data = transactions
        };
    }

    public async Task<object?> GetHisabDiaryTransactionByIdAsync(int id)
    {
        var transaction = await _db.HisabDiaryTransaction
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.CusId,
                x.TransactionType,
                x.SilverInGram,
                x.Cash,
                x.Comment,
                x.MediaUrls,
                x.TransactionDate
            })
            .FirstOrDefaultAsync();

        if (transaction == null)
            return null;

        return new
        {
            status = "success",
            data = transaction
        };
    }

    //public async Task<object> GetTransactionsByCustomerIdAsync(int cusId)
    //{
    //    var exists = await _db.HisabDiaryCustomer
    //        .AnyAsync(x => x.Id == cusId);

    //    if (!exists)
    //    {
    //        return new
    //        {
    //            status = "error",
    //            message = "Customer not found"
    //        };
    //    }

    //    var transactions = await _db.HisabDiaryTransaction
    //        .Where(x => x.CusId == cusId)
    //        .OrderByDescending(x => x.TransactionDate)
    //        .Select(x => new
    //        {
    //            x.Id,
    //            x.TransactionType,
    //            x.SilverInGram,
    //            x.Cash,
    //            x.Comment,
    //            x.MediaUrls,
    //            x.TransactionDate
    //        })
    //        .ToListAsync();

    //    return new
    //    {
    //        status = "success",
    //        data = transactions
    //    };
    //}

    public async Task<object> GetTransactionsByCustomerIdAsync(int cusId, int page = 1, int pageSize = 20)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        var exists = await _db.HisabDiaryCustomer.AnyAsync(x => x.Id == cusId);

        if (!exists)
        {
            return new { status = "error", message = "Customer not found" };
        }

        var allTransactions = await _db.HisabDiaryTransaction
            .Where(x => x.CusId == cusId)
            .OrderBy(x => x.TransactionDate).ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.TransactionType,
                x.SilverInGram,
                x.Cash,
                x.Comment,
                x.MediaUrls,
                x.TransactionDate
            })
            .ToListAsync();

        decimal totalNaamSilver = 0, totalNaamCash = 0;
        decimal totalJamaSilver = 0, totalJamaCash = 0;

        var processedList = new List<object>();

        foreach (var t in allTransactions)
        {
            var silver = t.SilverInGram ?? 0;
            var cash = t.Cash ?? 0;

            if (t.TransactionType == "N")
            {
                totalNaamSilver += silver;
                totalNaamCash += cash;
            }
            else if (t.TransactionType == "J")
            {
                totalJamaSilver += silver;
                totalJamaCash += cash;
            }

            processedList.Add(new
            {
                t.Id,
                t.TransactionType,
                t.SilverInGram,
                t.Cash,
                t.Comment,
                t.MediaUrls,
                t.TransactionDate
            });
        }

        // Latest first (Descending) karne ke liye reverse karenge
        processedList.Reverse();

        var totalRecords = processedList.Count;

        // Pagination apply karenge
        var paginatedData = processedList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new
        {
            status = "success",
            data = paginatedData,
            summary = new
            {
                TotalNaamSilver = totalNaamSilver,
                TotalNaamCash = totalNaamCash,
                TotalJamaSilver = totalJamaSilver,
                TotalJamaCash = totalJamaCash,
                NetSilver = totalJamaSilver - totalNaamSilver,
                NetCash = totalJamaCash - totalNaamCash
            },
            pagination = new
            {
                currentPage = page,
                pageSize = pageSize,
                totalRecords = totalRecords,
                totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize)
            }
        };
    }

    public async Task<object> UpdateHisabDiaryTransactionAsync(
    int id,
    string transactionType,
    decimal? silverInGram,
    decimal? cash,
    string? comment,
    DateTime transactionDate)
    {
        var transaction = await _db.HisabDiaryTransaction.FirstOrDefaultAsync(x => x.Id == id);

        if (transaction == null)
        {
            return new { status = "error", message = "Transaction not found" };
        }

        transaction.TransactionType = transactionType;
        transaction.SilverInGram = silverInGram;
        transaction.Cash = cash;
        transaction.Comment = comment;
        transaction.TransactionDate = transactionDate;

        await _db.SaveChangesAsync();

        return new
        {
            status = "success",
            message = "Transaction updated successfully",
            data = transaction
        };
    }

    public async Task<object> DeleteHisabDiaryTransactionAsync(int id)
    {
        var transaction = await _db.HisabDiaryTransaction
            .FirstOrDefaultAsync(x => x.Id == id);

        if (transaction == null)
        {
            return new
            {
                status = "error",
                message = "Transaction not found"
            };
        }

        _db.HisabDiaryTransaction.Remove(transaction);
        await _db.SaveChangesAsync();

        return new
        {
            status = "success",
            message = "Transaction deleted successfully"
        };
    }

    public async Task<object> SendWhatsAppReportAsync(int cusId)
    {
        var customer = await _db.HisabDiaryCustomer.FirstOrDefaultAsync(x => x.Id == cusId);
        if (customer == null) return new { status = "error", message = "Customer not found" };

        var transactions = await _db.HisabDiaryTransaction
            .Where(x => x.CusId == cusId)
            .OrderBy(x => x.TransactionDate).ThenBy(x => x.Id)
            .ToListAsync();

        string defaultPhone = _config["WhatsApp:PhoneNumber"];
        if (string.IsNullOrEmpty(defaultPhone))
            return new { status = "error", message = "Default WhatsApp number not configured in appsettings." };

        // Calculate balances
        decimal totalNaamSilver = 0, totalNaamCash = 0, totalJamaSilver = 0, totalJamaCash = 0;
        foreach (var t in transactions)
        {
            if (t.TransactionType == "N") { totalNaamSilver += t.SilverInGram ?? 0; totalNaamCash += t.Cash ?? 0; }
            else { totalJamaSilver += t.SilverInGram ?? 0; totalJamaCash += t.Cash ?? 0; }
        }
        decimal netSilver = totalNaamSilver - totalJamaSilver;
        decimal netCash = totalNaamCash - totalJamaCash;

        var sb = new StringBuilder();
        sb.AppendLine("*MJ PAYAL JEWELLERS,*");
        //sb.AppendLine("*Sev Ka Bazar,Agra*");
        sb.AppendLine();
        sb.AppendLine($"*Customer:* {customer.Name}");
        string reportDate = DateTime.Now.ToString("dd/MM/yyyy");
        sb.AppendLine($"*Report Date:* {reportDate}");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine("*CURRENT BALANCE:*");

        if (netSilver == 0 && netCash == 0)
        {
            sb.AppendLine("*✅ Account Clear - No pending balance*");
        }
        else
        {
            if (netSilver != 0) sb.AppendLine($"*{(netSilver > 0 ? "🔴 Udhaar" : "🟢 Advance")}:* {Math.Abs(netSilver):0.000} g Silver");
            if (netCash != 0) sb.AppendLine($"*{(netCash > 0 ? "🔴 Udhaar" : "🟢 Advance")}:* ₹{Math.Abs(netCash):N2}");
        }

        sb.AppendLine();
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine();
        sb.AppendLine("*DETAILED TRANSACTION HISTORY*");
        sb.AppendLine($"*Total Transactions: {transactions.Count}*");
        sb.AppendLine();

        int i = 1;
        foreach (var t in transactions)
        {
            sb.Append($"*{i}.* {t.TransactionDate:dd/MM/yyyy} ");
            sb.AppendLine(t.TransactionType == "N" ? "*NAAM (Given)*" : "*JAMA (Received)*");

            if (t.SilverInGram > 0) sb.AppendLine($"  *Silver:* {t.SilverInGram:0.000} g");
            if (t.Cash > 0) sb.AppendLine($"  *Amount:* ₹{t.Cash:N2}");
            if (!string.IsNullOrWhiteSpace(t.Comment)) sb.AppendLine($"  *Note:* {t.Comment}");
            sb.AppendLine();
            i++;
        }

        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        //sb.AppendLine("*📱 Generated by Hisab Diary App*");
        sb.AppendLine($"_Complete transaction record as of {reportDate}_");
        sb.AppendLine();
        sb.AppendLine("_Thank you for your business! 🙏_");
        sb.Append("MJ PAYAL JEWELLERS");

        try
        {
            await _whatsAppService.SendInvoiceMessage(defaultPhone, sb.ToString());
            return new { status = "success", message = "WhatsApp report sent successfully!" };
        }
        catch (Exception ex)
        {
            return new { status = "error", message = $"Failed to send WhatsApp: {ex.Message}" };
        }
    }

}