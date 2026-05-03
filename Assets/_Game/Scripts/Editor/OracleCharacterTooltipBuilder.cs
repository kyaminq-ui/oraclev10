#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Menu Oracle > Build Character Tooltip — ajoute HpTooltipWidget sur le Canvas.
/// L'UI se construit au lancement (HpTooltipWidget.Awake).
/// </summary>
public static class OracleCharacterTooltipBuilder
{
    const string MENU = "Oracle/Build Character Tooltip";

    [MenuItem(MENU)]
    public static void Build()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Oracle — Character Tooltip",
                "Aucun Canvas dans la scène.", "OK");
            return;
        }

        var existing = canvas.GetComponentInChildren<HpTooltipWidget>(true);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Oracle — Character Tooltip",
                    "HpTooltipWidget existe déjà. Reconstruire ?", "Oui", "Annuler"))
                return;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        var go = new GameObject("CharacterTooltip");
        Undo.RegisterCreatedObjectUndo(go, "Create CharacterTooltip");
        go.transform.SetParent(canvas.transform, false);
        go.AddComponent<HpTooltipWidget>();

        Selection.activeGameObject = go;
        EditorUtility.DisplayDialog("Oracle — Character Tooltip",
            "CharacterTooltip ajouté.\n\nSurvole un personnage en jeu pour voir l'infobulle.\n" +
            "Ajuste worldOffsetY dans l'Inspector si nécessaire.", "OK");
    }
}
#endif
