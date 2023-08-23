using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    ScriptableRenderContext context;

    Camera camera;

    Lighting lighting = new Lighting();

    const string bufferName = "Render Camera";

    CommandBuffer buffer = new CommandBuffer()
    {
        name = bufferName
    };

    CullingResults cullingResults;

    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    
    static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");

    public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing,ShadowSettings shadowSettings)
    {
        this.camera = camera;
        this.context = context;

        PrepareBuffer();
        PrepareForSceneWindow();
        if (!Cull(shadowSettings.maxDistance))
        {
            return;
        }

        Setup();
        lighting.Setup(context,cullingResults,shadowSettings);
        DrawVisibleGeometry(useDynamicBatching,useGPUInstancing);
        DrawUnsupportedShaders();
        DrawGizmos();
        lighting.Cleanup();
        Submit();
    }

    void DrawVisibleGeometry(bool useDynamicBatching,bool useGPUInstancing)
    {
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        var drawingSettings = new DrawingSettings(
            unlitShaderTagId, sortingSettings
        )
        {
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing
        };
        drawingSettings.SetShaderPassName(1,litShaderTagId);
        
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );
        
        context.DrawSkybox(camera);

        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange=RenderQueueRange.transparent;

        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );
    }

    void Submit()
    {
        buffer.EndSample(SampleName);
        ExecuteBuffer();
        context.Submit();
    }

    void Setup()
    {
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;
        buffer.BeginSample(SampleName);
        buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth,
            flags == CameraClearFlags.Color,
            flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear
        );
        ExecuteBuffer();
        //context.SetupCameraProperties(camera);
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    bool Cull(float maxShadowDistance)
    {
        //ScriptableCullingParameters p;
        //当作为输出参数是，可以在参数列表内敛变量声明,即(out ScriptableCullingParameters p)
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            p.shadowDistance = Mathf.Min(camera.farClipPlane,maxShadowDistance);
            cullingResults = context.Cull(ref p);
            return true;
        }

        return false;
    }
}