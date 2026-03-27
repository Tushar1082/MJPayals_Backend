using DotnetBoilerplate.Common;
using DotnetBoilerplate.Data;
using DotnetBoilerplate.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace DotnetBoilerplate.Features.Todos;

public class TodoEndpoints : IEndpointDefinition
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/todos")
            .WithTags("Todos")
            .RequireAuthorization();

        group.MapGet("/", async (ApplicationDbContext db) =>
            await db.Todos.ToListAsync());

        group.MapGet("/{id}", async (int id, ApplicationDbContext db) =>
            await db.Todos.FindAsync(id) is Todo todo ? Results.Ok(todo) : Results.NotFound());

        group.MapPost("/", async (Todo todo, ApplicationDbContext db) =>
        {
            db.Todos.Add(todo);
            await db.SaveChangesAsync();
            return Results.Created($"/todos/{todo.Id}", todo);
        });

        group.MapPut("/{id}", async (int id, Todo inputTodo, ApplicationDbContext db) =>
        {
            var todo = await db.Todos.FindAsync(id);
            if (todo is null) return Results.NotFound();

            todo.Title = inputTodo.Title;
            todo.IsCompleted = inputTodo.IsCompleted;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapDelete("/{id}", async (int id, ApplicationDbContext db) =>
        {
            if (await db.Todos.FindAsync(id) is Todo todo)
            {
                db.Todos.Remove(todo);
                await db.SaveChangesAsync();
                return Results.NoContent();
            }
            return Results.NotFound();
        });
    }
}
