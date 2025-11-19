using CryptoTrading.Data;
using CryptoTrading.Infrastructure.Cloudflare;
using CryptoTrading.Interfaces;
using CryptoTrading.Models;
using CryptoTrading.Repositories;
using CryptoTrading.Services;
using CryptoTrading.Services.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Thêm dịch vụ HttpClient
builder.Services.AddHttpClient();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Crypto Trading API",
        Version = "v1",
        Description = "API for Crypto Trading Platform with Authentication, 2FA, and Trading"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Enable XML documentation (temporarily disabled for debugging)
    // var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    // var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    // if (File.Exists(xmlPath))
    // {
    //     c.IncludeXmlComments(xmlPath);
    // }

    // Handle ambiguous actions (multiple endpoints with same HTTP method and route)
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

    // Ignore obsolete warnings
    c.IgnoreObsoleteActions();
    c.IgnoreObsoleteProperties();

    // Add schema filter to handle Dictionary<string, object> and object types
    c.SchemaFilter<CryptoTrading.Infrastructure.DictionaryObjectSchemaFilter>();
});

// Database (MySQL)
var mysqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(mysqlConnectionString, new MySqlServerVersion(new Version(8, 0, 21)),
        mySqlOptions => 
        {
            mySqlOptions.SchemaBehavior(Pomelo.EntityFrameworkCore.MySql.Infrastructure.MySqlSchemaBehavior.Ignore);
            mySqlOptions.CommandTimeout(30); // 30 second timeout for database queries
        }));

// JWT Settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

// JWT Authentication
if (jwtSettings != null)
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
 
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero
        };
    });
}

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://127.0.0.1:5500", "http://localhost:8080", "file://")
              .SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Infrastructure Services
builder.Services.Configure<CloudflareImagesOptions>(builder.Configuration.GetSection("Cloudflare"));
builder.Services.AddSingleton<ICloudflareImagesService, CloudflareImagesService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IEmailSender, EmailService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IDateTimeProvider, DateTimeProvider>();
builder.Services.AddScoped<ICurrentUser, CurrentUserService>();
builder.Services.AddScoped<ILoggingService, LoggingService>();

// Crypto Services
builder.Services.AddHttpClient<ICoinGeckoService, 
    CoinGeckoService>(client =>
{
    client.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
    client.DefaultRequestHeaders.Add("User-Agent", "CryptoTrading/1.0");
    client.Timeout = TimeSpan.FromSeconds(10); // 10 second timeout for external API
});
// Cache service must be usable from singleton hosted services (e.g., typed HttpClient in background services),
// so register it as a singleton to avoid "scoped service from root provider" errors.
builder.Services.AddSingleton<ICryptoCacheService, CryptoCacheService>();
builder.Services.AddMemoryCache();

// SignalR
builder.Services.AddSignalR();

// Business Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWatchlistService, WatchlistService>();
builder.Services.AddScoped<ICryptoDataSyncService, CryptoDataSyncService>();
builder.Services.AddScoped<CryptoTrading.Services.Trading.ITradingService, CryptoTrading.Services.Trading.TradingService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<ILevelService, LevelService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPortfolioService, CryptoTrading.Services.Portfolio.PortfolioService>();

// Bot Trading Services
builder.Services.AddSingleton<CryptoTrading.Interfaces.Bot.IStrategyRegistry, CryptoTrading.Services.Bot.StrategyRegistry>();
builder.Services.AddScoped<CryptoTrading.Interfaces.Bot.IBotApplicationService, CryptoTrading.Services.Bot.BotApplicationService>();
builder.Services.AddScoped<CryptoTrading.Interfaces.Bot.IMarketDataProvider, CryptoTrading.Services.Bot.MarketDataProvider>();
builder.Services.AddScoped<CryptoTrading.Interfaces.Bot.IPortfolioService, CryptoTrading.Services.Bot.PortfolioService>();
builder.Services.AddScoped<CryptoTrading.Interfaces.Bot.IRiskManager, CryptoTrading.Services.Bot.RiskManager>();
builder.Services.AddSingleton<CryptoTrading.Services.Bot.BotSignalRDispatcher>();

// Bot Strategies
builder.Services.AddTransient<CryptoTrading.Services.Bot.Strategies.GridTradingStrategy>();

// VNPay Service
builder.Services.AddScoped<CryptoTrading.Services.Payment.IVnPayService, CryptoTrading.Services.Payment.VnPayService>();

// Subscription Service
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

// Background Services
builder.Services.AddHostedService<CryptoSyncBackgroundService>();
builder.Services.AddHostedService<CryptoTrading.Services.RealtimeBroadcastService>();
builder.Services.AddHostedService<CryptoTrading.Services.OrderMatchingBackgroundService>();
builder.Services.AddHostedService<CryptoTrading.Services.Bot.BotExecutionHostedService>();
builder.Services.AddHostedService<CryptoTrading.Services.Bot.BotMonitorHostedService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Crypto Trading API V1");
        c.RoutePrefix = "swagger";
    });
}

// Use Error Handling Middleware
app.UseMiddleware<CryptoTrading.Middleware.ErrorHandlingMiddleware>();

