using System;
using UnityEngine;

/// <summary>
/// Charge les séquences PNG extraites des GIF (<c>Tools → Oracle → Extract Combat & HUD GIF Frames</c>)
/// depuis n’importe quel sous-dossier de <c>Assets/_Game/Resources/</c>.
/// </summary>
public static class CombatAnimationResources
{
    /// <summary>Charge tous les sprites d’un dossier Resources, triés par nom (ordre des frames).</summary>
    public static Sprite[] LoadAllSpritesSorted(string resourcesPath)
    {
        if (string.IsNullOrEmpty(resourcesPath)) return Array.Empty<Sprite>();
        var arr = Resources.LoadAll<Sprite>(resourcesPath);
        if (arr == null || arr.Length == 0) return Array.Empty<Sprite>();
        Array.Sort(arr, (a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
        return arr;
    }

    public static string FacingToIsoSuffix(FacingDirection f)
    {
        switch (f)
        {
            case FacingDirection.SouthWest: return "SO";
            case FacingDirection.SouthEast: return "SE";
            case FacingDirection.NorthEast: return "NE";
            case FacingDirection.NorthWest: return "NO";
            default:                        return "SE";
        }
    }

    /// <summary>Clip d’attaque pour une direction : dossier <c>CombatAnimations/Frames/{base}_{SO|SE|NE|NO}</c>.</summary>
    public static DirectionalAnimation LoadSpellCastClip(string animBase, FacingDirection facing, float fps)
    {
        if (string.IsNullOrEmpty(animBase)) return null;
        string folder = $"{animBase}_{FacingToIsoSuffix(facing)}";
        var frames = LoadAllSpritesSorted($"CombatAnimations/Frames/{folder}");
        if (frames.Length == 0) return null;
        return new DirectionalAnimation
        {
            frames = frames,
            fps    = Mathf.Max(1f, fps),
            loop   = false
        };
    }
}
