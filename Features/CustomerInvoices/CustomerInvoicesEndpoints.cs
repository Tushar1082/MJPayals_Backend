using DotnetBoilerplate.Services;

namespace DotnetBoilerplate.Features.CustomerInvoices;

public static class CustomerInvoicesEndpoints
{
    public static void MapCustomerInvoicesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/customer-invoice")
        .WithTags("CustomerInvoices");

        group.MapGet("/{invoiceId:int}/items",
            async (int invoiceId, ICustomerInvoicesService service) =>
            {
                if (invoiceId <= 0)
                    return Results.BadRequest("invoiceId is required");

                var items = await service.GetItemsByInvoiceIdAsync(invoiceId);

                if (items == null || items.Count == 0)
                    return Results.NotFound("No items found for this invoice");

                return Results.Ok(new
                {
                    status = "success",
                    invoiceId,
                    items
                });
            })
            .WithName("GetCustomerInvoiceItems");

        // Add this inside the MapCustomerInvoicesEndpoints method, right under your existing endpoints:
        group.MapPatch("/{invoiceId:int}/silver-rate", async (
            int invoiceId,
            [Microsoft.AspNetCore.Mvc.FromBody] UpdateSilverRateRequest req,
            ICustomerInvoicesService service) =>
        {
            if (invoiceId <= 0)
                return Results.BadRequest(new { status = "fail", message = "Valid invoiceId is required" });

            if (req.SilverRate <= 0)
                return Results.BadRequest(new { status = "fail", message = "Silver rate must be greater than zero" });

            var isUpdated = await service.UpdateSilverRateAsync(invoiceId, req.SilverRate);

            if (isUpdated)
            {
                return Results.Ok(new
                {
                    status = "success",
                    message = "Silver Rate Updated Successfully!"
                });
            }

            return Results.NotFound(new { status = "fail", message = "Invoice not found" });
        })
        .WithName("UpdateSilverRate");

    group.MapDelete("/delete", async (int invoiceId, ICustomerInvoicesService service) =>
        {
            if (invoiceId <= 0)
            {
                return Results.BadRequest(new
                {
                    status = "fail",
                    message = "invoiceId is required"
                });
            }

            var isDeleted = await service.DeleteInvoiceAndItemsAsync(invoiceId);

            if (isDeleted)
            {
                return Results.Ok(new
                {
                    status = "success",
                    message = "Invoice Deleted Successfully!"
                });
            }

            return Results.Ok(new
            {
                status = "fail",
                message = "Failed to delete invoice and its items."
            });
        });
            


    }
}


// Add this simple class at the bottom of your file (outside the main class):
public class UpdateSilverRateRequest
{
    public decimal SilverRate { get; set; }
}