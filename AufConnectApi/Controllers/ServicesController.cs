using AufConnectApi.Data;
using AufConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AufConnectApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly WebScrapingService _webScrapingService;

    public ServicesController(AppDbContext context, WebScrapingService webScrapingService)
    {
        _context = context;
        _webScrapingService = webScrapingService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Service>>> GetServices()
    {
        try
        {
            var services = await _context.Services.ToListAsync();
            return Ok(services);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error fetching services: {ex.Message}");
        }
    }

    [HttpPost("scrape-services")]
    public async Task<ActionResult<List<Service>>> ScrapeAndSaveServices([FromQuery] string url = "https://appelsprojets.auf.org/")
    {
        try
        {
            var scrapedServices = await _webScrapingService.ScrapeServicesAsync(url);
            
            if (scrapedServices == null || !scrapedServices.Any())
            {
                return BadRequest("No services found to scrape.");
            }

            var currentServiceCount = await _context.Services.CountAsync();
            var addedCount = 0;

            foreach (var service in scrapedServices)
            {
                var existingService = await _context.Services
                    .AnyAsync(s => s.Title == service.Title);
                
                if (!existingService)
                {
                    _context.Services.Add(service);
                    addedCount++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { 
                Message = $"Successfully scraped and saved {addedCount} new services (out of {scrapedServices.Count} found)",
                TotalServicesBeforeScrap = currentServiceCount,
                TotalServicesAfterScrap = currentServiceCount + addedCount,
                NewServicesAdded = addedCount,
                Services = scrapedServices 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error scraping services: {ex.Message}");
        }
    }

    [HttpPost("add-service")]
    public async Task<ActionResult<Service>> CreateService([FromBody] Service serviceRequest)
    {
        try
        {
            if (serviceRequest == null)
            {
                return BadRequest("Service data is required.");
            }

            // Generate new ID for the service
            serviceRequest.Id = Guid.NewGuid();
            
            // Add service to database
            _context.Services.Add(serviceRequest);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetServices), new { id = serviceRequest.Id }, serviceRequest);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error creating service: {ex.Message}");
        }
    }
}