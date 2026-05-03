#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Photon.Pun;

/// <summary>
/// Outil éditeur — prépare le Hub pour le multijoueur visible.
///
/// Menu Unity : Oracle > Setup Hub Multiplayer
///
/// Ce que ça fait :
///   1. Ouvre Hub.unity, trouve le GameObject "LocalPlayer".
///   2. Lui ajoute PhotonView + PhotonTransformView (sync position).
///   3. Sauvegarde un prefab "HubPlayer" dans Assets/_Game/Resources/
///      (requis par PhotonNetwork.Instantiate).
///   4. Supprime "LocalPlayer" de la scène Hub
///      (il sera remplacé à l'exécution par le prefab réseau).
///   5. Ajoute HubNetworkSpawner sur NetworkRoot.
///
/// Après cet outil, relance la scène Hub avec deux instances pour voir
/// les joueurs se voir avec leurs vrais sprites de personnage.
/// </summary>
public static class OracleHubNetworkSetup
{
    const string RESOURCES_PATH = "Assets/_Game/Resources";
    const string PREFAB_PATH    = "Assets/_Game/Resources/HubPlayer.prefab";
    const string HUB_SCENE_PATH = "Assets/_Game/Scenes/Hub.unity";

    [MenuItem("Oracle/Setup Hub Multiplayer")]
    public static void Setup()
    {
        if (!EditorUtility.DisplayDialog("Setup Hub Multiplayer",
                "Cet outil va :\n" +
                "  1. Extraire LocalPlayer de Hub.unity → prefab réseau HubPlayer\n" +
                "  2. Ajouter PhotonView + PhotonTransformView au prefab\n" +
                "  3. Remplacer LocalPlayer par HubNetworkSpawner dans la scène\n\n" +
                "Les sprites/animations déjà configurés sur LocalPlayer seront conservés.",
                "Continuer", "Annuler"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // ── 1. Dossier Resources ──────────────────────────────────────────
        if (!AssetDatabase.IsValidFolder(RESOURCES_PATH))
            AssetDatabase.CreateFolder("Assets/_Game", "Resources");

        // ── 2. Ouvrir Hub.unity ───────────────────────────────────────────
        var scene = EditorSceneManager.OpenScene(HUB_SCENE_PATH, OpenSceneMode.Single);

        // ── 3. Trouver LocalPlayer ────────────────────────────────────────
        GameObject localPlayer = null;
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name == "LocalPlayer") { localPlayer = go; break; }
        }

        if (localPlayer == null)
        {
            EditorUtility.DisplayDialog("LocalPlayer introuvable",
                "Aucun GameObject nommé 'LocalPlayer' trouvé dans Hub.unity.\n\n" +
                "Lance d'abord : Oracle > Create Hub Scene\n" +
                "puis : Oracle > Setup Hub Player\n" +
                "pour configurer les sprites, ensuite relance cet outil.",
                "OK");
            return;
        }

        // ── 4. Ajouter PhotonView + PhotonTransformView + HubNetworkSync ─────
        var observed = new System.Collections.Generic.List<Component>();

        PhotonView pv = localPlayer.GetComponent<PhotonView>();
        if (pv == null)
        {
            pv = localPlayer.AddComponent<PhotonView>();
            Debug.Log("[OracleHubNetworkSetup] PhotonView ajouté.");
        }
        pv.Synchronization = ViewSynchronization.UnreliableOnChange;

        PhotonTransformView ptv = localPlayer.GetComponent<PhotonTransformView>();
        if (ptv == null)
            ptv = localPlayer.AddComponent<PhotonTransformView>();
        ptv.m_SynchronizePosition = true;
        ptv.m_SynchronizeRotation = false;
        ptv.m_SynchronizeScale    = false;
        observed.Add(ptv);

        // HubNetworkSync — sync State + FacingDirection pour les animations distantes
        HubNetworkSync sync = localPlayer.GetComponent<HubNetworkSync>();
        if (sync == null)
            sync = localPlayer.AddComponent<HubNetworkSync>();
        observed.Add(sync);

        pv.ObservedComponents = observed;
        Debug.Log("[OracleHubNetworkSetup] PhotonView configuré avec PhotonTransformView + HubNetworkSync.");

        // ── 5. Sauvegarder en prefab ──────────────────────────────────────
        bool existed = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) != null;
        if (existed) AssetDatabase.DeleteAsset(PREFAB_PATH);

