using System.ComponentModel.DataAnnotations;

namespace AufConnectApi.Data;

public class Service
{
    [Key]
    public Guid Id { get; set; } = Guid.Empty;
    
    [Required]
    public string ImageUrl { get; set; } = string.Empty;
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string DateString { get; set; } = string.Empty;

    [Required]
    public bool IsClosed { get; set; } = false;
}