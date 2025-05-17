using UnityEngine;
using UnityEngine.Rendering;
[RequireComponent(typeof(Light))]
public class LightController : MonoBehaviour
{
    public bool enableRSM = true;
    public int rsmResolution = 1024;
    public float intensity = 1.0f;
    public Color lightColor = Color.white;
    
    public RenderTexture rsmPosition { get; private set; }
    public RenderTexture rsmNormal { get; private set; }
    public RenderTexture rsmFlux { get; private set; }
    public RenderTexture rsmDepth { get; private set; }
    public enum LightType{
        directional,
        point,
        spot
    }
    public LightType lightType{get;private set;}
    void Awake()
    {
        var light = GetComponent<Light>();
        if (light != null)
            lightColor = light.color;
    }
    public void CreateRSMTextures()
    {
        ReleaseRSMTextures();
        rsmPosition = new RenderTexture(rsmResolution, rsmResolution, 0, RenderTextureFormat.ARGBFloat);
        rsmPosition.name = "RSM_Position_" + name;
        rsmNormal = new RenderTexture(rsmResolution, rsmResolution, 0, RenderTextureFormat.ARGB2101010);
        rsmNormal.name = "RSM_Normal_" + name;
        rsmFlux = new RenderTexture(rsmResolution, rsmResolution, 0, RenderTextureFormat.ARGBFloat);
        rsmFlux.name = "RSM_Flux_" + name;
        rsmDepth = new RenderTexture(rsmResolution, rsmResolution, 0, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        rsmDepth.name = "RSM_Depth_" + name;
        Debug.Log("Create RSM Textures for light: " + name);
    }

    public void ReleaseRSMTextures()
    {
        if (rsmPosition != null) rsmPosition.Release();
        if (rsmNormal != null) rsmNormal.Release();
        if (rsmFlux != null) rsmFlux.Release();
        if (rsmDepth != null) rsmDepth.Release();
    }

    private void OnDestroy()
    {
        ReleaseRSMTextures();
    }
}