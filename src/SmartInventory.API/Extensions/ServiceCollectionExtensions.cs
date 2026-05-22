using System.Text;
using System.Diagnostics;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Console;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartInventory.API.Contracts;
using SmartInventory.API.Data;
using SmartInventory.API.Data.Repositories;
using SmartInventory.API.Filters;
using SmartInventory.API.Identity;
using SmartInventory.API.Infrastructure;
using SmartInventory.API.Services;
using SmartInventory.Application.Interfaces;
using SmartInventory.Application.Services;
using SmartInventory.Application.Validators;
using SmartInventory.Domain.Interfaces;

namespace SmartInventory.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSmartInventoryServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.Configure(options =>
            {
                options.ActivityTrackingOptions =
                    ActivityTrackingOptions.TraceId |
                    ActivityTrackingOptions.SpanId |
                    ActivityTrackingOptions.ParentId;
            });

            if (environment.IsDevelopment())
            {
                builder.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.IncludeScopes = true;
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
                    options.UseUtcTimestamp = true;
                });
            }
            else
            {
                builder.AddJsonConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
                    options.UseUtcTimestamp = true;
                });
            }
        });

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure(3)));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = false;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<IdentityRole<int>>()
            .AddSignInManager()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddMemoryCache();

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IInventoryTransactionRepository, InventoryTransactionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserLookupService, UserLookupService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IInventoryService, InventoryService>();

        services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();
        services.AddFluentValidationAutoValidation();

        services.AddCors(options =>
            options.AddPolicy("AngularApp", policy =>
                policy.WithOrigins("http://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()));

        services.AddJwtAuthentication(configuration);

        services.AddSmartInventoryRateLimiting();

        services.AddControllers(options =>
        {
            options.Filters.Add<ApiResponseFilter>();
        }).ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = CreateInvalidModelStateResponse;
        });

        services.AddEndpointsApiExplorer();
        services.AddSmartInventorySwagger();

        return services;
    }

    private static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
    }

    private static void AddSmartInventorySwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Smart Inventory API",
                Version = "v1",
                Description = """
                    A comprehensive **Smart Inventory Management API** built with Clean Architecture.

                    ### Features
                    - ASP.NET Core Identity authentication + JWT bearer
                    - Refresh token rotation (reuse detection)
                    - Server-side pagination where applicable
                    - Rate limiting for abuse protection
                    - Unified `ApiResponse<T>` envelope (includes `requestId` and `durationMs`)

                    ### Auth
                    Use the **Authorize** button with `Bearer {token}`.
                    """,
                Contact = new OpenApiContact
                {
                    Name = "SmartInventory Backend",
                    Email = "support@smartinventory.local"
                },
                License = new OpenApiLicense
                {
                    Name = "MIT"
                }
            });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT access token"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, xmlFile);
            if (System.IO.File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }

            c.TagActionsBy(api =>
            {
                if (!string.IsNullOrWhiteSpace(api.GroupName))
                {
                    return new[] { api.GroupName };
                }

                if (api.ActionDescriptor.RouteValues.TryGetValue("controller", out var controller) &&
                    !string.IsNullOrWhiteSpace(controller))
                {
                    return new[] { controller };
                }

                return new[] { "Endpoints" };
            });

            c.DocInclusionPredicate((_, api) => !string.IsNullOrWhiteSpace(api.RelativePath));

            c.SupportNonNullableReferenceTypes();
            c.UseInlineDefinitionsForEnums();
            c.CustomOperationIds(apiDescription =>
                apiDescription.ActionDescriptor is ControllerActionDescriptor cad ? cad.MethodInfo.Name : null);
        });
    }

    private static IActionResult CreateInvalidModelStateResponse(ActionContext context)
    {
        var errors = context.ModelState
            .Where(kvp => kvp.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        var requestId = context.HttpContext.TraceIdentifier;
        var durationMs = 0L;

        if (context.HttpContext.Items.TryGetValue(HttpContextItemKeys.Stopwatch, out var swObj) &&
            swObj is System.Diagnostics.Stopwatch sw)
        {
            durationMs = sw.ElapsedMilliseconds;
        }

        var payload = ApiResponse<object>.Fail(
            new ApiError { Message = "Validation failed", Details = errors },
            status: 400,
            durationMs: durationMs,
            requestId: requestId);

        return new ObjectResult(payload) { StatusCode = 400 };
    }
}
