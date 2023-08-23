using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using UnityEngine.UI;

public class Lighting
{
    const string bufferName = "Lighting";

    private CullingResults cullingResults;

    private const int maxDirLightCount = 4;

    private Shadows shadows = new Shadows();

    private static int
        // dirLightColorId = Shader.PropertyToID("_DirectionalLightColor"),
        // dirLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");
        dirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
        dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors"),
        dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");

    private Vector4[]
        dirLightColors = new Vector4[maxDirLightCount],
        dirLightDirections = new Vector4[maxDirLightCount];

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    public void Setup(ScriptableRenderContext context,CullingResults cullingResults,ShadowSettings shadowSettings)
    {
        this.cullingResults = cullingResults;
        buffer.BeginSample(bufferName);
        shadows.Setup(context,cullingResults,shadowSettings);
        // SetupDirectionalLight();
        SetupLights();
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    void SetupLights()
    {
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;

        int dirLightCount = 0;
        
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];
            if (visibleLight.lightType == LightType.Directional)
            {
                SetupDirectionalLight(dirLightCount++,ref visibleLight);
                if (dirLightCount >= maxDirLightCount)
                {
                    break;
                }                
            }
        }
        buffer.SetGlobalInt(dirLightCountId,visibleLights.Length);
        buffer.SetGlobalVectorArray(dirLightColorsId,dirLightColors);
        buffer.SetGlobalVectorArray(dirLightDirectionsId,dirLightDirections);
    }
    
    void SetupDirectionalLight(int index,ref VisibleLight visibleLight)
    {
        dirLightColors[index] = visibleLight.finalColor;
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        shadows.ReserveDirectionalShadows(visibleLight.light,index);
    }
}