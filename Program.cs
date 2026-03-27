using System.Text;
using DotnetBoilerplate.Common;
using DotnetBoilerplate.Data;
using DotnetBoilerplate.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.FileProviders;
using Scalar.AspNetCore;

using DotnetBoilerplate.Features.Customers;
using DotnetBoilerplate.Features.InvoiceItems;
using DotnetBoilerplate.Features.CustomerInvoices;
using DotnetBoilerplate.Features.Diary;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// JWT Authentication setup
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key missing"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // For dev purposes
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IInvoiceItemsService, InvoiceItemsService>();
builder.Services.AddScoped<ICustomerInvoicesService, CustomerInvoicesService>();
builder.Services.AddScoped<IDiaryCustomerService, DiaryService>();
builder.Services.AddHttpClient<WhatsAppService>();
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();

var corSettings = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                corSettings ?? Array.Empty<string>()
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
app.MapOpenApi();
app.MapScalarApiReference();
// }

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Auto-map Feature Endpoints
app.MapEndpointDefinitions();
app.MapCustomerEndpoints();
app.MapInvoiceItemsEndpoints();
app.MapCustomerInvoicesEndpoints();
app.MapDiaryEndpoints();

// var staticFilesSection = builder.Configuration.GetSection("StaticFiles");

// var assetsConfig = staticFilesSection.GetSection("Assets");
// var imagesConfig = staticFilesSection.GetSection("Images");

// app.UseStaticFiles(new StaticFileOptions
// {
//     FileProvider = new PhysicalFileProvider(assetsConfig["PhysicalPath"]!),
//     RequestPath = assetsConfig["RequestPath"]
// });

// app.UseStaticFiles(new StaticFileOptions
// {
//     FileProvider = new PhysicalFileProvider(imagesConfig["PhysicalPath"]!),
//     RequestPath = imagesConfig["RequestPath"]
// });

// app.UseStaticFiles(new StaticFileOptions
// {
//    FileProvider = new PhysicalFileProvider(
//        Path.Combine(builder.Environment.ContentRootPath, "Assets")),
//    RequestPath = "/Assets"
// });

app.Run();