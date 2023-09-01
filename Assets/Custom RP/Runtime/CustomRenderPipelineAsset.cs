using UnityEngine;
using UnityEngine.Rendering;

//RP资产的主要目的是提供一种方法来获取负责渲染的管线对象实例。资产本身只是一个句柄和存储设置的地方。

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]//向Asset/Create菜单中添加一个菜单条目。添加menuName将其放在Rendering/Custom Render Pipeline中
public class CustomRenderPipelineAsset : RenderPipelineAsset//资产类型必须继承自RenderPipelineAsset，该类在UnityEngine.Rendering命名空间下
{
    [SerializeField] 
    private ShadowSettings shadows = default;
    
    [SerializeField] 
    private bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;
    
    //CreatePipeline 返回一个 __CustomRenderPipeline__ 新实例。它会给我们一个有效且附带功能的管线实例。
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(useDynamicBatching,useGPUInstancing,useSRPBatcher,shadows);
    }
}