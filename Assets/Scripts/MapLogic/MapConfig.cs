using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMapConfig", menuName = "Generate Map Configuration")]
public class MapConfig: ScriptableObject
{
    [Header("Map Information")]
    public string mapName = "NewMap";
    public Sprite mapImage;

    public Vector2 worldSize = new Vector2(30f, 30f);
    public Vector2 worldCenter = Vector2.zero;
    public float targetMapHeightScaling = 800f;

    public List<MapPOIData> poiList;

    public Vector2 MapSize
    {
        get
        {
            if (worldSize.y <= 0)
            {
                Debug.LogWarning($"WorldSize.y is <= 0 in MapConfig '{mapName}', returning square map");
                return Vector2.one * targetMapHeightScaling;
            }

            float aspectRatio = worldSize.x / worldSize.y;
            float height = targetMapHeightScaling / aspectRatio;
            return new Vector2(targetMapHeightScaling, height);
        }
    }

    [ContextMenu("Show Calculated Map Size")]
    private void ShowCalculatedMapSize()
    {
#if UNITY_EDITOR
        Vector2 calculatedSize = MapSize;
        float worldAspect = worldSize.x / worldSize.y;
        Debug.Log($"<b>{mapName}</b> - Map Size Info:");
        Debug.Log($"  World Size: {worldSize.x} x {worldSize.y}");
        Debug.Log($"  World Aspect Ratio: {worldAspect:F3} ({worldSize.x}:{worldSize.y})");
        Debug.Log($"  Calculated Map Size: {calculatedSize.x:F0} x {calculatedSize.y:F0} pixels");
        Debug.Log($"  Map Aspect Ratio: {(calculatedSize.x / calculatedSize.y):F3}");
#endif
    }

    public Vector2 WorldToMapPosition(Vector3 playerPositionInWorld)
    {
        if (worldSize.x <= 0 || worldSize.y <= 0)
        {
            Debug.LogError($"WorldSize not valid! X={worldSize.x}, Y={worldSize.y}. in MapConfig {mapName}!");
            return Vector2.zero;
        }

        Vector2 calculatedMapSize = MapSize;
        if (calculatedMapSize.x <= 0 || calculatedMapSize.y <= 0)
        {
            Debug.LogError($"MapSize not valid! X={calculatedMapSize.x}, Y={calculatedMapSize.y}. In MapConfig {mapName}!");
            return Vector2.zero;
        }

        //UI Has a center at 0,0 we need to convert the player position to a percentage of the world size
        float playerPercentX = (playerPositionInWorld.x - (worldCenter.x - worldSize.x * 0.5f)) / worldSize.x;
        float playerPercentZ = (playerPositionInWorld.z - (worldCenter.y - worldSize.y * 0.5f)) / worldSize.y;

        float playerIconX = (playerPercentX - 0.5f) * calculatedMapSize.x;
        float playerIconY = (playerPercentZ - 0.5f) * calculatedMapSize.y;

        return new Vector2(playerIconX, playerIconY);
    }
}