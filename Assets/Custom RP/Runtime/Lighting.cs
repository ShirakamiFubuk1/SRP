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
        dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections"),
        dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

    private Vector4[]
        dirLightColors = new Vector4[maxDirLightCount],
        dirLightDirections = new Vector4[maxDirLightCount],
        dirLightShadowData = new Vector4[maxDirLightCount];

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
        shadows.Render();
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    void SetupLights()
    {
        //通过cullingResults的visibleLights属性检索所需要的数据
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;

        int dirLightCount = 0;
        
        //便利Lighting.SetupLighting中的所有可见光，并为每个元素调用SetupDirectionalLight
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];
            if (visibleLight.lightType == LightType.Directional)
            {
                //由于visibleLight结构非常大，故使用引用ref
                SetupDirectionalLight(dirLightCount++,ref visibleLight);
                if (dirLightCount >= maxDirLightCount)
                {
                    //仅支持四个方向光
                    break;
                }                
            }
        }
        
        //在缓冲区上调用SetGlobalXXX把数据发送到GPU
        buffer.SetGlobalInt(dirLightCountId,visibleLights.Length);
        buffer.SetGlobalVectorArray(dirLightColorsId,dirLightColors);
        buffer.SetGlobalVectorArray(dirLightDirectionsId,dirLightDirections);
        buffer.SetGlobalVectorArray(dirLightShadowDataId,dirLightShadowData);
    }
    
    void SetupDirectionalLight(int index,ref VisibleLight visibleLight)
    {
        dirLightColors[index] = visibleLight.finalColor;
        //前向矢量是矩阵的第三列,必须再次取反方向
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        dirLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light,index);
    }

    public void Cleanup()
    {
        shadows.Cleanup();
    }
}