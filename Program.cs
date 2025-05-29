using OnlineJudgeAPI.Controllers;
using Microsoft.EntityFrameworkCore;
using OnlineJudgeAPI.Services;
//using OnlineJudge.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.Text.Json.Serialization;
using OnlineJudgeAPI.Hubs;
using OnlineJudgeAPI.SignalR;


var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5024); // HTTP cho Docker
    // Nếu bạn cần HTTPS thì phải mount cert vào container (khó), nên tạm thời chạy HTTP là ổn
});
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
// Add DbContext with MySQL connection
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseMySql(
//        builder.Configuration.GetConnectionString("DefaultConnection"),
//        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
//    )
//);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
);
//builder.Services.AddScoped<CodeExecutor>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
// Add services to the container
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ICacheService, MemoryCacheService>();
// Add Swagger/OpenAPI support
builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<CodeExecutor>();
builder.Services.AddSingleton<SubmissionQueue>();
builder.Services.AddHostedService<SubmissionProcessingService>();
// builder.WebHost.ConfigureKestrel(options =>
// {
//     options.ListenAnyIP(80); // Bắt buộc Render dùng port này
// });

// builder.Services.AddSingleton<ExamService>();

var key = Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]);
// Program.cs or Startup.cs (minimal API version)
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//        var key = Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]);
//        options.TokenValidationParameters = new TokenValidationParameters
//        {
//            ValidateIssuer = true,
//            ValidIssuer = builder.Configuration["JwtSettings:Issuer"], // ✅ Đúng
//            ValidateAudience = true,
//            ValidAudience = builder.Configuration["JwtSettings:Audience"], // ✅ Đúng
//            ValidateIssuerSigningKey = true,
//            IssuerSigningKey = new SymmetricSecurityKey(key),
//            ValidateLifetime = true,
//        };

//    });

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"])
            ),

            // 👇 Quan trọng để `[Authorize(Roles = "Admin")]` hoạt động
            RoleClaimType = ClaimTypes.Role
        };
    });
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
//builder.Services.AddAuthorization();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
});
// Add CORS policy to allow all origins
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowAllOrigins", builder =>
//    {
//        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
//    });
//});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", builder =>
    {
        builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("Authorization"); // Đảm bảo token được gửi đúng
    });
});

var app = builder.Build();

//using (var scope = app.Services.CreateScope())
//{
//    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//    dbContext.AddResultColumn();
//}
// Enable Swagger UI
app.UseSwagger();
//app.UseSwaggerUI();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Online Judge API v1");
    c.RoutePrefix = "swagger"; // Truy cập: http://localhost:5024/swagger
});
// Enable static files
app.UseStaticFiles();

app.Use(async (context, next) =>
{
    var user = context.User;
    var role = user.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
    Console.WriteLine($"[DEBUG] Role: {role}");
    await next();
});
// Enable CORS
app.UseCors("AllowAllOrigins");

// Use HTTPS redirection, authorization middleware, and controllers
app.UseHttpsRedirection();
app.MapControllers();
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<TestCaseResultHub>("/testCaseHub");
app.MapHub<TestCaseResultHub>("/testcaseresulthub");
app.MapHub<LeaderboardHub>("/leaderboardHub");
//app.MapHub<ContestHub>("/contestHub");
//app.Lifetime.ApplicationStarted.Register(async () =>
//{
//    var executor = app.Services.GetRequiredService<CodeExecutor>();
//    await executor.RunCodeAsync( "int main() { return 0; }", "");
//});
//app.Lifetime.ApplicationStarted.Register(async () =>
//{
//    var executor = app.Services.GetRequiredService<CodeExecutor>();
//    await executor.RunCodeAsync("c++", "int main() { return 0; }", "");
//});
//app.Lifetime.ApplicationStarted.Register(async () =>
//{
//    var executor = app.Services.GetRequiredService<CodeExecutor>();
//    await executor.RunCodeAsync("c++", "int main() { return 0; }", "");
//});

app.Run();
