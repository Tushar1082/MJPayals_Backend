using DotnetBoilerplate.Data;
using DotnetBoilerplate.Models.Entities;
using DotnetBoilerplate.Models.Requests;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DotnetBoilerplate.Services;

public interface IHisabDiaryService
{
    // Customer Methods
    Task<object> AddHisabDiaryCustomerAsync(string name, string phone, string? address, string? city, string? firmName);
    Task<object> GetAllHisabDiaryCustomersAsync(int page = 1, int pageSize = 10);
    Task<object?> GetHisabDiaryCustomerByIdAsync(int id);
    Task<object> SearchHisabDiaryCustomersAsync(string term);

    // Transaction Methods
    Task<object> AddHisabDiaryTransactionAsync(int cusId, string transactionType, decimal? silverInGram, decimal? cash, string? comment, string? mediaUrls, DateTime transactionDate);
    Task<object> GetAllHisabDiaryTransactionsAsync();
    Task<object?> GetHisabDiaryTransactionByIdAsync(int id);
    Task<object> GetTransactionsByCustomerIdAsync(int cusId);
    Task<object> DeleteHisabDiaryTransactionAsync(int id);
}

public class HisabDiaryService : IHisabDiaryService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public HisabDiaryService(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
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

    public async Task<object> GetAllHisabDiaryCustomersAsync(int page = 1, int pageSize = 10)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _db.HisabDiaryCustomer
        .Select(c => new
        {
            c.Id,
            c.Name,
            c.Phone,
            c.Address,
            c.City,
            c.FirmName,
            c.CreatedAt,

            TotalNaam = _db.HisabDiaryTransaction
                .Where(t => t.CusId == c.Id && t.TransactionType == "N")
                .Sum(t => (decimal?)(t.SilverInGram)) ?? 0,

            TotalJama = _db.HisabDiaryTransaction
                .Where(t => t.CusId == c.Id && t.TransactionType == "J")
                .Sum(t => (decimal?)(t.SilverInGram)) ?? 0
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

    public async Task<object> GetTransactionsByCustomerIdAsync(int cusId)
    {
        var exists = await _db.HisabDiaryCustomer
            .AnyAsync(x => x.Id == cusId);

        if (!exists)
        {
            return new
            {
                status = "error",
                message = "Customer not found"
            };
        }

        var transactions = await _db.HisabDiaryTransaction
            .Where(x => x.CusId == cusId)
            .OrderByDescending(x => x.TransactionDate)
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

        return new
        {
            status = "success",
            data = transactions
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

}