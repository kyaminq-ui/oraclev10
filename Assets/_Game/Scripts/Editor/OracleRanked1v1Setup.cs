#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

/// <summary>
/// Outil éditeur — crée la scène Ranked1v1 depuis la scène Training existante.
///
/// Menu Unity : Oracle > Create Ranked1v1 Scene
///
/// Ce que ça fait :
///   1. Copie Assets/Monjeu.unity → Assets/_Game/Scenes/Ranked1v1.unity
///   2. Ouvre Ranked1v1.unity
///   3. Sur CombatInitializer :
///        • combatMode       = Ranked1v1    (réseau, pas d'IA)
///        • autoPlaceOpponent = false        (l'adversaire humain se place lui-même)
///        • skipPassiveSelection = false     (toujours sélection en ranked)
///   4. Sur OpponentAI (si présent) : désactive le composant
///      (il est déjà ignoré en IsNetworkDuel, mais on le désactive pour la clarté)
///   5. Ajoute les deux scènes (Monjeu + Ranked1v1) aux Build Settings.
/// </summary>
public static class OracleRanked1v1Setup
{
    const string SOURCE_SCENE = "Assets/Monjeu.unity";
    const string DEST_SCENE   = "Assets/_Game/Scenes/Ranked1v1.unity";

    [MenuItem("Oracle/Create Ranked1v1 Scene")]
    public static void CreateRanked1v1Scene()
    {
        if (!System.IO.File.Exists(System.IO.Path.Combine(
                System.IO.Directory.GetCurrentDirectory(), SOURCE_SCENE.Replace('/', '\\'))))
        {
            EditorUtility.DisplayDialog("Source introuvable",
                $"La scène source '{SOURCE_SCENE}' est introuvable.\n\n" +
                "Si ta scène Training a un autre nom, modifie SOURCE_SCENE\n" +
                "dans OracleRanked1v1Setup.cs.",
                "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Créer la scène Ranked1v1",
                $"Source : {SOURCE_SCENE}\n" +
                $"Destination : {DEST_SCENE}\n\n" +
                "La scène sera dupliquée, puis :\n" +
                "  • CombatMode → Ranked1v1\n" +
                "  • autoPlaceOpponent → false\n" +
                "  • OpponentAI désactivé\n\n" +
                "Scènes déjà sauvegardées avant la copie.",
                "Créer", "Annuler"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // ── 1. Copier la scène ────────────────────────────────────────────
        bool copied = AssetDatabase.CopyAsset(SOURCE_SCENE, DEST_SCENE);
        if (!copied)
        {
            // Si le fichier de destination existe déjà, le supprimer d'abord
            AssetDatabase.DeleteAsset(DEST_SCENE);
            copied = AssetDatabase.CopyAsset(SOURCE_SCENE, DEST_SCENE);
        }

        if (!copied)
        {
            EditorUtility.DisplayDialog("Erreur",
                $"Impossible de copier '{SOURCE_SCENE}' vers '{DEST_SCENE}'.\n" +
                "Vérifie que le dossier Assets/_Game/Scenes/ existe.",
                "OK");
            return;
        }

        AssetDatabase.Refresh();
        Debug.Log($"[OracleRanked1v1Setup] Scène copiée → {DEST_SCENE}");

        // ── 2. Ouvrir et patcher ──────────────────────────────────────────
        var scene = EditorSceneManager.OpenScene(DEST_SCENE, OpenSceneMode.Single);

        CombatInitializer ci = null;
        foreach (var go in scene.GetRootGameObjects())
        {
            ci = go.GetComponentInChildren<CombatInitializer>(includeInactive: true);
            if (ci != null) break;
        }

        if (ci == null)
        {
            Debug.LogWarning("[OracleRanked1v1Setup] CombatInitializer introuvable dans la scène dupliquée.");
        }
        else
        {
            ci.combatMode          = CombatInitializer.CombatMode.Ranked1v1;
            ci.autoPlaceOpponent   = false;   // l'adversaire humain se place lui-même
            ci.skipPassiveSelection = false;  // sélection active en ranked
            EditorUtility.SetDirty(ci);
            Debug.Log("[OracleRanked1v1Setup] CombatInitializer → Ranked1v1.");
        }

        // ── 3. Désactiver OpponentAI ──────────────────────────────────────
        foreach (var go in scene.GetRootGameObjects())
        {
            var ais = go.GetComponentsInChildren<OpponentAI>(includeInactive: true);
            foreach (var ai in ais)
            {
                ai.enabled = false;
                EditorUtility.SetDirty(ai);
            }
            if (ais.Length > 0)
                Debug.Log($"[OracleRanked1v1Setup] {ais.Length} OpponentAI désactivé(s).");
        }

        // ── 4. Supprimer NetworkRoot de la scène ──────────────────────────
        // OracleNetworkHub est DontDestroyOnLoad depuis le Hub : inutile de l'avoir aussi
        // dans la scène Ranked1v1, ce qui provoque un conflit PhotonView ID=1.
        // En standalone (test direct), il suffit de re-glisser le prefab NetworkRoot.
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name == "NetworkRoot" || go.GetComponent<OracleNetworkHub>() != null)
            {
                Debug.Log($"[OracleRanked1v1Setup] NetworkRoot supprimé de la scène Ranked1v1 " +
                          "(fourni par DontDestroyOnLoad du Hub en production).");
                Object.DestroyImmediate(go);
                break;
            }
        }

        // ── 4. Sauvegarder ───────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, DEST_SCENE);

        // ── 5. Build Settings ─────────────────────────────────────────────
        AddSceneToBuildSettings(SOURCE_SCENE);   // Training (Monjeu)
        AddSceneToBuildSettings(DEST_SCENE);     // Ranked1v1

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Scène Ranked1v1 créée !",
            $"Fichier : {DEST_SCENE}\n\n" +
            "Configuration appliquée :\n" +
            "  • CombatMode = Ranked1v1\n" +
            "  • autoPlaceOpponent = false\n" +
            "  • OpponentAI désactivé\n" +
            "  • NetworkRoot supprimé (DontDestroyOnLoad du Hub)\n\n" +
            "Les deux scènes sont dans les Build Settings.\n\n" +
            "Note : pour tester Ranked1v1 en standalone (sans passer\n" +
            "par le Hub), re-glisse le prefab NetworkRoot dans la scène.",
            "OK");
    }

    static void AddSceneToBuildSettings(string path)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.All(s => s.path != path))
        {
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[OracleRanked1v1Setup] '{path}' ajouté aux Build Settings.");
        }
    }
}
#endif
