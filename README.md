## M4Food

Cross-platform .NET MAUI application for the M4Food team.

About this app
--------------
M4Food is a cross-platform mobile app built with .NET MAUI that serves as a platform for people in need to claim free bread donations. Donors (stores or volunteers) can post available bread items with pickup details; users who need help can browse, reserve, and collect those free items. The app emphasizes simple pickup flows, reliable image upload to Cloudinary, and offline-capable local storage.

Key capabilities
----------------
- Browse stores and items, view item details and images  
- Add items to a local cart and checkout (order creation and basic order flow)  
- Register stores and upload store images (Cloudinary integration with direct-stream upload + fallback)  
- Local SQLite storage for stores, routes and image metadata for offline access  
- Offline map tile caching and offline route calculation utilities  

Tech stack
----------
- .NET 8 / .NET MAUI for cross-platform UI  
- C# with MVVM-friendly services and dependency injection (Microsoft.Extensions)  
- Firebase Authentication (Google Sign-In) for user auth  
- Cloudinary for image storage and delivery  
- SQLite for local storage / caching  
- Android tooling (adb/logcat) used for debugging and diagnostics

What this project achieves (E1 → E6)
------------------------------------
The project maps to the mandatory feature set (E1–E6) as defined in the course requirements:

- **E1 — Secure User Authentication System**  
  - Technical concepts covered: App class lifecycle, secure data storage (token handling), and connectivity.  
  - Implementation: Firebase Authentication with Google Sign-In and token handling flows.

- **E2 — MVVM-Driven Dashboard**  
  - Technical concepts covered: MVVM architecture, app lifecycle management, and data binding.  
  - Implementation: Pages and views wired to services via dependency injection, with binding-friendly view models and lifecycle-aware updates.

- **E3 — Location-Aware Service Feature**  
  - Technical concepts covered: Location services, navigation, and multimedia map integration.  
  - Implementation: Offline-capable route calculation and map tile caching utilities.

- **E4 — Interactive Data Submission**  
  - Technical concepts covered: Multimedia (camera/gallery) handling and data binding with validation.  
  - Implementation: Photo capture / picker flows and Cloudinary upload (direct-stream with safe fallback to temporary files).

- **E5 — Local Caching for Offline Use**  
  - Technical concepts covered: Data storage (SQLite / preferences) and app lifecycle-aware caching.  
  - Implementation: Local SQLite persistence for stores, routes and image metadata; cache management utilities.

- **E6 — Asynchronous Communication**  
  - Technical concepts covered: Notifications (push/local) and asynchronous messaging patterns (MessagingCenter / toasts).  
  - Implementation: Local notification helpers and background-safe handling for activity results and uploads.

### Development Environment
- .NET 8 SDK with the `android` workload installed
- Visual Studio 2022 17.8+ or VS Code with the MAUI extension
- Android SDK / Emulator (API 34)

### Getting Started (For Team Members)

After cloning the repository:

1. **Open the solution** (`M4Food.sln`) in Visual Studio 2022
2. **Wait for NuGet restore** - Visual Studio will automatically restore all packages (no manual download needed)
3. **Clean Solution** (optional, but recommended for first time)
   - Right-click solution → `Clean Solution`
4. **Rebuild Solution**
   - Right-click solution → `Rebuild Solution`
5. **Run the app** - Ready to develop!

**Note**: 
- All NuGet packages are defined in `M4Food.csproj` and will be automatically restored
- If you encounter build errors, try: Clean Solution → Rebuild Solution
- No manual NuGet package installation required

---

## Backend Implementation Status

### ✅ Completed Features

#### 1. Authentication & Registration
- **Google Sign-In Integration**
  - Firebase Authentication configured
  - Google Sign-In implemented and tested
  - User authentication flow ready

#### 2. Local Storage (SQLite)
- **Store Management**
  - Save, retrieve, update, and delete store information
  - Store data includes: ID, name, address, coordinates, description, phone
  - Offline-capable storage

