using DotnetBoilerplate.Models.Requests;
using DotnetBoilerplate.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotnetBoilerplate.Features.HisabDiary;

public static class HisabDiaryEndpoints
{
    public static void MapHisabDiaryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/hisabDiary")
            .WithTags("Hisab Diary");

        //============================================ /hisabDiary/customer ================================================

        group.MapGet("/customer", async (
            IHisabDiaryService hisabDiaryService,
            int page = 1,
            int pageSize = 10
        ) =>
        {
            return await hisabDiaryService.GetAllHisabDiaryCustomersAsync(page, pageSize);
        });

        group.MapPost("/customer/add", async (
            AddHisabDiaryCustomerRequest req,
            IHisabDiaryService hisabDiaryService) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name))
            {
                return Results.BadRequest(new
                {
                    status = "fail",
                    message = "Missing Field: name"
                });
            }

            if (string.IsNullOrWhiteSpace(req.Phone))
            {
                return Results.BadRequest(new
                {
                    status = "fail",
                    message = "Missing Field: phone"
                });
            }

            var result = await hisabDiaryService.AddHisabDiaryCustomerAsync(
                req.Name,
                req.Phone,
                req.Address,
                req.City,
                req.FirmName
            );

            return Results.Created("/customer/add", result);
        })
        .WithOpenApi();

        group.MapGet("/customer/{id:int}", async (
            IHisabDiaryService hisabDiaryService,
            int id
        ) =>
        {
            var result = await hisabDiaryService.GetHisabDiaryCustomerByIdAsync(id);

            return result is null
                ? Results.NotFound(new { status = "error", message = "Customer not found" })
                : Results.Ok(result);
        })
        .WithOpenApi();

        group.MapGet("/customer/search", async (
            IHisabDiaryService hisabDiaryService,
            string term
        ) =>
        {
            return await hisabDiaryService.SearchHisabDiaryCustomersAsync(term);
        })
        .WithOpenApi();

        //============================================ /hisabDiary/transaction ================================================

        group.MapPost("/transaction/add", async (
            AddHisabDiaryTransactionRequest req,
            IHisabDiaryService hisabDiaryService) =>
        {
            if (req.CusId <= 0)
            {
                return Results.BadRequest(new
                {
                    status = "fail",
                    message = "Invalid CusId"
                });
            }

            if (string.IsNullOrWhiteSpace(req.TransactionType))
            {
                return Results.BadRequest(new
                {
                    status = "fail",
                    message = "Missing Field: transactionType"
                });
            }

            var result = await hisabDiaryService.AddHisabDiaryTransactionAsync(
                req.CusId,
                req.TransactionType,
                req.SilverInGram,
                req.Cash,
                req.Comment,
                req.MediaUrls,
                req.Date ?? DateTime.Now
            );

            return Results.Created("/transaction/add", result);
        })
        .WithOpenApi();


        group.MapGet("/transaction", async (
            IHisabDiaryService hisabDiaryService
        ) =>
        {
            return await hisabDiaryService.GetAllHisabDiaryTransactionsAsync();
        });

        group.MapGet("/transaction/{id:int}", async (
            IHisabDiaryService hisabDiaryService,
            int id
        ) =>
        {
            var result = await hisabDiaryService.GetHisabDiaryTransactionByIdAsync(id);

            return result is null
                ? Results.NotFound(new
                {
                    status = "error",
                    message = "Transaction not found"
                })
                : Results.Ok(result);
        });

        group.MapGet("/transaction/customer/{cusId:int}", async (
            IHisabDiaryService hisabDiaryService,
            int cusId
        ) =>
        {
            var result = await hisabDiaryService.GetTransactionsByCustomerIdAsync(cusId);

            if (result is not null && result.GetType().GetProperty("status")?.GetValue(result)?.ToString() == "error")
            {
                return Results.NotFound(result);
            }

            return Results.Ok(result);
        });


        group.MapDelete("/transaction/{id:int}", async (
           IHisabDiaryService hisabDiaryService,
           int id
       ) =>
        {
            var result = await hisabDiaryService.DeleteHisabDiaryTransactionAsync(id);

            if (result is not null && result.GetType().GetProperty("status")?.GetValue(result)?.ToString() == "error")
            {
                return Results.NotFound(result);
            }

            return Results.Ok(result);
        })
       .WithOpenApi();

    }
}