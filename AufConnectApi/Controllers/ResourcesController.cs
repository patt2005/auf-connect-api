using AufConnectApi.Data;
using AufConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AufConnectApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResourcesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly WebScrapingService _webScrapingService;

    public ResourcesController(AppDbContext context, WebScrapingService webScrapingService)
    {
        _context = context;
        _webScrapingService = webScrapingService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PreviewResource>>> GetResources([FromQuery] ResourceType type, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        try
        {
            var url = GetResourceUrl(type);
            if (string.IsNullOrEmpty(url))
            {
                return BadRequest($"Invalid resource type: {type}");
            }

            var allResources = await _webScrapingService.ScrapeResourcePreviewsAsync(url);
            
            var totalCount = allResources.Count;
            var resources = allResources
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = new PagedResult<PreviewResource>
            {
                Data = resources,
                PageNumber = pageNumber,
                PageSize = resources.Count, // Use actual count of returned resources
                TotalCount = totalCount
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error fetching resources: {ex.Message}");
        }
    }

    private string GetResourceUrl(ResourceType type)
    {
        return type switch
        {
            ResourceType.Formation => "https://www.auf.org/ressources-et-services/formation/",
            ResourceType.Resources => "https://www.auf.org/ressources-et-services/ressource/",
            ResourceType.Expertise => "https://www.auf.org/ressources-et-services/expertise/",
            ResourceType.Innovation => "https://www.auf.org/ressources-et-services/innovation/",
            ResourceType.Prospective => "https://www.auf.org/ressources-et-services/prospective/",
            ResourceType.Allocation => "https://www.auf.org/ressources-et-services/bourses/",
            _ => ""
        };
    }

    [HttpPost("add-resource")]
    public async Task<ActionResult<Resource>> CreateResource([FromBody] Resource resourceRequest)
    {
        if (resourceRequest == null)
        {
            return BadRequest("Resource data is required.");
        }

        resourceRequest.Id = Guid.NewGuid();
        
        foreach (var section in resourceRequest.Sections)
        {
            section.Id = Guid.NewGuid();
            section.ResourceId = resourceRequest.Id;
        }
        
        _context.Resources.Add(resourceRequest);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetResources), new { id = resourceRequest.Id }, resourceRequest);
    }
}