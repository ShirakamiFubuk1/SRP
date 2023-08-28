using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class Shadows
{
    private const string bufferName = "Shadows";

    private CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };
    
    private const int 
        maxShadowedDirectionalLightCount = 4,
        maxCascades = 4;

    private ScriptableRenderContext context;

    private CullingResults cullingResults;

    private ShadowSettings setting;

    private int ShadowedDirectionalLightCount;

    private static int
        dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas"),
        dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices"),
        cascadeCountId = Shader.PropertyToID("_CascadeCount"),
        cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres"),
        cascadeDataId = Shader.PropertyToID("_CascadeData"),
        //shadowDistanceId = Shader.PropertyToID("_ShadowDistance");
        shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade"),
        shadowAtlasSized = Shader.PropertyToID("_ShadowAtlasSize");

    private static Vector4[]
        cascadeCullingSpheres = new Vector4[maxCascades],
        cascadeData = new Vector4[maxCascades];

    private static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];
    
    private ShadowedDirectionalLight[] ShadowedDirectionalLights =
        new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];
    
    private static string[] directionalFilterKeywords =
    {
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7"
    };

    private static string[] cascadeBlendKeywords =
    {
        "_CASCADE_BLEND_SOFT",
        "_CASCADE_BLEND_DITHER"
    };
    
     struct ShadowedDirectionalLight
     {
         public int visibleLightIndex;
         public float slopeScaleBias;
         public float nearPlaneOffset;
     }

    public Vector4 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount 
            && light.shadows != LightShadows.None 
            && light.shadowStrength > 0f 
            && cullingResults.GetShadowCasterBounds(visibleLightIndex,out Bounds b))
        {
            ShadowedDirectionalLights[ShadowedDirectionalLightCount] = new ShadowedDirectionalLight
            {
                visibleLightIndex = visibleLightIndex,
                slopeScaleBias = light.shadowBias,
                nearPlaneOffset = light.shadowNearPlane
            };
            return new Vector3(
                light.shadowStrength, 
                setting.directional.cascadeCount * ShadowedDirectionalLightCount++,
                light.shadowNormalBias
                );
        }
        return Vector3.zero;
    }
    
    public void Setup(
        ScriptableRenderContext context,CullingResults cullingResults,ShadowSettings setting
    )
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.setting = setting;

        ShadowedDirectionalLightCount = 0;
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    public void Render()
    {
        if (ShadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
    }

    void SetKeywords(string[] keywords,int enabledIndex)
    {
        //int enableIndex = (int)setting.directional.filter - 1;
        for (int i = 0; i < keywords.Length; i++)
        {
            if (i == enabledIndex)
            {
                buffer.EnableShaderKeyword(keywords[i]);
            }
            else
            {
                buffer.DisableShaderKeyword(keywords[i]);
            }
        }
    }

    void RenderDirectionalShadows()
    {
        int atlasSize = (int)setting.directional.atlasSize;
        buffer.GetTemporaryRT(dirShadowAtlasId,atlasSize,atlasSize,32,FilterMode.Bilinear,RenderTextureFormat.Shadowmap);
        buffer.SetRenderTarget(dirShadowAtlasId,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
        buffer.ClearRenderTarget(true,false,Color.clear);
        buffer.BeginSample(bufferName);
        ExecuteBuffer();

        int tiles = ShadowedDirectionalLightCount * setting.directional.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;

        for (int i = 0; i < ShadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i,split,tileSize);
        }
        
        buffer.SetGlobalInt(cascadeCountId,setting.directional.cascadeCount);
        buffer.SetGlobalVectorArray(cascadeCullingSpheresId,cascadeCullingSpheres);
        buffer.SetGlobalVectorArray(cascadeDataId,cascadeData);
        //buffer.SetGlobalFloat(shadowDistanceId,setting.maxDistance);

        float f = 1f - setting.directional.cascadeFade;
        buffer.SetGlobalVector(shadowDistanceFadeId,new Vector4(1f/setting.maxDistance,1f/setting.distanceFade , 1f/(1f - f * f)));
        buffer.SetGlobalMatrixArray(dirShadowMatricesId,dirShadowMatrices);
        SetKeywords(directionalFilterKeywords,(int)setting.directional.filter - 1);
        SetKeywords(cascadeBlendKeywords,(int)setting.directional.cascadeBlend - 1);
        buffer.SetGlobalVector(shadowAtlasSized,new Vector4(atlasSize,1f/atlasSize));
        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

    void RenderDirectionalShadows(int index,int split, int tileSize)
    {
        ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
        var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);

        int cascadeCount = setting.directional.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = setting.directional.CascadeRatios;

        for (int i = 0; i < cascadeCount; i++)
        {
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, i,
                cascadeCount, ratios,
                tileSize, light.nearPlaneOffset, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
            shadowSettings.splitData = splitData;
            if (index == 0)
            {
                SetCascadeData(i, splitData.cullingSphere, tileSize);
            }
            int tileIndex = tileOffset + i;
            // SetTileViewport(index,split,tileSize);
            dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix,SetTileViewport(tileIndex,split,tileSize),split);
            buffer.SetViewProjectionMatrices(viewMatrix,projectionMatrix);
            
            //buffer.SetGlobalDepthBias(0f,3f);
            buffer.SetGlobalDepthBias(0f,light.slopeScaleBias);
            ExecuteBuffer();
            context.DrawShadows(ref shadowSettings);
            buffer.SetGlobalDepthBias(0f,0f);
            //buffer.SetGlobalDepthBias(0f,0f);
        }

    }

    void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
    {   
        float texelSize = 2f * cullingSphere.w / tileSize;
        float filterSize = texelSize * ((float)setting.directional.filter + 1f);
        cullingSphere.w -= filterSize;
        cullingSphere.w *= cullingSphere.w;
        cascadeCullingSpheres[index] = cullingSphere;
        cascadeData[index] = new Vector4(1f / cullingSphere.w, filterSize * 1.4142136f);
    }

    public void Cleanup()
    {
        if (ShadowedDirectionalLightCount > 0)
        {
            buffer.ReleaseTemporaryRT(dirShadowAtlasId);
            ExecuteBuffer();
        }
    }

    Vector2 SetTileViewport(int index, int split,float tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);
        buffer.SetViewport(new Rect(offset.x*tileSize,offset.y*tileSize,tileSize,tileSize));
        return offset;
    }

    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 matrix, Vector2 offset, int split)
    {
        if (SystemInfo.usesReversedZBuffer)
        {
            matrix.m20 = -matrix.m20;
            matrix.m21 = -matrix.m21;
            matrix.m22 = -matrix.m22;
            matrix.m23 = -matrix.m23;
        }
        
        float scale = 1f / split;
        matrix.m00 = (0.5f * (matrix.m00 + matrix.m30) + offset.x * matrix.m30) * scale;
        matrix.m01 = (0.5f * (matrix.m01 + matrix.m31) + offset.x * matrix.m31) * scale;
        matrix.m02 = (0.5f * (matrix.m02 + matrix.m32) + offset.x * matrix.m32) * scale;
        matrix.m03 = (0.5f * (matrix.m03 + matrix.m33) + offset.x * matrix.m33) * scale;
        matrix.m10 = (0.5f * (matrix.m10 + matrix.m30) + offset.y * matrix.m30) * scale;
        matrix.m11 = (0.5f * (matrix.m11 + matrix.m31) + offset.y * matrix.m31) * scale;
        matrix.m12 = (0.5f * (matrix.m12 + matrix.m32) + offset.y * matrix.m32) * scale;
        matrix.m13 = (0.5f * (matrix.m13 + matrix.m33) + offset.y * matrix.m33) * scale;
        matrix.m20 = 0.5f * (matrix.m20 + matrix.m30);
        matrix.m21 = 0.5f * (matrix.m21 + matrix.m31);
        matrix.m22 = 0.5f * (matrix.m22 + matrix.m32);
        matrix.m23 = 0.5f * (matrix.m23 + matrix.m33);
        
        return matrix;
    }
}