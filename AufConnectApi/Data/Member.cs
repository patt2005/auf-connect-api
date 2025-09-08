using System.ComponentModel.DataAnnotations;

namespace AufConnectApi.Data;

public class Member
{
    [Key]
    public Guid Id { get; set; } = Guid.Empty;
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public string? Background { get; set; }
    
    public string ContactName { get; set; } = string.Empty;
    
    public string ContactTitle { get; set; } = string.Empty;
    
    public string StatutoryType { get; set; } = string.Empty;
    
    public string UniversityType { get; set; } = string.Empty;
    
    public string Address { get; set; } = string.Empty;
    
    public string Phone { get; set; } = string.Empty;
    
    public string Website { get; set; } = string.Empty;
    
    public string Region { get; set; } = string.Empty;
    
    public string FoundedYear { get; set; } = string.Empty;
}

public class PreviewMember
{
    public Guid Id { get; set; } = Guid.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
}
