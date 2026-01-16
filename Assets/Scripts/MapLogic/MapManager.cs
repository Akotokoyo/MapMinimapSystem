using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    [Header("Map UI")]
    [SerializeField] private GameObject map;
    [SerializeField] private RectTransform mapViewport;
    [SerializeField] private Sprite mapImage;
    [SerializeField] private RectTransform playerMarker;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private MapConfig mapConfig;

    [Header("POI System")]
    [SerializeField] private GameObject poiContainer;
    [SerializeField] private GameObject poiMarkerPrefab;
    [SerializeField] private float poiUpdateInterval = 0.1f;
    
    [SerializeField] private float panSpeed = 500f;    
    private Dictionary<MapPOI, RectTransform> poiToMarkerMap = new Dictionary<MapPOI, RectTransform>();
    private List<MapPOI> dynamicPOIs = new List<MapPOI>();
    private float poiUpdateTimer = 0f;
    
    private PlayerInput controls;
    public static bool isMapOpen = false;
    private readonly float[] zoomLevels = { 0.5f, 1f ,2f };
    private int currentZoomIndex = 1;
    private Vector2 mapPanOffset = Vector2.zero;

    private RectTransform mapRectTransform;
    private Image mapImageComponent;

#region Unity Methods
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple MapManagers in scene! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        if (map != null) {
            mapRectTransform = map.GetComponent<RectTransform>();
            mapImageComponent = map.GetComponent<Image>();
            map.SetActive(false);
        }

        controls = new PlayerInput();
        controls.PlayerMap.Map.performed += OnMapToggle; 
        
        if (mapConfig != null)
        {
            LoadMapConfiguration(mapConfig);
        }
        else
        {
            ApplyCurrentZoom();
        }

        InitializePOIContainer();
    }

    void Update()
    {
        if (!isMapOpen) return;

        HandleZoomInput();
        HandleMapPanning();

        if (playerMarker != null && 
           playerTransform != null && 
           mapConfig != null) 
        {
            UpdatePlayerMarker();
        }

        if (dynamicPOIs.Count > 0)
        {
            poiUpdateTimer += Time.deltaTime;
            if (poiUpdateTimer >= poiUpdateInterval)
            {
                poiUpdateTimer = 0f;
                UpdateDynamicPOIMarkers();
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (controls != null)
        {
            controls.PlayerMap.Map.performed -= OnMapToggle;
            controls.Dispose();
        }
    }

    private void OnValidate()
    {
        if (poiMarkerPrefab != null && poiMarkerPrefab.GetComponent<RectTransform>() == null)
        {
            Debug.LogError("POI Marker Prefab must have a RectTransform component!");
        }
        
        if (poiMarkerPrefab != null && poiMarkerPrefab.GetComponent<Image>() == null)
        {
            Debug.LogWarning("POI Marker Prefab should have an Image component for the icon.");
        }
    }
#endregion

#region Public Methods

    public void LoadMapConfiguration(MapConfig config)
    {
        if (config == null)
        {
            Debug.LogWarning("MapConfig is null!");
            return;
        }

        mapConfig = config;

        if (mapImageComponent != null && config.mapImage != null)
        {
            mapImage = config.mapImage;
            mapImageComponent.sprite = config.mapImage;
        }
        
        if (mapRectTransform != null)
        {
            mapRectTransform.sizeDelta = config.MapSize;
        }

        ApplyCurrentZoom();
        
        Debug.Log($"Map '{config.mapName}' loaded. Size: {config.MapSize}");
    }

    public void RegisterPOI(MapPOI poi)
    {
        if (poi == null)
        {
            Debug.LogWarning("Trying to register null POI!");
            return;
        }

        if (poiToMarkerMap.ContainsKey(poi))
        {
            Debug.LogWarning($"POI '{poi.poiName}' is already registered!");
            return;
        }

        CreatePOIMarker(poi);

        if (poi.trackPosition && !dynamicPOIs.Contains(poi))
        {
            dynamicPOIs.Add(poi);
        }
    }

    public void UnregisterPOI(MapPOI poi)
    {
        if (poi == null || !poiToMarkerMap.ContainsKey(poi))
            return;

        if (poiToMarkerMap.TryGetValue(poi, out RectTransform marker))
        {
            if (marker != null)
            {
                Destroy(marker.gameObject);
            }
            poiToMarkerMap.Remove(poi);
        }

        dynamicPOIs.Remove(poi);
    }

    public void UpdatePOIVisibility(MapPOI poi, bool visible)
    {
        if (poiToMarkerMap.TryGetValue(poi, out RectTransform marker))
        {
            if (marker != null)
            {
                marker.gameObject.SetActive(visible);
            }
        }
    }

#endregion

#region Private Methods

    private void OnMapToggle(InputAction.CallbackContext ctx)
    {
        ToggleMap();
    }

    private void ToggleMap(){
        isMapOpen = !isMapOpen;

        if(mapImage != null) map.SetActive(isMapOpen);

        if(isMapOpen) {
            CenterMapOnPlayer();
            ApplyCurrentZoom();
        }
    }

    private void CenterMapOnPlayer()
    {
        if (playerTransform == null || mapConfig == null) return;

        Vector2 playerMapPos = mapConfig.WorldToMapPosition(playerTransform.position);
        mapPanOffset = -playerMapPos;
    }

    private void UpdatePlayerMarker(){
        if (mapConfig == null)
        {
            Debug.LogWarning("mapConfig is null!");
            return;
        }
        
        if (playerTransform == null)
        {
            Debug.LogWarning("PlayerTransform not assigned!");
            return;
        }
        
        if (playerMarker == null)
        {
            Debug.LogWarning("PlayerMarker not assigned!");
            return;
        }

        Vector2 mapPosition = mapConfig.WorldToMapPosition(playerTransform.position);

        if (float.IsNaN(mapPosition.x) || float.IsNaN(mapPosition.y))
        {
            Debug.LogError($"Player position is NaN! Check worldSize and mapSize in MapConfig '{mapConfig.name}'");
            Debug.LogError($"WorldSize: {mapConfig.worldSize}, MapSize: {mapConfig.MapSize}");
            return;
        }

        playerMarker.anchoredPosition = mapPosition;
        playerMarker.sizeDelta = new Vector2(mapConfig.markerSize, mapConfig.markerSize);
    }

    void HandleZoomInput(){
        float scroll = Mouse.current?.scroll.ReadValue().y ?? 0f;
        if(Mathf.Abs(scroll) < 0.01f || zoomLevels.Length == 0) return;

        int direction = scroll > 0 ? 1 : -1;
        int newIndex = Mathf.Clamp(currentZoomIndex + direction, 0, zoomLevels.Length -1);

        if (newIndex == currentZoomIndex)
        {
            return;
        }

        currentZoomIndex = newIndex;
        ApplyCurrentZoom();
    }

    void HandleMapPanning(){
        Vector2 moveInput = controls.PlayerMap.Move.ReadValue<Vector2>();
        
        if (moveInput.magnitude > 0.01f)
        {
            mapPanOffset -= moveInput * panSpeed * Time.deltaTime;
            ApplyPanning();
        }
    }

    void ApplyCurrentZoom()
    {
        if (mapImage == null || zoomLevels.Length == 0)
        {
            return;
        }

        currentZoomIndex = Mathf.Clamp(currentZoomIndex, 0, zoomLevels.Length - 1);
        float zoom = zoomLevels[currentZoomIndex];
        mapRectTransform.localScale = Vector3.one * zoom;

        ApplyPanning();
    }

    void ApplyPanning()
    {
        if (mapImage == null || mapRectTransform == null) return;

        Vector2 mapSize = mapConfig.MapSize * zoomLevels[currentZoomIndex];
        
        Vector2 viewportSize;
        if (mapViewport != null)
        {
            viewportSize = mapViewport.rect.size;
        }
        else
        {
            Canvas canvas = map.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                viewportSize = canvasRect.rect.size;
            }
            else
            {
                viewportSize = new Vector2(Screen.width, Screen.height);
            }
        }

        float maxX = Mathf.Max(0, (mapSize.x - viewportSize.x) * 0.5f);
        float maxY = Mathf.Max(0, (mapSize.y - viewportSize.y) * 0.5f);

        mapPanOffset.x = Mathf.Clamp(mapPanOffset.x, -maxX, maxX);
        mapPanOffset.y = Mathf.Clamp(mapPanOffset.y, -maxY, maxY);

        mapRectTransform.anchoredPosition = mapPanOffset;
    }

    void InitializePOIContainer()
    {
        if (poiContainer == null && mapRectTransform != null)
        {
            poiContainer = new GameObject("POIContainer");
            poiContainer.transform.SetParent(mapRectTransform, false);
            RectTransform rt = poiContainer.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }
    }

    void CreatePOIMarker(MapPOI poi)
    {
        if (poi == null || !poi.isVisible || mapConfig == null || poiMarkerPrefab == null)
            return;

        if (poiContainer == null)
        {
            InitializePOIContainer();
        }

        GameObject markerObj = Instantiate(poiMarkerPrefab, poiContainer.transform);
        RectTransform marker = markerObj.GetComponent<RectTransform>();
        
        if (marker == null)
        {
            Debug.LogError("POI marker prefab doesn't have a RectTransform!");
            Destroy(markerObj);
            return;
        }

        Vector2 mapPos = mapConfig.WorldToMapPosition(poi.transform.position);
        marker.anchoredPosition = mapPos;
        marker.sizeDelta = new Vector2(mapConfig.markerSize, mapConfig.markerSize);
        
        Image markerImage = marker.GetComponent<Image>();
        if (markerImage != null && poi.poiSprite != null)
        {
            markerImage.sprite = poi.poiSprite;
        }

        poiToMarkerMap[poi] = marker;

        marker.name = $"POI_{poi.poiName}";
    }

    void UpdateDynamicPOIMarkers()
    {
        if (mapConfig == null) return;

        for (int i = dynamicPOIs.Count - 1; i >= 0; i--)
        {
            MapPOI poi = dynamicPOIs[i];
            
            if (poi == null)
            {
                dynamicPOIs.RemoveAt(i);
                continue;
            }
            
            if (poiToMarkerMap.TryGetValue(poi, out RectTransform marker) && marker != null)
            {
                Vector2 newMapPos = mapConfig.WorldToMapPosition(poi.transform.position);
                marker.anchoredPosition = newMapPos;
            }
        }
    }

    void OnEnable()
    {
        controls?.PlayerMap.Enable();
    }

    void OnDisable()
    {
        controls?.PlayerMap.Disable();
    }
#endregion
}
