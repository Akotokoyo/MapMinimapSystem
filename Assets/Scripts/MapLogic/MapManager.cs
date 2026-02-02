using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mouse = UnityEngine.InputSystem.Mouse;

public class MapManager : MonoBehaviour
{
    [SerializeField] private GameObject mapCanvas;
    [SerializeField] private GameObject mapImage;
    [SerializeField] private Image mapImageComponent;
    [SerializeField] private RectTransform playerMarker;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private MapConfig currentMapConfig;

    [SerializeField] private GameObject poiContainer;
    [SerializeField] private GameObject poiMarkerPrefab;


    private PlayerInput controls;
    public static bool isMapOpen = false;
    private readonly float[] zoomSteps = { 0.5f, 1f, 2f };
    private int currentZoomIndex = 1;
    private List<RectTransform> poiMarkers = new List<RectTransform>();

    [SerializeField] private float panSpeed = 500f;
    private Vector2 mapPanOffset = Vector2.zero;

    private RectTransform canvasRect;
    private RectTransform mapImageRect;

    void Awake()
    {
        controls = new PlayerInput();
        controls.PlayerMap.Map.performed += _ => ToggleMap();

        if (mapCanvas != null)
        {
            canvasRect = mapCanvas.GetComponent<RectTransform>();
        }

        if (mapImage != null)
        {
            mapImage.SetActive(false);
            mapImageRect = mapImage.GetComponent<RectTransform>();
        }

        if (currentMapConfig != null)
        {
            LoadMap(currentMapConfig);
        }
        else
        {
            ApplyCurrentZoom();
        }
    }

    void Update()
    {
        if (!isMapOpen)
        {
            return;
        }

        HandleZoomInput();
        HandleMapPanning();

        if (playerMarker != null && playerTransform != null && currentMapConfig != null)
        {
            UpdatePlayerMarkerPosition();
        }
    }

    void ToggleMap()
    {
        isMapOpen = !isMapOpen;

        if (mapImage != null)
        {
            mapImage.SetActive(isMapOpen);
        }

        if (isMapOpen)
        {
            CenterMapOnPlayer();
            ApplyCurrentZoom();
        }

    }

    void UpdatePlayerMarkerPosition()
    {
        if (currentMapConfig == null || playerTransform == null || playerMarker == null)
        {
            Debug.LogWarning($"Missing: CurrentMapConfig {currentMapConfig == null}, playerTransform {playerTransform == null}, playerMarker {playerMarker == null} parameter!");
            return;
        }

        Vector2 mapPosition = currentMapConfig.WorldToMapPosition(playerTransform.position);

        if (float.IsNaN(mapPosition.x) || float.IsNaN(mapPosition.y))
        {
            Debug.LogError($"Player position is NaN! Check worldSize and mapSize in MapConfig '{currentMapConfig.name}'");
            Debug.LogError($"WorldSize: {currentMapConfig.worldSize}, MapSize: {currentMapConfig.MapSize}");
            return;
        }

        playerMarker.anchoredPosition = mapPosition;
    }

    public void LoadMap(MapConfig config)
    {
        if (config == null)
        {
            return;
        }

        ClearPOIMarkers();
        int poiCount = config.poiList?.Count ?? 0;
        if (poiCount > 0)
        {
            poiMarkers.Capacity = poiCount;

            foreach (var poi in config.poiList)
            {
                if (!poi.IsVisible) continue;

                GameObject markerObj = Instantiate(poiMarkerPrefab, mapImage.transform);
                RectTransform marker = markerObj.GetComponent<RectTransform>();

                // Performance: Calculate position once for static POIs
                marker.anchoredPosition = config.WorldToMapPosition(poi.WorldPosition);
                marker.sizeDelta = new Vector2(poi.size, poi.size);

                Image markerImage = markerObj.GetComponent<Image>();
                if (markerImage != null)
                {
                    markerImage.sprite = poi.Icon;
                }

                poiMarkers.Add(marker);
            }
        }

        currentMapConfig = config;

        if (mapImageComponent != null && config.mapImage!= null)
        {
            mapImageComponent.sprite = config.mapImage;
        }

        if (mapImage != null)
        {
            mapImageRect.sizeDelta = config.MapSize;
        }

        ApplyCurrentZoom();

        Debug.Log($"Map loaded: {config.mapName}, Size: {config.MapSize.x}x{config.MapSize.y}");
    }

    void HandleZoomInput()
    {
        float scroll = Mouse.current?.scroll.ReadValue().y ?? 0f;
        if (Mathf.Abs(scroll) < 0.01f || zoomSteps.Length == 0)
        {
            return;
        }

        int direction = scroll > 0 ? 1 : -1;
        int newIndex = Mathf.Clamp(currentZoomIndex + direction, 0, zoomSteps.Length - 1);
        if (newIndex == currentZoomIndex)
        {
            return;
        }

        currentZoomIndex = newIndex;
        ApplyCurrentZoom();
    }

    void HandleMapPanning()
    {
        Vector2 moveInput = controls.PlayerMap.Move.ReadValue<Vector2>();

        if (moveInput.magnitude > 0.01f)
        {
            mapPanOffset -= moveInput * panSpeed * Time.deltaTime;
            ApplyPanning();
        }
    }

    void ApplyCurrentZoom()
    {
        if (mapImage == null || zoomSteps.Length == 0)
        {
            return;
        }

        currentZoomIndex = Mathf.Clamp(currentZoomIndex, 0, zoomSteps.Length - 1);
        float zoom = zoomSteps[currentZoomIndex];
        mapImageRect.localScale = new Vector3(zoom, zoom, 1f);

        ApplyPanning();
    }

    void ApplyPanning()
    {
        if (mapImage == null) return;

        Vector2 mapSize = currentMapConfig.MapSize * zoomSteps[currentZoomIndex];

        
        Vector2 viewportSize = canvasRect.rect.size;

        // Calculate max offset based on current zoom - allow panning when map is larger than viewport
        float maxX = Mathf.Max(0, (mapSize.x - viewportSize.x) * 0.5f);
        float maxY = Mathf.Max(0, (mapSize.y - viewportSize.y) * 0.5f);

        mapPanOffset.x = Mathf.Clamp(mapPanOffset.x, -maxX, maxX);
        mapPanOffset.y = Mathf.Clamp(mapPanOffset.y, -maxY, maxY);

        mapImageRect.anchoredPosition = mapPanOffset;
    }

    void CenterMapOnPlayer()
    {
        if (currentMapConfig == null || playerTransform == null) return;

        // Get player position on the map
        Vector2 playerMapPos = currentMapConfig.WorldToMapPosition(playerTransform.position);
        
        // Center the map on the player (invert the position)
        mapPanOffset = -playerMapPos;
    }

    void ClearPOIMarkers()
    {
        if (poiContainer != null)
        {
            Destroy(poiContainer);
        }

        poiContainer = new GameObject("POI Markers");
        poiContainer.transform.SetParent(mapImageRect, false);
        RectTransform rt = poiContainer.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    void OnEnable()
    {
        controls?.PlayerMap.Enable();
    }

    void OnDisable()
    {
        controls?.PlayerMap.Disable();
    }
}