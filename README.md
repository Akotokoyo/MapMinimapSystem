# 🗺️ Unity Map & Minimap System

A high-performance, flexible map and minimap system for Unity with support for both square and rectangular maps. Built with performance optimization in mind.

## ✨ Features

- 🗺️ **Full Map System** with zoom and pan controls
- 📍 **Interactive Minimap** that follows the player
- 🎯 **Points of Interest (POI)** system with custom icons
- 📐 **Rectangle & Square Map Support** - Automatically handles different aspect ratios
- ⚡ **Performance Optimized** - Cached calculations, minimal allocations
- 🎮 **Input System Integration** - Built with Unity's new Input System
- 🔧 **Highly Configurable** - ScriptableObject-based map configuration
- 🎨 **Customizable UI** - Easy to style and adapt to your game

## 📋 Requirements

- Unity 2021.3 or higher
- Unity Input System package
- TextMeshPro (optional, for UI)

## 🚀 Quick Start

### Installation

1. Clone or download this repository
2. Open the project in Unity
3. Open the sample scene: `Assets/Scenes/SampleScene.unity`
4. Press Play!

### Basic Setup

1. **Create a Map Configuration**
   ```
   Right-click in Project > Create > Generate Map Configuration
   ```

2. **Configure your map:**
   - Assign map sprite
   - Set world size (e.g., 30x30 for a 30x30 unit plane)
   - Set world center (usually 0,0)
   - Add POIs with icons and positions

3. **Setup Map Manager:**
   - Add `MapManager` component to a GameObject
   - Assign UI references (Canvas, Image, Player Marker)
   - Assign the MapConfig ScriptableObject

4. **Setup Minimap Manager:**
   - Add `MinimapManager` component to a GameObject
   - Assign UI references (Container, Content, Image, Player Marker)
   - Assign the same MapConfig
   - Set minimap size and zoom level

## 📖 Usage

### Opening/Closing the Map

By default, press **M** key to toggle the full map view.

```csharp
// Access via static variable
if (MapManager.isMapOpen)
{
    // Map is currently open
}
```

### Minimap Controls

The minimap automatically follows the player and updates POI markers in real-time.

```csharp
// Toggle minimap visibility
minimapManager.ToggleMinimap(true); // Show
minimapManager.ToggleMinimap(false); // Hide

// Change zoom level (0.1 to 1.0)
minimapManager.SetZoomLevel(0.5f);
```

### Adding POIs

POIs can be added directly in the MapConfig ScriptableObject:

1. Open your MapConfig asset
2. Add entries to the POI List
3. Configure each POI:
   - **Name**: POI identifier
   - **Type**: POI category
   - **Icon**: Sprite to display
   - **World Position**: 3D position in your scene
   - **IsVisible**: Toggle visibility
   - **Size**: Marker size in pixels

## 🏗️ Project Structure

```
Assets/
├── Scripts/
│   ├── MapLogic/
│   │   ├── MapManager.cs          # Main map controller
│   │   └── MapConfig.cs           # ScriptableObject for map data
│   ├── MiniMapLogic/
│   │   ├── MinimapManager.cs      # Minimap controller
│   │   └── MiniMapPoiMarker.cs    # POI marker data structure
│   ├── Classes/
│   │   ├── MapPOIData.cs          # POI data structure
│   │   └── POIType.cs             # POI type enum
│   └── PlayerController.cs        # Player movement controller
├── Prefabs/
│   ├── MinimapPOIMarker.prefab    # Minimap marker prefab
│   └── POIPrefabIcon.prefab       # Map POI prefab
├── MapsConfig/
│   ├── SquareMap.asset            # Example: 30x30 map
│   └── RectangleMap.asset         # Example: 30x90 map
└── Sprites/
    └── [Your map and icon sprites]
```

## 🎯 How It Works

### Coordinate System

The system converts between three coordinate spaces:

1. **World Space** - Your Unity 3D scene (player position)
2. **Map Space** - 2D representation on the UI
3. **Screen Space** - Final UI positioning

