using DotnetBoilerplate.Models.Requests;
using DotnetBoilerplate.Services;

namespace DotnetBoilerplate.Features.Customers;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/customer")
            .WithTags("Customers");

        group.MapPost("/", async (
            AddCustomerRequest req,
            ICustomerService customerService) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name))
            {
                return Results.BadRequest(new
                {
                    status = "fail",
                    message = "Missing Field: name"
                });
            } else if (req.CusType != 'W' && req.CusType != 'R' && req.CusType !='B') {
                return Results.BadRequest(new
                {
                    status = "fail",
                    message = "Invalid value inside type field"
                });
            }

            var (exists, result) = await customerService.AddCustomerAsync(
                req.Name,
                req.Phone,
                req.Address,
                req.City,
                req.CusType,
                req.CurrentCusId
            );

            return exists
                ? Results.Ok(result)
                : Results.Created("/customer", result);
        })
        .WithOpenApi();

        group.MapGet("/search", async (
            string q,
            char cusType,
            ICustomerService customerService) =>
        {
            if (string.IsNullOrWhiteSpace(q))
                return Results.Ok(new { status = "success", data = new List<string>() });

            if (cusType == default) // default(char) = '\0'
            {
                return Results.BadRequest(new
                {
                    status = "fail",
                    message = "cusType is required"
                });
            }
            else if (cusType != 'R' && cusType != 'W' && cusType != 'B')
            {
                return Results.BadRequest(new
                {
                    status = "fail",
                    message = "Invalid cusType value."
                });
            }

            var items = await customerService.SearchCustomerNamesAsync(q, cusType);

            return Results.Ok(new
            {
                status = "success",
                data = items
            });
        });

        group.MapGet("/details", async (
        int cusId,
        ICustomerService customerService) =>
            {
                if (cusId==null || cusId<=0)
                {
                    return Results.BadRequest(new
                    {
                        status = "fail",
                        message = "cusId is required"
                    });
                }

                var customer = await customerService.GetCustomerByNameAsync(cusId);

                if (customer == null)
                {
                    return Results.Ok(new
                    {
                        status = "fail",
                        message = "Customer not found"
                    });
                }

                return Results.Ok(new
                {
                    status = "success",
                    data = new
                    {
                        customer.Id,
                        customer.Name,
                        customer.Phone,
                        customer.Address,
                        customer.City,
                        customer.Type
                    }
                });
            })
    .WithOpenApi();

        group.MapGet("/summary", async (
            ICustomerService customerService) =>
                {
                    var data = await customerService.GetCustomersSummaryAsync();

                    return Results.Ok(new
                    {
                        status = "success",
                        data
                    });
                })
        .WithOpenApi();

        group.MapGet("/invoices", async (
    int page,
    int pageSize,
    string? searchQuery,
    DateTime? invoiceDateStart,
    DateTime? invoiceDateEnd,
    DateTime? customerDateStart,
    DateTime? customerDateEnd,
    char? cusType,
    ICustomerService customerService) =>
        {
            var result = await customerService.GetInvoiceListAsync(
                page,
                pageSize,
                searchQuery,
                invoiceDateStart,
                invoiceDateEnd,
                customerDateStart,
                customerDateEnd,
                cusType
            );

            return Results.Ok(new
            {
                status = "success",
                result
            });
        })
.WithOpenApi();

    }
}
