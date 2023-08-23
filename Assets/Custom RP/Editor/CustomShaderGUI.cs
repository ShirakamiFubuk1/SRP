using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomShaderGUI : ShaderGUI
{
    private MaterialEditor editor;
    private Object[] materials;
    private MaterialProperty[] properties;
    
    public override void OnGUI(
        MaterialEditor materialEditor, MaterialProperty[] properties
    ){
        base.OnGUI(materialEditor,properties);
        editor = materialEditor;
        materials = materialEditor.targets;
        this.properties = properties;
    }

    void SetProperty(string name, float value)
    {
        FindProperty(name, properties).floatValue = value;
    }

    void SetKeyword(string keyword, bool enable)
    {
        if (enable)
        {
            foreach (Material material in materials)
            {
                material.DisableKeyword(keyword);
            }
        }
        else
        {
            foreach (Material material in materials)
            {
                material.DisableKeyword(keyword);
            }
        }
    }

    void SetProperty(string name, string keyword, bool value)
    {
        SetProperty(name,value?1f:0f);
        SetKeyword(keyword,value);
    }

    private bool Clipping
    {
        set => SetProperty("_Clipping", "_CLIPPING", value);
    }

    private bool PremultiplyAlpha
    {
        set => SetProperty("_PremultiplyAlpha", "_PREMULTIPLYALPHA", value);
    }

    private BlendMode SrcBlend
    {
        set => SetProperty("_SrcBlend", (float)value);
    }

    private BlendMode DstBlend
    {
        set => SetProperty("_DstBlend", (float)value);
    }

    private bool ZWrite
    {
        set => SetProperty("_ZWrite", value ? 1f : 0f);
    }

    RenderQueue RenderQueue
    {
        set
        {
            foreach (Material material in materials)
            {
                material.renderQueue = (int)value;
            }
        }
    }
}