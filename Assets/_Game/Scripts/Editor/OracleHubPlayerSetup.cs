#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Outil éditeur — configure automatiquement le joueur dans la scène Hub.
/// Menu : Oracle > Setup Hub Player (from Combat Scene)
///
/// Ce qu'il fait :
///   1. Trouve « LocalPlayer » dans la scène courante (Hub)
///   2. Assigne CharacterStats + DeckData sur le TacticalCharacter
///   3. Ouvre Assets/Monjeu.unity en mode additif (pas de Play — juste la donnée sérialisée)
///   4. Copie le PlayerAnimator du personnage joueur (stats, frames Idle/Walk/Death,
///      spriteScale, visualOffset, fps, remappage de direction)
///   5. Ferme Monjeu.unity sans sauvegarder
///   6. Sauvegarde la scène Hub
///
/// À lancer UNE FOIS après "Oracle > Create Hub Scene", ou après chaque
/// modification des animations dans Monjeu.unity.
/// </summary>
public static class OracleHubPlayerSetup
{
    const string MONJEU_PATH    = "Assets/Monjeu.unity";
    const string PATH_STATS     = "Assets/_Game/ScriptableObjects/PlayerStats.asset";
    const string PATH_DECK      = "Assets/_Game/ScriptableObjects/PlayerDeck.asset";
    const string LOCAL_PLAYER   = "LocalPlayer";

    // ─────────────────────────────────────────────────────────────────

    [MenuItem("Oracle/Setup Hub Player (from Combat Scene)")]
    public static void SetupHubPlayer()
    {
        // ── 0. Vérifier la scène courante ─────────────────────────────
        var hubScene = EditorSceneManager.GetActiveScene();
        if (!hubScene.name.Contains("Hub"))
        {
            if (!EditorUtility.DisplayDialog("Scène non-Hub détectée",
                    $"La scène active est « {hubScene.name} ».\n" +
                    "Continuer quand même ?", "Oui", "Annuler"))
                return;
        }

        // ── 1. Trouver LocalPlayer ────────────────────────────────────
        var hubPlayerGo = GameObject.Find(LOCAL_PLAYER);
        if (hubPlayerGo == null)
        {
            EditorUtility.DisplayDialog("Erreur",
                $"Impossible de trouver un GameObject nommé « {LOCAL_PLAYER} » dans la scène active.\n" +
                "Crée d'abord la scène Hub via Oracle > Create Hub Scene.", "OK");
            return;
        }

        var hubTC = hubPlayerGo.GetComponent<TacticalCharacter>();
        var hubPA = hubPlayerGo.GetComponent<PlayerAnimator>();

        if (hubTC == null)
        {
            EditorUtility.DisplayDialog("Erreur",
                $"« {LOCAL_PLAYER} » n'a pas de composant TacticalCharacter.", "OK");
            return;
        }

        // ── 2. Assigner CharacterStats + DeckData ─────────────────────
        int assignedCount = 0;

        var stats = AssetDatabase.LoadAssetAtPath<CharacterStats>(PATH_STATS);
        var deck  = AssetDatabase.LoadAssetAtPath<DeckData>(PATH_DECK);

        if (stats != null) { hubTC.stats = stats; assignedCount++; }
        else Debug.LogWarning($"[HubPlayerSetup] {PATH_STATS} introuvable — assigne CharacterStats manuellement.");

        if (deck  != null) { hubTC.deck  = deck;  assignedCount++; }
        else Debug.LogWarning($"[HubPlayerSetup] {PATH_DECK} introuvable — assigne DeckData manuellement.");

        EditorUtility.SetDirty(hubTC);

        // ── 3. Ouvrir Monjeu.unity en mode additif ────────────────────
        var monjeuScene = EditorSceneManager.OpenScene(MONJEU_PATH, OpenSceneMode.Additive);

        // ── 4. Trouver le PlayerAnimator du joueur dans Monjeu ────────
        PlayerAnimator    sourcePA = null;
        TacticalCharacter sourceTC = null;

        foreach (var root in monjeuScene.GetRootGameObjects())
        {
            // On cherche la première racine contenant à la fois
            // TacticalCharacter ET PlayerAnimator (les personnages jouables).
            // On exclut les GO nommés "Opponent" / "Adversaire".
            foreach (var pa in root.GetComponentsInChildren<PlayerAnimator>(true))
            {
                var tc = pa.GetComponent<TacticalCharacter>();
                if (tc == null) continue;

                string n = pa.gameObject.name.ToLowerInvariant();
                if (n.Contains("opponent") || n.Contains("adversaire") || n.Contains("enemy"))
                    continue;

                sourcePA = pa;
                sourceTC = tc;
                break;
            }
            if (sourcePA != null) break;
        }

        if (sourcePA == null)
        {
            // Fallback : prendre le premier PlayerAnimator trouvé
            foreach (var root in monjeuScene.GetRootGameObjects())
            {
                sourcePA = root.GetComponentInChildren<PlayerAnimator>(true);
                if (sourcePA != null) { sourceTC = sourcePA.GetComponent<TacticalCharacter>(); break; }
            }
        }

        if (sourcePA != null && hubPA != null)
        {
            CopyPlayerAnimator(sourcePA, hubPA);
            assignedCount++;
            Debug.Log($"[HubPlayerSetup] Animations copiées depuis « {sourcePA.gameObject.name} » (Monjeu.unity).");
        }
        else if (hubPA == null)
        {
            Debug.LogWarning("[HubPlayerSetup] LocalPlayer n'a pas de PlayerAnimator — animations ignorées.");
        }
        else
        {
            Debug.LogWarning("[HubPlayerSetup] Aucun PlayerAnimator trouvé dans Monjeu.unity — anime le Hub player manuellement.");
        }

        // Copier le SpriteRenderer depuis TacticalCharacter source si le hub n'en a pas encore
        if (sourceTC != null && hubTC != null)
        {
            var hubSr = hubPlayerGo.GetComponent<SpriteRenderer>();
            if (hubSr != null && sourceTC.spriteRenderer != null)
            {
                hubSr.color  = sourceTC.spriteRenderer.color;
                hubSr.sprite = sourceTC.spriteRenderer.sprite;
                EditorUtility.SetDirty(hubSr);
            }
        }

        // ── 5. Fermer Monjeu.unity SANS sauvegarder ───────────────────
        EditorSceneManager.CloseScene(monjeuScene, removeScene: true);

        // ── 6. Sauvegarder la scène Hub ───────────────────────────────
        EditorSceneManager.MarkSceneDirty(hubScene);
        EditorSceneManager.SaveScene(hubScene);

        EditorUtility.DisplayDialog(
            "Hub Player configuré !",
            $"LocalPlayer mis à jour ({assignedCount} élément(s) assigné(s)) :\n\n" +
            $"  • CharacterStats : {(stats != null ? stats.name : "non trouvé")}\n" +
            $"  • DeckData       : {(deck  != null ? deck.name  : "non trouvé")}\n" +
            $"  • Animations     : {(sourcePA != null ? "copiées depuis Monjeu.unity" : "non trouvées")}\n\n" +
            "Lance la scène Hub pour tester le personnage.",
            "OK");
    }

