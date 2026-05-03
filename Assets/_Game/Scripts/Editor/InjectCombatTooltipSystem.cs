#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu Oracle → Inject CombatTooltipSystem
/// Ajoute automatiquement CombatTooltipSystem sur le Canvas de chaque scène de combat.
/// </summary>
public static class InjectCombatTooltipSystem
{
    static readonly string[] TargetScenes =
    {
        "Assets/Monjeu.unity",
        "Assets/_Game/Scenes/Ranked1v1.unity",
    };

    [MenuItem("Oracle/Inject CombatTooltipSystem")]
    static void Run()
    {
        // Sauvegarder la scène courante pour y revenir ensuite
        string originalScene = EditorSceneManager.GetActiveScene().path;

        // Sauvegarder les modifications en cours avant de changer de scène
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("[InjectCombatTooltipSystem] Annulé par l'utilisateur.");
            return;
        }

        int injected = 0;

        foreach (string scenePath in TargetScenes)
        {
            // Vérifier que la scène existe bien dans le projet
            if (!System.IO.File.Exists(
                    System.IO.Path.Combine(Application.dataPath, "..", scenePath)))
            {
                Debug.LogWarning($"[InjectCombatTooltipSystem] Scène introuvable : {scenePath}");
                continue;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            Canvas canvas = FindBestCanvas();
            if (canvas == null)
            {
                Debug.LogWarning($"[InjectCombatTooltipSystem] Aucun Canvas trouvé dans {scenePath}");
                continue;
            }

            // Ne pas ajouter si déjà présent
            if (canvas.GetComponentInChildren<CombatTooltipSystem>(true) != null)
            {
                Debug.Log($"[InjectCombatTooltipSystem] CombatTooltipSystem déjà présent dans {scenePath}");
                continue;
            }

            // Créer un GameObject dédié enfant du Canvas
            var go = new GameObject("CombatTooltipSystem");
            go.transform.SetParent(canvas.transform, false);
            go.AddComponent<CombatTooltipSystem>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            injected++;
            Debug.Log($"[InjectCombatTooltipSystem] Injecté dans {scenePath} (Canvas : {canvas.name})");
        }

        // Revenir à la scène d'origine
        if (!string.IsNullOrEmpty(originalScene))
            EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);

        EditorUtility.DisplayDialog(
            "Injection terminée",
            injected > 0
                ? $"CombatTooltipSystem injecté dans {injected} scène(s)."
                : "Rien à injecter (déjà présent ou scène introuvable).",
            "OK");
    }

    /// <summary>Choisit le Canvas Screen Space le plus adapté (pas World Space).</summary>
    static Canvas FindBestCanvas()
    {
        Canvas best = null;
        foreach (Canvas c in Object.FindObjectsOfType<Canvas>())
        {
            if (c.renderMode == RenderMode.WorldSpace) continue;
            // Préférer le Canvas racine (pas enfant d'un autre Canvas)
            if (c.transform.parent != null && c.transform.parent.GetComponentInParent<Canvas>() != null)
                continue;
            if (best == null || c.sortingOrder > best.sortingOrder)
                best = c;
        }
        return best;
    }
}
#endif
