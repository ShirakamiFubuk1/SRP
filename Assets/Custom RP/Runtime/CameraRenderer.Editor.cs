using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

//局部类,是一种将类或者结构定义拆分为多个部分的方法,本别存储在不同的文件中,目的是为了组织代码.
//典型用例是将自动生成的代码与手工编写的代码分开.

//partial 局部类
partial class CameraRenderer
{
    partial void DrawGizmos();

    partial void PrepareForSceneWindow();
    
    //为了在构建时将一个空值返回防止报错
    partial void DrawUnsupportedShaders();

    partial void PrepareBuffer();

#if UNITY_EDITOR

    //为了解决每次访问相机Name属性时分配内存，添加一个SampleName字符串属性。
    //在编辑器中就在Preparebuffer中设置缓冲区名称
    private string SampleName { get; set; }
    
    //设置Build-in Pass 中的常见Pass可以通过渲染
    static ShaderTagId[] legacyShaderTagIds =
    {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };

    private static Material errorMaterial;

    partial void DrawGizmos()
    {   
        //绘制Gizmos
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera,GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera,GizmoSubset.PostImageEffects);
        }
    }

    //添加一个进编辑器能用PrepareBuffer方法，是缓冲区名称和相机名称相等
    partial void PrepareBuffer()
    {
        Profiler.BeginSample("Editor Only");
        buffer.name = SampleName = camera.name;
        Profiler.EndSample();
    }

    //使用场景摄像机渲染
    partial void PrepareForSceneWindow()
    {
        if (camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }
    
    partial void DrawUnsupportedShaders()
    {
        if (errorMaterial == null)
        {
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }
        var drawingSettings = new DrawingSettings(
            legacyShaderTagIds[0], new SortingSettings(camera)
        )
        {
            overrideMaterial = errorMaterial
        };
        for (int i = 1; i < legacyShaderTagIds.Length; i++)
        {
            drawingSettings.SetShaderPassName(i,legacyShaderTagIds[i]);
        }
        var filteringSettings = FilteringSettings.defaultValue;
        context.DrawRenderers(cullingResults,ref drawingSettings,ref filteringSettings);
    }
#else
    
    const string SampleName = bufferName;    
    
#endif
    
}