using AufConnectApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AufConnectApi.Services;

namespace AufConnectApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly WebScrapingService _webScrapingService;

    public UserController(AppDbContext context, IConfiguration configuration, WebScrapingService webScrapingService)
    {
        _context = context;
        _configuration = configuration;
        _webScrapingService = webScrapingService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return BadRequest(new { message = "Email already exists" });
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            FcmToken = request.FcmToken,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            NotificationPreferences = new NotificationPreferences
            {
                Id = Guid.NewGuid(),
                CallNotifications = true,
                SavedProjectsNotifications = true,
                ResourceListNotifications = true,
                PasswordChangeNotifications = true,
                EventsNotifications = true,
                NewsletterNotifications = true
            }
        };

        user.NotificationPreferences.UserId = user.Id;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            User = new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                FcmToken = user.FcmToken,
                Country = user.Country,
                City = user.City,
                AddressLine1 = user.AddressLine1,
                AddressLine2 = user.AddressLine2,
                StateOrRegion = user.StateOrRegion,
                PostalCode = user.PostalCode,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                FavoriteProjectLinks = JsonSerializer.Deserialize<List<string>>(user.FavoriteProjectLinks) ?? new List<string>(),
                NotificationPreferences = new NotificationPreferencesDto
                {
                    CallNotifications = user.NotificationPreferences.CallNotifications,
                    SavedProjectsNotifications = user.NotificationPreferences.SavedProjectsNotifications,
                    ResourceListNotifications = user.NotificationPreferences.ResourceListNotifications,
                    PasswordChangeNotifications = user.NotificationPreferences.PasswordChangeNotifications,
                    EventsNotifications = user.NotificationPreferences.EventsNotifications,
                    NewsletterNotifications = user.NotificationPreferences.NewsletterNotifications
                }
            }
        });
    }

    [HttpGet("{userId}/favorite-projects")]
    public async Task<ActionResult<List<Project>>> GetFavoriteProjects(Guid userId)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound("User not found");
            }

            var favoriteLinks = JsonSerializer.Deserialize<List<string>>(user.FavoriteProjectLinks) ?? new List<string>();
            if (favoriteLinks.Count == 0)
            {
                return Ok(new List<Project>());
            }

            var tasks = favoriteLinks.Select(link => _webScrapingService.ScrapeProjectDetailsAsync(link));
            var results = await Task.WhenAll(tasks);

            var projects = results.Where(p => p != null).Cast<Project>().ToList();
            return Ok(projects);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error fetching favorite projects: {ex.Message}");
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.NotificationPreferences)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Username or password is incorrect" });
        }

        user.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(request.FcmToken))
        {
            user.FcmToken = request.FcmToken;
        }

        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            User = new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                FcmToken = user.FcmToken,
                Country = user.Country,
                City = user.City,
                AddressLine1 = user.AddressLine1,
                AddressLine2 = user.AddressLine2,
                StateOrRegion = user.StateOrRegion,
                PostalCode = user.PostalCode,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                FavoriteProjectLinks = JsonSerializer.Deserialize<List<string>>(user.FavoriteProjectLinks) ?? new List<string>(),
                NotificationPreferences = new NotificationPreferencesDto
                {
                    CallNotifications = user.NotificationPreferences.CallNotifications,
                    SavedProjectsNotifications = user.NotificationPreferences.SavedProjectsNotifications,
                    ResourceListNotifications = user.NotificationPreferences.ResourceListNotifications,
                    PasswordChangeNotifications = user.NotificationPreferences.PasswordChangeNotifications,
                    EventsNotifications = user.NotificationPreferences.EventsNotifications,
                    NewsletterNotifications = user.NotificationPreferences.NewsletterNotifications
                }
            }
        });
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"] ?? "default-secret-key-for-development"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("uid", user.Id.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"] ?? "AufConnectApi",
            audience: _configuration["JwtSettings:Audience"] ?? "AufConnectApi",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [HttpPut("edit/{userId}")]
    public async Task<ActionResult<UserResponse>> EditUser(Guid userId, [FromBody] EditUserRequest request)
    {
        var user = await _context.Users
            .Include(u => u.NotificationPreferences)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound("User not found");
        }

        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;
        user.FcmToken = request.FcmToken;
        user.Country = request.Country;
        user.City = request.City;
        user.AddressLine1 = request.AddressLine1;
        user.AddressLine2 = request.AddressLine2;
        user.StateOrRegion = request.StateOrRegion;
        user.PostalCode = request.PostalCode;
        user.UpdatedAt = DateTime.UtcNow;

        user.NotificationPreferences.CallNotifications = request.NotificationPreferences.CallNotifications;
        user.NotificationPreferences.SavedProjectsNotifications = request.NotificationPreferences.SavedProjectsNotifications;
        user.NotificationPreferences.ResourceListNotifications = request.NotificationPreferences.ResourceListNotifications;
        user.NotificationPreferences.PasswordChangeNotifications = request.NotificationPreferences.PasswordChangeNotifications;
        user.NotificationPreferences.EventsNotifications = request.NotificationPreferences.EventsNotifications;
        user.NotificationPreferences.NewsletterNotifications = request.NotificationPreferences.NewsletterNotifications;

        await _context.SaveChangesAsync();

        return Ok(new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            FcmToken = user.FcmToken,
            Country = user.Country,
            City = user.City,
            AddressLine1 = user.AddressLine1,
            AddressLine2 = user.AddressLine2,
            StateOrRegion = user.StateOrRegion,
            PostalCode = user.PostalCode,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            FavoriteProjectLinks = JsonSerializer.Deserialize<List<string>>(user.FavoriteProjectLinks) ?? new List<string>(),
            NotificationPreferences = new NotificationPreferencesDto
            {
                CallNotifications = user.NotificationPreferences.CallNotifications,
                SavedProjectsNotifications = user.NotificationPreferences.SavedProjectsNotifications,
                ResourceListNotifications = user.NotificationPreferences.ResourceListNotifications,
                PasswordChangeNotifications = user.NotificationPreferences.PasswordChangeNotifications,
                EventsNotifications = user.NotificationPreferences.EventsNotifications,
                NewsletterNotifications = user.NotificationPreferences.NewsletterNotifications
            }
        });
    }

    [HttpPost("{userId}/favorites")]
    public async Task<ActionResult> AddToFavorites(Guid userId, [FromBody] AddToFavoritesRequest request)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user == null)
            {
                return NotFound("User not found");
            }

            if (string.IsNullOrEmpty(request.ProjectLink))
            {
                return BadRequest("Project link is required");
            }

            // Decode JSON string to List<string>
            var favoriteLinks = JsonSerializer.Deserialize<List<string>>(user.FavoriteProjectLinks) ?? new List<string>();

            // Check if the link is already in favorites
            if (favoriteLinks.Contains(request.ProjectLink))
            {
                return BadRequest("Project is already in favorites");
            }

            // Add the project link to favorites
            favoriteLinks.Add(request.ProjectLink);
            
            // Encode back to JSON string
            user.FavoriteProjectLinks = JsonSerializer.Serialize(favoriteLinks);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Project added to favorites successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error adding project to favorites: {ex.Message}");
        }
    }

    [HttpDelete("{userId}/favorites")]
    public async Task<ActionResult> RemoveFromFavorites(Guid userId, [FromBody] RemoveFromFavoritesRequest request)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user == null)
            {
                return NotFound("User not found");
            }

            if (string.IsNullOrEmpty(request.ProjectLink))
            {
                return BadRequest("Project link is required");
            }

            // Decode JSON string to List<string>
            var favoriteLinks = JsonSerializer.Deserialize<List<string>>(user.FavoriteProjectLinks) ?? new List<string>();

            // Remove the project link from favorites
            bool removed = favoriteLinks.Remove(request.ProjectLink);
            
            if (!removed)
            {
                return NotFound("Project not found in favorites");
            }

            // Encode back to JSON string
            user.FavoriteProjectLinks = JsonSerializer.Serialize(favoriteLinks);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Project removed from favorites successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error removing project from favorites: {ex.Message}");
        }
    }

    [HttpGet("{userId}/favorites")]
    public async Task<ActionResult<List<string>>> GetFavorites(Guid userId)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Decode JSON string to List<string> for the response
            var favoriteLinks = JsonSerializer.Deserialize<List<string>>(user.FavoriteProjectLinks) ?? new List<string>();
            
            return Ok(favoriteLinks);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error fetching user favorites: {ex.Message}");
        }
    }

    [HttpPut("set-fcm-token")]
    public async Task<ActionResult> SetFcmToken([FromQuery] string id, [FromBody] SetFcmTokenRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("User id is required.");
            }

            if (!Guid.TryParse(id, out var userId))
            {
                return BadRequest("Invalid id format. Must be a valid GUID.");
            }

            if (string.IsNullOrEmpty(request.FcmToken))
            {
                return BadRequest("FCM token is required.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user == null)
            {
                return NotFound("User not found");
            }

            user.FcmToken = request.FcmToken;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "FCM token updated successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error setting FCM token: {ex.Message}");
        }
    }
}

public class AddToFavoritesRequest
{
    public string ProjectLink { get; set; } = string.Empty;
}

public class RemoveFromFavoritesRequest
{
    public string ProjectLink { get; set; } = string.Empty;
}

public class SetFcmTokenRequest
{
    public string FcmToken { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? FcmToken { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FcmToken { get; set; }
}

public class EditUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? FcmToken { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? StateOrRegion { get; set; }
    public string? PostalCode { get; set; }
    public NotificationPreferencesDto NotificationPreferences { get; set; } = null!;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public UserResponse User { get; set; } = null!;
}

public class UserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? FcmToken { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? StateOrRegion { get; set; }
    public string? PostalCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<string> FavoriteProjectLinks { get; set; } = new List<string>();
    public NotificationPreferencesDto NotificationPreferences { get; set; } = null!;
}

public class NotificationPreferencesDto
{
    public bool CallNotifications { get; set; }
    public bool SavedProjectsNotifications { get; set; }
    public bool ResourceListNotifications { get; set; }
    public bool PasswordChangeNotifications { get; set; }
    public bool EventsNotifications { get; set; }
    public bool NewsletterNotifications { get; set; }
}
