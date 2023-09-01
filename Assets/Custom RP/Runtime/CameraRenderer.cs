using UnityEngine;
using UnityEngine.Rendering;

//每个相机的渲染都是独立的.与其让__CustomRenderPipeline__渲染所有摄像机,倒不如用一个专门用来渲染单个摄像机的新类名为CameraRenderer
//并给他一个带有上下文和相机参数的公开方法.
public partial class CameraRenderer
{
    ScriptableRenderContext context;

    Camera camera;

    Lighting lighting = new Lighting();

    //上下文会延迟实际的渲染，直到我们提交他为止。在此之前，我们会对其进行配置并添加命令以供后续的执行。
    //某些任务(如绘制天空盒)提供了专属方法,但其他命令必须通关单独的命令缓冲区(command buffer)间接执行。
    //为了获得缓冲区，创建一个CommandBuffer对象实例.
    const string bufferName = "Render Camera";

    CommandBuffer buffer = new CommandBuffer()
    {
        name = bufferName
    };

    CullingResults cullingResults;

    static ShaderTagId 
        unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"), 
        //添加lit shader
        litShaderTagId = new ShaderTagId("CustomLit");
    
    public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing,ShadowSettings shadowSettings)
    {
        this.camera = camera;
        this.context = context;
        
        //使Frame Debugger支持多相机
        PrepareBuffer();
        //支持UI渲染
        PrepareForSceneWindow();
        
        //在Setup之前调用Cull，实际上是通过调用上下文的Cull来完成的，会产生一个CullingResults结构。
        if (!Cull(shadowSettings.maxDistance))
        {
            return;
        }
        
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
        lighting.Setup(context,cullingResults,shadowSettings);
        buffer.EndSample(SampleName);
        Setup();
        
        //CameraRenderer.Render的工作是绘制相机所能看到的所有几何图形。为了画质清晰，在单独的DrawVisibleGeometry方法中执行这个特定的工作任务。
        //第一步先绘制默认的Skybox，通过是用摄像机作为参数在上下文中调用DrawSkybox来完成。
        DrawVisibleGeometry(useDynamicBatching,useGPUInstancing);
        //用于支持管线不支持的其他着色器
        DrawUnsupportedShaders();
        //最后绘制Gizmos
        DrawGizmos();
        lighting.Cleanup();
        Submit();
    }

    void DrawVisibleGeometry(bool useDynamicBatching,bool useGPUInstancing)
    {
        //排序,用于确定是正交还是透视
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
        
        //指出那些Render队列是允许的.
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        //通过调用DrawRenderers作为参数来渲染
        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );
        
        //画天空盒
        context.DrawSkybox(camera);

        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange=RenderQueueRange.transparent;

        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );
    }

    //通过在上下文调用Submit来提交排队的工作才会执行。
    void Submit()
    {
        //使用统一的SampleName
        buffer.EndSample(SampleName);
        ExecuteBuffer();
        context.Submit();
    }

    void Setup()
    {
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;
        //使用命令缓冲区给Profiler注入样本，使其显示在Profiler和Frame Debugger
        buffer.BeginSample(SampleName);
        //清除之前RT绘制的内容，以消除就内容public void ClearRenderTarget(bool clearDepth, bool clearColor, Color backgroundColor)
        buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth,
            flags == CameraClearFlags.Color,
            flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear
        );
        ExecuteBuffer();
        //context.SetupCameraProperties(camera);
    }

    //执行缓冲区，从缓冲区调用（复制一份但不会清除），之后再调用Clear。
    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    bool Cull(float maxShadowDistance)
    {
        //ScriptableCullingParameters p;
        //当作为输出参数，可以在参数列表内敛变量声明,即(out ScriptableCullingParameters p)
        //Out关键字负责设置正确的参数，替换以前的值
        //裁剪看不见的物体
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            p.shadowDistance = Mathf.Min(camera.farClipPlane,maxShadowDistance);
            cullingResults = context.Cull(ref p);
            return true;
        }

        return false;
    }
}