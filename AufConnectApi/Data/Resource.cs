using System.ComponentModel.DataAnnotations;

namespace AufConnectApi.Data;

public enum ResourceType
{
    Formation,
    Resources,
    Expertise,
    Innovation,
    Prospective,
    Allocation
}

public class Resource
{
    [Key]
    public Guid Id { get; set; } = Guid.Empty;
    
    [Required]
    public ResourceType Type { get; set; }
    
    public ICollection<ResourceSection> Sections { get; set; } = new List<ResourceSection>();
}

public class PreviewResource
{
    public Guid Id { get; set; } = Guid.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}