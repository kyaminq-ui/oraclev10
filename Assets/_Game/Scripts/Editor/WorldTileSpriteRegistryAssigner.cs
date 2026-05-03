#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Remplit <see cref="TileSpriteRegistry"/> avec les textures du dossier
/// <c>Assets/_Game/Sprites/World</c> (copie du pack « World » à la racine du dépôt).
/// À exécuter une fois après import : menu <b>Oracle → Remplir TileSpriteRegistry depuis Sprites/World</b>.
/// </summary>
public static class WorldTileSpriteRegistryAssigner
{
    const string WorldFolder   = "Assets/_Game/Sprites/World";
    const string RegistryAsset = "Assets/_Game/ScriptableObjects/TileSpriteRegistry.asset";

    [MenuItem("Oracle/Remplir TileSpriteRegistry depuis Sprites/World")]
    public static void AssignFromWorldFolder()
    {
        var registry = AssetDatabase.LoadAssetAtPath<TileSpriteRegistry>(RegistryAsset);
        if (registry == null)
        {
            Debug.LogError($"[WorldTiles] Asset introuvable : {RegistryAsset}");
            return;
        }

        var missing = new List<string>();

        Sprite S(string fileSansExtension)
        {
            string path = $"{WorldFolder}/{fileSansExtension}.png";
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null) missing.Add(path);
            return s;
        }

        Sprite[] A(params string[] names)
        {
            var list = new List<Sprite>();
            foreach (var n in names)
            {
                var sp = S(n);
                if (sp != null) list.Add(sp);
            }
            return list.ToArray();
        }

        registry.groundTiles = A("sol_dallage_sombre", "sol_pierre_craquele_rouge", "sol_rune_violette");

        var blood = S("sol_flaque_sang");
        registry.groundBloodTile     = blood;
        registry.groundBloodVariants = blood != null ? new[] { blood } : System.Array.Empty<Sprite>();

        var mousse = S("sol_mousse_venimeuse");
        var lueur  = S("sol_lueur_spectrale");
        registry.groundGrassTile = mousse != null ? mousse : lueur;
        registry.groundGrassOrCursedVariants = A("sol_mousse_venimeuse", "sol_lueur_spectrale");

        registry.centerArenaFloorTile = S("sp_cercle_rituel");
        registry.spawnCalmFloorTiles = A("sol_dallage_sombre", "sol_rune_violette");

        registry.obstacleTiles = A(
            "mur_chaines",
            "mur_crane_incruste",
            "mur_pierre_sombre",
            "mur_runique_violet",
            "mur_torche",
            "struct_creneau_parapet",
            "struct_pilier_brise",
            "struct_pilier_massif",
            "prop_autel_maudit",
            "prop_brasero_maudit",
            "prop_cage_fer",
            "prop_rocher_debris",
            "prop_statue_gargouille",
            "prop_tonneau_venin",
            "sp_coffre_maudit",
            "sp_piege_pointes");

        var portail = S("sp_portail_spawn");
        registry.groundDecorationChance = portail != null ? 0.05f : 0f;
        registry.decorationMagicTiles   = portail != null ? new[] { portail } : System.Array.Empty<Sprite>();
        registry.groundDecorationTiles    = System.Array.Empty<Sprite>();
        registry.decorationBloodTiles     = System.Array.Empty<Sprite>();

        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();

        if (missing.Count > 0)
        {
            Debug.LogWarning("[WorldTiles] Sprites manquants (vérifie l’import PNG) :\n" + string.Join("\n", missing));
        }
        else
            Debug.Log("[WorldTiles] TileSpriteRegistry mis à jour depuis Sprites/World.");
    }
}
#endif
