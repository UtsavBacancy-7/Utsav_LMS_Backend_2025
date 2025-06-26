using System.Text;
using Asp.Versioning;
using LMS_Backend.LMS.API.Middlewares;
using LMS_Backend.LMS.Infrastructure.Context;
using LMS_Backend.LMS.Infrastructure.Extensions;
using LMS_Backend.LMS.Infrastructure.Mappers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Swashbuckle.AspNetCore.Filters;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.Converters.Add(new StringEnumConverter());
    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
});
  
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Explicitly set the configuration for appsettings.json
builder.Configuration.SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "LMS.API"))
                     .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                     .AddEnvironmentVariables();

// Database connection
builder.Services.AddDbContext<ApplicationDBContext>(option => {
    option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"])),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
    };
});

// Register all repositories dynamically
builder.Services.AddRepositories();

// Register all services dynamically
builder.Services.AddServices();

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.ReportApiVersions = true;
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"));
})
.AddMvc()
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});

// Fetch Data from appsettings.json for CORS
var corsSettings = builder.Configuration.GetSection("Cors");

// CORS Policies
builder.Services.AddCors(options =>
{
    options.AddPolicy("RestrictedCors", policy =>
    {
        // Get arrays from configuration
        var origins = corsSettings.GetSection("AllowedOrigins").Get<string[]>();
        var methods = corsSettings.GetSection("AllowedMethods").Get<string[]>();
        var headers = corsSettings.GetSection("AllowedHeaders").Get<string[]>();
        var exposedHeaders = corsSettings.GetSection("ExposedHeaders").Get<string[]>();

        // Apply CORS policy
        if (origins != null && origins.Length > 0)
        {
            policy.WithOrigins(origins);
        }

        if (methods != null && methods.Length > 0)
        {
            policy.WithMethods(methods);
        }

        if (headers != null && headers.Length > 0)
        {
            policy.WithHeaders(headers);
        }

        if (corsSettings["AllowCredentials"]?.ToLower() == "true")
        {
            policy.AllowCredentials();
        }

        if (exposedHeaders != null && exposedHeaders.Length > 0)
        {
            policy.WithExposedHeaders(exposedHeaders);
        }
    });
});

// Register AutoMapper 
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Swagger Authentication button 
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "LMS API", Version = "v1" });

    // Define the security scheme for JWT Authentication
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter 'Bearer' followed by a space and your JWT token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    // Allow Swagger to remember the token for secured endpoints
    options.OperationFilter<SecurityRequirementsOperationFilter>();
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("RestrictedCors");

app.UseMiddleware<LogMiddleware>();

app.UseMiddleware<RateLimitMiddleware>();

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();