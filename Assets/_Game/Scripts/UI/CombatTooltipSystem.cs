using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Système unifié d'infobulles de combat.
/// Remplace HpTooltipWidget + AoETooltipOverlay — un seul composant à poser dans la scène.
///
/// Comportement :
///   • Survol sans sort   → infobulle : nom + PV
///   • Survol avec sort   → infobulle : nom + PV + prévisualisation dégâts/soins
///   • Personnage dans la zone AoE (hors souris) → même infobulle au-dessus de lui automatiquement
/// </summary>
public class CombatTooltipSystem : MonoBehaviour
{
    [Header("Caméra (Camera.main si vide)")]
    public Camera cam;

    [Header("Décalage vertical au-dessus du personnage (unités monde)")]
    public float worldOffsetY = 1.4f;

    // ------------------------------------------------------------------ internes

    Canvas _canvas;

    class Panel
    {
        public RectTransform   root;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI previewText;
    }

    // index 0 = personnage sous la souris, 1+ = personnages dans la zone AoE
    readonly List<Panel> _pool = new List<Panel>();

    // ------------------------------------------------------------------ lifecycle

    void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null) _canvas = FindObjectOfType<Canvas>();
    }

    void Update()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || _canvas == null || GridManager.Instance == null) { HideAll(); return; }

        if (CombatInitializer.Instance != null &&
            CombatInitializer.Instance.CurrentPhase != CombatInitializer.CombatPhase.Combat)
        { HideAll(); return; }

        var sc     = SpellCaster.Active;
        var caster = sc != null ? sc.GetComponent<TacticalCharacter>() : null;

        // ---- 1. personnage directement sous la souris ----
        Cell hoveredCell = GridManager.Instance.GetCellAtScreenPosition(cam, Input.mousePosition);
        TacticalCharacter hoveredChar = null;
        if (hoveredCell != null && hoveredCell.IsOccupied)
            hoveredChar = hoveredCell.Occupant?.GetComponent<TacticalCharacter>();

        // ---- 2. personnages dans la zone AoE (hors case survolée) ----
        var aoeTargets = new List<TacticalCharacter>();
        if (sc != null)
        {
            foreach (Cell cell in sc.AoEPreviewCells)
            {
                if (cell == hoveredCell || !cell.IsOccupied) continue;
                var tc = cell.Occupant?.GetComponent<TacticalCharacter>();
                if (tc != null && !aoeTargets.Contains(tc))
                    aoeTargets.Add(tc);
            }
        }

        // ---- 3. calcul du nombre de panels nécessaires ----
        int needed = (hoveredChar != null ? 1 : 0) + aoeTargets.Count;
        if (needed == 0) { HideAll(); return; }

        while (_pool.Count < needed) _pool.Add(BuildPanel());
        HideAll();

        int idx = 0;

        // panel pour le personnage sous la souris
        if (hoveredChar != null)
        {
            bool showPreview = sc != null &&
                               hoveredChar.CurrentCell != null &&
                               (sc.IsValidTarget(hoveredChar.CurrentCell) ||
                                sc.IsInAoEPreview(hoveredChar.CurrentCell));
            ShowPanel(_pool[idx++], hoveredChar, showPreview ? sc : null, caster);
        }

        // panels pour les personnages dans la zone AoE
        foreach (var tc in aoeTargets)
            ShowPanel(_pool[idx++], tc, sc, caster);
    }

    // ------------------------------------------------------------------ affichage d'un panel

    void ShowPanel(Panel p, TacticalCharacter tc, SpellCaster sc, TacticalCharacter caster)
    {
        if (tc.stats == null) return;

        // Nom
        p.nameText.text = tc.name;

        // PV
        float ratio = tc.stats.maxHP > 0 ? (float)tc.CurrentHP / tc.stats.maxHP : 0f;
        p.hpText.color = ratio > 0.5f  ? new Color(0.35f, 0.85f, 0.40f)
                       : ratio > 0.25f ? new Color(0.90f, 0.75f, 0.10f)
                                       : new Color(0.90f, 0.25f, 0.25f);
        p.hpText.text = $"{tc.CurrentHP} / {tc.stats.maxHP} PV";

        // Prévisualisation (seulement si sort actif passé en paramètre)
        if (sc != null)
        {
            var (dmg, heal) = SpellCaster.ComputePreview(sc.SelectedSpell, caster, tc);
            if (dmg > 0 && heal > 0)
            {
                p.previewText.text  = $"- {dmg}  |  + {heal} PV";
                p.previewText.color = new Color(1f, 0.78f, 0.30f);
                p.previewText.gameObject.SetActive(true);
            }
            else if (dmg > 0)
            {
                p.previewText.text  = $"- {dmg} PV";
                p.previewText.color = new Color(1f, 0.38f, 0.38f);
                p.previewText.gameObject.SetActive(true);
            }
            else if (heal > 0)
            {
                p.previewText.text  = $"+ {heal} PV";
                p.previewText.color = new Color(0.42f, 1f, 0.52f);
                p.previewText.gameObject.SetActive(true);
            }
            else
            {
                p.previewText.gameObject.SetActive(false);
            }
        }
        else
        {
            p.previewText.gameObject.SetActive(false);
        }

        // Position écran
        Vector3 worldAbove = tc.transform.position + Vector3.up * worldOffsetY;
        Vector3 screenPos  = cam.WorldToScreenPoint(worldAbove);
        if (screenPos.z < 0f) return;

        Vector2 localPos;
        if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            localPos = new Vector2(screenPos.x, screenPos.y);
        }
        else
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.GetComponent<RectTransform>(), screenPos, cam, out localPos);
        }

        p.root.anchoredPosition = localPos;
        p.root.gameObject.SetActive(true);
    }

    // ------------------------------------------------------------------ utilitaires

    void HideAll()
    {
        foreach (var p in _pool)
            if (p.root != null) p.root.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ construction des panels

    Panel BuildPanel()
    {
        var go = new GameObject("CombatTooltipPanel", typeof(RectTransform));
        go.transform.SetParent(_canvas.transform, false);

        var rt       = (RectTransform)go.transform;
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.sizeDelta = new Vector2(160f, 0f);

        go.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.09f, 0.88f);
        go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 2f;
        vlg.padding = new RectOffset(8, 8, 6, 6);

        var nameT    = MakeLabel(go.transform, "Name",    13f, FontStyles.Bold,   new Color(0.788f, 0.659f, 0.298f));
        var hpT      = MakeLabel(go.transform, "HP",      11f, FontStyles.Normal, new Color(0.35f,  0.85f,  0.40f));
        var previewT = MakeLabel(go.transform, "Preview", 11f, FontStyles.Bold,   Color.white);
        previewT.gameObject.SetActive(false);

        go.SetActive(false);

        return new Panel { root = rt, nameText = nameT, hpText = hpT, previewText = previewT };
    }

    static TextMeshProUGUI MakeLabel(Transform parent, string goName, float size, FontStyles style, Color color)
    {
        var go = new GameObject(goName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize      = size;
        t.fontStyle     = style;
        t.color         = color;
        t.alignment     = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        go.AddComponent<LayoutElement>().flexibleWidth = 1f;
        return t;
    }
}
