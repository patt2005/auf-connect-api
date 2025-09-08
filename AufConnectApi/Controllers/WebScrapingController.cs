using AufConnectApi.Data;
using AufConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace AufConnectApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebScrapingController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly WebScrapingService _webScrapingService;

    public WebScrapingController(AppDbContext context, WebScrapingService webScrapingService)
    {
        _context = context;
        _webScrapingService = webScrapingService;
    }

    [HttpPost("scrape-events")]
    public async Task<ActionResult<List<Event>>> ScrapeAndSaveEvents()
    {
        try
        {
            var savedEvents = new List<Event>();
            var pageNumber = 1;
            
            while (true)
            {
                var url = pageNumber == 1 
                    ? "https://www.francophonie.org/actualites-medias?type=page_evenement"
                    : $"https://www.francophonie.org/actualites-medias?type=page_evenement&pays=All&event=All&eventart=All&pers=All&inst=All&parten=All&uhs=All&page={pageNumber - 1}";

                var eventPreviews = await _webScrapingService.ScrapeEventPreviewsAsync(url);
                
                if (eventPreviews == null || eventPreviews.Count == 0)
                {
                    break;
                }
                
                foreach (var eventPreview in eventPreviews)
                {
                    var existingEvent = await _context.Events
                        .FirstOrDefaultAsync(e => e.Title == eventPreview.Title && e.Date == eventPreview.Date);
                    
                    if (existingEvent == null && !string.IsNullOrEmpty(eventPreview.Link))
                    {
                        // Convert relative URL to absolute URL if needed
                        var absoluteUrl = eventPreview.Link;
                        if (!eventPreview.Link.StartsWith("http"))
                        {
                            absoluteUrl = eventPreview.Link.StartsWith("/") 
                                ? $"https://www.francophonie.org{eventPreview.Link}"
                                : $"https://www.francophonie.org/{eventPreview.Link}";
                        }
                        
                        var fullEvent = await _webScrapingService.ScrapeEventDetailsAsync(absoluteUrl);
                        
                        if (fullEvent != null)
                        {
                            fullEvent.Id = Guid.NewGuid();
                            _context.Events.Add(fullEvent);
                            savedEvents.Add(fullEvent);
                        }
                    }
                    else if (existingEvent != null)
                    {
                        savedEvents.Add(existingEvent);
                    }
                }
                
                pageNumber++;
            }
            
            await _context.SaveChangesAsync();
            
            return Ok(savedEvents);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error scraping and saving events: {ex.Message}");
        }
    }

    [HttpPost("scrape-members")]
    public async Task<ActionResult<List<Member>>> ScrapeAndSaveMembers()
    {
        try
        {
            var savedMembers = new List<Member>();
            var pageNumber = 1;
            
            // Loop through all pages until no more members are returned
            while (true)
            {
                var url = pageNumber == 1 
                    ? "https://www.auf.org/les_membres/nos-membres/" 
                    : $"https://www.auf.org/les_membres/nos-membres/page/{pageNumber}/";

                var memberPreviews = await _webScrapingService.ScrapeMemberPreviewsAsync(url);
                
                // Break if no members found on this page
                if (memberPreviews == null || memberPreviews.Count == 0)
                {
                    break;
                }
                
                // Fetch full details for each preview member and save to database
                foreach (var memberPreview in memberPreviews)
                {
                    // Check if member already exists to avoid duplicates
                    var existingMember = await _context.Members
                        .FirstOrDefaultAsync(m => m.Name == memberPreview.Name && m.Address == memberPreview.Address);
                    
                    if (existingMember == null && !string.IsNullOrEmpty(memberPreview.Link))
                    {
                        // Convert relative URL to absolute URL if needed
                        var absoluteUrl = memberPreview.Link;
                        if (!memberPreview.Link.StartsWith("http"))
                        {
                            absoluteUrl = memberPreview.Link.StartsWith("/") 
                                ? $"https://www.auf.org{memberPreview.Link}"
                                : $"https://www.auf.org/{memberPreview.Link}";
                        }
                        
                        // Fetch full member details using the link from preview
                        var fullMember = await _webScrapingService.ScrapeMemberDetailsAsync(absoluteUrl);
                        
                        if (fullMember != null)
                        {
                            fullMember.Id = Guid.NewGuid();
                            _context.Members.Add(fullMember);
                            savedMembers.Add(fullMember);
                        }
                    }
                    else if (existingMember != null)
                    {
                        savedMembers.Add(existingMember);
                    }
                }
                
                pageNumber++;
            }
            
            await _context.SaveChangesAsync();
            
            return Ok(savedMembers);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error scraping and saving members: {ex.Message}");
        }
    }

    [HttpPost("scrape-partners")]
    public async Task<ActionResult<List<Partner>>> ScrapeAndSavePartners()
    {
        try
        {
            var savedPartners = new List<Partner>();
            
            //
            var partnerPreviews = await _webScrapingService.ScrapePartnerPreviewsAsync("https://www.auf.org/partenaires/nos-partenaires/");
            
            if (partnerPreviews != null && partnerPreviews.Count > 0)
            {
                foreach (var partnerPreview in partnerPreviews)
                {
                    var existingPartner = await _context.Partners
                        .FirstOrDefaultAsync(p => p.Name == partnerPreview.Name);
                    
                    if (existingPartner == null)
                    {
                        // Convert relative URL to absolute URL if needed for PartnerUrl
                        var absolutePartnerUrl = partnerPreview.Link;
                        if (!string.IsNullOrEmpty(partnerPreview.Link) && !partnerPreview.Link.StartsWith("http"))
                        {
                            absolutePartnerUrl = partnerPreview.Link.StartsWith("/") 
                                ? $"https://www.auf.org{partnerPreview.Link}"
                                : $"https://www.auf.org/{partnerPreview.Link}";
                        }
                        
                        var newPartner = new Partner
                        {
                            Id = Guid.NewGuid(),
                            Name = partnerPreview.Name,
                            LogoUrl = partnerPreview.ImageUrl,
                            PartnerUrl = absolutePartnerUrl
                        };
                        
                        _context.Partners.Add(newPartner);
                        savedPartners.Add(newPartner);
                    }
                    else
                    {
                        savedPartners.Add(existingPartner);
                    }
                }
            }
            
            await _context.SaveChangesAsync();
            
            return Ok(savedPartners);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error scraping and saving partners: {ex.Message}");
        }
    }

    [HttpPost("scrape-projects")]
    public async Task<ActionResult<List<Project>>> ScrapeAndSaveProjects()
    {
        try
        {
            var savedProjects = new List<Project>();
            var pageNumber = 1;
            
            while (true)
            {
                var url = pageNumber == 1 
                    ? "https://www.auf.org/nos-actions/" 
                    : $"https://www.auf.org/nos-actions/page/{pageNumber}/";

                var projectPreviews = await _webScrapingService.ScrapeProjectPreviewsAsync(url);
                
                if (projectPreviews == null || projectPreviews.Count == 0)
                {
                    break;
                }
                
                foreach (var projectPreview in projectPreviews)
                {
                    var existingProject = await _context.Projects
                        .FirstOrDefaultAsync(p => p.Title == projectPreview.Title);
                    
                    if (existingProject == null && !string.IsNullOrEmpty(projectPreview.Link))
                    {
                        try
                        {
                            var absoluteUrl = projectPreview.Link;
                            if (!projectPreview.Link.StartsWith("http"))
                            {
                                absoluteUrl = projectPreview.Link.StartsWith("/") 
                                    ? $"https://www.auf.org{projectPreview.Link}"
                                    : $"https://www.auf.org/{projectPreview.Link}";
                            }
                            
                            var fullProject = await _webScrapingService.ScrapeProjectDetailsAsync(absoluteUrl);
                            
                            if (fullProject != null)
                            {
                                fullProject.Id = Guid.NewGuid();
                                _context.Projects.Add(fullProject);
                                savedProjects.Add(fullProject);
                            }
                        }
                        catch (Exception ex) when (ex.Message.Contains("404") || ex.Message.Contains("Not Found"))
                        {
                            // Skip this project if it returns 404 and continue with others
                            Console.WriteLine($"Skipping project '{projectPreview.Title}' due to 404 error: {ex.Message}");
                            continue;
                        }
                        catch (Exception ex)
                        {
                            // Log other errors but continue processing
                            Console.WriteLine($"Error scraping project '{projectPreview.Title}': {ex.Message}");
                            continue;
                        }
                    }
                    else if (existingProject != null)
                    {
                        savedProjects.Add(existingProject);
                    }
                }
                
                pageNumber++;
            }
            
            await _context.SaveChangesAsync();
            
            return Ok(savedProjects);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error scraping and saving projects: {ex.Message}");
        }
    }

    [HttpPost("scrape-resources")]
    public async Task<ActionResult<List<Resource>>> ScrapeAndSaveResources()
    {
        try
        {
            var savedResources = new List<Resource>();
            
            // Loop through all resource types
            foreach (ResourceType type in Enum.GetValues<ResourceType>())
            {
                var url = GetResourceUrl(type);
                if (string.IsNullOrEmpty(url))
                {
                    continue; // Skip invalid resource types
                }

                var allResources = await _webScrapingService.ScrapeResourcePreviewsAsync(url);
                
                // Skip if no resources found for this type
                if (allResources == null || allResources.Count == 0)
                {
                    continue;
                }
                
                // Find or create the Resource container for this type
                var resource = await _context.Resources
                    .Include(r => r.Sections)
                    .FirstOrDefaultAsync(r => r.Type == type);
                    
                if (resource == null)
                {
                    resource = new Resource
                    {
                        Id = Guid.NewGuid(),
                        Type = type,
                        Sections = new List<ResourceSection>()
                    };
                    _context.Resources.Add(resource);
                }
                
                // Add new resource sections
                foreach (var resourcePreview in allResources)
                {
                    // Check if section already exists to avoid duplicates
                    var existingSection = resource.Sections
                        .FirstOrDefault(s => s.Title == resourcePreview.Title);
                    
                    if (existingSection == null)
                    {
                        // Convert relative URL to absolute URL if needed
                        var absoluteUrl = resourcePreview.Link;
                        if (!string.IsNullOrEmpty(resourcePreview.Link) && !resourcePreview.Link.StartsWith("http"))
                        {
                            absoluteUrl = resourcePreview.Link.StartsWith("/") 
                                ? $"https://www.auf.org{resourcePreview.Link}"
                                : $"https://www.auf.org/{resourcePreview.Link}";
                        }
                        
                        var newSection = new ResourceSection
                        {
                            Id = Guid.NewGuid(),
                            Title = resourcePreview.Title,
                            Description = resourcePreview.Description,
                            ImageUrl = resourcePreview.ImageUrl,
                            Url = absoluteUrl,
                            ResourceId = resource.Id
                        };
                        
                        resource.Sections.Add(newSection);
                    }
                }
                
                savedResources.Add(resource);
            }
            
            await _context.SaveChangesAsync();
            
            return Ok(savedResources);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error scraping and saving resources: {ex.Message}");
        }
    }

    private string GetResourceUrl(ResourceType type)
    {
        return type switch
        {
            ResourceType.Formation => "https://www.auf.org/ressources-et-services/formation/",
            ResourceType.Resources => "https://www.auf.org/ressources-et-services/ressource/",
            ResourceType.Expertise => "https://www.auf.org/ressources-et-services/expertise/",
            ResourceType.Innovation => "https://www.auf.org/ressources-et-services/innovation/",
            ResourceType.Prospective => "https://www.auf.org/ressources-et-services/prospective/",
            ResourceType.Allocation => "https://www.auf.org/ressources-et-services/bourses/",
            _ => ""
        };
    }
}