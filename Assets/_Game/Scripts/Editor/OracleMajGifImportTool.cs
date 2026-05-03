#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Extrait les GIF (<c>attack_*</c>, <c>survival_*</c>, <c>tactic_*</c>, sprint, dégâts…) depuis
/// <c>GifSource</c> en PNG pour <see cref="CombatAnimationResources"/> / HUD.
/// Menu : Tools → Oracle → Extract Combat & HUD GIF Frames (mise à jour)
/// </summary>
public static class OracleMajGifImportTool
{
    const string FramesSubfolder = "Frames";

    static readonly (string gifDir, string framesRoot)[] Roots =
    {
        ("Assets/_Game/Resources/CombatAnimations/GifSource", "Assets/_Game/Resources/CombatAnimations"),
        ("Assets/_Game/Resources/OracleHUD/GifSource", "Assets/_Game/Resources/OracleHUD"),
    };

    [MenuItem("Tools/Oracle/Extract Combat & HUD GIF Frames (mise à jour)")]
    public static void ExtractAll()
    {
        int total = 0;
        foreach (var root in Roots)
        {
            if (!AssetDatabase.IsValidFolder(root.gifDir))
            {
                Debug.LogWarning($"[OracleMajGif] Dossier GIF absent : {root.gifDir}");
                continue;
            }
            total += ExtractGifsInTree(root.gifDir, root.framesRoot);
        }
        AssetDatabase.Refresh();
        foreach (var root in Roots)
            ConfigureAllPngsUnder($"{root.framesRoot}/{FramesSubfolder}");
        AssetDatabase.Refresh();
        Debug.Log($"[OracleMajGif] Extraction terminée — {total} GIF traités.");
    }

    static int ExtractGifsInTree(string gifSourceDirAsset, string framesParentAssetPath)
    {
        int n = 0;
        string absSource = AssetPathToAbsolute(gifSourceDirAsset);
        if (!Directory.Exists(absSource))
            return 0;

        foreach (var gifAbs in Directory.GetFiles(absSource, "*.gif", SearchOption.AllDirectories))
        {
            string gifName = Path.GetFileNameWithoutExtension(gifAbs);
            string relFramesAsset = $"{framesParentAssetPath}/{FramesSubfolder}/{gifName}";
            string absFrames = AssetPathToAbsolute(relFramesAsset);
            Directory.CreateDirectory(absFrames);
            if (ExtractGifToFolder(gifAbs, absFrames))
                n++;
        }
        return n;
    }

    static string AssetPathToAbsolute(string assetPath)
    {
        string rel = assetPath.StartsWith("Assets/", StringComparison.Ordinal)
            ? assetPath.Substring(7)
            : assetPath;
        return Path.GetFullPath(Path.Combine(Application.dataPath, rel.Replace('/', Path.DirectorySeparatorChar)));
    }

    static bool ExtractGifToFolder(string gifAbsPath, string outDirAbs)
    {
        string gifName = Path.GetFileNameWithoutExtension(gifAbsPath);
        try
        {
            using var gif = System.Drawing.Image.FromFile(gifAbsPath);
            var fd = new System.Drawing.Imaging.FrameDimension(gif.FrameDimensionsList[0]);
            int frameCount = gif.GetFrameCount(fd);
            for (int i = 0; i < frameCount; i++)
            {
                gif.SelectActiveFrame(fd, i);
                string pngAbs = Path.Combine(outDirAbs, $"{gifName}_{i:D3}.png");
                using var bmp = new System.Drawing.Bitmap(gif.Width, gif.Height);
                using var g = System.Drawing.Graphics.FromImage(bmp);
                g.DrawImage(gif, 0, 0);
                bmp.Save(pngAbs, System.Drawing.Imaging.ImageFormat.Png);
            }
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OracleMajGif] {gifName} : {ex.Message}");
            return false;
        }
    }

    static void ConfigureAllPngsUnder(string framesRootAsset)
    {
        if (!AssetDatabase.IsValidFolder(framesRootAsset))
            return;
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { framesRootAsset });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
            ConfigurePngImport(path);
        }
    }

    static void ConfigurePngImport(string assetPath)
    {
        var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (imp == null) return;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.spritePixelsPerUnit = 100f;
        imp.SaveAndReimport();
    }
}
#endif
