#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// Outil éditeur — crée et configure la scène Hub isométrique 2.5D.
/// Menu : Oracle > Create Hub Scene
///
/// Structure générée :
///   ├─ Main Camera              (IsometricCamera — suit le joueur)
///   ├─ GridManager              (même GridConfig que le combat)
///   ├─ ArenaGenerator           (même ArenaConfig — génère la map iso)
///   ├─ HubManager               (DontDestroyOnLoad — transitions de scène)
///   ├─ LocalPlayer              (TacticalCharacter + PlayerAnimator
///   │                            + HubCharacterController)
///   ├─ NetworkRoot              (Photon, autoJoinRoomOnConnect = false)
///   ├─ HubCanvas                (Canvas ScreenSpaceOverlay + HubHUD)
///   └─ EventSystem
///
/// Assets réutilisés (depuis le combat) :
///   GridConfig_Combat.asset  ← ArenaGenerator écrit les dimensions dedans
///   ArenaConfig_1v1.asset
///   TileSpriteRegistry.asset
///   PlayerStats.asset
///   CellPrefab.prefab
/// </summary>
public static class OracleHubSceneBuilder
{
    const string HUB_SCENE_PATH = "Assets/_Game/Scenes/Hub.unity";
    const string HUB_SCENE_NAME = "Hub";

    // ── Chemins des assets partagés avec le combat ────────────────────
    const string PATH_ARENA_CONFIG  = "Assets/_Game/ScriptableObjects/ArenaConfig_1v1.asset";
    const string PATH_GRID_CONFIG   = "Assets/_Game/ScriptableObjects/GridConfig_Combat.asset";
    const string PATH_TILE_REGISTRY = "Assets/_Game/ScriptableObjects/TileSpriteRegistry.asset";
    const string PATH_PLAYER_STATS  = "Assets/_Game/ScriptableObjects/PlayerStats.asset";
    const string PATH_PLAYER_DECK   = "Assets/_Game/ScriptableObjects/PlayerDeck.asset";
    const string PATH_CELL_PREFAB   = "Assets/_Game/Prefabs/CellPrefab.prefab";
    const string PATH_NETWORK_ROOT  = "Assets/_Game/Prefabs/NetworkRoot.prefab";

    // ─────────────────────────────────────────────────────────────────

