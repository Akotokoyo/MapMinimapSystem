using UnityEngine;

[CreateAssetMenu(fileName = "NewMapConfig", menuName = "Generate Map Configuration")]
public class MapConfig: ScriptableObject
{
    [Header("Map Information")]
    public string mapName = "NewMap";
    public Sprite mapImage;

    [Tooltip("World ground size (ex: plane scala (3,1,3) = 30x30)")]
    public Vector2 worldSize = new Vector2(30, 30);
    [Tooltip("Center of the world (usually 0,0)")]
    public Vector2 worldCenter = new Vector2(0, 0);
    [Tooltip("Target map width in pixels (width is calculated automatically to maintain aspect ratio)")]    
    public float targetMapWidthScaling = 800f;
    public float markerSize = 32f;

    private Vector2 cachedMapSize;
    private bool mapSizeDirty = true;

    public Vector2 MapSize
    {
        get 
        {
            if (mapSizeDirty)
            {
                cachedMapSize = CalculateMapSize();
                mapSizeDirty = false;
            }
            return cachedMapSize;
        }
    }

    private Vector2 CalculateMapSize()
    {
        if(worldSize.y <= 0)
        {
            Debug.LogError($"WorldSize.y is <=0 in MapConfig {mapName}");
            return Vector2.one * targetMapWidthScaling;
        }

        float aspectRatio = worldSize.x / worldSize.y;
        float height = targetMapWidthScaling / aspectRatio;
        return new Vector2(targetMapWidthScaling, height);
    }

    private void OnValidate()
    {
        mapSizeDirty = true;
    }

    [ContextMenu("Show Calculated Map Size")]
    private void ShowCalculatedMapSize()
    {
        Vector2 calculatedSize = MapSize;
        float worldAspect = worldSize.x / worldSize.y;
        Debug.Log($"<b>{mapName}</b> - Map Size Info:");
        Debug.Log($"  World Size: {worldSize.x} x {worldSize.y}");
        Debug.Log($"  World Aspect Ratio: {worldAspect:F3} ({worldSize.x}:{worldSize.y})");
        Debug.Log($"  Target Width: {targetMapWidthScaling}px");
        Debug.Log($"  Calculated Map Size: {calculatedSize.x:F0} x {calculatedSize.y:F0} pixels");
        Debug.Log($"  Map Aspect Ratio: {(calculatedSize.x / calculatedSize.y):F3}");
    }

    public Vector2 WorldToMapPosition(Vector3 playerPositionInWorld){

        if(worldSize.y <= 0 || worldSize.x <= 0)
        {
            Debug.LogError($"WorldSize not valid! X={worldSize.x} Y={worldSize.y}");
            return Vector2.zero;
        }

        Vector2 calculatedMapSize = MapSize;
        if(calculatedMapSize.y <= 0 || calculatedMapSize.x <= 0)
        {
            Debug.LogError($"MapSize not valid! X={calculatedMapSize.x} Y={calculatedMapSize.y}");
            return Vector2.zero;
        }

        //UI Has a center at 0,0 we need to convert the player position to a percentage of the world size
        float playerPercentX = (playerPositionInWorld.x - (worldCenter.x - worldSize.x * 0.5f)) / worldSize.x;
        float playerPercentZ = (playerPositionInWorld.z - (worldCenter.y - worldSize.y * 0.5f)) / worldSize.y;

        float playerIconX = (playerPercentX -0.5f) * calculatedMapSize.x;
        float playerIconY = (playerPercentZ -0.5f) * calculatedMapSize.y;
        
        return new Vector2(playerIconX, playerIconY);
    }
}