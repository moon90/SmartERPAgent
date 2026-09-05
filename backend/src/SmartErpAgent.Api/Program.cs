using Microsoft.AspNetCore.SignalR;
using Microsoft.OpenApi.Models;
using SmartErpAgent.AgentEngine;
using SmartErpAgent.Api.Hubs;
using SmartErpAgent.Api.Middlewares;
using SmartErpAgent.Infrastructure;
using SmartErpAgent.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// 1. Add presentation services
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddTransient<IHubContext<SmartErpAgent.Application.Hubs.AgentHub>, AgentHubContextBridge>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Smart ERP Agent API",
        Version = "v1",
        Description = "Enterprise B2B SaaS Multi-Agent Automation Platform API with Multi-Tenancy and Semantic Kernel Orchestration."
    });

    // Add global X-Tenant-ID header parameter to Swagger UI
    options.AddSecurityDefinition("TenantHeader", new OpenApiSecurityScheme
    {
        Name = "X-Tenant-ID",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Tenant GUID identifier for multi-tenant data isolation."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "TenantHeader"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 2. Configure CORS for Angular Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// 3. Add Infrastructure (EF Core SQL Server, DbContext, Multi-Tenancy)
builder.Services.AddInfrastructure(builder.Configuration);

// 4. Add Agent Engine (Semantic Kernel, Plugins, Orchestrator)
builder.Services.AddAgentEngine();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart ERP Agent API v1");
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAngularApp");

// Custom Multi-Tenant resolution middleware
app.UseMultiTenant();

app.UseAuthorization();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    TimestampUtc = DateTime.UtcNow,
    Service = "Smart ERP Agent Web API",
    Version = "1.0.0"
}));

// Database Seeder endpoint
app.MapPost("/api/database/seed", async (ApplicationDbContext dbContext, ILogger<Program> logger, CancellationToken cancellationToken) =>
{
    var result = await DatabaseSeeder.SeedAsync(dbContext, logger, cancellationToken);
    return Results.Ok(new
    {
        Message = "Database seeding processed successfully.",
        result.TenantsAdded,
        result.InventoryItemsAdded,
        result.InvoicesAdded
    });
}).WithTags("Database").WithSummary("Seeds demo tenants (ACME_CORP, GLOBAL_LOGISTICS, BIOTECH_MED) and sample inventory/invoices.");

// Seed database demo tenants, inventory SKUs, and invoices on startup in Development or if --seed flag is passed
if (app.Environment.IsDevelopment() || args.Contains("--seed"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await DatabaseSeeder.SeedAsync(dbContext, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database seeding on startup.");
    }
}

app.MapControllers();
app.MapHub<AgentHub>("/hubs/agent");

app.Run();
