using DotnetBoilerplate.Data;
using DotnetBoilerplate.Models.Entities;
using DotnetBoilerplate.Utils;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace DotnetBoilerplate.Services;

public interface ICustomerService
{
    Task<(bool exists, object result)> AddCustomerAsync(
        string name,
        string? phone,
        string? address,
        string? city,
        char cusType,
        int? currentCusId
    );
    Task<List<object>> SearchCustomerNamesAsync(string query, char cusType);
    Task<Customer?> GetCustomerByNameAsync(int cusId);
    Task<List<object>> GetCustomersSummaryAsync();
    Task<object> GetInvoiceListAsync(
        int page = 1,
        int pageSize = 20,
        string? searchQuery = null,
        DateTime? invoiceDateStart = null,
        DateTime? invoiceDateEnd = null,
        DateTime? customerDateStart = null,
        DateTime? customerDateEnd = null,
        char? cusType = null
    );
    //Task<List<object>> GetInvoiceListAsync();
    //Task<List<object>> GetCustomersSummaryAsync();
}

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _db;

    public CustomerService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(bool exists, object result)> AddCustomerAsync(
    string name,
    string? phone,
    string? address,
    string? city,
    char cusType,
    int? currentCusId
)
    {
        var cusName = name.Trim();

        // 1️. EDIT MODE (Bill already generated)
        if (currentCusId != null && currentCusId > 0)
        {
            var existing = await _db.Customers
                .FirstOrDefaultAsync(x => x.Id == currentCusId);

            if (existing == null)
                return (false, new { status = "error", message = "Customer not found" });

            existing.Name = cusName;
            existing.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
            existing.Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
            existing.City = string.IsNullOrWhiteSpace(city) ? null : city.Trim();
            existing.Type = cusType;

            await _db.SaveChangesAsync();

            return (true, new
            {
                status = "success",
                message = "Customer Updated Successfully!",
                data = existing,
                updatedCustomer = true
            });
        }

        // 2️. CREATE MODE

        var existingCustomers = await _db.Customers
            .Where(x => x.Name == cusName && x.Type == cusType)
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
                Normalize(existing.Phone) == Normalize(phone) &&
                Normalize(existing.Address) == Normalize(address) &&
                Normalize(existing.City) == Normalize(city);

            if (exactMatch)
            {
                return (true, new
                {
                    status = "success",
                    message = "Customer Already Exists!",
                    data = existing,
                    oldCustomer = true
                });
            }

            // 🔎 First check for conflict
            bool hasConflict =
                (!string.IsNullOrWhiteSpace(existing.Phone) &&
                 !string.IsNullOrWhiteSpace(phone) &&
                 Normalize(existing.Phone) != Normalize(phone))

                ||

                (!string.IsNullOrWhiteSpace(existing.Address) &&
                 !string.IsNullOrWhiteSpace(address) &&
                 Normalize(existing.Address) != Normalize(address))

                ||

                (!string.IsNullOrWhiteSpace(existing.City) &&
                 !string.IsNullOrWhiteSpace(city) &&
                 Normalize(existing.City) != Normalize(city));

            if (hasConflict)
                continue; // Create new customer


            // ✅ Safe to update NULL fields now
            bool shouldUpdate = false;

            if (string.IsNullOrWhiteSpace(existing.Phone) &&
                !string.IsNullOrWhiteSpace(phone))
            {
                existing.Phone = phone.Trim();
                shouldUpdate = true;
            }

            if (string.IsNullOrWhiteSpace(existing.Address) &&
                !string.IsNullOrWhiteSpace(address))
            {
                existing.Address = address.Trim();
                shouldUpdate = true;
            }

            if (string.IsNullOrWhiteSpace(existing.City) &&
                !string.IsNullOrWhiteSpace(city))
            {
                existing.City = city.Trim();
                shouldUpdate = true;
            }

            if (shouldUpdate)
            {
                await _db.SaveChangesAsync();

                return (true, new
                {
                    status = "success",
                    message = "Customer Updated Successfully!",
                    data = existing,
                    updatedCustomer = true
                });
            }
        }

        // 3️. If partial match → create new (NO UPDATE)

        var customer = new Customer
        {
            Name = cusName,
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim(),
            City = string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
            Type = cusType
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        return (false, new
        {
            status = "success",
            message = "Customer Added Successfully!",
            data = customer
        });
    }


    //public async Task<(bool exists, object result)> AddCustomerAsync(
    //    string name,
    //    string? phone,
    //    string? address,
    //    string? city,
    //    char cusType,
    //    int? currentCusId
    //)
    //{
    //    var cusName = name.Trim();
    //    var existingCustomers;

    //    if (currentCusId!= null && currentCusId>0) {
    //        existingCustomers = await _db.Customers
    //        .Where(x => x.Id == currentCusId)
    //        .ToListAsync();
    //    }
    //    else { 
    //        existingCustomers = await _db.Customers
    //        .Where(x => x.Name.ToLower() == cusName.ToLower())
    //        .ToListAsync();
    //    }

    //    foreach (var existing in existingCustomers)
    //    {
    //        bool exactMatch =
    //            string.Equals(existing.Phone, phone, StringComparison.OrdinalIgnoreCase) &&
    //            string.Equals(existing.Address, address, StringComparison.OrdinalIgnoreCase) &&
    //            string.Equals(existing.City, city, StringComparison.OrdinalIgnoreCase) &&
    //            existing.Type == cusType;

    //        if (exactMatch)
    //        {
    //            return (true, new
    //            {
    //                status = "success",
    //                message = "Customer Already Exists!",
    //                data = existing,
    //                oldCustomer = true
    //            });
    //        }

    //        // 🔥 Partial match: fill missing fields
    //        if (existing.Type == cusType)
    //        {
    //            bool updated = false;

    //            if (existing.Phone == null && !string.IsNullOrWhiteSpace(phone))
    //            {
    //                existing.Phone = phone.Trim();
    //                updated = true;
    //            }

    //            if (existing.Address == null && !string.IsNullOrWhiteSpace(address))
    //            {
    //                existing.Address = address.Trim();
    //                updated = true;
    //            }

    //            if (existing.City == null && !string.IsNullOrWhiteSpace(city))
    //            {
    //                existing.City = city.Trim();
    //                updated = true;
    //            }

    //            if (updated)
    //            {
    //                await _db.SaveChangesAsync();

    //                return (true, new
    //                {
    //                    status = "success",
    //                    message = "Customer Updated Successfully!",
    //                    data = existing,
    //                    updatedCustomer = true
    //                });
    //            }
    //        }
    //    }


    //    var customer = new Customer
    //    {
    //        Name = cusName,
    //        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
    //        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim(),
    //        City = string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
    //        Type = cusType
    //    };

    //    _db.Customers.Add(customer);
    //    await _db.SaveChangesAsync();

    //    return (false, new
    //    {
    //        status = "success",
    //        message = "Customer Added Successfully!",
    //        data = new
    //            {
    //                customer.Id,
    //                customer.Name,
    //                customer.Phone,
    //                customer.Address,
    //                customer.City,
    //                customer.Type
    //            }
    //    });
    //}


    public async Task<List<object>> SearchCustomerNamesAsync(string query, char cusType)
    {
        query = query.Trim().ToLower();

        return await _db.Customers
            .Where(x => x.Name.ToLower().Contains(query) && x.Type == cusType )
            .Select(x => new { x.Id, x.Name, x.City })
            //.Distinct()
            .OrderBy(x => x.Name)
            .ToListAsync<object>();
    }

    public async Task<Customer?> GetCustomerByNameAsync(int cusId)
    {
        return await _db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cusId);
    }

    //public async Task<List<object>> GetCustomersSummaryAsync()
    //{
    //    var customers = await _db.Customers
    //        .AsNoTracking()
    //        .Select(c => new
    //        {
    //            c.Id,
    //            c.Name,
    //            c.Phone,
    //            c.Address,
    //            c.City,
    //            c.Type,
    //            c.CreatedAt,

    //            Invoices = _db.CustomerInvoices
    //                .Where(inv => inv.CustomerId == c.Id)
    //                .Select(inv => new
    //                {
    //                    inv.Id,
    //                    inv.InvoiceDate,
    //                    inv.SilverRate,

    //                    Items = _db.InvoiceItems
    //                        .Where(item => item.InvoiceId == inv.Id)
    //                        .Select(item => new
    //                        {
    //                            item.NetWeight,
    //                            item.Rate,
    //                            item.MeasurementValue,
    //                            item.LabourAmount
    //                        })
    //                        .ToList()
    //                })
    //                .OrderByDescending(inv => inv.InvoiceDate)
    //                .ToList(),
    //                LatestInvoiceDate = _db.CustomerInvoices
    //                    .Where(inv => inv.CustomerId == c.Id)
    //                    .Max(inv => (DateTime?)inv.InvoiceDate)
    //        })
    //        .OrderByDescending(c => c.LatestInvoiceDate)
    //        //.OrderByDescending(c => c.CreatedAt)
    //        .ToListAsync();

    //    var result = customers.Select(c =>
    //    {
    //        decimal customerTotalSpent = 0;

    //        var invoices = c.Invoices.Select(inv =>
    //        {
    //            decimal invoiceNetWeight = 0;
    //            decimal invoiceTotalAmount = 0;

    //            foreach (var item in inv.Items)
    //            {
    //                var netWeight = item.NetWeight ?? 0m;
    //                var rate = item.Rate ?? 0m;

    //                invoiceNetWeight += netWeight;

    //                if (item.MeasurementValue == 'G')
    //                {
    //                    if (c.Type == 'W') { 
    //                        invoiceTotalAmount += Math.Round(((netWeight * rate)+ (item.LabourAmount ?? 0)), 0, MidpointRounding.AwayFromZero);
    //                    }
    //                    else
    //                        invoiceTotalAmount += Math.Round(netWeight * rate, 0, MidpointRounding.AwayFromZero);
    //                }
    //                else if (item.MeasurementValue == 'P')
    //                {
    //                    if (c.Type == 'W') {
    //                        decimal silverRateGm = inv.SilverRate / 1000m; //kg --> gm
    //                        decimal fine = netWeight * (rate/100);
    //                        decimal am = Math.Round(fine * silverRateGm) + (item.LabourAmount ?? 0);
    //                        invoiceTotalAmount += am;
    //                    }else
    //                        invoiceTotalAmount += Math.Round((netWeight * ((inv.SilverRate * (rate / 100) / 1000))), 0, MidpointRounding.AwayFromZero); ;
    //                }
    //            }

    //            customerTotalSpent += invoiceTotalAmount;

    //            return new
    //            {
    //                id = inv.Id,
    //                invoiceDate = inv.InvoiceDate,
    //                silverRate = inv.SilverRate,
    //                //netWeight = Math.Round(invoiceNetWeight, 3),
    //                netWeight = Math.Truncate(invoiceNetWeight * 100) / 100,
    //                totalAmount = invoiceTotalAmount
    //            };
    //        }).ToList();

    //        return new
    //        {
    //            cusId = c.Id,
    //            name = c.Name,
    //            phone = c.Phone,
    //            address = c.Address,
    //            city = c.City,
    //            cusType = c.Type,
    //            createdAt = c.CreatedAt,

    //            lastPurchase = invoices.Any()
    //                ? invoices.Max(i => i.invoiceDate).ToString("dd-MM-yyyy")
    //                : null,

    //            totalSpent = customerTotalSpent,
    //            invoices
    //        };
    //    });

    //    return result.Cast<object>().ToList();
    //}


    public async Task<List<object>> GetCustomersSummaryAsync()
    {
        var customers = await _db.Customers
            .AsNoTracking()
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Phone,
                c.Address,
                c.City,
                c.Type,
                c.CreatedAt,
                Invoices = _db.CustomerInvoices
                    .Where(inv => inv.CustomerId == c.Id)
                    .Select(inv => new
                    {
                        inv.Id,
                        inv.InvoiceDate,
                        inv.SilverRate,
                        Items = _db.InvoiceItems
                            .Where(item => item.InvoiceId == inv.Id)
                            .Select(item => new
                            {
                                item.GrossWeight,
                                item.Polythenes,
                                item.Rate,
                                item.MeasurementValue,
                                item.LabourType,
                                item.LabourRate,
                                item.LabourAmount
                            })
                            .ToList()
                    })
                    .OrderByDescending(inv => inv.InvoiceDate)
                    .ToList(),
                LatestInvoiceDate = _db.CustomerInvoices
                    .Where(inv => inv.CustomerId == c.Id)
                    .Max(inv => (DateTime?)inv.InvoiceDate)
            })
            .OrderByDescending(c => c.LatestInvoiceDate)
            .ToListAsync();

        var result = customers.Select(c =>
        {
            decimal customerTotalSpent = 0;

            var invoices = c.Invoices.Select(inv =>
            {
                decimal invoiceNetWeight = 0;
                decimal invoiceTotalAmount = 0;

                foreach (var item in inv.Items)
                {
                    // Parse polythenes from XML/JSON string (if exists)
                    List<BillingHelper.PolytheneRow> ppRows = new List<BillingHelper.PolytheneRow>();

                    if (!string.IsNullOrEmpty(item.Polythenes))
                    {
                        try
                        {
                            // If stored as JSON array
                            var polythenesList = JsonSerializer.Deserialize<List<PolytheneDto>>(item.Polythenes);
                            ppRows = polythenesList?.Select(p => new BillingHelper.PolytheneRow
                            {
                                Count = p.NoOfPPs ?? 0,
                                Weight = p.Weight ?? 0
                            }).ToList() ?? new List<BillingHelper.PolytheneRow>();
                        }
                        catch
                        {
                            // If parsing fails, use empty list
                            ppRows = new List<BillingHelper.PolytheneRow>();
                        }
                    }

                    // Calculate labour num pieces (reverse calculate from amount if needed)
                    int labourNumPieces = 0;
                    if (item.LabourType == 'P' && item.LabourAmount.HasValue && item.LabourRate.HasValue && item.LabourRate.Value > 0)
                    {
                        labourNumPieces = (int)(item.LabourAmount.Value / item.LabourRate.Value);
                    }

                    // ✅ Use BillingHelper for consistent calculation
                    var calculationResult = BillingHelper.CalculateItemAmount(
                        cusType: c.Type.ToString(),
                        grossWeight: item.GrossWeight,
                        ppRows: ppRows,
                        rateGm: item.MeasurementValue == 'G' ? item.Rate : null,
                        rateKg: item.MeasurementValue == 'K' ? item.Rate : null,
                        ratePer: item.MeasurementValue == 'P' ? item.Rate : null,
                        silverRate: inv.SilverRate,
                        labourType: item.LabourType?.ToString(),
                        labourRate: item.LabourRate ?? 0,
                        labourNumPieces: labourNumPieces
                    );

                    // Accumulate totals
                    invoiceNetWeight += calculationResult.NetWeight;
                    invoiceTotalAmount += calculationResult.Amount;
                }

                customerTotalSpent += invoiceTotalAmount;

                return new
                {
                    id = inv.Id,
                    invoiceDate = inv.InvoiceDate,
                    silverRate = inv.SilverRate,
                    netWeight = BillingHelper.RoundTo(invoiceNetWeight, 2),  // ✅ Consistent rounding
                    totalAmount = (int)invoiceTotalAmount  // ✅ Integer amount
                };
            }).ToList();

            return new
            {
                cusId = c.Id,
                name = c.Name,
                phone = c.Phone,
                address = c.Address,
                city = c.City,
                cusType = c.Type,
                createdAt = c.CreatedAt,
                lastPurchase = invoices.Any()
                    ? invoices.Max(i => i.invoiceDate).ToString("dd-MM-yyyy")
                    : null,
                totalSpent = (int)customerTotalSpent,  // ✅ Integer total
                invoices
            };
        });

        return result.Cast<object>().ToList();
    }

    // DTO class for parsing polythenes JSON
    public class PolytheneDto
    {
        public decimal? NoOfPPs { get; set; }
        public decimal? Weight { get; set; }
    }

    public async Task<object> GetInvoiceListAsync(
    int page = 1,
    int pageSize = 20,
    string? searchQuery = null,
    DateTime? invoiceDateStart = null,
    DateTime? invoiceDateEnd = null,
    DateTime? customerDateStart = null,
    DateTime? customerDateEnd = null,
    char? cusType = null
)
    {
        var query = _db.CustomerInvoices
            .AsNoTracking()
            .Join(
                _db.Customers.AsNoTracking(),
                inv => inv.CustomerId,
                c => c.Id,
                (inv, c) => new { inv, c }
            );
            //.Where(x => x.c.Type != 'B');

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            searchQuery = searchQuery.Trim().ToLower();
            query = query.Where(x =>
                x.c.Name.ToLower().Contains(searchQuery) ||
                (x.c.Phone != null && x.c.Phone.Contains(searchQuery)) ||
                x.inv.Id.ToString().Contains(searchQuery)
            );
        }

        if (invoiceDateStart.HasValue)
        {
            query = query.Where(x => x.inv.InvoiceDate >= invoiceDateStart.Value);
        }

        if (invoiceDateEnd.HasValue)
        {
            var endDate = invoiceDateEnd.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.inv.InvoiceDate <= endDate);
        }

        if (customerDateStart.HasValue)
        {
            query = query.Where(x => x.c.CreatedAt >= customerDateStart.Value);
        }

        if (customerDateEnd.HasValue)
        {
            var endDate = customerDateEnd.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.c.CreatedAt <= endDate);
        }

        if (cusType.HasValue)
        {
            query = query.Where(x => x.c.Type == cusType.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination and ordering
        var invoices = await query
            .OrderByDescending(x => x.inv.InvoiceDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                invoiceNo = x.inv.Id,
                invoiceDate = x.inv.InvoiceDate,
                silverRate = x.inv.SilverRate,
                cusId = x.c.Id,
                customerName = x.c.Name,
                phone = x.c.Phone,
                city = x.c.City,
                cusType = x.c.Type
            })
            .ToListAsync();

        return new
        {
            data = invoices,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            hasMore = page * pageSize < totalCount
        };
    }

}
