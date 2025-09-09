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
    
    public string? ContactName { get; set; }
    
    public string? ContactTitle { get; set; }
    
    public string? StatutoryType { get; set; }
    
    public string? UniversityType { get; set; }
    
    public string? Address { get; set; }
    
    public string? Phone { get; set; }
    
    public string? Website { get; set; }
    
    public string? Region { get; set; }
    
    public string? FoundedYear { get; set; }
}

public class PreviewMember
{
    public Guid Id { get; set; } = Guid.Empty;
    public string Name { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Link { get; set; }
}
