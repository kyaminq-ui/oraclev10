using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Infobulle au survol d'un personnage : nom + PV, et prévisualisation dégâts/soins
/// quand un sort est sélectionné et que la cible est dans sa portée ou sa zone.
/// </summary>
public class HpTooltipWidget : MonoBehaviour
{
    [Header("Références (auto si vides)")]
    public Camera cam;

    [Header("Panel (auto-créé si vide)")]
    public RectTransform   panel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;

    [Header("Position au-dessus du personnage")]
    public float worldOffsetY = 1.4f;

    Canvas          _canvas;
    TextMeshProUGUI _previewText;

    // ------------------------------------------------------------------ lifecycle

    void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null) _canvas = FindObjectOfType<Canvas>();

        if (panel == null) BuildPanel();
        else               panel.gameObject.SetActive(false);
    }

    void Update()
    {
        // Remplacé par CombatTooltipSystem — ce composant est désactivé fonctionnellement
        Hide(); return;
#pragma warning disable CS0162
        if (cam == null) cam = Camera.main;
        if (cam == null || GridManager.Instance == null) { Hide(); return; }
#pragma warning restore CS0162

        if (CombatInitializer.Instance != null &&
            CombatInitializer.Instance.CurrentPhase != CombatInitializer.CombatPhase.Combat)
        { Hide(); return; }

        Cell cell = GridManager.Instance.GetCellAtScreenPosition(cam, Input.mousePosition);
        if (cell == null || !cell.IsOccupied) { Hide(); return; }

        var tc = cell.Occupant != null ? cell.Occupant.GetComponent<TacticalCharacter>() : null;
        if (tc == null || tc.stats == null) { Hide(); return; }

        Show(tc);
    }

    // ------------------------------------------------------------------ show / hide

    void Show(TacticalCharacter tc)
    {
        if (panel == null) return;

        // Nom
        if (nameText != null)
            nameText.text = tc.name;

        // PV
        if (hpText != null)
        {
            float ratio = tc.stats.maxHP > 0 ? (float)tc.CurrentHP / tc.stats.maxHP : 0f;
            hpText.color = ratio > 0.5f  ? new Color(0.35f, 0.85f, 0.40f)
                         : ratio > 0.25f ? new Color(0.90f, 0.75f, 0.10f)
                                         : new Color(0.90f, 0.25f, 0.25f);
            hpText.text = $"{tc.CurrentHP} / {tc.stats.maxHP} PV";
        }

        // Prévisualisation sort sélectionné
        RefreshPreview(tc);

        // Position écran
        Vector3 worldAbove = tc.transform.position + Vector3.up * worldOffsetY;
        Vector3 screenPos  = cam.WorldToScreenPoint(worldAbove);
        if (screenPos.z < 0f) { Hide(); return; }

        Vector2 localPos;
        if (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            localPos = new Vector2(screenPos.x, screenPos.y);
        }
        else
        {
            var canvasRt = _canvas != null
                ? _canvas.GetComponent<RectTransform>()
                : panel.parent as RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRt, screenPos, cam, out localPos);
        }

        panel.anchoredPosition = localPos;
        panel.gameObject.SetActive(true);
    }

    void Hide()
    {
        if (panel != null) panel.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ preview sort

    void RefreshPreview(TacticalCharacter tc)
    {
        if (_previewText == null) return;

        var sc = SpellCaster.Active;
        if (sc == null || tc.CurrentCell == null ||
            (!sc.IsValidTarget(tc.CurrentCell) && !sc.IsInAoEPreview(tc.CurrentCell)))
        {
            _previewText.gameObject.SetActive(false);
            return;
        }

        var caster = sc.GetComponent<TacticalCharacter>();
        var (dmg, heal) = SpellCaster.ComputePreview(sc.SelectedSpell, caster, tc);

        if (dmg > 0 && heal > 0)
        {
            _previewText.text  = $"- {dmg}  |  + {heal} PV";
            _previewText.color = new Color(1f, 0.78f, 0.30f);
        }
        else if (dmg > 0)
        {
            _previewText.text  = $"- {dmg} PV";
            _previewText.color = new Color(1f, 0.38f, 0.38f);
        }
        else if (heal > 0)
        {
            _previewText.text  = $"+ {heal} PV";
            _previewText.color = new Color(0.42f, 1f, 0.52f);
        }
        else
        {
            _previewText.gameObject.SetActive(false);
            return;
        }

        _previewText.gameObject.SetActive(true);
    }

    // ------------------------------------------------------------------ construction du panel

    void BuildPanel()
    {
        if (_canvas == null) { Debug.LogWarning("[HpTooltipWidget] Aucun Canvas trouvé."); return; }

        var go = new GameObject("CharacterTooltipPanel", typeof(RectTransform));
        go.transform.SetParent(_canvas.transform, false);

        panel           = (RectTransform)go.transform;
        panel.pivot     = new Vector2(0.5f, 0f);
        panel.anchorMin = panel.anchorMax = Vector2.zero;
        panel.sizeDelta = new Vector2(160f, 0f);

        go.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.09f, 0.88f);
        go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 2f;
        vlg.padding = new RectOffset(8, 8, 6, 6);

        nameText     = MakeLabel(go.transform, "Name",    13f, FontStyles.Bold,   new Color(0.788f, 0.659f, 0.298f));
        hpText       = MakeLabel(go.transform, "HP",      11f, FontStyles.Normal, new Color(0.35f,  0.85f,  0.40f));
        _previewText = MakeLabel(go.transform, "Preview", 11f, FontStyles.Bold,   Color.white);
        _previewText.gameObject.SetActive(false);

        panel.gameObject.SetActive(false);
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
