using AufConnectApi.Data;
using AufConnectApi.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for large file downloads
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // Allow large file responses (100MB max)
    serverOptions.Limits.MaxResponseBufferSize = 100 * 1024 * 1024;
    serverOptions.Limits.MaxRequestBodySize = 100 * 1024 * 1024;
    // Increase timeout for large file downloads
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(2);
});

// Configure Firebase
var credentials = Environment.GetEnvironmentVariable("FirebaseAuthCredentials");
if (!string.IsNullOrEmpty(credentials))
{
    FirebaseApp.Create(new AppOptions()
    {
        Credential = GoogleCredential.FromJson(credentials)
    });
}

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("Default");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'Default' not found in configuration.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null
        );
    }));

builder.Services.AddHttpClient<WebScrapingService>();
builder.Services.AddScoped<WebScrapingService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "AufConnectApi",
            ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "AufConnectApi",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"] ?? "default-secret-key-for-development"))
        };
    });

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Configure MIME types for static files
var provider = new FileExtensionContentTypeProvider();
// Add mapping for APK files
provider.Mappings[".apk"] = "application/vnd.android.package-archive";

// Enable static files with custom MIME types and support for large files
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider,
    // Enable range requests for large file downloads (allows resume)
    HttpsCompression = Microsoft.AspNetCore.Http.Features.HttpsCompressionMode.DoNotCompress,
    OnPrepareResponse = ctx =>
    {
        // For APK files, set appropriate headers
        if (ctx.File.Name.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
        {
            // Enable range requests (important for large files)
            ctx.Context.Response.Headers.Append("Accept-Ranges", "bytes");
            // Set cache control
            ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=3600");
            // Ensure Content-Disposition for download
            ctx.Context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{ctx.File.Name}\"");
        }
    }
});
app.UseDefaultFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Run migrations at project startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while applying database migrations.");
        throw;
    }
}

app.Run();