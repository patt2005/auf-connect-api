using AufConnectApi.Data;
using AufConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

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
            var url = pageNumber == 1 
                ? "https://www.francophonie.org/actualites-medias?type=page_evenement"
                : $"https://www.francophonie.org/actualites-medias?type=page_evenement&pays=All&event=All&eventart=All&pers=All&inst=All&parten=All&uhs=All&page={pageNumber - 1}";
            
            var events = await _webScrapingService.ScrapeEventPreviewsAsync(url);
            
            var totalCount = pageNumber == 1 && events.Count < pageSize ? events.Count : pageNumber * pageSize;

            var result = new PagedResult<PreviewEvent>
            {
                Data = events,
                PageNumber = pageNumber,
                PageSize = events.Count,
                TotalCount = totalCount
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error fetching events: {ex.Message}");
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