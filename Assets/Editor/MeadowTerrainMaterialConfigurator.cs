using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MeadowTerrainMaterialConfigurator
{
    private const string MaterialPath = "Assets/Materials/Gameplay/Map/MAT_CoreTest_MeadowTerrainBlend.mat";
    private const string ShaderName = "OZ/Map/Dirt Road Texture Blend";
    private const string DirtBasePath = "Assets/ThirdParty/01_Core/STYLIZED Meadow Nature/Materials/Textures/M_dirt_BaseColor.png";
    private const string DirtGrassBasePath = "Assets/ThirdParty/01_Core/STYLIZED Meadow Nature/Materials/Textures/M_DirtGrass_BaseMap.png";
    private const string GrassBasePath = "Assets/ThirdParty/01_Core/STYLIZED Meadow Nature/Materials/Textures/M_grass_BaseMap.png";
    private const string DirtNormalPath = "Assets/ThirdParty/01_Core/STYLIZED Meadow Nature/Materials/Textures/M_dirt_Normal.png";
    private const string DirtGrassNormalPath = "Assets/ThirdParty/01_Core/STYLIZED Meadow Nature/Materials/Textures/M_DirtGrass_Normal.png";
    private const string GrassNormalPath = "Assets/ThirdParty/01_Core/STYLIZED Meadow Nature/Materials/Textures/M_grass_Normal.png";
    private const string SourceTerrainTemplatePath = "Assets/ThirdParty/01_Core/STYLIZED Meadow Nature/Materials/Terrain/Terrain Meadow Nature.asset";
    private const string ProjectTerrainTemplatePath = "Assets/Art/Map/Generated/Terrain_CoreTestMeadowNature_Copy.asset";

    [MenuItem("Tools/OZ/Map/Configure Meadow Terrain Material")]
    public static void ConfigureFromMenu()
    {
        ConfigureMaterial();
    }

    public static void RunOnceFromCommandLine()
    {
        ConfigureMaterial();
    }

    [MenuItem("Tools/OZ/Map/Create CoreTest Meadow TerrainData Copy")]
    public static void CreateMeadowTerrainCopyFromMenu()
    {
        CreateMeadowTerrainCopy(true);
    }

    public static void CreateMeadowTerrainCopyFromCommandLine()
    {
        CreateMeadowTerrainCopy(true);
    }

    private static void ConfigureMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            throw new InvalidOperationException($"Material not found: {MaterialPath}");
        }

        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            throw new InvalidOperationException($"Shader not found: {ShaderName}");
        }

        material.shader = shader;
        material.SetTexture("_MainTex01", LoadTexture(DirtBasePath));
        material.SetTexture("_MainTex02", LoadTexture(DirtGrassBasePath));
        material.SetTexture("_MainTex03", LoadTexture(GrassBasePath));
        material.SetTexture("_NormalTex01", LoadTexture(DirtNormalPath));
        material.SetTexture("_NormalTex02", LoadTexture(DirtGrassNormalPath));
        material.SetTexture("_NormalTex03", LoadTexture(GrassNormalPath));
        material.SetColor("_Tint01", Color.white);
        material.SetColor("_Tint02", Color.white);
        material.SetColor("_Tint03", Color.white);
        material.SetFloat("_TileSize01", 5f);
        material.SetFloat("_TileSize02", 5f);
        material.SetFloat("_TileSize03", 6f);
        material.SetFloat("_NormalStrength", 0.78f);
        material.SetFloat("_BlendNoiseScale", 0.16f);
        material.SetFloat("_BlendNoiseStrength", 0.16f);
        material.SetFloat("_MacroTintScale", 0.045f);
        material.SetFloat("_MacroTintStrength", 0.055f);
        material.SetFloat("_ControlMapBlend", 0f);
        material.SetVector("_ControlMapCenterSize", new Vector4(0f, 0f, 200f, 0f));
        material.SetFloat("_DetailScale", 38f);
        material.SetFloat("_DetailStrength", 0.035f);
        material.SetFloat("_ShadowStrength", 0.42f);
        material.SetFloat("_AntiTileStrength", 0.18f);
        material.SetFloat("_UvWarpStrength", 0.012f);

        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(MaterialPath, ImportAssetOptions.ForceUpdate);
        Debug.Log($"[MeadowTerrainMaterialConfigurator] Configured {MaterialPath}");
    }

    private static void CreateMeadowTerrainCopy(bool forceUpdate)
    {
        string targetDirectory = Path.GetDirectoryName(ProjectTerrainTemplatePath);
        if (!string.IsNullOrEmpty(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        TerrainData source = AssetDatabase.LoadAssetAtPath<TerrainData>(SourceTerrainTemplatePath);
        if (source == null)
        {
            throw new InvalidOperationException($"Source TerrainData not found: {SourceTerrainTemplatePath}");
        }

        TerrainData target = AssetDatabase.LoadAssetAtPath<TerrainData>(ProjectTerrainTemplatePath);
        if (target == null)
        {
            if (!AssetDatabase.CopyAsset(SourceTerrainTemplatePath, ProjectTerrainTemplatePath))
            {
                throw new InvalidOperationException($"Failed to copy TerrainData: {SourceTerrainTemplatePath} -> {ProjectTerrainTemplatePath}");
            }

            target = AssetDatabase.LoadAssetAtPath<TerrainData>(ProjectTerrainTemplatePath);
        }
        else if (forceUpdate)
        {
            EditorUtility.CopySerialized(source, target);
        }

        if (target == null)
        {
            throw new InvalidOperationException($"Copied TerrainData not found: {ProjectTerrainTemplatePath}");
        }

        target.name = Path.GetFileNameWithoutExtension(ProjectTerrainTemplatePath);
        target.treePrototypes = Array.Empty<TreePrototype>();
        target.SetTreeInstances(Array.Empty<TreeInstance>(), false);

        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(ProjectTerrainTemplatePath, ImportAssetOptions.ForceUpdate);
        Debug.Log($"[MeadowTerrainMaterialConfigurator] Created CoreTest meadow TerrainData copy without trees: {ProjectTerrainTemplatePath}");
    }

    private static Texture2D LoadTexture(string path)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
        {
            throw new InvalidOperationException($"Texture not found: {path}");
        }

        return texture;
    }
}
