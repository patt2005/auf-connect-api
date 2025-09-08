using AufConnectApi.Data;
using AufConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace AufConnectApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly WebScrapingService _webScrapingService;

    public ProjectsController(AppDbContext context, WebScrapingService webScrapingService)
    {
        _context = context;
        _webScrapingService = webScrapingService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PreviewProject>>> GetProjects(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery(Name = "region")] List<string>? regions = null,
        [FromQuery(Name = "axe")] List<string>? axes = null,
        [FromQuery(Name = "statut")] List<string>? statuses = null)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        try
        {
            var baseUrl = pageNumber == 1 
                ? "https://www.auf.org/nos-actions/" 
                : $"https://www.auf.org/nos-actions/page/{pageNumber}/";

            var url = baseUrl;
            var query = new Dictionary<string, string?>();

            if (regions != null && regions.Count > 0)
            {
                for (int i = 0; i < regions.Count; i++)
                {
                    query[$"region[{i}]"] = regions[i];
                }
            }
            if (axes != null && axes.Count > 0)
            {
                for (int i = 0; i < axes.Count; i++)
                {
                    query[$"axe[{i}]"] = axes[i];
                }
            }
            if (statuses != null && statuses.Count > 0)
            {
                for (int i = 0; i < statuses.Count; i++)
                {
                    query[$"statut[{i}]"] = statuses[i];
                }
            }
            if (query.Count > 0)
            {
                url = QueryHelpers.AddQueryString(baseUrl, query);
            }
            
            var projects = await _webScrapingService.ScrapeProjectPreviewsAsync(url);
            
            var totalCount = pageNumber == 1 && projects.Count < pageSize ? projects.Count : pageNumber * pageSize;

            var result = new PagedResult<PreviewProject>
            {
                Data = projects,
                PageNumber = pageNumber,
                PageSize = projects.Count,
                TotalCount = totalCount
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error fetching projects: {ex.Message}");
        }
    }

    [HttpGet("details")]
    public async Task<ActionResult<Project>> GetProjectByLink([FromQuery] string link)
    {
        try
        {
            if (string.IsNullOrEmpty(link))
            {
                return BadRequest("Project link is required.");
            }
            
            var project = await _webScrapingService.ScrapeProjectDetailsAsync(link);

            if (project == null)
            {
                return NotFound($"Project with link '{link}' not found or could not be scraped.");
            }

            return Ok(project);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error fetching project details: {ex.Message}");
        }
    }
    
    [HttpPost("add-project")]
    public async Task<ActionResult<Project>> CreateProject([FromBody] Project projectRequest)
    {
        if (projectRequest == null)
        {
            return BadRequest("Project data is required.");
        }

        projectRequest.Id = Guid.NewGuid();
        
        _context.Projects.Add(projectRequest);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProjects), new { id = projectRequest.Id }, projectRequest);
    }
}
