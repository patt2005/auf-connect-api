using Microsoft.AspNetCore.Mvc;

namespace AufConnectApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DownloadController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public DownloadController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Redirects to the APK download URL from Firebase Storage
    /// </summary>
    [HttpGet("apk")]
    public IActionResult DownloadApk()
    {
        // Get the APK URL from configuration or use the default Firebase URL
        var apkUrl = _configuration["ApkDownloadUrl"] ??
                     "https://firebasestorage.googleapis.com/v0/b/nightee-66b26.firebasestorage.app/o/auf-connect.apk?alt=media&token=c124d270-ef17-4318-9588-9897bb9747d4";

        // Redirect to the Firebase Storage URL
        return Redirect(apkUrl);
    }

    /// <summary>
    /// Alternative endpoint that returns the APK download URL as JSON
    /// </summary>
    [HttpGet("apk/url")]
    public IActionResult GetApkUrl()
    {
        var apkUrl = _configuration["ApkDownloadUrl"] ??
                     "https://firebasestorage.googleapis.com/v0/b/nightee-66b26.firebasestorage.app/o/auf-connect.apk?alt=media&token=c124d270-ef17-4318-9588-9897bb9747d4";

        return Ok(new { url = apkUrl, version = "v1.0.0" });
    }
}