    [MenuItem("Oracle/Create Hub Scene")]
    public static void CreateHubScene()
    {
        if (!EditorUtility.DisplayDialog("Créer la scène Hub",
                $"Créer la scène Hub isométrique dans :\n{HUB_SCENE_PATH}\n\n" +
                "Si elle existe déjà, son contenu sera remplacé.\n\n" +
                "Assets réutilisés : ArenaConfig_1v1, GridConfig_Combat, TileSpriteRegistry, PlayerStats.\n\n" +
                "Après création, lance Oracle > Setup Hub Player pour injecter les sprites du personnage.",
                "Créer", "Annuler"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        // ── Charger les assets partagés ───────────────────────────────
        var arenaConfig  = AssetDatabase.LoadAssetAtPath<ArenaConfig>(PATH_ARENA_CONFIG);
        var gridConfig   = AssetDatabase.LoadAssetAtPath<GridConfig>(PATH_GRID_CONFIG);
        var tileRegistry = Load<ScriptableObject>(PATH_TILE_REGISTRY, "TileSpriteRegistry");
        var playerStats  = AssetDatabase.LoadAssetAtPath<CharacterStats>(PATH_PLAYER_STATS);
        var playerDeck   = AssetDatabase.LoadAssetAtPath<DeckData>(PATH_PLAYER_DECK);
        var cellPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>(PATH_CELL_PREFAB);
        var netRootPrefab= AssetDatabase.LoadAssetAtPath<GameObject>(PATH_NETWORK_ROOT);

        // ── Nouvelle scène vierge ─────────────────────────────────────
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ════════════════════════════════════════════════════════════════
        // CAMÉRA ISOMÉTRIQUE — centrée sur la grille dès le premier frame
        // ════════════════════════════════════════════════════════════════
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.AddComponent<AudioListener>();

        var cam = camGo.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 5f;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.04f, 0.04f, 0.07f);
        cam.nearClipPlane    = 0.1f;
        cam.farClipPlane     = 100f;

        // Calculer le centre monde de la grille pour l'initialisation
        // (GridToWorld(cx, cy) avec cx = width/2, cy = height/2)
        Vector3 gridCenter = Vector3.zero;
        if (gridConfig != null && arenaConfig != null)
        {
            float cx  = arenaConfig.arenaWidth  / 2f;
            float cy  = arenaConfig.arenaHeight / 2f;
            float wx  = (cx - cy) * (gridConfig.tileWidth  / 2f);
            float wy  = (cx + cy) * (gridConfig.tileHeight / 2f);
            gridCenter = new Vector3(
                gridConfig.gridOrigin.x + wx,
                gridConfig.gridOrigin.y + wy,
                0f);
        }
        cam.transform.position = new Vector3(gridCenter.x, gridCenter.y, -10f);
        cam.transform.rotation = Quaternion.identity;

        var isoCam = camGo.AddComponent<IsometricCamera>();
        isoCam.isometricAngle = 30f;
        isoCam.defaultZoom    = 5f;
        isoCam.minZoom        = 2f;
        isoCam.maxZoom        = 12f;
        isoCam.pixelPerfect   = true;
        isoCam.pixelsPerUnit  = 32f;
        isoCam.useBounds      = false;
        isoCam.followSpeed    = 6f;
        // target assigné au runtime par HubCharacterController.Start()

        // ════════════════════════════════════════════════════════════════
        // GRID MANAGER
        // ════════════════════════════════════════════════════════════════
        var gmGo = new GameObject("GridManager");
        var gm   = gmGo.AddComponent<GridManager>();

        if (gridConfig  != null) gm.config     = gridConfig;
        else Debug.LogWarning("[OracleHubSceneBuilder] GridConfig_Combat introuvable — assigne GridConfig manuellement sur GridManager.");

        if (cellPrefab  != null) gm.cellPrefab = cellPrefab;
        EditorUtility.SetDirty(gm);

        // ════════════════════════════════════════════════════════════════
        // ARENA GENERATOR  (génère la map iso au démarrage)
        // ════════════════════════════════════════════════════════════════
        var agGo = new GameObject("ArenaGenerator");
        var ag   = agGo.AddComponent<ArenaGenerator>();

        if (arenaConfig != null) ag.arenaConfig = arenaConfig;
        else Debug.LogWarning("[OracleHubSceneBuilder] ArenaConfig_1v1 introuvable — assigne ArenaConfig manuellement.");
        if (gridConfig  != null) ag.gridConfig  = gridConfig;
        ag.generateOnStart = true;
        ag.showDebugGizmos = false;
        EditorUtility.SetDirty(ag);

        // ════════════════════════════════════════════════════════════════
        // JOUEUR LOCAL  (TacticalCharacter + PlayerAnimator + HubCharacterController)
        // ════════════════════════════════════════════════════════════════
        var playerGo = new GameObject("LocalPlayer");
        playerGo.transform.position = Vector3.zero;

        // SpriteRenderer (PlayerAnimator crée un child SpriteVisual au runtime)
        playerGo.AddComponent<SpriteRenderer>();

        // TacticalCharacter — assignment direct sur les champs publics
        var tc = playerGo.AddComponent<TacticalCharacter>();
        if (playerStats != null) tc.stats = playerStats;
        else Debug.LogWarning("[OracleHubSceneBuilder] PlayerStats introuvable → lance Oracle > Setup Hub Player après création.");
        if (playerDeck  != null) tc.deck  = playerDeck;
        EditorUtility.SetDirty(tc);

        // PlayerAnimator (animations directionnelles — frames à assigner dans l'Inspector)
        playerGo.AddComponent<PlayerAnimator>();

        playerGo.AddComponent<HubCharacterController>();
        playerGo.AddComponent<HubChatBubble>();
        playerGo.AddComponent<HubPlayerLabel>();

        // ════════════════════════════════════════════════════════════════
        // HUB MANAGER  (DontDestroyOnLoad)
        // ════════════════════════════════════════════════════════════════
        var hubMgrGo = new GameObject("HubManager");
        var hubMgr   = hubMgrGo.AddComponent<HubManager>();
        hubMgr.trainingSceneName = "Monjeu";
        hubMgr.hubSceneName      = HUB_SCENE_NAME;

        // ════════════════════════════════════════════════════════════════
        // NETWORK ROOT  (Photon — connexion sans rejoindre de room)
        // ════════════════════════════════════════════════════════════════
        if (netRootPrefab != null)
        {
            var netRoot = (GameObject)PrefabUtility.InstantiatePrefab(netRootPrefab);
            netRoot.name = "NetworkRoot";
            var netHub = netRoot.GetComponent<OracleNetworkHub>();
            if (netHub != null)
            {
                netHub.autoConnectOnStart    = true;
                netHub.autoJoinRoomOnConnect = false;
            }
        }
        else
            Debug.LogWarning("[OracleHubSceneBuilder] NetworkRoot.prefab introuvable.");

        // ════════════════════════════════════════════════════════════════
        // CANVAS + HUDHUD
        // ════════════════════════════════════════════════════════════════
        var canvasGo = new GameObject("HubCanvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        var hudGo = new GameObject("HubHUD");
        hudGo.transform.SetParent(canvasGo.transform, false);
        var hudRt = hudGo.AddComponent<RectTransform>();
        hudRt.anchorMin = Vector2.zero; hudRt.anchorMax = Vector2.one;
        hudRt.offsetMin = hudRt.offsetMax = Vector2.zero;
        hudGo.AddComponent<HubHUD>();

        // ════════════════════════════════════════════════════════════════
        // EVENT SYSTEM
        // ════════════════════════════════════════════════════════════════
        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();

        // ── Sauvegarde ───────────────────────────────────────────────
        EditorSceneManager.SaveScene(scene, HUB_SCENE_PATH);
        AddSceneToBuildSettings(HUB_SCENE_PATH);
        AssetDatabase.Refresh();

        Debug.Log($"[OracleHubSceneBuilder] Scène Hub créée → {HUB_SCENE_PATH}");
        EditorUtility.DisplayDialog(
            "Hub créé !",
            $"Scène Hub générée dans :\n{HUB_SCENE_PATH}\n\n" +
            "Étape suivante obligatoire :\n" +
            "  → Oracle > Setup Hub Player (from Combat Scene)\n" +
            "    Injecte automatiquement sprites + animations\n" +
            "    depuis Monjeu.unity dans LocalPlayer.\n\n" +
            "Ensuite : Lance la scène Hub et clique gauche pour te déplacer.",
            "OK");
    }

    // ── Helpers ───────────────────────────────────────────────────────

    static T Load<T>(string path, string friendlyName) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
            Debug.LogWarning($"[OracleHubSceneBuilder] '{friendlyName}' non trouvé à {path} — assigne-le manuellement.");
        return asset;
    }

    static void AddSceneToBuildSettings(string path)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.All(s => s.path != path))
        {
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[OracleHubSceneBuilder] '{path}' ajouté aux Build Settings (index {scenes.Count - 1}).");
        }
    }
}
#endif
