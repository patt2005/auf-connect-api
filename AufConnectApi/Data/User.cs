using System.ComponentModel.DataAnnotations;

namespace AufConnectApi.Data;

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    [Required]
    public string FullName { get; set; } = string.Empty;
    
    public string? PhoneNumber { get; set; }
    
    public string? FcmToken { get; set; }

    public string? Country { get; set; }
    public string? City { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? StateOrRegion { get; set; }
    public string? PostalCode { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public string FavoriteProjectLinks { get; set; } = "[]";
    
    public NotificationPreferences NotificationPreferences { get; set; } = new NotificationPreferences();
}

public class NotificationPreferences
{
    [Key]
    public Guid Id { get; set; } = Guid.Empty;
    
    public Guid UserId { get; set; }
    
    [Required]
    public bool CallNotifications { get; set; } = true;
    
    [Required]
    public bool SavedProjectsNotifications { get; set; } = true;
    
    [Required]
    public bool ResourceListNotifications { get; set; } = true;
    
    [Required]
    public bool PasswordChangeNotifications { get; set; } = true;
    
    [Required]
    public bool EventsNotifications { get; set; } = true;
    
    [Required]
    public bool NewsletterNotifications { get; set; } = true;
    
    public User User { get; set; } = null!;
}
