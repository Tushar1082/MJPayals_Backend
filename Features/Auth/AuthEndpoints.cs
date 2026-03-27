using BCrypt.Net;
using DotnetBoilerplate.Common;
using DotnetBoilerplate.Data;
using DotnetBoilerplate.Models.Entities;
using DotnetBoilerplate.Models.Enums;
using DotnetBoilerplate.Services;
using Microsoft.EntityFrameworkCore;

namespace DotnetBoilerplate.Features.Auth;

public class AuthEndpoints : IEndpointDefinition
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/login", async (LoginRequest request, ApplicationDbContext db, IAuthService authService) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Results.Unauthorized();

            var token = authService.GenerateToken(user);
            return Results.Ok(new { Token = token });
        });

        group.MapPost("/register", async (RegisterRequest request, ApplicationDbContext db) =>
        {
            if (await db.Users.AnyAsync(u => u.Username == request.Username))
                return Results.BadRequest("Username already exists");

            var user = new User
            {
                Username = request.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();
            return Results.Created($"/users/{user.Id}", new { user.Id, user.Username, user.Role });
        });
    }
}

public record LoginRequest(string Username, string Password);
public record RegisterRequest(string Username, string Password, UserRole Role);
