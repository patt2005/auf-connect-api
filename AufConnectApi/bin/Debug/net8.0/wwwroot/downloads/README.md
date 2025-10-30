# Downloads Folder - AUF Connect

This folder contains downloadable files for the AUF Connect landing page.

## Current Files

### auf-connect.apk
- ✅ **Status**: Ready for download
- **Size**: 49 MB
- **Type**: Android Application Package (APK)
- **MIME Type**: `application/vnd.android.package-archive`

## APK File Configuration

The server is now configured to properly serve APK files with the correct MIME type. This ensures that when users click the download button on the landing page, they will receive a proper APK file that can be installed on Android devices.

### MIME Type Configuration

The following configuration has been added to `Program.cs`:

```csharp
var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".apk"] = "application/vnd.android.package-archive";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});
```

This ensures that:
- ✅ APK files are served with the correct `Content-Type` header
- ✅ Browsers recognize the file as an Android package
- ✅ Download prompts work correctly on all devices
- ✅ No file extension confusion (.apk.txt, etc.)

## Download URL

The APK is accessible at:
```
https://your-domain.com/downloads/auf-connect.apk
```

Or locally:
```
http://localhost:5000/downloads/auf-connect.apk
```

## Updating the APK

To update the APK file:

1. Build your Android app
2. Generate a signed APK or AAB
3. Replace the existing `auf-connect.apk` file
4. Restart the server (if needed)

```bash
# Example: Copy new APK
cp /path/to/your/new-app.apk ./auf-connect.apk

# Restart server
dotnet run
```

## File Naming

**Important**: Keep the filename as `auf-connect.apk` as it's referenced in the HTML:

```html
<a href="/downloads/auf-connect.apk" class="btn btn-download" download>
    Télécharger APK
</a>
```

If you change the filename, update it in:
- `wwwroot/index.html` (download button href)
- Any documentation

## Security Considerations

### For Production

When deploying to production:

1. **HTTPS Only**: Always serve APK files over HTTPS
2. **File Integrity**: Consider adding SHA-256 checksums
3. **Version Control**: Include version numbers in filename or separate file
4. **Access Control**: Consider rate limiting for large files
5. **CDN**: Use a CDN for better download performance

### APK Signing

Ensure your APK is properly signed:
- Use a release keystore (not debug)
- Keep your keystore secure
- Document the signing key for future updates

## Download Statistics (Optional)

To track downloads, you can:

1. Add a controller endpoint that logs downloads
2. Use analytics to track button clicks
3. Monitor server logs for `/downloads/auf-connect.apk` requests

Example controller:

```csharp
[HttpGet("api/download/apk")]
public IActionResult DownloadApk()
{
    // Log download
    _logger.LogInformation("APK downloaded at {Time}", DateTime.UtcNow);

    // Serve file
    var filePath = Path.Combine(_environment.WebRootPath, "downloads", "auf-connect.apk");
    return PhysicalFile(filePath, "application/vnd.android.package-archive", "auf-connect.apk");
}
```

## Troubleshooting

### Download Issues

**Problem**: File downloads as `.txt` or wrong extension
**Solution**: ✅ Fixed! MIME type is now properly configured in `Program.cs`

**Problem**: File not found (404)
**Solution**:
- Check file exists: `ls -la wwwroot/downloads/`
- Check file permissions: `chmod 644 auf-connect.apk`
- Check server logs

**Problem**: Slow downloads
**Solution**:
- Use a CDN
- Enable compression (not for APK files)
- Check server bandwidth

**Problem**: Browser blocks download
**Solution**:
- Serve over HTTPS
- Add proper headers
- Check browser security settings

## File Size Optimization

The current APK is 49 MB. To optimize:

1. **Enable ProGuard/R8** in Android build
2. **Remove unused resources**
3. **Use App Bundles** (AAB) for Play Store
4. **Split APKs** by architecture (arm64-v8a, armeabi-v7a, x86)
5. **Compress assets**

## Alternative Distribution

Consider these alternatives:

1. **Google Play Store**: Official distribution
2. **Firebase App Distribution**: For beta testing
3. **GitHub Releases**: For open source projects
4. **Direct Download**: Current method (this landing page)

## Testing the Download

Test the download functionality:

```bash
# Start the server
dotnet run

# Test download (in another terminal)
curl -I http://localhost:5000/downloads/auf-connect.apk

# Expected response:
# Content-Type: application/vnd.android.package-archive
# Content-Length: 51380224 (49MB)
```

## Support

For issues with downloading:
1. Check browser console for errors
2. Try a different browser
3. Check server logs
4. Verify file permissions
5. Test with curl/wget

---

**Last Updated**: October 2025
**APK Version**: 1.0.0
**APK Size**: 49 MB
**Min Android Version**: Check AndroidManifest.xml