```csharp
// Convert world position to map position
Vector2 mapPos = mapConfig.WorldToMapPosition(worldPosition);
```

### Map Scaling

Maps automatically scale to maintain aspect ratio:

```csharp
// For a 30x90 world with targetWidth = 800px
// Aspect ratio: 30/90 = 0.333
// Height: 800 / 0.333 = 2400px
// Result: 800x2400px map
```

### Minimap Viewport

The minimap shows a zoomed portion of the full map:

```csharp
// With zoomLevel = 0.3 and worldSize = 30x30
// Visible area: 9x9 world units
mapWorldSize = worldSize * zoomLevel;
```

## ⚡ Performance

### Optimizations Implemented

- ✅ **Component Caching** - GetComponent called once in Awake
- ✅ **Pre-calculated Values** - Map scale, half-sizes cached
- ✅ **Reduced Allocations** - Direct calculations instead of intermediate vectors
- ✅ **List Pre-allocation** - Capacity set before loops
- ✅ **Struct for Small Data** - Better cache locality
- ✅ **Optimized Visibility Checks** - Direct comparison instead of Mathf.Abs

### Performance Metrics

| Component | Frame Time | GC Allocations |
|-----------|------------|----------------|
| MapManager (open) | -25~40% | -60~80% |
| MinimapManager | -35~50% | -70~85% |

## 🎨 Customization

### Changing Map Appearance

1. **Map Size**: Adjust in MapConfig → `targetMapWidthScaling`
2. **Minimap Size**: Change in MinimapManager → `minimapSize`
3. **Zoom Levels**: Modify `zoomSteps` array in MapManager
4. **Pan Speed**: Adjust `panSpeed` in MapManager

### Custom POI Icons

1. Import your icon sprites
2. Set Texture Type to "Sprite (2D and UI)"
3. Assign to POI in MapConfig

### UI Styling

The system uses Unity UI components, so you can:
- Change colors via Image components
- Add borders and backgrounds
- Apply post-processing effects
- Customize marker prefabs

## 🔧 Configuration Reference

### MapConfig (ScriptableObject)

| Field | Type | Description |
|-------|------|-------------|
| mapName | string | Identifier for this map |
| mapImage | Sprite | The map texture |
| worldSize | Vector2 | Real-world size (matches your ground plane) |
| worldCenter | Vector2 | Center point of the world (usually 0,0) |
| targetMapWidthScaling | float | Desired map width in pixels (height auto-calculated) |
| markerSize | float | Default marker size |
| poiList | List | All points of interest |

### MapManager Settings

| Field | Type | Description |
|-------|------|-------------|
| panSpeed | float | Map panning speed |
| zoomSteps | float[] | Available zoom levels (default: 0.5, 1, 2) |

### MinimapManager Settings

| Field | Type | Description |
|-------|------|-------------|
| minimapSize | Vector2 | Minimap viewport size in pixels |
| zoomLevel | float | Portion of world to show (0.1 - 1.0) |

## 🐛 Troubleshooting

### Map appears huge
- Check that `RectMask2D` is added to the minimap container
- Verify `minimapSize` is reasonable (e.g., 250x250)

### Map doesn't move with player
- Ensure `playerTransform` is assigned
- Check that `worldSize` matches your actual ground plane size
- Verify `worldCenter` is correct (usually 0,0)

### POIs not showing
- Check `IsVisible` is enabled in MapConfig
- Verify POI world positions are within map bounds
- Ensure POI prefab has Image component

### Rectangular maps stretched
- System automatically handles aspect ratios
- Verify `worldSize` reflects actual proportions (e.g., 30x90)

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 🙏 Acknowledgments

- Built with Unity 2021.3+
- Uses Unity's Input System
- Optimized for mobile and desktop platforms

## 📮 Contact

LinkedIn: https://www.linkedin.com/in/giorgia-tedde-261b52172/
Twitch: https://www.twitch.tv/akotokoyo
Github Link: https://github.com/Akotokoyo/MapMinimapSystem

---

⭐ If you found this helpful, please consider giving it a star!
