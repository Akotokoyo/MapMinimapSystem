using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject minimapContainer;
    [SerializeField] private RectTransform mapContent;
    [SerializeField] private Image minimapImage;
    [SerializeField] private RectTransform playerMarker;

    [Header("POI Settings")]
    [SerializeField] private Transform poiContainer;
    [SerializeField] private GameObject poiMarkerPrefab;

    [Header("World References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private MapConfig mapConfig;

    [Header("Minimap Settings")]
    [SerializeField] private Vector2 minimapSize = new Vector2(250f, 250f);
    [SerializeField] private float zoomLevel = 0.3f;

    private Vector2 mapWorldSize;
    private List<MiniMapPoiMarker> poiMarkers = new List<MiniMapPoiMarker>();
    private Vector2 mapScaleSize;
    private float mapScaleCached;

    private float halfMinimapWidth;
    private float halfMinimapHeight;

    void Start()
    {
        SetupMinimap();

        if (mapConfig != null)
        {
            LoadMap();
        }
        else
        {
            Debug.LogError("MapConfig non assegnato in MinimapManager!");
        }
    }

    void SetupMinimap()
    {
        // IMPORTANT: These RectTransform must be anchored to the center, not stretch!
        SetRectTransformToX(mapContent);
        SetRectTransformToX(poiContainer.GetComponent<RectTransform>());
        SetRectTransformToX(playerMarker);
    }

    void LoadMap()
    {
        if (minimapImage != null && mapConfig.mapImage != null)
        {
            minimapImage.sprite = mapConfig.mapImage;

            // IMPORTANT: Set the MiniMapImage Size equale to MapSize
            RectTransform imageRect = minimapImage.GetComponent<RectTransform>();
            if (imageRect != null)
            {
                imageRect.sizeDelta = mapConfig.MapSize;
                imageRect.anchoredPosition = Vector2.zero;
            }

            Debug.Log($"Minimappa caricata: {mapConfig.mapName}, dimensione immagine: {mapConfig.MapSize}");
        }

        // Calculate the part of the world to shows into the minimap
        // Ex: if worldSize = (30, 30) and zoomLevel = 0.3
        // mapWorldSize = (9, 9) -> shows 9x9 unit of the world
        mapWorldSize = mapConfig.worldSize * zoomLevel;

        ClearPOIMarkers();
        CreatePOIMarkers();

        Debug.Log($"Minimappa caricata: {mapConfig.mapName}, mostra {mapWorldSize.x}x{mapWorldSize.y} unità di mondo");
    }

    void RecalculateMapScale()
    {
        mapScaleSize = mapConfig.MapSize;
        mapScaleCached = minimapSize.x / (mapScaleSize.x * zoomLevel);
        halfMinimapWidth = minimapSize.x * 0.5f;
        halfMinimapHeight = minimapSize.y * 0.5f;
    }

    void ClearPOIMarkers()
    {
        foreach (var marker in poiMarkers)
        {
            if (marker.markerObject != null)
            {
                Destroy(marker.markerObject);
            }
        }
        poiMarkers.Clear();
    }

    void CreatePOIMarkers()
    {
        if (mapConfig == null || poiContainer == null || playerMarker == null)
        {
            Debug.LogWarning($"Missing: CurrentMapConfig {mapConfig == null}, poiContainer {poiContainer == null}, playerMarker {playerMarker == null} parameter!");
            return;
        }

        int visibleCount = 0;
        foreach (var poi in mapConfig.poiList)
        {
            if (!poi.IsVisible)
            {
                continue;
            }

            visibleCount++;

            GameObject markerObj = Instantiate(poiMarkerPrefab, poiContainer, false);
            markerObj.name = $"POI_{poi.Name}";
            markerObj.SetActive(true);

            RectTransform markerRect = markerObj.GetComponent<RectTransform>();
            Image markerImage = markerObj.GetComponent<Image>();

            markerRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);

            if (markerImage != null && poi.Icon != null)
            {
                    markerImage.sprite = poi.Icon;
            }

            poiMarkers.Add(new MiniMapPoiMarker
            {
                markerObject = markerObj,
                markerRect = markerRect,
                worldPosition = poi.WorldPosition
            });
        }
    }

    void LateUpdate()
    {
        if (playerTransform == null || mapConfig == null)
            return;

        RecalculateMapScale();
        UpdateMinimapPosition();
        UpdatePOIMarkers();
    }

    void UpdateMinimapPosition()
    {
        Vector3 playerWorldPos = playerTransform.position;

        Vector2 playerPosOnMap = mapConfig.WorldToMapPosition(playerWorldPos);

        mapContent.localScale = Vector3.one * mapScaleCached;

        float offsetX = -playerPosOnMap.x * mapScaleCached;
        float offsetY = -playerPosOnMap.y * mapScaleCached;

        mapContent.anchoredPosition = new Vector2(offsetX, offsetY);
    }

    void UpdatePOIMarkers()
    {
        Vector2 playerPosOnMap = mapConfig.WorldToMapPosition(playerTransform.position);

        foreach (var marker in poiMarkers)
        {
            if (marker.markerRect == null) continue;

            Vector2 poiPosOnMap = mapConfig.WorldToMapPosition(marker.worldPosition);

            // IMPORTANT: Calculate the player relative position
            float relativePosX = (poiPosOnMap.x - playerPosOnMap.x) * mapScaleCached;
            float relativePosY = (poiPosOnMap.y - playerPosOnMap.y) * mapScaleCached;

            marker.markerRect.anchoredPosition = new Vector2(relativePosX, relativePosY);

            bool isVisible = relativePosX >= -halfMinimapWidth && relativePosX <= halfMinimapWidth &&
                             relativePosY >= -halfMinimapHeight && relativePosY <= halfMinimapHeight;

            marker.markerObject.SetActive(isVisible);
        }
    }

    public void SetZoomLevel(float newZoom)
    {
        zoomLevel = Mathf.Clamp(newZoom, 0.1f, 1f);
        if (mapConfig != null)
        {
            mapWorldSize = mapConfig.worldSize * zoomLevel;
        }
    }

    public void ToggleMinimap(bool show)
    {
        if (minimapContainer != null)
        {
            minimapContainer.SetActive(show);
        }
    }

    private void SetRectTransformToX(RectTransform content)
    {
        if (content != null)
        {
            content.anchorMin = new Vector2(0.5f, 0.5f);
            content.anchorMax = new Vector2(0.5f, 0.5f);
            content.pivot = new Vector2(0.5f, 0.5f);
            content.anchoredPosition = Vector2.zero;
        }
    }
}