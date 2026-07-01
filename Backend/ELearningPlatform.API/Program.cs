using ELearningPlatform.Application.Services;
using ELearningPlatform.Core.Interfaces;
using ELearningPlatform.API.Middleware;
using ELearningPlatform.Infrastructure.Cloudinary;
using ELearningPlatform.Infrastructure.DbContext;
using ELearningPlatform.Infrastructure.UnitOfWork;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using ELearningPlatform.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "E-Learning API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme"
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
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));
//test
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://angular.runasp.net",
            "https://angular.runasp.net", 
            "http://localhost:4200", "https://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];
var key = Encoding.UTF8.GetBytes(secretKey!);
// Add JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
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

        // Reject tokens whose session has been terminated (logout / "sign out everywhere").
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var sessionId = context.Principal?.FindFirst("sessionId")?.Value;
                if (!string.IsNullOrEmpty(sessionId))
                {
                    var sessionService = context.HttpContext.RequestServices.GetRequiredService<ISessionService>();
                    if (!await sessionService.IsSessionActiveAsync(sessionId))
                    {
                        context.Fail("Session has been terminated.");
                    }
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("InstructorOnly", policy => policy.RequireRole("Instructor", "Admin"));
    options.AddPolicy("StudentOnly", policy => policy.RequireRole("Student"));
});

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IRefundService, RefundService>();

builder.Services.AddScoped<ICloudinaryVideoService, CloudinaryVideoService>();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddMemoryCache();

var app = builder.Build();

// Exception file logging must be first to catch everything
app.UseExceptionFileLogging();

// Log every request/action to a daily file (path configured in appsettings "FileLogging").
app.UseRequestFileLogging();

app.UseSwagger();
app.UseSwaggerUI();
app.MapGet("/", () => Microsoft.AspNetCore.Http.Results.Redirect("/swagger"));
//if (app.Environment.IsDevelopment())
//{
//}

app.UseHttpsRedirection();

app.UseCors("AllowAngular");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
