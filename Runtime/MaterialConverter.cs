#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Experimental.Rendering;

[CustomEditor(typeof(MaterialConverter))]
public class MaterialConverterEditor : Editor
{
    MaterialConverter materialConverter => target as MaterialConverter;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Convert"))
            materialConverter.Convert();

        if (GUILayout.Button("Refresh PBR"))
            materialConverter.RefreshPBR();

        if (GUILayout.Button("Switch File Types"))
            materialConverter.ChangeFileType();

        if (GUILayout.Button("Reset Properties"))
            materialConverter.ResetProperties();
    }
}

public class MaterialConverter : MonoBehaviour
{
    [SerializeField] Material material;

    [SerializeField] bool convertAllInPath;

    [Header("Attributes")]

    [SerializeField] string diffuseAttribute = "_BaseMap";

    [SerializeField] string specularAttribute = "_SpecGlossMap";

    [SerializeField] string normalAttribute = "_DetailNormalMap";

    [Header("Refresh PBR")]

    [SerializeField] bool flipGreenChannel;

    [Header("Switch File Types")]

    [SerializeField] string fromExtension;

    [SerializeField] string toExtension;

    public void Convert()
    {
        if (!material) return;

        if (convertAllInPath)
        {
            string materialPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(material));

            foreach (string thisAssetPath in Directory.GetFiles(materialPath))
            {
                if (Path.GetExtension(thisAssetPath) == ".mat")
                {
                    Material thisMaterial = AssetDatabase.LoadAssetAtPath<Material>(thisAssetPath);

                    ConvertMaterial(thisMaterial);
                }
            }
        }
        else
            ConvertMaterial(material);

