using System.ComponentModel.DataAnnotations;

namespace AufConnectApi.Data;

public class Project
{
    [Key]
    public Guid Id { get; set; } = Guid.Empty;
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    public string? ImageUrl { get; set; }
    
    [Required]
    public string Objectives { get; set; } = string.Empty;
    
    [Required]
    public string TargetAudience { get; set; } = string.Empty;
    
    public string? OverallBudget { get; set; }
    
    [Required]
    public string CountryOfIntervention { get; set; } = string.Empty;
    
    [Required]
    public List<string> RoleOfAufInAction { get; set; } = new();
    
    [Required]
    public string Period { get; set; } = string.Empty;
    
    [Required]
    public string ProjectsFor2024_2025 { get; set; } = string.Empty;
    
    [Required]
    public string ProjectsFor2023_2024 { get; set; } = string.Empty;
    
    [Required]
    public string ProjectsFor2021_2022 { get; set; } = string.Empty;
    
    public string? Device { get; set; }
    
    [Required]
    public List<string> OperationalPartners { get; set; } = new();
}

public class PreviewProject
{
    public Guid Id { get; set; } = Guid.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}