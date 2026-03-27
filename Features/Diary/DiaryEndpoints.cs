using DotnetBoilerplate.Models.Requests;
using DotnetBoilerplate.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotnetBoilerplate.Features.Diary;

public static class DiaryEndpoints
{
    public static void MapDiaryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/diary")
            .WithTags("Diary");

        //============================================ /diary/customer ================================================
        // Endpoint to add or update a diary customer
        group.MapPost("/customer", async (
            AddDiaryCustomerRequest req,
            IDiaryCustomerService diaryService) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name))
            {
                return Results.BadRequest(new
                {
                    status = "fail",
                    message = "Missing Field: name"
                });
            }

            var (exists, result) = await diaryService.AddDiaryCustomerAsync(
                req.Name,
                req.Phone,
                req.CurrentCusId
            );

            return exists
                ? Results.Ok(result)
                : Results.Created("/diary/customer", result);
        })
        .WithName("AddOrUpdateDiaryCustomer")
        .WithOpenApi();

        group.MapGet("/customer/search", async (
    string q,
    IDiaryCustomerService diaryService) =>
        {
            if (string.IsNullOrWhiteSpace(q))
                return Results.Ok(new { status = "success", data = new List<object>() });

            var items = await diaryService.SearchDiaryCustomerNamesAsync(q);

            return Results.Ok(new
            {
                status = "success",
                data = items
            });
        })
.WithName("SearchDiaryCustomers")
.WithOpenApi();

        group.MapGet("/customer/details", async (
    int cusId,
    IDiaryCustomerService diaryService) =>
        {
            if (cusId <= 0)
            {
                return Results.BadRequest(new
                {
                    status = "fail",
                    message = "cusId is required"
                });
            }

            var customer = await diaryService.GetDiaryCustomerByIdAsync(cusId);

            if (customer == null)
            {
                return Results.Ok(new
                {
                    status = "fail",
                    message = "Customer not found"
                });
            }

            // GET ITEMS FOR THIS CUSTOMER
            var items = await diaryService.GetDiaryItemsByCustomerIdAsync(cusId);

            return Results.Ok(new
            {
                status = "success",
                data = new
                {
                    customer = new
                    {
                        customer.Id,
                        customer.Name,
                        customer.Phone
                    },
                    items = items
                }
            });
        })
.WithName("GetDiaryCustomerDetails")
.WithOpenApi();


        //============================================ /diary/items ================================================
        // Endpoint to add items for a specific diary customer
        group.MapPost("/items", async (
            AddDiaryItemsRequest req,
            IDiaryCustomerService diaryService) =>
        {
            try
            {
                if (req.CusId <= 0)
                {
                    return Results.BadRequest(new
                    {
                        status = "fail",
                        error = "Invalid CusId",
                        message = "Customer Id must be greater than 0"
                    });
                }

                if (req.Items == null || req.Items.Count == 0)
                {
                    return Results.BadRequest(new
                    {
                        status = "fail",
                        error = "Missing Items",
                        message = "Items array is required and cannot be empty"
                    });
                }

                await diaryService.AddDiaryItemsAsync(req.CusId, req.Items);

                return Results.Created("/diary/items", new
                {
                    status = "success",
                    message = "Items added successfully",
                    cusId = req.CusId,
                    itemCount = req.Items.Count
                });
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new
                {
                    status = "fail",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Failed to add diary items");
            }
        })
        .WithName("AddDiaryItems")
        .WithOpenApi();

        // Endpoint to update items for a specific diary customer (delete all old, add new)
        group.MapPut("/items", async (
            AddDiaryItemsRequest req,
            IDiaryCustomerService diaryService) =>
        {
            try
            {
                if (req.CusId <= 0)
                {
                    return Results.BadRequest(new
                    {
                        status = "fail",
                        error = "Invalid CusId",
                        message = "Customer Id must be greater than 0"
                    });
                }

                if (req.Items == null || req.Items.Count == 0)
                {
                    return Results.BadRequest(new
                    {
                        status = "fail",
                        error = "Missing Items",
                        message = "Items array is required and cannot be empty"
                    });
                }

                await diaryService.UpdateDiaryItemsAsync(req.CusId, req.Items);

                return Results.Ok(new
                {
                    status = "success",
                    message = "Items updated successfully",
                    cusId = req.CusId,
                    itemCount = req.Items.Count
                });
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new
                {
                    status = "fail",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Failed to update diary items");
            }
        })
        .WithName("UpdateDiaryItems")
        .WithOpenApi();
    }
}