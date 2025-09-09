using HtmlAgilityPack;
using AufConnectApi.Data;

namespace AufConnectApi.Services;

public class WebScrapingService
{
    private readonly HttpClient _httpClient;

    public WebScrapingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<PreviewProject>> ScrapeProjectPreviewsAsync(string url)
    {
        var projects = new List<PreviewProject>();
        
        try
        {
            var html = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var sections = doc.DocumentNode
                .SelectNodes("//section[@class='section']//div[@class='teaser main-teaser has-thumb clearfix']");

            if (sections != null)
            {
                foreach (var section in sections)
                {
                    var project = ExtractProjectFromSection(section);
                    if (project != null)
                    {
                        projects.Add(project);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log exception
            throw new Exception($"Error scraping projects: {ex.Message}", ex);
        }

        return projects;
    }

    public async Task<List<PreviewMember>> ScrapeMemberPreviewsAsync(string url)
    {
        var members = new List<PreviewMember>();
        
        try
        {
            var html = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var memberSections = doc.DocumentNode
                .SelectNodes("//section[@class='section section-members']//div[@class='teaser member-teaser clearfix']");

            if (memberSections != null)
            {
                foreach (var section in memberSections)
                {
                    var member = ExtractMemberFromSection(section);
                    if (member != null)
                    {
                        members.Add(member);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log exception
            throw new Exception($"Error scraping members: {ex.Message}", ex);
        }

        return members;
    }

    public async Task<List<PreviewPartner>> ScrapePartnerPreviewsAsync(string url)
    {
        var partners = new List<PreviewPartner>();
        
        try
        {
            var html = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var partnerSections = doc.DocumentNode
                .SelectNodes("//div[@class='entry-content clearfix']//div[contains(@class,'wp-caption')]");

            if (partnerSections != null)
            {
                foreach (var section in partnerSections)
                {
                    var partner = ExtractPartnerFromSection(section);
                    if (partner != null)
                    {
                        partners.Add(partner);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log exception
            throw new Exception($"Error scraping partners: {ex.Message}", ex);
        }

        return partners;
    }

    public async Task<List<PreviewResource>> ScrapeResourcePreviewsAsync(string url)
    {
        var resources = new List<PreviewResource>();
        
        try
        {
            var html = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var resourceSections = doc.DocumentNode
                .SelectNodes("//section[@class='section section-default']//div[@class='entry-content clearfix']");

            if (resourceSections != null)
            {
                foreach (var section in resourceSections)
                {
                    var resource = ExtractResourceFromSection(section);
                    if (resource != null)
                    {
                        resources.Add(resource);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log exception
            throw new Exception($"Error scraping resources: {ex.Message}", ex);
        }

        return resources;
    }

    public async Task<List<PreviewEvent>> ScrapeEventPreviewsAsync(string url)
    {
        var events = new List<PreviewEvent>();
        
        try
        {
            var html = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var eventSections = doc.DocumentNode
                .SelectNodes("//div[@id='lightgallery']//div[contains(@class,'portfolio-item')]");

            if (eventSections != null)
            {
                foreach (var section in eventSections)
                {
                    var eventItem = ExtractEventFromSection(section);
                    if (eventItem != null)
                    {
                        events.Add(eventItem);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log exception
            throw new Exception($"Error scraping events: {ex.Message}", ex);
        }

        return events;
    }

    public async Task<Project?> ScrapeProjectDetailsAsync(string url)
    {
        try
        {
            var html = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Extract title from h1.entry-title
            var titleNode = doc.DocumentNode.SelectSingleNode("//h1[@class='entry-title']");
            var title = titleNode?.InnerText?.Trim();
            if (string.IsNullOrEmpty(title))
                return null;

            // Extract image URL
            var imageNode = doc.DocumentNode.SelectSingleNode("//figure[@class='image']//img");
            var imageUrl = imageNode?.GetAttributeValue("src", "");

            // Extract main content
            var contentNode = doc.DocumentNode.SelectSingleNode("//div[@class='entry-content']");
            var objectives = ExtractProjectObjectives(contentNode);
            var targetAudience = ExtractProjectTargetAudience(contentNode);
            var budget = ExtractProjectBudget(contentNode);
            var period = ExtractProjectPeriod(contentNode);
            var operationalPartners = ExtractProjectPartners(contentNode);

            // Extract region from sidebar
            var regionNode = doc.DocumentNode.SelectSingleNode("//div[@class='block block-tags']//a[contains(@class,'lnk-region')]");
            var region = regionNode?.InnerText?.Trim()?.Replace("+", "")?.Trim();

            return new Project
            {
                Id = Guid.NewGuid(),
                Title = title,
                ImageUrl = imageUrl,
                Objectives = objectives,
                TargetAudience = targetAudience,
                OverallBudget = budget,
                CountryOfIntervention = region ?? "",
                Period = period,
                OperationalPartners = operationalPartners,
                RoleOfAufInAction = ExtractAufRole(contentNode),
                ProjectsFor2024_2025 = "",
                ProjectsFor2023_2024 = "",
                ProjectsFor2021_2022 = "",
                Device = ""
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Error scraping project details: {ex.Message}", ex);
        }
    }

    public async Task<Member?> ScrapeMemberDetailsAsync(string url)
    {
        try
        {
            var html = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var titleNode = doc.DocumentNode.SelectSingleNode("//h1[@class='entry-title']");
            var memberName = titleNode?.InnerText?.Trim();
            if (string.IsNullOrEmpty(memberName))
                return null;

            var descriptionNode = doc.DocumentNode.SelectSingleNode("//div[@class='entry-content'][1]//p");
            var description = descriptionNode?.InnerText?.Trim() ?? "";

            var historyNode = doc.DocumentNode.SelectSingleNode("//div[@class='entry-content entry-history']//p");
            var background = historyNode?.InnerText?.Trim() ?? "";

            var contactsBlock = doc.DocumentNode.SelectSingleNode("//div[@class='block block-contacts']");

            var contactNameNode = contactsBlock?.SelectSingleNode(".//div[@class='name'][2]//strong");
            var contactName = contactNameNode?.InnerText?.Trim() ?? "";

            var contactTitleNode = contactsBlock?.SelectSingleNode(".//div[@class='occupation']");
            var contactTitle = contactTitleNode?.InnerText?.Trim() ?? "";

            var statusNode = contactsBlock?.SelectSingleNode(".//div[@class='status']");
            var statutoryType = "";
            var universityType = "";

            if (statusNode != null)
            {
                var statusText = statusNode.InnerText;
                
                var statutoryMatch = System.Text.RegularExpressions.Regex.Match(statusText, @"Type statutaire\s*:\s*([^\r\n]+)");
                if (statutoryMatch.Success)
                    statutoryType = statutoryMatch.Groups[1].Value.Trim();

                var universityMatch = System.Text.RegularExpressions.Regex.Match(statusText, @"Type universitaire\s*:\s*([^\r\n]+)");
                if (universityMatch.Success)
                    universityType = universityMatch.Groups[1].Value.Trim();
            }

            // Extract address
            var addressNode = contactsBlock?.SelectSingleNode(".//address[@class='address']");
            var address = addressNode?.InnerText?.Trim() ?? "";

            // Extract phone
            var phoneNode = contactsBlock?.SelectSingleNode(".//div[@class='tel']");
            var phone = phoneNode?.InnerText?.Replace("Téléphone :", "").Trim() ?? "";

            // Extract website
            var websiteNode = contactsBlock?.SelectSingleNode(".//div[@class='website']//a");
            var website = websiteNode?.GetAttributeValue("href", "") ?? "";

            // Extract region from tags block
            var regionNode = doc.DocumentNode.SelectSingleNode("//div[@class='block block-tags']//a[contains(@class,'lnk-region')]");
            var region = regionNode?.InnerText?.Trim()?.Replace("+", "")?.Replace("AUF - ", "").Trim() ?? "";

            // Extract founded year from background/history
            var foundedYear = "";
            if (!string.IsNullOrEmpty(background))
            {
                var yearMatch = System.Text.RegularExpressions.Regex.Match(background, @"Fondée en (\d{4})|fonde en (\d{4})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (yearMatch.Success)
                    foundedYear = yearMatch.Groups[1].Value.Length > 0 ? yearMatch.Groups[1].Value : yearMatch.Groups[2].Value;
            }

            return new Member
            {
                Id = Guid.NewGuid(),
                Name = memberName,
                Description = description,
                Background = background,
                ContactName = contactName,
                ContactTitle = contactTitle,
                StatutoryType = statutoryType,
                UniversityType = universityType,
                Address = address,
                Phone = phone,
                Website = website,
                Region = region,
                FoundedYear = foundedYear
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Error scraping member details: {ex.Message}", ex);
        }
    }

    public async Task<Event?> ScrapeEventDetailsAsync(string url)
    {
        try
        {
            var html = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Extract title
            var titleNode = doc.DocumentNode.SelectSingleNode("//h2[@class='Font-Montserrat font-weight-bold']//span[@class='field field--name-title field--type-string field--label-hidden']");
            var title = titleNode?.InnerText?.Trim();
            if (string.IsNullOrEmpty(title))
                return null;

            // Extract date and event type
            var dateEventTypeNode = doc.DocumentNode.SelectSingleNode("//span[@class='date text-green font-weight-bold']");
            var dateEventText = dateEventTypeNode?.InnerText?.Trim() ?? "";
            
            var date = "";
            var eventType = "";
            if (!string.IsNullOrEmpty(dateEventText))
            {
                var parts = dateEventText.Split('|');
                if (parts.Length >= 1)
                {
                    date = parts[0].Replace("le ", "").Trim();
                }
                if (parts.Length >= 2)
                {
                    eventType = parts[1].Trim();
                }
            }

            // Extract main image URL
            var imageNode = doc.DocumentNode.SelectSingleNode("//div[@class='bg-cover rounded nwsbig gradient position-relative']");
            var imageUrl = "";
            if (imageNode != null)
            {
                var styleAttr = imageNode.GetAttributeValue("style", "");
                var match = System.Text.RegularExpressions.Regex.Match(styleAttr, @"background-image:\s*url\(['\""]?([^'\"")]+)['\""]?\)");
                if (match.Success)
                {
                    imageUrl = match.Groups[1].Value;
                    if (imageUrl.StartsWith("/"))
                    {
                        imageUrl = "https://www.francophonie.org" + imageUrl;
                    }
                }
            }

            // Check for YouTube video specifically in the main event content area (not in "see also" section)
            var videoUrl = "";
            
            // First, try to find video in the main banner/hero section
            var heroVideoNode = doc.DocumentNode.SelectSingleNode("//section[contains(@class, 'noneinner')]//a[@data-fancybox][contains(@href, 'youtube.com')]");
            if (heroVideoNode != null)
            {
                videoUrl = heroVideoNode.GetAttributeValue("href", "");
            }
            else
            {
                // If no video in hero section, check in the main event sections (carousel) but exclude "newssec" (see also section)
                var carouselVideoNode = doc.DocumentNode.SelectSingleNode("//section[contains(@class, 'abo_franc')]//a[@data-fancybox][contains(@href, 'youtube.com')]");
                if (carouselVideoNode != null)
                {
                    videoUrl = carouselVideoNode.GetAttributeValue("href", "");
                }
            }

            // Extract theme/description from the first section
            var themeNode = doc.DocumentNode.SelectSingleNode("//div[@class='item'][1]//p");
            var theme = themeNode?.InnerText?.Trim() ?? "";

            // Extract hashtags
            var hashtagNode = doc.DocumentNode.SelectSingleNode("//div[@class='field field--name-field-hashtag-de-l-evenement field--type-string field--label-visually_hidden']//div[@class='field__item']");
            var hashtags = hashtagNode?.InnerText?.Trim() ?? "";

            // Extract event sections from carousel
            var sections = new List<EventSection>();
            var sectionNodes = doc.DocumentNode.SelectNodes("//div[@class='item']");
            if (sectionNodes != null)
            {
                foreach (var sectionNode in sectionNodes)
                {
                    var sectionTitleNode = sectionNode.SelectSingleNode(".//h6[@class='Libre-bold']");
                    var sectionDescNode = sectionNode.SelectSingleNode(".//p[@class='py-4']");
                    var sectionLinkNode = sectionNode.SelectSingleNode(".//a[@class='btn btn-outline-orange rounded-50']");

                    if (sectionTitleNode != null)
                    {
                        var section = new EventSection
                        {
                            Title = sectionTitleNode.InnerText?.Trim() ?? "",
                            Description = sectionDescNode?.InnerText?.Trim() ?? "",
                            LinkUrl = sectionLinkNode?.GetAttributeValue("href", "") ?? "",
                            LinkText = sectionLinkNode?.InnerText?.Replace("<i class=\"fa fa-long-arrow-right pl-2\"></i>", "").Trim() ?? ""
                        };

                        // Convert relative URLs to absolute
                        if (!string.IsNullOrEmpty(section.LinkUrl) && section.LinkUrl.StartsWith("/"))
                        {
                            section.LinkUrl = "https://www.francophonie.org" + section.LinkUrl;
                        }

                        sections.Add(section);
                    }
                }
            }

            var eventId = Guid.NewGuid();
            
            // Set EventId for all sections
            foreach (var section in sections)
            {
                section.Id = Guid.NewGuid();
                section.EventId = eventId;
            }

            return new Event
            {
                Id = eventId,
                Title = title,
                Description = theme,
                ImageUrl = imageUrl,
                VideoUrl = videoUrl,
                Date = date,
                EventType = eventType,
                Theme = theme,
                Hashtags = hashtags,
                Sections = sections
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Error scraping event details: {ex.Message}", ex);
        }
    }

    private PreviewProject? ExtractProjectFromSection(HtmlNode section)
    {
        try
        {
            var titleNode = section.SelectSingleNode(".//h3[@class='title']");
            var title = titleNode?.InnerText?.Trim();
            if (string.IsNullOrEmpty(title))
                return null;

            var linkNode = section.SelectSingleNode(".//a[@href]");
            var link = linkNode?.GetAttributeValue("href", "");

            var regionNode = section.SelectSingleNode(".//div[@class='regions']//a[@class='lnk-region']");
            var region = regionNode?.InnerText?.Trim()?.Replace("+", "")?.Trim();

            var descriptionNode = section.SelectSingleNode(".//div[@class='text']");
            var description = descriptionNode?.InnerText?.Trim();

            var imageNode = section.SelectSingleNode(".//img");
            var imageUrl = imageNode?.GetAttributeValue("src", "");

            return new PreviewProject
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description ?? "",
                Region = region ?? "",
                Link = link ?? "",
                ImageUrl = imageUrl
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private PreviewMember? ExtractMemberFromSection(HtmlNode section)
    {
        try
        {
            var titleNode = section.SelectSingleNode(".//h3[@class='title']");
            var title = titleNode?.InnerText?.Trim();
            if (string.IsNullOrEmpty(title))
                return null;

            var linkNode = section.SelectSingleNode(".//a[@class='lnk-more']");
            var link = linkNode?.GetAttributeValue("href", "");

            // Convert relative URL to absolute URL
            if (!string.IsNullOrEmpty(link) && link.StartsWith("/"))
            {
                link = "https://www.auf.org" + link;
            }

            var addressNode = section.SelectSingleNode(".//span[@class='address']");
            var address = addressNode?.InnerText?.Trim();

            var regionNode = section.SelectSingleNode(".//div[@class='regions']//a[@class='lnk-region']");
            var region = regionNode?.InnerText?.Trim()?.Replace("+", "")?.Trim();

            return new PreviewMember
            {
                Id = Guid.NewGuid(),
                Name = title,
                Address = address ?? "",
                Region = region ?? "",
                Link = link ?? ""
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private PreviewPartner? ExtractPartnerFromSection(HtmlNode section)
    {
        try
        {
            var captionNode = section.SelectSingleNode(".//p[@class='wp-caption-text']");
            var name = captionNode?.InnerText?.Trim();
            if (string.IsNullOrEmpty(name))
                return null;

            var linkNode = section.SelectSingleNode(".//a[@href]");
            var link = linkNode?.GetAttributeValue("href", "");

            var imageNode = section.SelectSingleNode(".//img");
            var imageUrl = imageNode?.GetAttributeValue("src", "");
            var imageAlt = imageNode?.GetAttributeValue("alt", "");

            return new PreviewPartner
            {
                Id = Guid.NewGuid(),
                Name = name,
                Link = link ?? "",
                ImageUrl = imageUrl,
                Description = imageAlt ?? ""
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private PreviewResource? ExtractResourceFromSection(HtmlNode section)
    {
        try
        {
            var titleNode = section.SelectSingleNode(".//h2");
            var title = titleNode?.InnerText?.Trim();
            if (string.IsNullOrEmpty(title))
                return null;

            // Get the content text excluding the title
            var contentNodes = section.SelectNodes(".//p");
            var content = "";
            if (contentNodes != null)
            {
                content = string.Join(" ", contentNodes.Select(p => p.InnerText?.Trim()).Where(t => !string.IsNullOrEmpty(t)));
            }

            // Look for links within the content
            var linkNode = section.SelectSingleNode(".//a[@href]");
            var link = linkNode?.GetAttributeValue("href", "");

            // Look for images
            var imageNode = section.SelectSingleNode(".//img");
            var imageUrl = imageNode?.GetAttributeValue("src", "");

            return new PreviewResource
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = content.Length > 500 ? content.Substring(0, 500) + "..." : content,
                Link = link ?? "",
                ImageUrl = imageUrl
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private PreviewEvent? ExtractEventFromSection(HtmlNode section)
    {
        try
        {
            var titleNode = section.SelectSingleNode(".//h1[@class='Libre-bold text-white pt-2']");
            var title = titleNode?.InnerText?.Trim();
            if (string.IsNullOrEmpty(title))
                return null;

            // Extract date information
            var dateSpan = section.SelectSingleNode(".//span[contains(text(),'du ') or contains(text(),'le ')]");
            var dateText = dateSpan?.InnerText?.Trim() ?? "";

            // Extract city/location information
            var locationSpans = section.SelectNodes(".//p[img[@src='/themes/francophonie/images/map-marker.png']]/span");
            var city = "";
            if (locationSpans != null && locationSpans.Count > 0)
            {
                city = locationSpans.LastOrDefault()?.InnerText?.Trim() ?? "";
            }

            // Extract link
            var linkNode = section.SelectSingleNode(".//a[@href]");
            var link = linkNode?.GetAttributeValue("href", "");

            return new PreviewEvent
            {
                Id = Guid.NewGuid(),
                Title = title,
                Date = dateText,
                City = city,
                Link = link ?? ""
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private string ExtractProjectObjectives(HtmlNode? contentNode)
    {
        if (contentNode == null) return "";

        var objectivesSection = contentNode.SelectSingleNode(".//p[contains(text(), 'OBJECTIFS')]");
        if (objectivesSection != null)
        {
            var objectives = new List<string>();
            var nextSibling = objectivesSection.NextSibling;
            while (nextSibling != null && nextSibling.Name != "p" || (nextSibling.Name == "p" && !nextSibling.InnerText.Contains("IMPACT") && !nextSibling.InnerText.Contains("CIBLE")))
            {
                if (nextSibling.Name == "ul")
                {
                    var listItems = nextSibling.SelectNodes(".//li");
                    if (listItems != null)
                    {
                        objectives.AddRange(listItems.Select(li => li.InnerText?.Trim()).Where(t => !string.IsNullOrEmpty(t)));
                    }
                    break;
                }
                nextSibling = nextSibling.NextSibling;
            }
            return string.Join("; ", objectives);
        }

        // Fallback: get first few paragraphs as objectives
        var paragraphs = contentNode.SelectNodes(".//p[not(contains(text(), 'PARTENAIRES')) and not(contains(text(), 'BUDGET'))]");
        if (paragraphs != null && paragraphs.Count > 0)
        {
            return string.Join(" ", paragraphs.Take(2).Select(p => p.InnerText?.Trim()).Where(t => !string.IsNullOrEmpty(t)));
        }

        return "";
    }

    private string ExtractProjectTargetAudience(HtmlNode? contentNode)
    {
        if (contentNode == null) return "";

        var targetSection = contentNode.SelectSingleNode(".//p[contains(text(), 'CIBLE')]");
        if (targetSection != null)
        {
            var targets = new List<string>();
            var nextSibling = targetSection.NextSibling;
            while (nextSibling != null)
            {
                if (nextSibling.Name == "ul")
                {
                    var listItems = nextSibling.SelectNodes(".//li");
                    if (listItems != null)
                    {
                        targets.AddRange(listItems.Select(li => li.InnerText?.Trim()).Where(t => !string.IsNullOrEmpty(t)));
                    }
                    break;
                }
                nextSibling = nextSibling.NextSibling;
            }
            return string.Join("; ", targets);
        }

        return "Établissements d'enseignement supérieur et étudiants";
    }

    private string ExtractProjectBudget(HtmlNode? contentNode)
    {
        if (contentNode == null) return "";

        var budgetSection = contentNode.SelectSingleNode(".//p[contains(text(), 'BUDGET GLOBAL')]");
        if (budgetSection != null)
        {
            var budgetText = budgetSection.NextSibling?.InnerText?.Trim();
            if (!string.IsNullOrEmpty(budgetText))
                return budgetText;
        }

        // Look for euro symbol or budget mentions
        var allText = contentNode.InnerText;
        var euroIndex = allText.IndexOf('€');
        if (euroIndex > 0)
        {
            var budgetMatch = allText.Substring(Math.Max(0, euroIndex - 20), Math.Min(40, allText.Length - Math.Max(0, euroIndex - 20)));
            return budgetMatch.Trim();
        }

        return "";
    }

    private string ExtractProjectPeriod(HtmlNode? contentNode)
    {
        if (contentNode == null) return "";

        var periodSection = contentNode.SelectSingleNode(".//li[contains(text(), 'Durée')]");
        if (periodSection != null)
        {
            return periodSection.InnerText?.Replace("Durée", "").Trim().TrimStart(':').Trim() ?? "";
        }

        return "";
    }

    private List<string> ExtractProjectPartners(HtmlNode? contentNode)
    {
        if (contentNode == null) return new List<string>();

        var partners = new List<string>();
        var partnersSection = contentNode.SelectSingleNode(".//p[contains(text(), 'PARTENAIRES')]");
        
        if (partnersSection != null)
        {
            var nextSibling = partnersSection.NextSibling;
            while (nextSibling != null)
            {
                if (nextSibling.Name == "ul")
                {
                    var listItems = nextSibling.SelectNodes(".//li");
                    if (listItems != null)
                    {
                        partners.AddRange(listItems.Select(li => li.InnerText?.Trim()).Where(t => !string.IsNullOrEmpty(t)));
                    }
                    
                    // Look for nested lists
                    var nestedLists = nextSibling.SelectNodes(".//ul//li");
                    if (nestedLists != null)
                    {
                        partners.AddRange(nestedLists.Select(li => li.InnerText?.Trim()).Where(t => !string.IsNullOrEmpty(t)));
                    }
                    break;
                }
                nextSibling = nextSibling.NextSibling;
            }
        }

        return partners.Distinct().ToList();
    }

    private List<string> ExtractAufRole(HtmlNode? contentNode)
    {
        if (contentNode == null) return new List<string>();

        var aufRoleSection = contentNode.SelectSingleNode(".//p[contains(text(), \"RÔLE DE L\") and contains(text(), \"AUF\")]");
        if (aufRoleSection != null)
        {
            var roleText = aufRoleSection.NextSibling?.InnerText?.Trim();
            if (!string.IsNullOrEmpty(roleText))
            {
                return new List<string> { roleText };
            }
        }

        return new List<string> { "Coordination et mise en œuvre" };
    }

    public async Task<List<Member>> ScrapeResuffMembersAsync(string url)
    {
        var members = new List<Member>();
        
        try
        {
            var html = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Find all region sections (Amériques, Europe, Afrique, Asie)
            var regionSections = doc.DocumentNode.SelectNodes("//div[@class='groupeCarte']");

            if (regionSections != null)
            {
                foreach (var regionSection in regionSections)
                {
                    var regionTitleNode = regionSection.SelectSingleNode(".//h3");
                    var regionName = regionTitleNode?.InnerText?.Trim() ?? "";

                    var textDoubleNode = regionSection.SelectSingleNode(".//div[@class='txtDouble']");
                    if (textDoubleNode != null)
                    {
                        var memberEntries = ExtractMembersFromRegionText(textDoubleNode.InnerHtml, regionName);
                        members.AddRange(memberEntries);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error scraping RESUFF members: {ex.Message}", ex);
        }

        return members;
    }

    private List<Member> ExtractMembersFromRegionText(string htmlContent, string region)
    {
        var members = new List<Member>();
        
        try
        {
            // Split by <br> tags and process each member entry
            var memberBlocks = htmlContent.Split(new string[] { "&nbsp;<br>" }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var block in memberBlocks)
            {
                var member = ParseMemberBlock(block.Trim(), region);
                if (member != null)
                {
                    members.Add(member);
                }
            }
        }
        catch (Exception)
        {
            // Continue processing other members if one fails
        }

        return members;
    }

    private Member? ParseMemberBlock(string memberBlock, string region)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(memberBlock)) return null;

            var doc = new HtmlDocument();
            doc.LoadHtml(memberBlock);

            // Extract name from <strong class="violet"> tag
            var nameNode = doc.DocumentNode.SelectSingleNode(".//strong[@class='violet']");
            if (nameNode == null) return null;

            var fullName = nameNode.InnerText?.Trim();
            if (string.IsNullOrEmpty(fullName)) return null;

            // Get the text content and clean it
            var fullText = System.Web.HttpUtility.HtmlDecode(doc.DocumentNode.InnerText)
                .Replace("&nbsp;", " ")
                .Trim();

            // Split into lines and process
            var lines = fullText.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .ToList();

            if (lines.Count == 0) return null;

            // First line should contain the name, extract the rest as description
            var description = "";
            var contactTitle = "";
            var contactName = fullName;

            // Check if there's additional role information (like committee membership)
            var specialRoleMatch = System.Text.RegularExpressions.Regex.Match(fullText, 
                @"(Présidente du RESUFF|Vice-Présidente du RESUFF|Secrétaire du RESUFF|Trésorière du RESUFF|Membre du Comité scientifique du RESUFF|Présidente du Comité scientifique du RESUFF)");
            
            var specialRole = specialRoleMatch.Success ? specialRoleMatch.Groups[1].Value : "";

            // Extract the description (everything after the name line)
            if (lines.Count > 1)
            {
                var descriptionLines = lines.Skip(1).ToList();
                
                // Remove the special role if it was found in the lines
                if (!string.IsNullOrEmpty(specialRole))
                {
                    descriptionLines = descriptionLines.Where(line => !line.Contains(specialRole.Replace("du RESUFF", "").Trim())).ToList();
                }
                
                description = string.Join(" ", descriptionLines);
                
                // Try to extract the most recent/primary title (usually the first line after name)
                if (descriptionLines.Count > 0)
                {
                    contactTitle = descriptionLines[0];
                }
            }

            // Extract institution/university name (usually the last line or contains "Université")
            var institutionMatch = System.Text.RegularExpressions.Regex.Match(description, 
                @"(Université[^,]*|École[^,]*|Institut[^,]*|Centre[^,]*)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            var institution = institutionMatch.Success ? institutionMatch.Groups[1].Value.Trim() : "";

            return new Member
            {
                Id = Guid.NewGuid(),
                Name = fullName,
                Description = description.Length > 500 ? description.Substring(0, 500) + "..." : description,
                Background = !string.IsNullOrEmpty(specialRole) ? $"RESUFF Role: {specialRole}" : null,
                ContactName = contactName,
                ContactTitle = contactTitle.Length > 200 ? contactTitle.Substring(0, 200) + "..." : contactTitle,
                StatutoryType = !string.IsNullOrEmpty(specialRole) ? "RESUFF Leadership" : null,
                UniversityType = !string.IsNullOrEmpty(institution) ? "Academic Institution" : null,
                Address = institution.Length > 200 ? institution.Substring(0, 200) + "..." : institution,
                Phone = null,
                Website = null,
                Region = region,
                FoundedYear = null
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<List<Resource>> ScrapeResuffResourcesAsync(string url)
    {
        var resources = new List<Resource>();
        
        try
        {
            var html = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Find all resource documents with class "ligneDoc"
            var resourceNodes = doc.DocumentNode.SelectNodes("//div[@class='ligneDoc']");

            if (resourceNodes != null)
            {
                foreach (var resourceNode in resourceNodes)
                {
                    var resource = ExtractResourceFromResuffNode(resourceNode);
                    if (resource != null)
                    {
                        resources.Add(resource);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error scraping RESUFF resources: {ex.Message}", ex);
        }

        return resources;
    }

    private Resource? ExtractResourceFromResuffNode(HtmlNode resourceNode)
    {
        try
        {
            var txtDocNode = resourceNode.SelectSingleNode(".//div[@class='txtDoc']");
            if (txtDocNode == null) return null;

            var violetSpan = txtDocNode.SelectSingleNode(".//span[@class='violet']");
            var moyenSpan = txtDocNode.SelectSingleNode(".//span[@class='moyen']");

            if (violetSpan == null || moyenSpan == null) return null;

            var resourceType = violetSpan.InnerText?.Trim();
            var title = System.Web.HttpUtility.HtmlDecode(moyenSpan.InnerText?.Trim());

            if (string.IsNullOrEmpty(resourceType) || string.IsNullOrEmpty(title)) return null;

            var linkNode = resourceNode.SelectSingleNode(".//a[@href]");
            var link = linkNode?.GetAttributeValue("href", "");

            if (!string.IsNullOrEmpty(link) && !link.StartsWith("http"))
            {
                if (link.StartsWith("/"))
                {
                    link = "https://www.resuff.org" + link;
                }
                else
                {
                    link = "https://www.resuff.org/" + link;
                }
            }

            var enumType = DetermineResourceType(resourceType, title);

            var resource = new Resource
            {
                Id = Guid.NewGuid(),
                Type = enumType,
                Link = link
            };

            return resource;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private ResourceType DetermineResourceType(string resourceTypeText, string title)
    {
        var lowerType = resourceTypeText.ToLower();
        var lowerTitle = title.ToLower();

        if (lowerType.Contains("colloque") || lowerType.Contains("atelier"))
            return ResourceType.Formation;
        
        if (lowerType.Contains("bio"))
            return ResourceType.Expertise;
        
        if (lowerType.Contains("publication"))
            return ResourceType.Resources;
        
        if (lowerTitle.Contains("synthèse"))
            return ResourceType.Formation;
        
        if (lowerTitle.Contains("programme"))
            return ResourceType.Innovation;

        return ResourceType.Resources;
    }
}