        AssetDatabase.SaveAssets();
    }

    public void RefreshPBR()
    {
        if (!material) return;

        if (convertAllInPath)
        {
            string materialPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(material));

            foreach (string thisAssetPath in Directory.GetFiles(materialPath))
            {
                if (Path.GetExtension(thisAssetPath) == ".mat")
                {
                    Material thisMaterial = AssetDatabase.LoadAssetAtPath<Material>(thisAssetPath);

                    RefreshMaterialPBR(thisMaterial, true, true);
                }
            }
        }
        else
            RefreshMaterialPBR(material, true, true);

        AssetDatabase.SaveAssets();

        AssetDatabase.Refresh();
    }

    public void ChangeFileType()
    {
        if (!material) return;

        if (convertAllInPath)
        {
            string materialPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(material));

            foreach (string thisAssetPath in Directory.GetFiles(materialPath))
            {
                if (Path.GetExtension(thisAssetPath) == ".mat")
                {
                    Material thisMaterial = AssetDatabase.LoadAssetAtPath<Material>(thisAssetPath);

                    ChangeMaterialFileType(thisMaterial);
                }
            }
        }
        else
            ChangeMaterialFileType(material);

        AssetDatabase.SaveAssets();

        AssetDatabase.Refresh();
    }

    public void ResetProperties()
    {
        if (!material) return;

        if (convertAllInPath)
        {
            string materialPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(material));

            foreach (string thisAssetPath in Directory.GetFiles(materialPath))
            {
                if (Path.GetExtension(thisAssetPath) == ".mat")
                {
                    Material thisMaterial = AssetDatabase.LoadAssetAtPath<Material>(thisAssetPath);

                    ResetMaterialProperties(thisMaterial);
                }
            }
        }
        else
            ResetMaterialProperties(material);

        AssetDatabase.SaveAssets();

        AssetDatabase.Refresh();
    }

    void ConvertMaterial(Material material)
    {
        Texture diffuse = material.GetTexture(diffuseAttribute);

        Texture specular = material.GetTexture(specularAttribute);

        Texture normal = material.GetTexture(normalAttribute);

        material.shader = Shader.Find("Shader Graphs/Hedgehog Engine PBR");

        if (diffuse)
            material.SetTexture("_Diffuse", diffuse);

        if (specular)
            SetSpecular(material, specular);

        if (normal)
            SetSpecular(material, normal);

        EditorUtility.SetDirty(material);
    }

    void RefreshMaterialPBR(Material material, bool specular, bool normal)
    {
        Texture diffuse = material.GetTexture("_Diffuse");

        if (!diffuse) return;

        if (GraphicsFormatUtility.HasAlphaChannel(diffuse.graphicsFormat))
            material.SetFloat("_AlphaClip", 1);

        if (normal)
        {
            if (MatchTextureBySuffix(diffuse, "dif", "nrm", out Texture2D normal1))
                SetNormal(material, normal1);
        }

        if (MatchTextureBySuffix(diffuse, "dif", "env", out Texture2D environment))
            SetEnvironment(material, environment);

        if (specular)
        {
            if (MatchTextureBySuffix(diffuse, "dif", "spc", out Texture2D specular1))
                SetSpecular(material, specular1);
            else if (MatchTextureBySuffix(diffuse, "dif", "pow", out Texture2D specular2))
                SetSpecular(material, specular2);
        }

        if (MatchTextureBySuffix(diffuse, "dif", "pow", out Texture2D power))
            SetFalloff(material, power);

        EditorUtility.SetDirty(material);

        bool MatchTextureBySuffix(Texture originalTexture, string originalSuffix, string suffix, out Texture2D texture2D)
        {
            string name = originalTexture.name.Substring(0, originalTexture.name.Length - originalSuffix.Length);

            texture2D = null;

            string texturePath = AssetDatabase.GetAssetPath(originalTexture);

            string textureDirectory = Path.GetDirectoryName(texturePath);

            string textureExtention = Path.GetExtension(texturePath);

            foreach (string thisAssetPath in Directory.GetFiles(textureDirectory))
            {
                if (Path.GetExtension(thisAssetPath) == textureExtention)
                {
                    Texture2D thisTexture2D = AssetDatabase.LoadAssetAtPath<Texture2D>(thisAssetPath);

                    if (thisTexture2D.name == name + suffix)
                    {
                        texture2D = thisTexture2D;

                        return true;
                    }
                }
            }

            return false;
        }
    }

    void ChangeMaterialFileType(Material material)
    {
        if (fromExtension == toExtension) return;

        foreach (string thisTexturePropertyName in material.GetTexturePropertyNames())
        {
            Texture thisTexture = material.GetTexture(thisTexturePropertyName);

            string texturePath = AssetDatabase.GetAssetPath(thisTexture);

            if (texturePath == string.Empty) continue;

            string textureDirectory = Path.GetDirectoryName(texturePath);

            string textureExtention = Path.GetExtension(texturePath);

            if (textureExtention != fromExtension) continue;

            foreach (string thisAssetPath in Directory.GetFiles(textureDirectory))
            {
                if (Path.GetExtension(thisAssetPath) == toExtension)
                {
                    Texture2D thisTexture2D = AssetDatabase.LoadAssetAtPath<Texture2D>(thisAssetPath);

                    if (thisTexture2D.name == thisTexture.name)
                    {
                        material.SetTexture(thisTexturePropertyName, thisTexture2D);

                        break;
                    }
                }
            }
        }

        EditorUtility.SetDirty(material);
    }

    void ResetMaterialProperties(Material material)
    {
        Texture diffuse = material.GetTexture("_Diffuse");

        if (!diffuse) return;

        Material material1 = new(material.shader);

        material1.SetTexture("_Diffuse", diffuse);

        material.CopyPropertiesFromMaterial(material1);

        Destroy(material1);

        EditorUtility.SetDirty(material);
    }

    void SetNormal(Material material, Texture texture)
    {
        material.SetTexture("_Normal", texture);

        string path = AssetDatabase.GetAssetPath(texture);

        TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

        if (textureImporter != null)
        {
            textureImporter.textureType = TextureImporterType.NormalMap;

            textureImporter.flipGreenChannel = flipGreenChannel;

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }

    void SetEnvironment(Material material, Texture texture)
    {
        material.SetTexture("_MatCap", texture);

        material.SetFloat("_REFLECTION_MODE", 1);
    }

    void SetSpecular(Material material, Texture texture)
    {
        material.SetTexture("_Specular", texture);

        material.SetColor("_Specular_Color", Color.white);
    }

    void SetFalloff(Material material, Texture texture)
    {
        material.SetTexture("_Falloff", texture);

        material.SetColor("_Specular_Color", Color.white);

        string path = AssetDatabase.GetAssetPath(texture);

        TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

        if (textureImporter != null)
        {
            textureImporter.sRGBTexture = false;

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }
}

#endif
