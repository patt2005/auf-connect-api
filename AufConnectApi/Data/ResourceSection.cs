using System.ComponentModel.DataAnnotations;

namespace AufConnectApi.Data;

public class ResourceSection
{
    [Key]
    public Guid Id { get; set; } = Guid.Empty;
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    public string? ImageUrl { get; set; }
    
    [Required]
    public string Url { get; set; } = string.Empty;
    
    public Guid ResourceId { get; set; }
    public Resource? Resource { get; set; } = null!;
}