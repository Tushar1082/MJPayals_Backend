using DotnetBoilerplate.Models.Requests;
using DotnetBoilerplate.Services;

namespace DotnetBoilerplate.Features.InvoiceItems;

public static class InvoiceItemsEndpoints
{
    public static void MapInvoiceItemsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/invoice-items")
            .WithTags("Invoice Items");

        group.MapPost("/add", async (
        AddItemsRequest req,
        IInvoiceItemsService service) =>
        {
            try
            {
                //if (req.CusId <= 0)
                //    return Results.BadRequest("cus_id is required");

                //if (req.Items == null || req.Items.Count == 0)
                //    return Results.BadRequest("Items array is required");

                if (req.CusId <= 0)
                {
                    return Results.BadRequest(new
                    {
                        success = false,
                        error = "Require Cus_id Field.",
                        message = "Customer Id is required"
                    });
                }

                if (req.Items == null || req.Items.Count == 0)
                {
                    return Results.BadRequest(new
                    {
                        success = false,
                        error = "Require Items Field.",
                        message = "Items array is required"
                    });
                }

                if (req.SilverRate == null || req.SilverRate<=0) {
                    return Results.BadRequest(new
                    {
                        success = false,
                        error = "Require Valid Silver Rate.",
                        message = "Silver Rate is required"
                    });
                }


                var (isOldInvoice, invoiceId, silverRate) =
                    await service.AddItemsAsync(req.CusId, req.Items, req.InvoiceNo, req.SilverRate, req.CusType, req.CustomerName, req.SendWhatsAppMsg);

                return Results.Created("", new
                {
                    status = "success",
                    inv_no = invoiceId,
                    silverRate = silverRate,
                    message = isOldInvoice
                        ? "Invoice updated successfully"
                        : "Invoice created successfully"
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Invoice operation failed");
            }
        });

        group.MapPost("/delete", async (
        DeleteItemRequest req,
        IInvoiceItemsService service) =>
            {
                if (req.ItemId <= 0)
                    return Results.BadRequest("item_id is required");

                var deleted = await service.DeleteItemAsync(
                    req.ItemId
                );

                return deleted
                    ? Results.Ok(new { status = "success", message = "Item deleted successfully" })
                    : Results.Ok(new { status = "fail", message = "Item not found" });
            });


        group.MapGet("/", async (
            int cus_id,
            IInvoiceItemsService service) =>
        {
            if (cus_id <= 0)
                return Results.BadRequest("cus_id is required");

            var items = await service.GetItemsAsync(cus_id);

            return items.Any()
                ? Results.Ok(new { status = "success", data = items })
                : Results.Ok(new { status = "fail", message = "Customer Items Not Found!" });
        });

        group.MapGet("/fetch-rate", async (
        string item_name,
        char cusType,
        IInvoiceItemsService service) =>
        {
            if (string.IsNullOrWhiteSpace(item_name))
            {
                return Results.BadRequest(new
                {
                    status = "fail",
                    message = "item_name is required"
                });
            } else if (cusType == default) // default(char) = '\0'
            {
                return Results.BadRequest(new
                {
                    status = "fail",
                    message = "cusType is required"
                });
            }else if (cusType != 'R' && cusType != 'W' && cusType != 'B')
            {
                return Results.BadRequest(new
                {
                    status = "fail",
                    message = "Invalid cusType value."
                });
            }

                var result = await service.FetchLatestRateAsync(item_name, cusType);

            if (result.HasValue)
            {
                return Results.Ok(new
                {
                    status = "success",
                    rateType = result.Value.rateType,
                    rate = result.Value.rate,
                    polythenes = result.Value.polythenes,
                    labourType = result.Value.labourType,
                    labourRate = result.Value.labourRate,
                    labourAmount = result.Value.labourAmount
                });
            }

            return Results.Ok(new
            {
                status = "fail",
                message = "Item rate not found"
            });
        })
    .WithOpenApi();

        group.MapGet("/search", async (
        string q,
        char cusType,
        IInvoiceItemsService service) =>
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

                var items = await service.SearchItemNamesAsync(q, cusType);

                return Results.Ok(new
                {
                    status = "success",
                    data = items
                });
            });

    }
}