    // ── Copie complète du PlayerAnimator ──────────────────────────────

    static void CopyPlayerAnimator(PlayerAnimator src, PlayerAnimator dst)
    {
        // Taille & position
        dst.spriteScale   = src.spriteScale;
        dst.visualOffset  = src.visualOffset;

        // Animations Idle
        dst.idleSO = CopyAnim(src.idleSO);
        dst.idleSE = CopyAnim(src.idleSE);
        dst.idleNE = CopyAnim(src.idleNE);
        dst.idleNO = CopyAnim(src.idleNO);

        // Animations Walk
        dst.walkSO = CopyAnim(src.walkSO);
        dst.walkSE = CopyAnim(src.walkSE);
        dst.walkNE = CopyAnim(src.walkNE);
        dst.walkNO = CopyAnim(src.walkNO);

        // Animations Death
        dst.deathSO = CopyAnim(src.deathSO);
        dst.deathSE = CopyAnim(src.deathSE);
        dst.deathNE = CopyAnim(src.deathNE);
        dst.deathNO = CopyAnim(src.deathNO);

        // FPS de marche
        dst.walkFpsNormal = src.walkFpsNormal;
        dst.walkFpsSlow   = src.walkFpsSlow;
        dst.sprintFps     = src.sprintFps;
        dst.hurtFps       = src.hurtFps;
        dst.castFps       = src.castFps;

        dst.sprintSO = CopyAnim(src.sprintSO);
        dst.sprintSE = CopyAnim(src.sprintSE);
        dst.sprintNE = CopyAnim(src.sprintNE);
        dst.sprintNO = CopyAnim(src.sprintNO);

        dst.hurtSO = CopyAnim(src.hurtSO);
        dst.hurtSE = CopyAnim(src.hurtSE);
        dst.hurtNE = CopyAnim(src.hurtNE);
        dst.hurtNO = CopyAnim(src.hurtNO);

        // Remappage de directions
        dst.mapSO = src.mapSO;
        dst.mapSE = src.mapSE;
        dst.mapNE = src.mapNE;
        dst.mapNO = src.mapNO;

        EditorUtility.SetDirty(dst);
    }

    /// <summary>
    /// Copie profonde d'un DirectionalAnimation (clone le tableau de sprites
    /// mais garde les références aux assets Sprite intactes).
    /// </summary>
    static DirectionalAnimation CopyAnim(DirectionalAnimation src)
    {
        if (src == null) return new DirectionalAnimation();

        var dst = new DirectionalAnimation
        {
            fps  = src.fps,
            loop = src.loop,
        };

        if (src.frames != null)
        {
            dst.frames = new Sprite[src.frames.Length];
            System.Array.Copy(src.frames, dst.frames, src.frames.Length);
        }
        else
        {
            dst.frames = new Sprite[0];
        }

        return dst;
    }
}
#endif
