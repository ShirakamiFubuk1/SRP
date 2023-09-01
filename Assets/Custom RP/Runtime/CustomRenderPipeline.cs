using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class CustomRenderPipeline : RenderPipeline//资产返回RP实例所使用的类型,必须继承RenderPipeline
{
    //camera render相当于通用RP的scriptalbe renderer。这种方法能让每个相机更容易支持不同的渲染方式。如一个渲染第一人称试图，一个渲染三维地图等。但现在会用同样的方式渲染所有摄像机
    CameraRenderer renderer = new CameraRenderer();

    bool useDynamicBatching, useGPUInstancing;

    private ShadowSettings shadowSettings;

    public CustomRenderPipeline(bool useDynamicBatching,bool useGPUInstancing,bool useSRPBatcher,ShadowSettings shadowSettings)
    {
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        this.shadowSettings = shadowSettings;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
    }
    
    //RenderPipeline定义了一个受保护的抽象Render方法,重写这个方法来创建一个具体的管线.
    //每一帧Unity都会调用RP实例的Render方法.他传递一个上下文结构,该结构会提供一个到当前引擎的链接,我们可以用它来渲染.它需要传递一个相机数组,因为可以有多个活动相机.根据摄像机顺序进行渲染.
    //让CustomRenderPipeline在初始化的时候，创建一个CameraRenderer实例，然后遍历所有相机
    protected override void Render(ScriptableRenderContext context,Camera[] cameras)
    {
        foreach (Camera camera in cameras)
        {
            renderer.Render(context,camera,useDynamicBatching,useGPUInstancing,shadowSettings);
        }
    }
}