        PrefabUtility.SaveAsPrefabAsset(localPlayer, PREFAB_PATH);
        Debug.Log($"[OracleHubNetworkSetup] Prefab sauvegardé → {PREFAB_PATH}");

        // ── 6. Supprimer LocalPlayer de la scène ──────────────────────────
        // (remplacé à l'exécution par PhotonNetwork.Instantiate via HubNetworkSpawner)
        Object.DestroyImmediate(localPlayer);
        Debug.Log("[OracleHubNetworkSetup] LocalPlayer supprimé de la scène Hub.");

        // ── 7. Ajouter HubNetworkSpawner sur NetworkRoot ──────────────────
        PatchNetworkRoot(scene);

        // ── 8. Sauvegarde ─────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Setup terminé !",
            "Prefab réseau créé : Assets/_Game/Resources/HubPlayer.prefab\n\n" +
            "LocalPlayer supprimé de Hub.unity — il sera instancié via Photon au runtime.\n\n" +
            "Lance maintenant deux instances (éditeur + build ou ParrelSync)\n" +
            "et les joueurs se verront avec leurs vrais sprites !",
            "OK");
    }

    // ─────────────────────────────────────────────────────────────────────

    static void PatchNetworkRoot(UnityEngine.SceneManagement.Scene scene)
    {
        // Chercher NetworkRoot dans la scène
        GameObject networkRoot = null;
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name == "NetworkRoot") { networkRoot = go; break; }
            var found = go.transform.Find("NetworkRoot");
            if (found != null) { networkRoot = found.gameObject; break; }
        }

        if (networkRoot == null)
        {
            networkRoot = new GameObject("HubNetworkSpawner");
            Debug.LogWarning("[OracleHubNetworkSetup] NetworkRoot introuvable — " +
                             "HubNetworkSpawner ajouté à la racine.");
        }

        if (networkRoot.GetComponent<HubNetworkSpawner>() == null)
        {
            var spawner = networkRoot.AddComponent<HubNetworkSpawner>();
            spawner.avatarPrefabName = "HubPlayer";
            spawner.hubRoomName      = "HUB_GLOBAL";
            Debug.Log($"[OracleHubNetworkSetup] HubNetworkSpawner ajouté sur '{networkRoot.name}'.");
        }
        else
        {
            var spawner = networkRoot.GetComponent<HubNetworkSpawner>();
            spawner.avatarPrefabName = "HubPlayer";
            Debug.Log("[OracleHubNetworkSetup] HubNetworkSpawner déjà présent — prefab mis à jour.");
        }

        if (networkRoot.GetComponent<HubMatchmaker>() == null)
        {
            var mm = networkRoot.AddComponent<HubMatchmaker>();
            mm.combatSceneName = "Ranked1v1";
            Debug.Log($"[OracleHubNetworkSetup] HubMatchmaker ajouté sur '{networkRoot.name}'.");
        }
        else
        {
            // S'assurer que le nom de scène est correct
            var mm = networkRoot.GetComponent<HubMatchmaker>();
            mm.combatSceneName = "Ranked1v1";
        }

        // OracleCombatNetBridge — DOIT être sur NetworkRoot (DontDestroyOnLoad)
        // pour que IsNetworkDuel soit vrai dans la scène Ranked1v1.
        if (networkRoot.GetComponent<OracleCombatNetBridge>() == null)
        {
            networkRoot.AddComponent<OracleCombatNetBridge>();
            Debug.Log($"[OracleHubNetworkSetup] OracleCombatNetBridge ajouté sur '{networkRoot.name}'.");
        }
    }
}
#endif
