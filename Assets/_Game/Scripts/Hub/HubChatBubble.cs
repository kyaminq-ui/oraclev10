using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bulle de dialogue blanche au-dessus du personnage local dans le hub.
/// Placer sur LocalPlayer (même GO que HubCharacterController).
/// Appeler ShowMessage(text) pour déclencher la bulle ; elle disparaît automatiquement.
/// </summary>
public class HubChatBubble : MonoBehaviour
{
    [Header("Position")]
    [Tooltip("Décalage en unités monde par rapport au pivot du personnage.")]
    public Vector3 worldOffset = new Vector3(0f, 1.25f, 0f);

    [Header("Timing")]
    public float showDuration = 4f;
    public float fadeDuration = 0.6f;
    public float maxWidth     = 200f;

    // ── Palette bulle ────────────────────────────────────────────────────
    static readonly Color BG_WHITE = new Color(1f,    1f,    1f,    0.97f);
    static readonly Color TXT_DARK = new Color(0.10f, 0.10f, 0.12f, 1f);

    // ── Refs ─────────────────────────────────────────────────────────────
    Camera          _cam;
    Canvas          _canvas;
    RectTransform   _root;
    Image           _bgImg;
    TextMeshProUGUI _tmp;
    Coroutine       _co;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Start()
    {
        _cam    = Camera.main;
        _canvas = FindObjectOfType<Canvas>();
        if (_canvas != null) CreateBubble();
    }

    void LateUpdate()
    {
        if (_root == null || !_root.gameObject.activeSelf || _cam == null) return;
        Vector3 sp     = _cam.WorldToScreenPoint(transform.position + worldOffset);
        _root.position = new Vector3(sp.x, sp.y, 0f);
    }

    // ── Construction UI ───────────────────────────────────────────────────

    void CreateBubble()
    {
        // ── Racine : Image blanche + LayoutGroup + ContentSizeFitter ──
        var go = new GameObject("HubSpeechBubble", typeof(RectTransform));
        go.transform.SetParent(_canvas.transform, false);

        _root           = (RectTransform)go.transform;
        _root.sizeDelta = new Vector2(maxWidth, 30f);
        _root.pivot     = new Vector2(0.5f, 0f);   // ancré par le bas

        _bgImg       = go.AddComponent<Image>();
        _bgImg.color = BG_WHITE;

        // VerticalLayoutGroup pour que ContentSizeFitter puisse calculer la hauteur
        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(8, 8, 5, 5);

        go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Texte (enfant direct — le LayoutGroup drive sa hauteur) ──
        var tGo = new GameObject("Text", typeof(RectTransform));
        tGo.transform.SetParent(go.transform, false);

        _tmp                    = tGo.AddComponent<TextMeshProUGUI>();
        _tmp.fontSize           = 11f;
        _tmp.color              = TXT_DARK;
        _tmp.alignment          = TextAlignmentOptions.Center;
        _tmp.enableWordWrapping = true;
        _tmp.raycastTarget      = false;

        go.SetActive(false);
    }

    // ── API publique ──────────────────────────────────────────────────────

    /// <summary>Affiche la bulle blanche avec le message au-dessus du personnage.</summary>
    public void ShowMessage(string text)
    {
        if (_root == null) return;

        _tmp.text     = text;
        _bgImg.color  = BG_WHITE;
        _tmp.color    = TXT_DARK;
        _root.gameObject.SetActive(true);

        // Forcer la mise à jour du layout avant de positionner
        Canvas.ForceUpdateCanvases();
        LateUpdate();

        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(HideRoutine());
    }

    // ── Coroutine ─────────────────────────────────────────────────────────

    IEnumerator HideRoutine()
    {
        yield return new WaitForSeconds(showDuration);
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = 1f - t / fadeDuration;
            _bgImg.color = new Color(BG_WHITE.r, BG_WHITE.g, BG_WHITE.b, BG_WHITE.a * a);
            _tmp.color   = new Color(TXT_DARK.r, TXT_DARK.g, TXT_DARK.b, a);
            yield return null;
        }
        _root.gameObject.SetActive(false);
    }
}
