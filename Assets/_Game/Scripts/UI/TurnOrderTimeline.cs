using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Timeline d'ordre de tour style Dofus : portraits des combattants en bas à droite du HUD,
/// slot actif mis en avant, infobulle au survol listant tous les effets actifs.
/// S'auto-construit — aucune config requise, juste poser le composant dans le Canvas.
/// </summary>
public class TurnOrderTimeline : MonoBehaviour
{
    // ── Constantes visuelles ───────────────────────────────────
    const float SlotActive      = 76f;
    const float SlotInactive    = 58f;
    const float SlotSpacing     = 10f;
    const float StripEdgeMargin = 10f;
    const float HpBarHeight     = 5f;
    const float BorderThick     = 3f;

    static readonly Color C_BgDark       = new Color(0.05f, 0.05f, 0.09f, 0.92f);
    static readonly Color C_BorderActive = new Color(0.87f, 0.72f, 0.25f, 1f);
    static readonly Color C_BorderIdle   = new Color(0.20f, 0.20f, 0.28f, 1f);
    static readonly Color C_HpFull       = new Color(0.30f, 0.82f, 0.35f, 1f);
    static readonly Color C_HpLow        = new Color(0.90f, 0.25f, 0.25f, 1f);
    static readonly Color C_TeamA        = new Color(0.35f, 0.60f, 1.00f, 1f);
    static readonly Color C_TeamB        = new Color(1.00f, 0.35f, 0.35f, 1f);
    static readonly Color C_Debuff       = new Color(1.00f, 0.38f, 0.38f, 1f);
    static readonly Color C_Buff         = new Color(0.42f, 0.85f, 1.00f, 1f);
    static readonly Color C_Gold         = new Color(0.79f, 0.66f, 0.30f, 1f);

    // ── État interne ───────────────────────────────────────────
    Canvas        _canvas;
    RectTransform _strip;

    // Tooltip partagé
    RectTransform   _tipRt;
    TextMeshProUGUI _tipText;

    class SlotView
    {
        public TacticalCharacter character;
        public RectTransform     root;
        public Image             border;
        public Image             avatar;
        public Image             hpFill;
        public LayoutElement     layoutElem;
    }

    readonly List<SlotView> _slots = new List<SlotView>();

    // ── Lifecycle ─────────────────────────────────────────────

    void Awake()
    {
        // Remonter jusqu'au canvas racine pour avoir le GraphicRaycaster
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null && !_canvas.isRootCanvas)
            _canvas = _canvas.rootCanvas;
        if (_canvas == null)
            _canvas = FindObjectOfType<Canvas>();

