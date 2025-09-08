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
            var query = _context.Resources
                .Include(r => r.Sections)
                .Where(r => r.Type == type);

            var totalSectionsCount = await query
                .SelectMany(r => r.Sections)
                .CountAsync();
            
            var resourceSections = await query
                .SelectMany(r => r.Sections)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new PreviewResource
                {
                    Id = s.Id,
                    Title = s.Title,
                    Description = s.Description,
                    Link = s.Url,
                    ImageUrl = s.ImageUrl
                })
                .ToListAsync();

            var result = new PagedResult<PreviewResource>
            {
                Data = resourceSections,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalSectionsCount
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error fetching resources: {ex.Message}");
        }
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