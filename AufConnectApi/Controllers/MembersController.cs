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
            var url = pageNumber == 1 
                ? "https://www.auf.org/les_membres/nos-membres/" 
                : $"https://www.auf.org/les_membres/nos-membres/page/{pageNumber}/";
            
            var members = await _webScrapingService.ScrapeMemberPreviewsAsync(url);
            
            var totalCount = pageNumber == 1 && members.Count < pageSize ? members.Count : pageNumber * pageSize;

            var result = new PagedResult<PreviewMember>
            {
                Data = members,
                PageNumber = pageNumber,
                PageSize = members.Count,
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
    public async Task<ActionResult<Member>> GetMemberByLink([FromQuery] string link)
    {
        try
        {
            if (string.IsNullOrEmpty(link))
            {
                return BadRequest("Member link is required.");
            }
            
            var member = await _webScrapingService.ScrapeMemberDetailsAsync(link);

            if (member == null)
            {
                return NotFound($"Member with link '{link}' not found or could not be scraped.");
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
}