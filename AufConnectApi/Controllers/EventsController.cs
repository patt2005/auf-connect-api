using AufConnectApi.Data;
using AufConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AufConnectApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly WebScrapingService _webScrapingService;

    public EventsController(AppDbContext context, WebScrapingService webScrapingService)
    {
        _context = context;
        _webScrapingService = webScrapingService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PreviewEvent>>> GetEvents([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        try
        {
            var totalCount = await _context.Events.CountAsync();
            
            var events = await _context.Events
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new PreviewEvent
                {
                    Id = e.Id,
                    Title = e.Title,
                    Date = e.Date,
                    City = e.City,
                    Link = $"/api/events/{e.Id}"
                })
                .ToListAsync();

            var result = new PagedResult<PreviewEvent>
            {
                Data = events,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error fetching events: {ex.Message}");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Event>> GetEventById(Guid id)
    {
        try
        {
            var eventDetail = await _context.Events
                .Include(e => e.Sections)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventDetail == null)
            {
                return NotFound($"Event with ID '{id}' not found.");
            }

            return Ok(eventDetail);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error fetching event details: {ex.Message}");
        }
    }

    [HttpGet("details")]
    public async Task<ActionResult<Event>> GetEventByLink([FromQuery] string link)
    {
        try
        {
            if (string.IsNullOrEmpty(link))
            {
                return BadRequest("Event link is required.");
            }
            
            var fullLink = link.StartsWith("http") ? link : $"https://www.francophonie.org{link}";
            Console.WriteLine(fullLink);
            
            var eventDetail = await _webScrapingService.ScrapeEventDetailsAsync(fullLink);

            if (eventDetail == null)
            {
                return NotFound($"Event with link '{link}' not found or could not be scraped.");
            }

            return Ok(eventDetail);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error fetching event details: {ex.Message}");
        }
    }

    [HttpPost("add-event")]
    public async Task<ActionResult<Event>> CreateEvent([FromBody] Event eventRequest)
    {
        if (eventRequest == null)
        {
            return BadRequest("Event data is required.");
        }

        eventRequest.Id = Guid.NewGuid();
        
        _context.Events.Add(eventRequest);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvents), new { id = eventRequest.Id }, eventRequest);
    }
}