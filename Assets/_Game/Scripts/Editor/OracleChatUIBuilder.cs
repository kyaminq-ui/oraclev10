#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Menu Oracle > Build Chat UI — ajoute le panneau CombatChatUI sur le Canvas de la scène.
/// L'UI complète se construit au lancement (CombatChatUI.Awake).
/// </summary>
public static class OracleChatUIBuilder
{
    const string MENU = "Oracle/Build Chat UI";

    [MenuItem(MENU)]
    public static void Build()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Oracle — Chat UI",
                "Aucun Canvas dans la scène.\nCrée d'abord un Canvas (GameObject > UI > Canvas).", "OK");
            return;
        }

        // Supprimer l'existant
        var existing = canvas.GetComponentInChildren<CombatChatUI>(true);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Oracle — Chat UI",
                    "CombatChatUI existe déjà dans la scène. Reconstruire ?", "Oui", "Annuler"))
                return;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        var go = new GameObject("CombatChatUI");
        Undo.RegisterCreatedObjectUndo(go, "Create CombatChatUI");
        go.transform.SetParent(canvas.transform, false);
        go.AddComponent<CombatChatUI>();

        Selection.activeGameObject = go;
        EditorUtility.DisplayDialog("Oracle — Chat UI",
            "CombatChatUI ajouté au Canvas.\n\nL'interface se construit au lancement du jeu.\n" +
            "Ajustez offsetX / offsetY dans l'Inspector pour repositionner le panneau.", "OK");
    }
}
#endif
