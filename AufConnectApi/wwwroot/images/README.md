# Images Folder - AUF Connect

This folder contains images for the AUF Connect landing page.

## Required Images

### screenshot.png
**Purpose**: App screenshot displayed inside the phone mockup on the hero section

**Specifications**:
- **Dimensions**: 1080 x 2340px (18.5:9 ratio - modern phone aspect ratio)
- **Format**: PNG (preferred) or JPG
- **File size**: < 500KB (optimized for web)
- **Content**: Should show the main screen of your AUF Connect app

**Recommended screens to capture**:
- Home screen with the main navigation
- Resources section
- Partners section
- Or the landing screen with the "Francophonie" branding

### How to Add Your Screenshot

1. **Take a screenshot** from your Android app:
   - Open your AUF Connect app
   - Navigate to the main screen you want to showcase
   - Take a screenshot (Power + Volume Down on most Android devices)

2. **Optimize the image** (optional but recommended):
   - Use an online tool like TinyPNG (https://tinypng.com)
   - Or use ImageOptim (Mac) / FileOptimizer (Windows)
   - Target file size: 200-500KB

3. **Add to project**:
   ```bash
   # Copy your screenshot to this folder
   cp /path/to/your/screenshot.png ./screenshot.png
   ```

4. **Test**:
   - Run `dotnet run`
   - Visit the landing page
   - The screenshot should appear inside the phone mockup

## Image Display

The screenshot will be displayed:
- ✅ Inside a realistic phone mockup
- ✅ With proper aspect ratio (cover fit)
- ✅ Centered and cropped to fit
- ✅ With a gradient fallback if image fails to load

## Fallback

If the image is not found or fails to load:
- A gradient background will be shown (blue to purple)
- "AUF Connect" text will be displayed in the center

## Additional Images (Optional)

You can add more images for future enhancements:

### app-icon.png
- App icon for favicon or branding
- Size: 512 x 512px

### og-image.png
- Open Graph image for social media sharing
- Size: 1200 x 630px

### feature-screenshots/
- Additional screenshots for a gallery section
- Size: 1080 x 2340px each

## Image Optimization Tips

1. **Use PNG for screenshots** with text (better quality)
2. **Use JPG for photos** (smaller file size)
3. **Compress images** before uploading (use TinyPNG or similar)
4. **Use WebP format** for modern browsers (optional)
5. **Provide 2x versions** for Retina displays (optional)

## Example File Structure

```
images/
├── screenshot.png          # Main app screenshot (REQUIRED)
├── app-icon.png           # Optional
├── og-image.png           # Optional
└── feature-screenshots/   # Optional
    ├── home.png
    ├── resources.png
    └── partners.png
```

## Need Help?

If your screenshot doesn't appear:
1. Check the file name is exactly `screenshot.png`
2. Check the file is in the correct folder (`wwwroot/images/`)
3. Check file permissions (should be readable)
4. Clear browser cache and refresh
5. Check browser console for errors

---

**Last Updated**: October 2025
