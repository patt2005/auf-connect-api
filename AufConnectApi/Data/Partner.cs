using System.ComponentModel.DataAnnotations;

namespace AufConnectApi.Data;

public class Partner
{
    [Key]
    public Guid Id { get; set; } = Guid.Empty;
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? LogoUrl { get; set; }
    
    [Required]
    public string PartnerUrl { get; set; } = string.Empty;
}

public class PreviewPartner
{
    public Guid Id { get; set; } = Guid.Empty;
    public string Name { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string Description { get; set; } = string.Empty;
}