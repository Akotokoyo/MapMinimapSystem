using UnityEngine;

public class MapPOI : MonoBehaviour 
{
    public string poiName = "New POI";
    public Sprite poiSprite;
    public POIType poiType = POIType.Other;    
    public bool isVisible = true;
    
    [Tooltip("If true, the marker position updates every frame (used for moving NPCs)")]
    public bool trackPosition = true;

    private void Start()
    {
        if (MapManager.Instance != null) MapManager.Instance.RegisterPOI(this);
        else Debug.LogWarning($"MapPOI '{poiName}': MapManager not found in scene!");
    }

    private void OnDestroy()
    {
        if (MapManager.Instance != null)
        {
            MapManager.Instance.UnregisterPOI(this);
        }
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        MapManager.Instance?.UpdatePOIVisibility(this, visible);
    }
}