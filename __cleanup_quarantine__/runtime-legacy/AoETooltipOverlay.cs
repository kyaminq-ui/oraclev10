using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Affiche une infobulle au-dessus de chaque personnage présent dans la zone d'effet AoE
/// du sort en cours de prévisualisation — SAUF celui directement sous la souris
/// (déjà géré par HpTooltipWidget).
/// </summary>
public class AoETooltipOverlay : MonoBehaviour
{
    [Header("Caméra (Camera.main si vide)")]
    public Camera cam;

    [Header("Décalage vertical (unités monde)")]
    public float worldOffsetY = 1.4f;

    Canvas _canvas;

    class TooltipPanel
    {
        public RectTransform   root;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI previewText;
    }

    readonly List<TooltipPanel> _pool = new List<TooltipPanel>();

    // ------------------------------------------------------------------ lifecycle

    void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null) _canvas = FindObjectOfType<Canvas>();
    }

    void Update()
    {
        // Remplacé par CombatTooltipSystem — ce composant est désactivé fonctionnellement
        HideAll(); return;
#pragma warning disable CS0162
        if (cam == null) cam = Camera.main;
        if (cam == null || _canvas == null) { HideAll(); return; }
#pragma warning restore CS0162

        if (CombatInitializer.Instance != null &&
            CombatInitializer.Instance.CurrentPhase != CombatInitializer.CombatPhase.Combat)
        { HideAll(); return; }

        var sc = SpellCaster.Active;
        if (sc == null || sc.AoEPreviewCells.Count == 0) { HideAll(); return; }

        // Case directement sous la souris : HpTooltipWidget s'en charge, on la saute
        Cell hoveredCell = GridManager.Instance != null
            ? GridManager.Instance.GetCellAtScreenPosition(cam, Input.mousePosition)
            : null;

        var caster  = sc.GetComponent<TacticalCharacter>();
        var targets = CollectTargets(sc.AoEPreviewCells, hoveredCell);

        if (targets.Count == 0) { HideAll(); return; }

        while (_pool.Count < targets.Count)
            _pool.Add(BuildPanel());

        HideAll();
        for (int i = 0; i < targets.Count; i++)
            ShowPanel(_pool[i], targets[i], sc, caster);
    }

    // ------------------------------------------------------------------ affichage

    void ShowPanel(TooltipPanel p, TacticalCharacter tc, SpellCaster sc, TacticalCharacter caster)
    {
        if (tc.stats == null) return;

        p.nameText.text = tc.name;

        float ratio = tc.stats.maxHP > 0 ? (float)tc.CurrentHP / tc.stats.maxHP : 0f;
        p.hpText.color = ratio > 0.5f  ? new Color(0.35f, 0.85f, 0.40f)
                       : ratio > 0.25f ? new Color(0.90f, 0.75f, 0.10f)
                                       : new Color(0.90f, 0.25f, 0.25f);
        p.hpText.text = $"{tc.CurrentHP} / {tc.stats.maxHP} PV";

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

        Vector3 worldAbove = tc.transform.position + Vector3.up * worldOffsetY;
        Vector3 screenPos  = cam.WorldToScreenPoint(worldAbove);
        if (screenPos.z < 0f) return;

        Vector2 localPos;
        if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            localPos = new Vector2(screenPos.x, screenPos.y);
        else
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.GetComponent<RectTransform>(), screenPos, cam, out localPos);

        p.root.anchoredPosition = localPos;
        p.root.gameObject.SetActive(true);
    }

    // ------------------------------------------------------------------ utilitaires

    static List<TacticalCharacter> CollectTargets(IReadOnlyList<Cell> cells, Cell skip)
    {
        var list = new List<TacticalCharacter>();
        foreach (Cell cell in cells)
        {
            if (cell == skip || !cell.IsOccupied) continue;
            var tc = cell.Occupant?.GetComponent<TacticalCharacter>();
            if (tc != null && !list.Contains(tc))
                list.Add(tc);
        }
        return list;
    }

    void HideAll()
    {
        foreach (var p in _pool)
            if (p.root != null) p.root.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ construction du panel

    TooltipPanel BuildPanel()
    {
        var go = new GameObject("AoETooltipPanel", typeof(RectTransform));
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

        return new TooltipPanel { root = rt, nameText = nameT, hpText = hpT, previewText = previewT };
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
