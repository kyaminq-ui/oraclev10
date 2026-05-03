#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu Oracle → Inject TurnOrderTimeline
/// Ajoute automatiquement TurnOrderTimeline sur le Canvas de chaque scène de combat.
/// </summary>
public static class InjectTurnOrderTimeline
{
    static readonly string[] TargetScenes =
    {
        "Assets/Monjeu.unity",
        "Assets/_Game/Scenes/Ranked1v1.unity",
    };

    [MenuItem("Oracle/Inject TurnOrderTimeline")]
    static void Run()
    {
        string originalScene = EditorSceneManager.GetActiveScene().path;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("[InjectTurnOrderTimeline] Annulé par l'utilisateur.");
            return;
        }

        int injected = 0;

        foreach (string scenePath in TargetScenes)
        {
            if (!System.IO.File.Exists(
                    System.IO.Path.Combine(Application.dataPath, "..", scenePath)))
            {
                Debug.LogWarning($"[InjectTurnOrderTimeline] Scène introuvable : {scenePath}");
                continue;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            Canvas canvas = FindBestCanvas();
            if (canvas == null)
            {
                Debug.LogWarning($"[InjectTurnOrderTimeline] Aucun Canvas trouvé dans {scenePath}");
                continue;
            }

            if (canvas.GetComponentInChildren<TurnOrderTimeline>(true) != null)
            {
                Debug.Log($"[InjectTurnOrderTimeline] TurnOrderTimeline déjà présent dans {scenePath}");
                continue;
            }

            var go = new GameObject("TurnOrderTimeline");
            go.transform.SetParent(canvas.transform, false);
            go.AddComponent<TurnOrderTimeline>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            injected++;
            Debug.Log($"[InjectTurnOrderTimeline] Injecté dans {scenePath} (Canvas : {canvas.name})");
        }

        if (!string.IsNullOrEmpty(originalScene))
            EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);

        EditorUtility.DisplayDialog(
            "Injection terminée",
            injected > 0
                ? $"TurnOrderTimeline injecté dans {injected} scène(s)."
                : "Rien à injecter (déjà présent ou scène introuvable).",
            "OK");
    }

    static Canvas FindBestCanvas()
    {
        Canvas best = null;
        foreach (Canvas c in Object.FindObjectsOfType<Canvas>())
        {
            if (c.renderMode == RenderMode.WorldSpace) continue;
            if (c.transform.parent != null && c.transform.parent.GetComponentInParent<Canvas>() != null)
                continue;
            if (best == null || c.sortingOrder > best.sortingOrder)
                best = c;
        }
        return best;
    }
}
#endif