        // Le canvas racine doit avoir un GraphicRaycaster pour les événements souris UI
        if (_canvas != null && _canvas.GetComponent<GraphicRaycaster>() == null)
            _canvas.gameObject.AddComponent<GraphicRaycaster>();
    }

    IEnumerator Start()
    {
        yield return new WaitUntil(() =>
            TurnManager.Instance != null && TurnManager.Instance.TurnOrder.Count > 0);

        BuildStrip();
        BuildTooltip();

        Refresh(TurnManager.Instance.CurrentCharacter);
        TurnManager.Instance.OnTurnStart += Refresh;
    }

    void OnDestroy()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnStart -= Refresh;
    }

    // ── Update : détection souris sur les slots ────────────────

    void Update()
    {
        if (_tipRt == null || _slots.Count == 0) return;

        // Caméra null pour ScreenSpaceOverlay, sinon worldCamera
        Camera rayCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : _canvas.worldCamera;

        SlotView hovered = null;
        foreach (var s in _slots)
        {
            if (s.root != null &&
                RectTransformUtility.RectangleContainsScreenPoint(s.root, Input.mousePosition, rayCam))
            {
                hovered = s;
                break;
            }
        }

        if (hovered != null)
            ShowTooltip(hovered);
        else
            HideTooltip();
    }

    // ── Construction ──────────────────────────────────────────

    void BuildStrip()
    {
        var go = new GameObject("TurnTimeline", typeof(RectTransform));
        go.transform.SetParent(_canvas.transform, false);

        _strip = (RectTransform)go.transform;
        _strip.anchorMin = _strip.anchorMax = new Vector2(1f, 0f);
        _strip.pivot     = new Vector2(1f, 0f);
        _strip.anchoredPosition = new Vector2(-StripEdgeMargin, StripEdgeMargin);

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment        = TextAnchor.UpperCenter;
        hlg.spacing               = SlotSpacing;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;

        var csf = go.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        var order = TurnManager.Instance.TurnOrder;
        for (int i = 0; i < order.Count; i++)
            _slots.Add(BuildSlot(order[i], i));
    }

    SlotView BuildSlot(TacticalCharacter tc, int idx)
    {
        float sz = SlotInactive;

        var rootGo = new GameObject($"Slot_{tc.name}", typeof(RectTransform));
        rootGo.transform.SetParent(_strip, false);
        var root = (RectTransform)rootGo.transform;

        var le = rootGo.AddComponent<LayoutElement>();
        le.preferredWidth  = sz;
        le.preferredHeight = sz + HpBarHeight + 4f;

        // Fond sombre — raycastTarget true (default) pour détecter la souris
        rootGo.AddComponent<Image>().color = C_BgDark;

        // ── Bordure ──
        var borderGo = new GameObject("Border", typeof(RectTransform));
        borderGo.transform.SetParent(rootGo.transform, false);
        var borderRt = (RectTransform)borderGo.transform;
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.offsetMin = borderRt.offsetMax = Vector2.zero;
        var border = borderGo.AddComponent<Image>();
        border.color = C_BorderIdle;
        border.raycastTarget = false;

        // ── Avatar ──
        var avGo = new GameObject("Avatar", typeof(RectTransform));
        avGo.transform.SetParent(rootGo.transform, false);
        var avRt = (RectTransform)avGo.transform;
        float avSz = sz - BorderThick * 2f;
        avRt.anchorMin = new Vector2(0.5f, 1f);
        avRt.anchorMax = new Vector2(0.5f, 1f);
        avRt.pivot     = new Vector2(0.5f, 1f);
        avRt.sizeDelta = new Vector2(avSz, avSz);
        avRt.anchoredPosition = new Vector2(0f, -BorderThick);
        var avatar = avGo.AddComponent<Image>();
        avatar.preserveAspect = true;
        avatar.raycastTarget  = false;

        var sp = tc.spriteRenderer != null ? tc.spriteRenderer.sprite : null;
        if (sp != null) { avatar.sprite = sp; avatar.color = Color.white; }
        else             avatar.color = (idx == 0 ? C_TeamA : C_TeamB);

        // ── Barre HP ──
        var hpTrackGo = new GameObject("HpTrack", typeof(RectTransform));
        hpTrackGo.transform.SetParent(rootGo.transform, false);
        var hpTrackRt = (RectTransform)hpTrackGo.transform;
        hpTrackRt.anchorMin = new Vector2(0f, 0f);
        hpTrackRt.anchorMax = new Vector2(1f, 0f);
        hpTrackRt.pivot     = new Vector2(0.5f, 0f);
        hpTrackRt.sizeDelta = new Vector2(-BorderThick * 2f, HpBarHeight);
        hpTrackRt.anchoredPosition = new Vector2(0f, BorderThick);
        hpTrackGo.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f);
        hpTrackGo.GetComponent<Image>().raycastTarget = false;

        var hpFillGo = new GameObject("Fill", typeof(RectTransform));
        hpFillGo.transform.SetParent(hpTrackGo.transform, false);
        var hpFillRt = (RectTransform)hpFillGo.transform;
        hpFillRt.anchorMin = Vector2.zero;
        hpFillRt.anchorMax = Vector2.one;
        hpFillRt.offsetMin = hpFillRt.offsetMax = Vector2.zero;
        var hpFill = hpFillGo.AddComponent<Image>();
        hpFill.type          = Image.Type.Filled;
        hpFill.fillMethod    = Image.FillMethod.Horizontal;
        hpFill.raycastTarget = false;
        UpdateHpFill(hpFill, tc);

        tc.OnHPChanged += (cur, max) => UpdateHpFill(hpFill, tc);

        return new SlotView
        {
            character  = tc,
            root       = root,
            border     = border,
            avatar     = avatar,
            hpFill     = hpFill,
            layoutElem = le,
        };
    }

    static void UpdateHpFill(Image fill, TacticalCharacter tc)
    {
        if (fill == null) return;
        float r = tc.stats != null && tc.stats.maxHP > 0
            ? Mathf.Clamp01((float)tc.CurrentHP / tc.stats.maxHP) : 1f;
        fill.fillAmount = r;
        fill.color = Color.Lerp(C_HpLow, C_HpFull, r);
    }

    // ── Construction du tooltip ───────────────────────────────
    // Même pattern que CombatTooltipSystem : panel simple, pas de Canvas imbriqué,
    // ancre centrale (0.5, 0.5) pour que anchoredPosition = coordonnées locales du canvas.

    void BuildTooltip()
    {
        var go = new GameObject("TurnTooltip", typeof(RectTransform));
        go.transform.SetParent(_canvas.transform, false);
        // Doit être au-dessus de tout — on le place en dernier enfant du canvas
        go.transform.SetAsLastSibling();

        _tipRt        = (RectTransform)go.transform;
        _tipRt.pivot  = new Vector2(0.5f, 0f);   // bas-centre : la bulle monte au-dessus du slot
        // Ancre CENTRE (0.5, 0.5) : anchoredPosition = coordonnées locales centrées du canvas
        _tipRt.anchorMin = _tipRt.anchorMax = new Vector2(0.5f, 0.5f);
        _tipRt.sizeDelta = new Vector2(220f, 0f);

        var bg = go.AddComponent<Image>();
        bg.color         = C_BgDark;
        bg.raycastTarget = false;

        go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 3f;
        vlg.padding = new RectOffset(10, 10, 8, 8);

        var tGo = new GameObject("Text", typeof(RectTransform));
        tGo.transform.SetParent(go.transform, false);
        _tipText = tGo.AddComponent<TextMeshProUGUI>();
        _tipText.fontSize           = 11f;
        _tipText.color              = Color.white;
        _tipText.raycastTarget      = false;
        _tipText.enableWordWrapping = false;
        tGo.AddComponent<LayoutElement>().flexibleWidth = 1f;

        go.SetActive(false);
    }

    // ── Refresh (changement de tour) ─────────────────────────

    void Refresh(TacticalCharacter active)
    {
        foreach (var s in _slots)
        {
            bool cur  = s.character == active;
            float sz  = cur ? SlotActive : SlotInactive;

            s.layoutElem.preferredWidth  = sz;
            s.layoutElem.preferredHeight = sz + HpBarHeight + 4f;
            s.border.color = cur ? C_BorderActive : C_BorderIdle;

            float avSz = sz - BorderThick * 2f;
            s.avatar.rectTransform.sizeDelta = new Vector2(avSz, avSz);

            UpdateHpFill(s.hpFill, s.character);
        }
    }

    // ── Tooltip ───────────────────────────────────────────────

    void ShowTooltip(SlotView slot)
    {
        if (_tipRt == null) return;
        var tc = slot.character;
        var sb = new StringBuilder();

        // ── Nom ──
        sb.AppendLine($"<b><color=#{ColorUtility.ToHtmlStringRGB(C_Gold)}>{tc.name}</color></b>");

        if (tc.stats != null)
        {
            // PV
            float r = tc.stats.maxHP > 0 ? (float)tc.CurrentHP / tc.stats.maxHP : 0f;
            Color hpCol = r > 0.5f ? C_HpFull : r > 0.25f ? new Color(0.9f, 0.75f, 0.1f) : C_HpLow;
            sb.AppendLine($"<color=#{ColorUtility.ToHtmlStringRGB(hpCol)}>{tc.CurrentHP} / {tc.stats.maxHP} PV</color>");

            // PA
            string paBonus = tc.NextTurnBonusPA > 0
                ? $"  <color=#{ColorUtility.ToHtmlStringRGB(C_Buff)}>(+{tc.NextTurnBonusPA} prochain tour)</color>" : "";
            sb.AppendLine($"PA : {tc.CurrentPA} / {tc.stats.maxPA}{paBonus}");

            // PM
            string pmBonus = tc.NextTurnBonusPM > 0
                ? $"  <color=#{ColorUtility.ToHtmlStringRGB(C_Buff)}>(+{tc.NextTurnBonusPM} prochain tour)</color>" : "";
            sb.AppendLine($"PM : {tc.CurrentPM} / {tc.stats.maxPM}{pmBonus}");
        }

        // ── Séparateur + effets ──
        sb.AppendLine("<color=#444455>───────────────</color>");
        var effects = tc.ActiveStatusEffects;
        if (effects != null && effects.Count > 0)
        {
            foreach (var e in effects)
            {
                Color c = e.isDebuff ? C_Debuff : C_Buff;
                sb.AppendLine($"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{EffectLabel(e)}</color>");
            }
        }
        else
        {
            sb.AppendLine("<color=#556655>Aucun effet actif</color>");
        }

        _tipText.text = sb.ToString().TrimEnd('\n', '\r');

        // ── Position au-dessus du slot ──
        // Utilise les coins world du slot → screen → local canvas (coordonnées centrées)
        Vector3[] corners = new Vector3[4];
        slot.root.GetWorldCorners(corners);
        // corners[1]=top-left  corners[2]=top-right (dans ScreenSpaceOverlay, ce sont des coords écran)
        Vector3 topCenter = (corners[1] + corners[2]) * 0.5f;

        Camera rayCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : _canvas.worldCamera;

        // Convertit vers coordonnées locales centrées du canvas
        // (anchorMin = anchorMax = (0.5,0.5) → anchoredPosition = coordonnées locales)
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)_canvas.transform,
            new Vector2(topCenter.x, topCenter.y),
            rayCam,
            out Vector2 localPos);

        // Clamp horizontal : pivot (0.5,0) → les bords de la bulle sont à ±halfTipW du centre
        var canvasRt  = (RectTransform)_canvas.transform;
        float halfCW  = canvasRt.rect.width  * 0.5f;
        float halfTipW = _tipRt.sizeDelta.x  * 0.5f;
        const float margin = 8f;
        float clampedX = Mathf.Clamp(localPos.x,
            -halfCW + halfTipW + margin,
             halfCW - halfTipW - margin);

        _tipRt.anchoredPosition = new Vector2(clampedX, localPos.y + 12f);

        // S'assurer que la bulle est rendue par-dessus tout le reste
        _tipRt.SetAsLastSibling();
        _tipRt.gameObject.SetActive(true);
    }

    void HideTooltip()
    {
        if (_tipRt != null && _tipRt.gameObject.activeSelf)
            _tipRt.gameObject.SetActive(false);
    }

    // ── Labels des effets ─────────────────────────────────────

    static string EffectLabel(StatusEffect e)
    {
        string d = e.turnsRemaining > 0 ? $"  ({e.turnsRemaining}T)" : "";
        switch (e.type)
        {
            case StatusEffectType.Bleed:           return $"Saignement  -{e.value} PV/tour{d}";
            case StatusEffectType.Silence:         return $"Silence{d}";
            case StatusEffectType.DamageReduction: return $"Réduction dégâts  -{e.value}/hit{d}";
            case StatusEffectType.Thorns:          return $"Épines  +{e.value} dmg renvoyés{d}";
            case StatusEffectType.Invisible:       return $"Invisible{d}";
            case StatusEffectType.Shield:          return $"Bouclier  {e.value} PV absorbés{d}";
            case StatusEffectType.BonusPANextTurn: return $"+{e.value} PA au prochain tour";
            case StatusEffectType.GravityDebuff:   return $"Gravité  (pas de dash/téléport){d}";
            case StatusEffectType.LastBreath:      return "Second Souffle  (survie à 1 PV)";
            case StatusEffectType.ReducedAttack:   return $"Prochaine attaque réduite{d}";
            case StatusEffectType.ReducedPM:       return $"PM réduit  (-{e.value} PM){d}";
            default:                               return e.type.ToString() + d;
        }
    }
}
