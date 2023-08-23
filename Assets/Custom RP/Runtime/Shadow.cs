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

    private ScriptableRenderContext context;

    private CullingResults cullingResults;

    private ShadowSettings setting;

    public void Setup(
        ScriptableRenderContext context,CullingResults cullingResults,ShadowSettings setting
    )
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.setting = setting;
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
}