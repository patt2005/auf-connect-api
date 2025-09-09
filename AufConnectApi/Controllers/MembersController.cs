using AufConnectApi.Data;
using AufConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AufConnectApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MembersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly WebScrapingService _webScrapingService;

    public MembersController(AppDbContext context, WebScrapingService webScrapingService)
    {
        _context = context;
        _webScrapingService = webScrapingService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PreviewMember>>> GetMembers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        try
        {
            var totalCount = await _context.Members.CountAsync();
            
            var members = await _context.Members
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new PreviewMember
                {
                    Id = m.Id,
                    Name = m.Name,
                    Region = m.Region ?? string.Empty,
                    Address = m.Address,
                    Link = m.Website
                })
                .ToListAsync();

            var result = new PagedResult<PreviewMember>
            {
                Data = members,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error fetching members: {ex.Message}");
        }
    }

    [HttpGet("details")]
    public async Task<ActionResult<Member>> GetMemberByIdOrName([FromQuery] string? id, [FromQuery] string? name)
    {
        try
        {
            if (string.IsNullOrEmpty(id) && string.IsNullOrEmpty(name))
            {
                return BadRequest("Either id or name must be provided.");
            }

            Member? member = null;

            if (!string.IsNullOrEmpty(id))
            {
                if (Guid.TryParse(id, out var guidId))
                {
                    member = await _context.Members
                        .FirstOrDefaultAsync(m => m.Id == guidId);
                }
                else
                {
                    return BadRequest("Invalid id format. Must be a valid GUID.");
                }
            }
            else if (!string.IsNullOrEmpty(name))
            {
                member = await _context.Members
                    .FirstOrDefaultAsync(m => m.Name == name);
            }

            if (member == null)
            {
                var searchParam = !string.IsNullOrEmpty(id) ? $"id '{id}'" : $"name '{name}'";
                return NotFound($"Member with {searchParam} not found.");
            }

            return Ok(member);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error fetching member details: {ex.Message}");
        }
    }

    [HttpPost("add-member")]
    public async Task<ActionResult<Member>> CreateMember([FromBody] Member memberRequest)
    {
        if (memberRequest == null)
        {
            return BadRequest("Member data is required.");
        }

        memberRequest.Id = Guid.NewGuid();
        
        _context.Members.Add(memberRequest);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMembers), new { id = memberRequest.Id }, memberRequest);
    }

    [HttpPost("scrape-resuff-members")]
    public async Task<ActionResult<List<Member>>> ScrapeAndSaveResuffMembers([FromQuery] string url = "https://www.resuff.org/membres.php")
    {
        try
        {
            var scrapedMembers = await _webScrapingService.ScrapeResuffMembersAsync(url);
            
            if (scrapedMembers == null || !scrapedMembers.Any())
            {
                return BadRequest("No members found to scrape.");
            }

            // Clear existing members if needed (optional)
            // _context.Members.RemoveRange(_context.Members);
            
            // Add scraped members to database
            foreach (var member in scrapedMembers)
            {
                // Check if member already exists by name to avoid duplicates
                var existingMember = await _context.Members
                    .FirstOrDefaultAsync(m => m.Name == member.Name);
                
                if (existingMember == null)
                {
                    _context.Members.Add(member);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { 
                Message = $"Successfully scraped and saved {scrapedMembers.Count} members", 
                Members = scrapedMembers 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error scraping RESUFF members: {ex.Message}");
        }
    }
}