app.UseCors("AllowFrontend");
// Skip HTTPS redirection in development when running HTTP only
if (app.Environment.IsProduction() || app.Configuration["ASPNETCORE_URLS"]?.Contains("https") == true)
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// SignalR Hubs
app.MapHub<CryptoTrading.Hubs.MarketHub>("/marketHub");
app.MapHub<CryptoTrading.Hubs.BotHub>("/botHub");

// Root endpoint
app.MapGet("/", () => Results.Ok(new { 
    message = "Welcome to Crypto Trading API", 
    version = "v1.0",
    endpoints = new {
        swagger = "/swagger",
        health = "/health",
      
        weatherforecast = "/weatherforecast",
        auth = "/api/auth",
        market = "/api/market",
        trading = "/api/trading",
        portfolio = "/api/portfolio",
        payment = "/api/payment"
    },
    timestamp = DateTime.UtcNow 
}));

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Keep the weather forecast for testing
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
   
    return forecast;
})
.WithName("GetWeatherForecast");


async Task SeedDatabase(IServiceProvider serviceProvider, ILogger logger)
{
    using var scope = serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var strategyRegistry = scope.ServiceProvider.GetRequiredService<CryptoTrading.Interfaces.Bot.IStrategyRegistry>();
    
    // --- 1. Seed Default Roles ---
    var defaultRoles = new List<string> { "Admin", "User", "Manager" };
    var existingRoles = await context.Set<Role>().Select(r => r.Name).ToListAsync();
    var rolesToSeed = defaultRoles.Except(existingRoles, StringComparer.OrdinalIgnoreCase).ToList();

    if (rolesToSeed.Any())
    {
        logger.LogInformation("Seeding default roles: {Roles}", string.Join(", ", rolesToSeed));
        var newRoles = rolesToSeed.Select(name => new Role { Id = 0, Name = name, Description = $"Default system role: {name}" });
        await context.Set<Role>().AddRangeAsync(newRoles);
    }
    
    // --- 2. Seed Default Levels ---
    var defaultLevels = new List<string> { "Beginner" };
    var existingLevels = await context.Set<Level>().Select(l => l.Name).ToListAsync();
    var levelsToSeed = defaultLevels.Except(existingLevels, StringComparer.OrdinalIgnoreCase).ToList();

    if (levelsToSeed.Any())
    {
        logger.LogInformation("Seeding default levels: {Levels}", string.Join(", ", levelsToSeed));
        var maxLevelNumber = await context.Set<Level>().AnyAsync() ? await context.Set<Level>().MaxAsync(l => l.Number) : 0;
        
        var newLevels = levelsToSeed.Select((name, index) => new Level 
        { 
            Id = 0,
            Name = name, 
            Number = maxLevelNumber + index + 1,
            Description = $"Default starting level: {name}",
            MinBalance = 0
        });
        await context.Set<Level>().AddRangeAsync(newLevels);
    }

    // --- 3. Seed Built-in Bot Strategies ---
    var gridStrategy = scope.ServiceProvider.GetRequiredService<CryptoTrading.Services.Bot.Strategies.GridTradingStrategy>();
    var existingStrategy = await context.BotStrategyDefinitions
        .FirstOrDefaultAsync(s => s.StrategyKey == gridStrategy.Key);
    
    if (existingStrategy == null)
    {
        logger.LogInformation("Seeding built-in strategy: {StrategyKey}", gridStrategy.Key);
        var strategyDef = new CryptoTrading.Models.BotStrategyDefinition
        {
            Id = Guid.NewGuid(),
            StrategyKey = gridStrategy.Key,
            Version = gridStrategy.Metadata.Version,
            DisplayName = gridStrategy.Metadata.DisplayName,
            Description = gridStrategy.Metadata.Description,
            ParametersSchema = gridStrategy.Metadata.ParametersSchemaJson,
            MaxConcurrency = gridStrategy.Metadata.MaxConcurrency,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await context.BotStrategyDefinitions.AddAsync(strategyDef);
        
        // Register strategy in registry
        strategyRegistry.RegisterStrategy(gridStrategy);
    }
    else
    {
        // Make sure strategy is registered
        strategyRegistry.RegisterStrategy(gridStrategy);
    }

    if (rolesToSeed.Any() || levelsToSeed.Any() || existingStrategy == null)
    {
        await context.SaveChangesAsync();
        logger.LogInformation("Default data seeding complete");
    }
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Checking database migrations...");
        
        // Check if there are pending migrations without applying them
        var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            logger.LogWarning("There are {Count} pending migrations. Please run 'dotnet ef database update' manually.", pendingMigrations.Count());
            logger.LogWarning("Pending migrations: {Migrations}", string.Join(", ", pendingMigrations));
        }
        else
        {
            logger.LogInformation("Database is up to date with all migrations.");
        }
        
        // Only apply migrations if explicitly enabled via environment variable
        if (Environment.GetEnvironmentVariable("AUTO_APPLY_MIGRATIONS") == "true")
        {
            logger.LogInformation("Auto-applying migrations (AUTO_APPLY_MIGRATIONS=true)...");
            await db.Database.MigrateAsync();
            logger.LogInformation("Migrations applied successfully.");
        }
        
        await SeedDatabase(services, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while checking migrations or seeding the database.");
        // Don't crash the application - continue running even if migration check fails
        logger.LogWarning("Application will continue running. Please check database connection and migrations manually.");
    }
}

app.Run();
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
