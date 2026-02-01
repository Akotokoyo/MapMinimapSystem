using UnityEngine;

[System.Serializable]
public class MapPOIData
{
    public string Name;
    public POIType Type;
    public Sprite Icon;
    public Color Color = Color.white;
    public Vector3 WorldPosition;
    public bool IsVisible;
    public float size = 32f;
}