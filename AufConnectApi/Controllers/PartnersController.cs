using AufConnectApi.Data;
using AufConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AufConnectApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PartnersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly WebScrapingService _webScrapingService;

    public PartnersController(AppDbContext context, WebScrapingService webScrapingService)
    {
        _context = context;
        _webScrapingService = webScrapingService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PreviewPartner>>> GetPartners([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        try
        {
            var totalCount = await _context.Partners.CountAsync();
            
            var partners = await _context.Partners
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PreviewPartner
                {
                    Id = p.Id,
                    Name = p.Name,
                    Link = p.PartnerUrl,
                    ImageUrl = p.LogoUrl,
                    Description = string.Empty
                })
                .ToListAsync();

            var result = new PagedResult<PreviewPartner>
            {
                Data = partners,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error fetching partners: {ex.Message}");
        }
    }

    [HttpPost("add-partner")]
    public async Task<ActionResult<Partner>> CreatePartner([FromBody] Partner partnerRequest)
    {
        if (partnerRequest == null)
        {
            return BadRequest("Partner data is required.");
        }

        partnerRequest.Id = Guid.NewGuid();
        
        _context.Partners.Add(partnerRequest);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPartners), new { id = partnerRequest.Id }, partnerRequest);
    }
}