using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AppSettingsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AppSettingsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // GET: api/AppSettings
    [HttpGet]
    public ActionResult<AppSettings> GetSettings()
    {
        var settings = new AppSettings
        {
            AppVersion = "1.0.0",
            SupportEmail = "support@tmss.com",
            SupportPhone = "+84-123-456-789",
            PrivacyPolicyUrl = "https://tmss.com/privacy",
            TermsOfServiceUrl = "https://tmss.com/terms",
            NotificationSettings = new NotificationSettings
            {
                PushNotifications = true,
                TaskAssigned = true,
                TaskUpdates = true,
                Geofencing = true,
                Messages = true
            }
        };

        return Ok(settings);
    }

    // GET: api/AppSettings/version
    [HttpGet("version")]
    public ActionResult<AppVersion> GetVersion()
    {
        return Ok(new AppVersion
        {
            Version = "1.0.0",
            BuildNumber = "100",
            ReleaseDate = new DateTime(2025, 12, 13),
            ForceUpdate = false,
            UpdateUrl = "https://play.google.com/store/apps/details?id=com.tmss.driver"
        });
    }

    // GET: api/AppSettings/terms
    [HttpGet("terms")]
    public ActionResult<DocumentContent> GetTermsOfService()
    {
        return Ok(new DocumentContent
        {
            Title = "Terms of Service",
            Content = "These are the terms of service for the TMSS Driver App...",
            LastUpdated = new DateTime(2025, 12, 1)
        });
    }

    // GET: api/AppSettings/privacy
    [HttpGet("privacy")]
    public ActionResult<DocumentContent> GetPrivacyPolicy()
    {
        return Ok(new DocumentContent
        {
            Title = "Privacy Policy",
            Content = "This privacy policy explains how we collect and use your data...",
            LastUpdated = new DateTime(2025, 12, 1)
        });
    }
}

public class AppSettings
{
    public string AppVersion { get; set; } = string.Empty;
    public string SupportEmail { get; set; } = string.Empty;
    public string SupportPhone { get; set; } = string.Empty;
    public string PrivacyPolicyUrl { get; set; } = string.Empty;
    public string TermsOfServiceUrl { get; set; } = string.Empty;
    public NotificationSettings NotificationSettings { get; set; } = new();
}

public class NotificationSettings
{
    public bool PushNotifications { get; set; }
    public bool TaskAssigned { get; set; }
    public bool TaskUpdates { get; set; }
    public bool Geofencing { get; set; }
    public bool Messages { get; set; }
}

public class AppVersion
{
    public string Version { get; set; } = string.Empty;
    public string BuildNumber { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public bool ForceUpdate { get; set; }
    public string UpdateUrl { get; set; } = string.Empty;
}

public class DocumentContent
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}