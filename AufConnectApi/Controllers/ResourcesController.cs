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

    [HttpGet("details")]
    public async Task<ActionResult<ResourceSection>> GetResourceByIdOrName([FromQuery] string? id, [FromQuery] string? name)
    {
        try
        {
            if (string.IsNullOrEmpty(id) && string.IsNullOrEmpty(name))
            {
                return BadRequest("Either id or name must be provided.");
            }

            ResourceSection? resourceSection = null;

            if (!string.IsNullOrEmpty(id))
            {
                if (Guid.TryParse(id, out var guidId))
                {
                    resourceSection = await _context.ResourceSections
                        .Include(rs => rs.Resource)
                        .FirstOrDefaultAsync(rs => rs.Id == guidId);
                }
                else
                {
                    return BadRequest("Invalid id format. Must be a valid GUID.");
                }
            }
            else if (!string.IsNullOrEmpty(name))
            {
                resourceSection = await _context.ResourceSections
                    .Include(rs => rs.Resource)
                    .FirstOrDefaultAsync(rs => rs.Title == name);
            }

            if (resourceSection == null)
            {
                var searchParam = !string.IsNullOrEmpty(id) ? $"id '{id}'" : $"name '{name}'";
                return NotFound($"Resource with {searchParam} not found.");
            }

            return Ok(resourceSection);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error fetching resource details: {ex.Message}");
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

    [HttpPost("add-resource-section")]
    public async Task<ActionResult<ResourceSection>> AddResourceSection([FromQuery] Guid resourceId, [FromBody] ResourceSection sectionRequest)
    {
        try
        {
            if (sectionRequest == null)
            {
                return BadRequest("Section data is required.");
            }

            // Check if the resource exists
            var resource = await _context.Resources
                .Include(r => r.Sections)
                .FirstOrDefaultAsync(r => r.Id == resourceId);

            if (resource == null)
            {
                return NotFound($"Resource with ID '{resourceId}' not found.");
            }

            // Generate new ID for the section and set the resourceId
            sectionRequest.Id = Guid.NewGuid();
            sectionRequest.ResourceId = resourceId;
            sectionRequest.Resource = null; // Clear navigation property

            // Add the section to the database
            _context.ResourceSections.Add(sectionRequest);
            await _context.SaveChangesAsync();

            // Reload the section with the resource to return complete data
            var createdSection = await _context.ResourceSections
                .Include(s => s.Resource)
                .FirstOrDefaultAsync(s => s.Id == sectionRequest.Id);

            return CreatedAtAction(nameof(GetResourceByIdOrName), new { id = createdSection.Id }, createdSection);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error adding resource section: {ex.Message}");
        }
    }

    [HttpPost("scrape-resuff-resources")]
    public async Task<ActionResult<List<Resource>>> ScrapeAndSaveResuffResources([FromQuery] string url = "https://www.resuff.org/ressources.php")
    {
        try
        {
            var scrapedResources = await _webScrapingService.ScrapeResuffResourcesAsync(url);

            if (scrapedResources == null || !scrapedResources.Any())
            {
                return BadRequest("No resources found to scrape.");
            }

            // Get the current count of resources to determine the starting position for new resources
            var currentResourceCount = await _context.Resources.CountAsync();
            var addedCount = 0;

            // Add scraped resources to database at the end of the existing list
            foreach (var resource in scrapedResources)
            {
                // Check if resource already exists by link to avoid duplicates
                var existingResource = await _context.Resources
                    .AnyAsync(r => r.Link == resource.Link);

                if (!existingResource)
                {
                    _context.Resources.Add(resource);
                    addedCount++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new {
                Message = $"Successfully scraped and saved {addedCount} new resources (out of {scrapedResources.Count} found)",
                TotalResourcesBeforeScrap = currentResourceCount,
                TotalResourcesAfterScrap = currentResourceCount + addedCount,
                NewResourcesAdded = addedCount,
                Resources = scrapedResources
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error scraping RESUFF resources: {ex.Message}");
        }
    }

    [HttpDelete("{resourceId}/sections/{sectionId}")]
    public async Task<ActionResult> DeleteResourceSection(Guid resourceId, Guid sectionId)
    {
        try
        {
            // First, check if the resource exists
            var resource = await _context.Resources
                .Include(r => r.Sections)
                .FirstOrDefaultAsync(r => r.Id == resourceId);

            if (resource == null)
            {
                return NotFound($"Resource with ID '{resourceId}' not found.");
            }

            // Find the section within that resource
            var section = resource.Sections.FirstOrDefault(s => s.Id == sectionId);

            if (section == null)
            {
                return NotFound($"Section with ID '{sectionId}' not found in resource '{resourceId}'.");
            }

            // Remove the section
            _context.ResourceSections.Remove(section);
            await _context.SaveChangesAsync();

            return Ok(new {
                Message = "Resource section deleted successfully.",
                DeletedSectionId = sectionId,
                ResourceId = resourceId
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error deleting resource section: {ex.Message}");
        }
    }
}