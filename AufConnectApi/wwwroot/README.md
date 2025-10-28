# AUF Connect Landing Page

This is the landing page for the AUF Connect Francophonie mobile application.

## Structure

```
wwwroot/
├── index.html          # Main landing page
├── css/
│   └── styles.css      # Styling
├── js/
│   └── main.js         # JavaScript functionality
├── images/             # App screenshots and images
│   └── app-screenshot.png (optional)
└── downloads/
    └── auf-connect.apk # Android APK file
```

## Setup

### 1. APK File
The Android APK file is already in place:
- ✅ **Location**: `wwwroot/downloads/auf-connect.apk`
- ✅ **Size**: 49 MB
- ✅ **MIME Type**: Properly configured as `application/vnd.android.package-archive`
- ✅ **Download**: Ready for users to download from the landing page

### 2. App Screenshot
The app screenshot is already included:
- ✅ `screenshot.png` - Main app screenshot displayed in the phone mockup
- Dimensions: 1206 x 2622 px
- File size: ~854KB

The screenshot will be automatically displayed inside the phone mockup on the landing page hero section.

### 3. Run the Application
```bash
dotnet run
```

The landing page will be available at:
- `http://localhost:5000` (or your configured port)
- `https://localhost:5001` (or your configured HTTPS port)

## Features

The landing page includes:

- **Hero Section** - Main banner with app branding and download CTA
- **Features Section** - Showcases 6 main app features:
  - Resources (Ressources)
  - Partners (Partenaires)
  - Calls (Appels à projets)
  - Events (Événements)
  - Members (Membres)
  - Projects (Projets)
- **About Section** - Information about AUF and OIF with statistics
- **Download Section** - APK download button with version info
- **Footer** - Links and legal information

## Customization

### Colors
Main colors are defined in CSS variables in `styles.css`:
- Primary: `#5B5FFF` (Blue/Purple)
- Primary Hover: `#4A4EE8`
- Primary Light: `#EEF0FF`

### Content
Edit `index.html` to update:
- Text content
- Links
- Statistics
- Footer information

### Styling
Modify `css/styles.css` to change:
- Layout
- Colors
- Typography
- Responsive breakpoints

## Browser Support

- Modern browsers (Chrome, Firefox, Safari, Edge)
- Mobile responsive design
- Smooth scrolling and animations

## Notes

- The landing page is served as a static website alongside your ASP.NET Core Web API
- Static files middleware is enabled in `Program.cs`
- Default files middleware serves `index.html` when accessing the root URL
