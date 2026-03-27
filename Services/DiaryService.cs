using DotnetBoilerplate.Data;
using DotnetBoilerplate.Models.Entities;
using DotnetBoilerplate.Models.Requests;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DotnetBoilerplate.Services;

public interface IDiaryCustomerService
{
    Task<(bool exists, object result)> AddDiaryCustomerAsync(
        string name,
        string? phone,
        int? currentCusId
    );

    Task<bool> AddDiaryItemsAsync(
        int cusId,
        List<DiaryItemDto> items
    );

    Task<bool> UpdateDiaryItemsAsync(
        int cusId,
        List<DiaryItemDto> items
    );

    Task<List<object>> SearchDiaryCustomerNamesAsync(string query);
    Task<DiaryCustomer?> GetDiaryCustomerByIdAsync(int cusId);
    Task<string> SaveImageAsync(string base64Image);  // Remove cusId and itemIndex params
    Task<List<object>> GetDiaryItemsByCustomerIdAsync(int cusId);  // Change return type

}

public class DiaryService : IDiaryCustomerService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public DiaryService(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<List<object>> GetDiaryItemsByCustomerIdAsync(int cusId)
    {
        var items = await _db.DiaryItems
            .Where(i => i.CusId == cusId)
            .OrderBy(i => i.Id)
            .ToListAsync();

        return items.Select(item => new
        {
            item.Id,
            item.FineSilver,
            item.LabourBalance,
            item.Comment,
            CommentMediaLinks = !string.IsNullOrWhiteSpace(item.CommentMediaLinks)
                ? JsonSerializer.Deserialize<List<string>>(item.CommentMediaLinks)
                : new List<string>()
        }).ToList<object>();
    }

    public async Task<(bool exists, object result)> AddDiaryCustomerAsync(
        string name,
        string? phone,
        int? currentCusId
    )
    {
        var cusName = name.Trim();
        var normalizedPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

        // 1️⃣ EDIT MODE (Customer already exists with ID)
        if (currentCusId != null && currentCusId > 0)
        {
            var existing = await _db.DiaryCustomer
                .FirstOrDefaultAsync(x => x.Id == currentCusId);

            if (existing == null)
                return (false, new { status = "error", message = "Customer not found" });

            existing.Name = cusName;
            existing.Phone = normalizedPhone;

            await _db.SaveChangesAsync();

            return (true, new
            {
                status = "success",
                message = "Customer Updated Successfully!",
                data = existing,
                isOldCustomer = true,
                wasUpdated = true
            });
        }

        // 2️⃣ CREATE MODE - Check if customer exists

        var existingCustomers = await _db.DiaryCustomer
            .Where(x => x.Name == cusName)
            .ToListAsync();

        string Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? ""
                : value.Trim().ToLower();
        }

        foreach (var existing in existingCustomers)
        {
            bool exactMatch =
                Normalize(existing.Phone) == Normalize(normalizedPhone);

            if (exactMatch)
            {
                return (true, new
                {
                    status = "success",
                    message = "Customer Already Exists!",
                    data = existing,
                    isOldCustomer = true,
                    wasUpdated = false
                });
            }

            bool hasConflict =
                !string.IsNullOrWhiteSpace(existing.Phone) &&
                !string.IsNullOrWhiteSpace(normalizedPhone) &&
                Normalize(existing.Phone) != Normalize(normalizedPhone);

            if (hasConflict)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(existing.Phone) &&
                !string.IsNullOrWhiteSpace(normalizedPhone))
            {
                existing.Phone = normalizedPhone;
                await _db.SaveChangesAsync();

                return (true, new
                {
                    status = "success",
                    message = "Customer Updated Successfully!",
                    data = existing,
                    isOldCustomer = true,
                    wasUpdated = true
                });
            }

            if (!string.IsNullOrWhiteSpace(existing.Phone) &&
                string.IsNullOrWhiteSpace(normalizedPhone))
            {
                return (true, new
                {
                    status = "success",
                    message = "Customer Already Exists!",
                    data = existing,
                    isOldCustomer = true,
                    wasUpdated = false
                });
            }
        }

        // 3️⃣ CREATE NEW CUSTOMER
        var customer = new DiaryCustomer
        {
            Name = cusName,
            Phone = normalizedPhone
        };

        _db.DiaryCustomer.Add(customer);
        await _db.SaveChangesAsync();

        return (false, new
        {
            status = "success",
            message = "Customer Added Successfully!",
            data = customer,
            isOldCustomer = false,
            wasUpdated = false
        });
    }

    public async Task<bool> AddDiaryItemsAsync(
    int cusId,
    List<DiaryItemDto> items
)
    {
        var customerExists = await _db.DiaryCustomer
            .AnyAsync(c => c.Id == cusId);

        if (!customerExists)
        {
            throw new InvalidOperationException($"Customer with ID {cusId} not found");
        }

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];

            // Save images and get filenames
            List<string>? imageFileNames = null;
            if (item.Images != null && item.Images.Count > 0)
            {
                imageFileNames = new List<string>();
                foreach (var base64Image in item.Images)
                {
                    var fileName = await SaveImageAsync(base64Image);
                    imageFileNames.Add(fileName);
                }
            }

            // Store as JSON array
            string? jsonImages = imageFileNames != null && imageFileNames.Count > 0
                ? JsonSerializer.Serialize(imageFileNames)
                : null;

            _db.DiaryItems.Add(new DiaryItems
            {
                CusId = cusId,
                FineSilver = item.FineSilver,
                LabourBalance = item.LbrBalance,
                Comment = item.Comment,
                CommentMediaLinks = jsonImages  // Store JSON array
            });
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateDiaryItemsAsync(
    int cusId,
    List<DiaryItemDto> items
)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            var customerExists = await _db.DiaryCustomer
                .AnyAsync(c => c.Id == cusId);

            if (!customerExists)
            {
                throw new InvalidOperationException($"Customer with ID {cusId} not found");
            }

            // 1. Get old items to delete their images
            var existingItems = await _db.DiaryItems
                .Where(i => i.CusId == cusId)
                .ToListAsync();

            // Delete old images from filesystem
            foreach (var oldItem in existingItems)
            {
                if (!string.IsNullOrWhiteSpace(oldItem.CommentMediaLinks))
                {
                    try
                    {
                        var fileNames = JsonSerializer.Deserialize<List<string>>(oldItem.CommentMediaLinks);
                        if (fileNames != null)
                        {
                            foreach (var fileName in fileNames)
                            {
                                DeleteImageFromFileSystem(fileName);
                            }
                        }
                    }
                    catch { }
                }
            }

            // 2. Delete all existing items
            _db.DiaryItems.RemoveRange(existingItems);

            // 3. Add new items with new images
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];

                // Save images and get filenames
                List<string>? imageFileNames = null;
                if (item.Images != null && item.Images.Count > 0)
                {
                    imageFileNames = new List<string>();
                    foreach (var base64Image in item.Images)
                    {
                        var fileName = await SaveImageAsync(base64Image);
                        imageFileNames.Add(fileName);
                    }
                }

                // Store as JSON array
                string? jsonImages = imageFileNames != null && imageFileNames.Count > 0
                    ? JsonSerializer.Serialize(imageFileNames)
                    : null;

                _db.DiaryItems.Add(new DiaryItems
                {
                    CusId = cusId,
                    FineSilver = item.FineSilver,
                    LabourBalance = item.LbrBalance,
                    Comment = item.Comment,
                    CommentMediaLinks = jsonImages
                });
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<string> SaveImageAsync(string base64Image)
    {
        try
        {
            return "";
            // Remove data:image/xxx;base64, prefix if exists
            // var base64Data = base64Image;
            // if (base64Image.Contains(","))
            // {
            //     base64Data = base64Image.Split(',')[1];
            // }

            // var imageBytes = Convert.FromBase64String(base64Data);

            // // Get base path from appsettings
            // var basePath = _config["ImageStorage:BasePath"] ?? "D:\\Assets\\Images";

            // // Create directory if doesn't exist
            // if (!Directory.Exists(basePath))
            // {
            //     Directory.CreateDirectory(basePath);
            // }

            // // Generate unique filename
            // var fileName = $"{Guid.NewGuid()}.jpg";
            // var filePath = Path.Combine(basePath, fileName);

            // // Save file
            // await File.WriteAllBytesAsync(filePath, imageBytes);

            // // Return just the filename (not full path)
            // return fileName;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to save image: {ex.Message}");
        }
    }

    private void DeleteImageFromFileSystem(string fileName)
    {
        try
        {
            return;
            // if (string.IsNullOrWhiteSpace(fileName)) return;

            // var basePath = _config["ImageStorage:BasePath"] ?? "D:\\Assets\\Images";
            // var fullPath = Path.Combine(basePath, fileName);

            // if (File.Exists(fullPath))
            // {
            //     File.Delete(fullPath);
            // }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to delete image {fileName}: {ex.Message}");
        }
    }

    public async Task<List<object>> SearchDiaryCustomerNamesAsync(string query)
    {
        query = query.Trim().ToLower();

        return await _db.DiaryCustomer
            .Where(x => x.Name.ToLower().Contains(query))
            .Select(x => new { x.Id, x.Name })
            .OrderBy(x => x.Name)
            .ToListAsync<object>();
    }

    public async Task<DiaryCustomer?> GetDiaryCustomerByIdAsync(int cusId)
    {
        return await _db.DiaryCustomer
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cusId);
    }
}