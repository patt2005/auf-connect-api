using System.ComponentModel.DataAnnotations;

namespace AufConnectApi.Data;

public class Event
{
    [Key]
    public Guid Id { get; set; } = Guid.Empty;
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public string? ImageUrl { get; set; }
    
    public string? VideoUrl { get; set; }
    
    public string Date { get; set; } = string.Empty;
    
    public string City { get; set; } = string.Empty;
    
    public string EventType { get; set; } = string.Empty;
    
    public string Theme { get; set; } = string.Empty;
    
    public string Hashtags { get; set; } = string.Empty;
    
    public List<EventSection> Sections { get; set; } = new List<EventSection>();
}

public class EventSection
{
    [Key]
    public Guid Id { get; set; } = Guid.Empty;
    
    public Guid EventId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LinkUrl { get; set; } = string.Empty;
    public string LinkText { get; set; } = string.Empty;
    
    public Event Event { get; set; } = null!;
}

public class PreviewEvent
{
    [Key]
    public Guid Id { get; set; } = Guid.Empty;
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Date { get; set; } = string.Empty;
    
    [Required]
    public string City { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
}