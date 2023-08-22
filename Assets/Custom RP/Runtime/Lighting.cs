using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

public class Lighting
{
    const string bufferName = "Lighting";

    private CullingResults cullingResults;

    static int 
        dirLightColorId = Shader.PropertyToID("_DirectionalLightColor"),
        dirLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    public void Setup(ScriptableRenderContext context,CullingResults cullingResults)
    {
        this.cullingResults = cullingResults;
        buffer.BeginSample(bufferName);
        // SetupDirectionalLight();
        SetupLights();
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    void SetupLights()
    {
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
    }
    
    void SetupDirectionalLight()
    {
        Light light = RenderSettings.sun;
        buffer.SetGlobalVector(dirLightColorId,light.color.linear * light.intensity);
        buffer.SetGlobalVector(dirLightDirectionId,-light.transform.forward);
    }
}