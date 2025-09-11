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
                FavoriteProjectIds = JsonSerializer.Deserialize<List<Guid>>(user.FavoriteProjectIds) ?? new List<Guid>(),
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

            var favoriteIds = JsonSerializer.Deserialize<List<Guid>>(user.FavoriteProjectIds) ?? new List<Guid>();
            if (favoriteIds.Count == 0)
            {
                return Ok(new List<Project>());
            }

            var projects = await _context.Projects
                .Where(p => favoriteIds.Contains(p.Id))
                .ToListAsync();

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
                FavoriteProjectIds = JsonSerializer.Deserialize<List<Guid>>(user.FavoriteProjectIds) ?? new List<Guid>(),
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
            FavoriteProjectIds = JsonSerializer.Deserialize<List<Guid>>(user.FavoriteProjectIds) ?? new List<Guid>(),
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

            if (request.ProjectId == Guid.Empty)
            {
                return BadRequest("Project ID is required");
            }

            // Check if project exists
            var projectExists = await _context.Projects.AnyAsync(p => p.Id == request.ProjectId);
            if (!projectExists)
            {
                return NotFound("Project not found");
            }

            // Decode JSON string to List<Guid>
            var favoriteIds = JsonSerializer.Deserialize<List<Guid>>(user.FavoriteProjectIds) ?? new List<Guid>();

            // Check if the project is already in favorites
            if (favoriteIds.Contains(request.ProjectId))
            {
                return BadRequest("Project is already in favorites");
            }

            // Add the project ID to favorites
            favoriteIds.Add(request.ProjectId);
            
            // Encode back to JSON string
            user.FavoriteProjectIds = JsonSerializer.Serialize(favoriteIds);
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

            if (request.ProjectId == Guid.Empty)
            {
                return BadRequest("Project ID is required");
            }

            var favoriteIds = JsonSerializer.Deserialize<List<Guid>>(user.FavoriteProjectIds) ?? new List<Guid>();

            bool removed = favoriteIds.Remove(request.ProjectId);
            
            if (!removed)
            {
                return NotFound("Project not found in favorites");
            }

            user.FavoriteProjectIds = JsonSerializer.Serialize(favoriteIds);
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
    public async Task<ActionResult<List<Guid>>> GetFavorites(Guid userId)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user == null)
            {
                return NotFound("User not found");
            }

            var favoriteIds = JsonSerializer.Deserialize<List<Guid>>(user.FavoriteProjectIds) ?? new List<Guid>();
            
            return Ok(favoriteIds);
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

    [HttpPut("reset-password")]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            // Validate that either userId or email is provided
            if (request.UserId == Guid.Empty && string.IsNullOrEmpty(request.Email))
            {
                return BadRequest("Either User ID or Email is required");
            }

            if (string.IsNullOrEmpty(request.OldPassword))
            {
                return BadRequest("Old password is required");
            }

            if (string.IsNullOrEmpty(request.NewPassword))
            {
                return BadRequest("New password is required");
            }

            if (request.NewPassword.Length < 6)
            {
                return BadRequest("New password must be at least 6 characters long");
            }

            // Find the user by either ID or email
            User? user = null;
            
            if (request.UserId != Guid.Empty)
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
            }
            else if (!string.IsNullOrEmpty(request.Email))
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            }
            
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Verify old password
            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
            {
                return BadRequest("Current password is incorrect");
            }

            // Check if new password is different from old password
            if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
            {
                return BadRequest("New password must be different from the current password");
            }

            // Hash the new password and update
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password reset successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error resetting password: {ex.Message}");
        }
    }
}

public class AddToFavoritesRequest
{
    public Guid ProjectId { get; set; }
}

public class RemoveFromFavoritesRequest
{
    public Guid ProjectId { get; set; }
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
    public List<Guid> FavoriteProjectIds { get; set; } = new List<Guid>();
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

public class ResetPasswordRequest
{
    public Guid UserId { get; set; }
    public string? Email { get; set; }
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
