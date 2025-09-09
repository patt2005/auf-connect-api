using AufConnectApi.Data;
using AufConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            var query = _context.Projects.AsQueryable();
            
            if (regions != null && regions.Count > 0)
            {
                var regionMappings = new Dictionary<string, string>
                {
                    ["ocean-indien"] = "AUF - Afrique Australe et Océan Indien",
                    ["afrique-centrale-grands-lacs"] = "AUF - Afrique Centrale et Grands Lacs",
                    ["afrique-ouest"] = "AUF - Afrique de l'Ouest",
                    ["maghreb"] = "AUF - Afrique du Nord",
                    ["ameriques"] = "AUF - Amériques",
                    ["asie-pacifique"] = "AUF - Asie-Pacifique",
                    ["caraibe"] = "AUF - Caraïbe",
                    ["europe-centrale-orientale"] = "AUF - Europe Centrale et Orientale",
                    ["europe-ouest"] = "AUF - Europe Occidentale",
                    ["moyen-orient"] = "AUF - Moyen-Orient",
                    ["international"] = "International"
                };
                
                var mappedRegions = regions.Where(r => regionMappings.ContainsKey(r.ToLower()))
                                          .Select(r => regionMappings[r.ToLower()])
                                          .ToList();
                
                if (mappedRegions.Any())
                {
                    query = query.Where(p => mappedRegions.Any(r => p.CountryOfIntervention.Contains(r)));
                }
            }
            
            if (axes != null && axes.Count > 0)
            {
                var axeMappings = new Dictionary<string, string>
                {
                    ["accreditation"] = "Accréditation",
                    ["clef"] = "CLEF",
                    ["developpement"] = "Développement",
                    ["emploi"] = "Emploi",
                    ["employabilite-et-insertion-professionnelle"] = "Employabilité et insertion professionnelle",
                    ["entrepreneuriat"] = "Entrepreneuriat",
                    ["formation"] = "Formation",
                    ["francophonie"] = "Francophonie",
                    ["gouvernance"] = "Gouvernance",
                    ["intelligence-artificielle"] = "Intelligence artificielle",
                    ["qualite"] = "Qualité",
                    ["recherche"] = "Recherche"
                };
                
                var mappedAxes = axes.Where(a => axeMappings.ContainsKey(a.ToLower()))
                                    .Select(a => axeMappings[a.ToLower()])
                                    .ToList();
                
                if (mappedAxes.Any())
                {
                    query = query.Where(p => p.RoleOfAufInAction.Any(role => mappedAxes.Any(axe => role.Contains(axe))));
                }
            }
            
            if (statuses != null && statuses.Count > 0)
            {
                query = query.Where(p => statuses.Any(status => 
                    p.ProjectsFor2024_2025.ToLower().Contains(status.ToLower()) ||
                    p.ProjectsFor2023_2024.ToLower().Contains(status.ToLower()) ||
                    p.ProjectsFor2021_2022.ToLower().Contains(status.ToLower()) ||
                    p.Period.ToLower().Contains(status.ToLower())));
            }
            
            var totalCount = await query.CountAsync();
            
            var projects = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PreviewProject
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Objectives,
                    Region = p.CountryOfIntervention,
                    Link = string.Empty,
                    ImageUrl = p.ImageUrl
                })
                .ToListAsync();

            var result = new PagedResult<PreviewProject>
            {
                Data = projects,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error fetching projects: {ex.Message}");
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<PreviewProject>>> SearchProjects(
        [FromQuery] string query, 
        [FromQuery] int pageNumber = 1, 
        [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Search query is required.");
            }

            var searchQuery = _context.Projects.AsQueryable();

            // Search across multiple fields
            searchQuery = searchQuery.Where(p => 
                p.Title.Contains(query) ||
                p.Objectives.Contains(query) ||
                p.TargetAudience.Contains(query) ||
                p.CountryOfIntervention.Contains(query) ||
                p.Period.Contains(query) ||
                p.ProjectsFor2024_2025.Contains(query) ||
                p.ProjectsFor2023_2024.Contains(query) ||
                p.ProjectsFor2021_2022.Contains(query));

            var totalCount = await searchQuery.CountAsync();
            
            var projects = await searchQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PreviewProject
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Objectives,
                    Region = p.CountryOfIntervention,
                    Link = string.Empty,
                    ImageUrl = p.ImageUrl
                })
                .ToListAsync();

            var result = new PagedResult<PreviewProject>
            {
                Data = projects,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error searching projects: {ex.Message}");
        }
    }

    [HttpGet("details")]
    public async Task<ActionResult<Project>> GetProjectByIdOrName([FromQuery] string? id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Either id or name must be provided.");
            }

            Project? project = null;

            if (!string.IsNullOrEmpty(id))
            {
                if (Guid.TryParse(id, out var guidId))
                {
                    project = await _context.Projects
                        .FirstOrDefaultAsync(p => p.Id == guidId);
                }
                else
                {
                    return BadRequest("Invalid id format. Must be a valid GUID.");
                }
            }

            if (project == null)
            {
                return NotFound($"Project with id {id} not found.");
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