- **Route Management**
  - Save and retrieve route data between locations
  - Route data includes: start/end points, distance, duration, path data (JSON)
  - Automatic caching for offline access

- **Image Metadata Storage**
  - Store image information (URLs, paths, dimensions)
  - Link images to stores
  - Track upload status

#### 3. Cloudinary Image Service
- **Image Upload**
  - Upload images to Cloudinary cloud storage
  - Get optimized image URLs
  - Delete images from cloud
  - Configuration completed and tested

#### 4. Offline Map Features
- **Map Tile Caching**
  - Download and cache map tiles for offline use
  - Manage cache size and cleanup
  - Support for OpenStreetMap tiles

- **Offline Route Calculation**
  - Calculate routes between two points without internet
  - Calculate routes between stores
  - Distance calculation using Haversine formula
  - Automatic route caching

### 📋 Available Services (Ready for Frontend Integration)

All services are registered in dependency injection and ready to use:

- `ILocalCacheService` - Local SQLite storage operations
- `ICloudinaryService` - Image upload and management
- `IMapTileCacheService` - Offline map tile caching
- `IOfflineRouteService` - Offline route calculation

### 🔄 Pending Frontend Implementation

The following features are **backend-ready** and **awaiting frontend UI implementation**:

1. **Store Management UI**
   - Display stores list
   - Create/edit store forms
   - Store details page
   - Map integration for store locations

2. **Route Display & Navigation**
   - Map view with route visualization
   - Route calculation UI
   - Navigation between stores
   - Offline route display

3. **Image Management UI**
   - Image picker/selector
   - Image upload interface
   - Image gallery for stores
   - Image preview and display

4. **Offline Map Display**
   - Map component integration
   - Tile loading from cache
   - Map tile download interface
   - Cache management UI

### 📚 Available Service Interfaces

All services are ready for frontend integration. Services can be injected via dependency injection:

**Local Storage Services:**
- `ILocalCacheService` - Store, route, and image data management
  - `SaveStoreAsync()`, `GetStoreAsync()`, `GetStoresAsync()`, `DeleteStoreAsync()`
  - `SaveRouteAsync()`, `GetRouteAsync()`, `GetRoutesAsync()`, `ClearOldRoutesAsync()`
  - `SaveImageAsync()`, `GetImageAsync()`, `GetImagesByStoreIdAsync()`, `DeleteImageAsync()`

**Cloudinary Services:**
- `ICloudinaryService` - Image upload and management
  - `UploadImageAsync()` - Upload image from file path
  - `UploadImageStreamAsync()` - Upload image from stream
  - `DeleteImageAsync()` - Delete image from cloud
  - `GetOptimizedUrl()` - Get optimized image URL with transformations

**Offline Map Services:**
- `IMapTileCacheService` - Offline map tile caching
  - `DownloadAndCacheTilesAsync()` - Download and cache map tiles for area
  - `GetTilePathAsync()` - Get cached tile file path
  - `IsTileCachedAsync()` - Check if tile is cached
  - `GetCacheSizeAsync()` - Get total cache size
  - `ClearOldTilesAsync()` - Clear old cached tiles

**Offline Route Services:**
- `IOfflineRouteService` - Offline route calculation
  - `CalculateRouteAsync()` - Calculate route between coordinates
  - `CalculateRouteBetweenStoresAsync()` - Calculate route between stores
  - `CalculateDistance()` - Calculate distance between points
  - `GenerateRoutePath()` - Generate route path data

---

### Known Issues

**Build fails with `System.IO.IOException` mentioning files like `classes.jar` being locked**
- Happens when cloned repositories still carry stale locks inside `bin` / `obj`.
- Close Visual Studio, emulators, and any `dotnet` / `msbuild` processes.
- Delete `M4Food/bin` and `M4Food/obj`.
- In Visual Studio run `Clean Solution` → `Rebuild Solution`.
- If it still fails, reboot or check antivirus/sync tools that may lock the directory.