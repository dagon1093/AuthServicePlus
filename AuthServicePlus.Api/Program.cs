using AuthServicePlus.Api.Extensions;
using AuthServicePlus.Application.Interfaces;
using AuthServicePlus.Domain.Interfaces;
using AuthServicePlus.Infrastructure.Options;
using AuthServicePlus.Infrastructure.Services;
using AuthServicePlus.Persistence.Context;
using AuthServicePlus.Persistence.Repositories;
using HealthChecks.NpgSql;
using HealthChecks.UI.Client;                        // красивый JSON-ответ для readiness
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks; // MapHealthChecks + HealthCheckOptions
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;                           // AddNpgSql(...) — проверка Postgres
using Serilog;
using System.Security.Claims;
using System.Text;


Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()   
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);



var cs = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(cs))
    throw new InvalidOperationException("Connection string 'DefaultConnection' is missing or empty (Program.cs).");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(cs));

// Program.cs
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection"),
    name: "postgres",
    tags: new[] { "ready" }
    );

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(
        serviceName: "AuthServicePlus.Api",
        serviceVersion: "1.0.0"))
    .WithTracing(tracer => tracer.AddAspNetCoreInstrumentation(o =>
    {
        o.RecordException = true;
        o.Filter = ctx =>
        {
            var p = ctx.Request.Path.Value ?? string.Empty;
            return !(p.StartsWith("/health") || p.StartsWith("/metrics"));
        };
    })
    .AddHttpClientInstrumentation()
    .AddConsoleExporter()
    )
    .WithMetrics(meter => meter
        .AddAspNetCoreInstrumentation()  // метрики по входящим HTTP
        .AddHttpClientInstrumentation()  // метрики по исходящим HttpClient
        .AddPrometheusExporter()        // отдаём в формате Prometheus
    ); 

// Add services to the container.

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenHasher, RefreshTokenHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "One or more validation errors occurred.",
                Instance = context.HttpContext.Request.Path
            };
            return new BadRequestObjectResult(problemDetails);
        };
    });



//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AuthServicePlus API", Version = "v1" });

    // 1) Îïèñàíèå ñõåìû Bearer
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Âñòàâü òîëüêî JWT (áåç 'Bearer '), çàòåì íàæìè Authorize.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // 2) Ãëîáàëüíî òðåáóåì Bearer-ñõåìó (÷åðåç Reference)
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
});



// JWT
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtKey = builder.Configuration["Jwt:Key"];

//Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // true â ïðîäå çà HTTPS
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        RoleClaimType = ClaimTypes.Role,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromSeconds(30) 
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            
            var logger = ctx.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("JWT");
            var auth = ctx.Request.Headers.Authorization.ToString();
            logger.LogInformation("Authorization header: '{Auth}'", auth);
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = ctx =>
        {
            var logger = ctx.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("JWT");
            logger.LogError(ctx.Exception, "JWT fail: {Message}", ctx.Exception.Message);
            return Task.CompletedTask;
        },
        OnChallenge = ctx =>
        {
            var logger = ctx.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("JWT");
            logger.LogWarning("JWT challenge: {Error} {Description}", ctx.Error, ctx.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});


builder.Host.UseSerilog((context, configuration) =>
{
    configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext();
});



builder.Services.AddAuthorization();

var app = builder.Build();
app.MapPrometheusScrapingEndpoint("/metrics");

app.UseSerilogRequestLogging(opts =>
{
    opts.GetLevel = (httpContext, elapsed, ex) =>
    {
        var p = httpContext.Request.Path.Value ?? "";
        if (p.StartsWith("/health") || p.StartsWith("/metrics"))
            return Serilog.Events.LogEventLevel.Debug; // не шумим
        return ex != null
            ? Serilog.Events.LogEventLevel.Error
            : Serilog.Events.LogEventLevel.Information;
    };
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}


app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.MapOpenApi();
//}


app.UseHttpsRedirection();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
