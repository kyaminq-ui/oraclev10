using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Panneau de chat en combat : logs de sort + messagerie joueur.
/// Se construit seul au démarrage. Ajoutez-le via Oracle > Build Chat UI.
/// </summary>
public class CombatChatUI : MonoBehaviour
{
    [Header("Taille du panneau")]
    public float panelWidth  = 310f;
    public float panelHeight = 200f;

    [Header("Position (ancre bas-gauche du Canvas)")]
    public float offsetX = 12f;
    public float offsetY = 160f;

    [Header("Messages")]
    public int maxMessages = 60;

    // ---- couleurs ----
    static readonly Color BgPanel     = new Color(0.05f, 0.05f, 0.08f, 0.82f);
    static readonly Color BgInputRow  = new Color(0.08f, 0.08f, 0.12f, 1.00f);
    static readonly Color BgField     = new Color(0.14f, 0.14f, 0.18f, 1.00f);
    static readonly Color AccentGold  = new Color(0.788f, 0.659f, 0.298f, 1f);
    static readonly Color LogColor    = new Color(0.78f, 0.78f, 0.78f, 1f);
    static readonly Color ChatSelf    = new Color(0.90f, 0.85f, 0.50f, 1f);
    static readonly Color ChatOther   = Color.white;
    static readonly Color Placeholder = new Color(0.42f, 0.42f, 0.45f, 1f);

    // ---- runtime refs ----
    GameObject     _panelRoot;
    RectTransform  _content;
    ScrollRect     _scroll;
    TMP_InputField _input;
    readonly List<GameObject> _messages = new();

    const float INPUT_H = 28f;
    const float DRAG_HEADER_H = 20f;
    const float RESIZE_GRIP = 16f;
    const float BTN_W   = 56f;
    const float PAD     = 4f;

    // ------------------------------------------------------------------ lifecycle

    void Awake() => BuildUI();

    void Start()
    {
        LoadChatGeometry();
        CombatLog.OnMessage     += OnCombatMessage;
        CombatLog.OnChatMessage += OnChatMessage;
    }

    void OnDestroy()
    {
        CombatLog.OnMessage     -= OnCombatMessage;
        CombatLog.OnChatMessage -= OnChatMessage;
        if (_panelRoot != null) Destroy(_panelRoot);
    }

    // ------------------------------------------------------------------ handlers

    void OnCombatMessage(string msg) => AppendLine(msg, LogColor);

    void OnChatMessage(string sender, string msg)
    {
        bool isSelf = sender == LocalPlayerName();
        AppendLine($"<b>{sender}</b> : {msg}", isSelf ? ChatSelf : ChatOther);
    }

    // ------------------------------------------------------------------ display

    void AppendLine(string text, Color color)
    {
        if (_content == null) return;

        if (_messages.Count >= maxMessages)
        {
            Destroy(_messages[0]);
            _messages.RemoveAt(0);
        }

        var go = new GameObject("Msg", typeof(RectTransform));
        go.transform.SetParent(_content, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text               = text;
        tmp.fontSize           = 11f;
        tmp.color              = color;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget      = false;

        // Donne une hauteur préférée basée sur le contenu
        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;

        _messages.Add(go);
        StartCoroutine(ScrollToBottom());
    }

    IEnumerator ScrollToBottom()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (_scroll != null)
            _scroll.verticalNormalizedPosition = 0f;
    }

    // ------------------------------------------------------------------ input

    void OnSendClicked() => Submit(_input != null ? _input.text : string.Empty);

    void Submit(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        string msg = text.Trim();
        if (_input != null) _input.text = string.Empty;

        string sender = LocalPlayerName();
        if (OracleCombatNetBridge.Instance != null &&
            OracleCombatNetBridge.Instance.ShouldSendCommandsOverNetwork)
            OracleCombatNetBridge.Instance.TrySubmitChat(sender, msg);
        else
            CombatLog.AppendChat(sender, msg);
    }

    string LocalPlayerName()
    {
        var ci = CombatInitializer.Instance;
        if (ci == null) return "Joueur";
        var bridge = OracleCombatNetBridge.Instance;
        var ch = bridge != null ? bridge.GetLocalControlledCharacter() : ci.player;
        return ch != null ? ch.name : "Joueur";
    }

    // ------------------------------------------------------------------ construction UI
    // Positionnement RectTransform explicite — aucun LayoutGroup sur la racine.

    void BuildUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[CombatChatUI] Aucun Canvas dans la scène.");
            return;
        }

        // ── Racine du panneau ────────────────────────────────────────────
        _panelRoot = new GameObject("ChatPanel", typeof(RectTransform));
        _panelRoot.transform.SetParent(canvas.transform, false);

        var root = RT(_panelRoot);
        root.anchorMin        = root.anchorMax = Vector2.zero;
        root.pivot            = Vector2.zero;
        root.sizeDelta        = new Vector2(panelWidth, panelHeight);
        root.anchoredPosition = new Vector2(offsetX, offsetY);

        _panelRoot.AddComponent<Image>().color = BgPanel;

        // ── Barre de titre (glisser-déposer le panneau) ─────────────────
        var headerGo = UIChild(_panelRoot, "ChatDragHeader");
        var headerRt = RT(headerGo);
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.sizeDelta = new Vector2(0f, DRAG_HEADER_H);
        headerRt.anchoredPosition = Vector2.zero;
        headerGo.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.14f, 0.98f);
        var titleGo = UIChild(headerGo, "Title");
        Stretch(RT(titleGo));
        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "Chat — glisser";
        titleTmp.fontSize = 10f;
        titleTmp.color = AccentGold;
        titleTmp.alignment = TextAlignmentOptions.Left;
        titleTmp.margin = new Vector4(8f, 2f, 4f, 0f);
        titleTmp.raycastTarget = false;

        var mover = headerGo.AddComponent<CombatChatPanelMover>();
        mover.panel = root;
        mover.onLayoutEnd = SaveChatGeometry;

        // ── Zone de scroll (remplit tout sauf en-tête + ligne d'input) ──
        var scrollGo = UIChild(_panelRoot, "Scroll");
        var scrollRt = RT(scrollGo);
        scrollRt.anchorMin  = Vector2.zero;
        scrollRt.anchorMax  = Vector2.one;
        scrollRt.offsetMin  = new Vector2(0f, INPUT_H);
        scrollRt.offsetMax  = new Vector2(0f, -DRAG_HEADER_H);

        _scroll = scrollGo.AddComponent<ScrollRect>();
        _scroll.horizontal        = false;
        _scroll.vertical          = true;
        _scroll.movementType      = ScrollRect.MovementType.Clamped;
        _scroll.scrollSensitivity = 20f;
        _scroll.verticalScrollbar = null;

        // Viewport (masque les débordements)
        var vpGo = UIChild(scrollGo, "Viewport");
        var vpRt = RT(vpGo);
        vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = vpRt.offsetMax = Vector2.zero;
        vpGo.AddComponent<RectMask2D>();   // plus simple et fiable que Mask + Image
        _scroll.viewport = vpRt;

        // Content (s'agrandit avec les messages, ancré en haut)
        var contentGo = UIChild(vpGo, "Content");
        _content = RT(contentGo);
        _content.anchorMin        = new Vector2(0f, 1f);
        _content.anchorMax        = new Vector2(1f, 1f);
        _content.pivot            = new Vector2(0.5f, 1f);
        _content.sizeDelta        = Vector2.zero;
        _content.anchoredPosition = Vector2.zero;

        var cVlg = contentGo.AddComponent<VerticalLayoutGroup>();
        cVlg.childControlWidth     = true;
        cVlg.childControlHeight    = true;
        cVlg.childForceExpandWidth  = true;
        cVlg.childForceExpandHeight = false;
        cVlg.spacing = 2f;
        cVlg.padding = new RectOffset(6, 6, 4, 4);

        contentGo.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        _scroll.content = _content;

        // ── Ligne d'input (bande en bas, hauteur fixe) ──────────────────
        var rowGo = UIChild(_panelRoot, "InputRow");
        var rowRt = RT(rowGo);
        rowRt.anchorMin        = Vector2.zero;
        rowRt.anchorMax        = new Vector2(1f, 0f);
        rowRt.pivot            = Vector2.zero;
        rowRt.sizeDelta        = new Vector2(0f, INPUT_H);
        rowRt.anchoredPosition = Vector2.zero;
        rowGo.AddComponent<Image>().color = BgInputRow;

        // Champ de saisie
        var ifGo = UIChild(rowGo, "Field");
        var ifRt = RT(ifGo);
        ifRt.anchorMin = Vector2.zero;
        ifRt.anchorMax = Vector2.one;
        ifRt.offsetMin = new Vector2(PAD, 3f);
        ifRt.offsetMax = new Vector2(-(BTN_W + PAD + 2f), -3f);
        ifGo.AddComponent<Image>().color = BgField;

        _input = ifGo.AddComponent<TMP_InputField>();
        _input.lineType = TMP_InputField.LineType.SingleLine;

        // Zone de texte avec clipping
        var taGo = UIChild(ifGo, "TextArea");
        var taRt = RT(taGo);
        taRt.anchorMin = Vector2.zero; taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(5f, 1f); taRt.offsetMax = new Vector2(-5f, -1f);
        taGo.AddComponent<RectMask2D>();
        _input.textViewport = taRt;

        var phGo  = UIChild(taGo, "Placeholder");
        Stretch(RT(phGo));
        var phTmp = phGo.AddComponent<TextMeshProUGUI>();
        phTmp.text              = "Envoyer un message…";
        phTmp.fontSize          = 10f;
        phTmp.color             = Placeholder;
        phTmp.fontStyle         = FontStyles.Italic;
        phTmp.raycastTarget     = false;
        phTmp.enableWordWrapping = false;

        var txtGo  = UIChild(taGo, "Text");
        Stretch(RT(txtGo));
        var txtTmp = txtGo.AddComponent<TextMeshProUGUI>();
        txtTmp.fontSize          = 10f;
        txtTmp.color             = Color.white;
        txtTmp.raycastTarget     = false;
        txtTmp.enableWordWrapping = false;

        _input.textComponent = txtTmp;
        _input.placeholder   = phTmp;
        _input.onSubmit.AddListener(t => Submit(t));

        // Bouton Envoyer
        var btnGo = UIChild(rowGo, "Send");
        var btnRt = RT(btnGo);
        btnRt.anchorMin        = new Vector2(1f, 0f);
        btnRt.anchorMax        = new Vector2(1f, 1f);
        btnRt.pivot            = new Vector2(1f, 0.5f);
        btnRt.sizeDelta        = new Vector2(BTN_W, -6f);
        btnRt.anchoredPosition = new Vector2(-PAD, 0f);

        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = AccentGold;
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        var cs = btn.colors;
        cs.highlightedColor = new Color(0.88f, 0.75f, 0.38f, 1f);
        cs.pressedColor     = new Color(0.60f, 0.50f, 0.22f, 1f);
        btn.colors = cs;
        btn.onClick.AddListener(OnSendClicked);

        var lblGo  = UIChild(btnGo, "Lbl");
        Stretch(RT(lblGo));
        var lblTmp = lblGo.AddComponent<TextMeshProUGUI>();
        lblTmp.text          = "Envoyer";
        lblTmp.fontSize      = 8.5f;
        lblTmp.alignment     = TextAlignmentOptions.Center;
        lblTmp.color         = new Color(0.10f, 0.07f, 0.03f, 1f);
        lblTmp.raycastTarget = false;

        // ── Coin redimensionnement ────────────────────────────────────
        var gripGo = UIChild(_panelRoot, "ResizeGrip");
        var gripRt = RT(gripGo);
        gripRt.anchorMin = gripRt.anchorMax = new Vector2(1f, 0f);
        gripRt.pivot = new Vector2(1f, 0f);
        gripRt.sizeDelta = new Vector2(RESIZE_GRIP, RESIZE_GRIP);
        gripRt.anchoredPosition = new Vector2(-1f, INPUT_H + 1f);
        gripGo.AddComponent<Image>().color = new Color(0.788f, 0.659f, 0.298f, 0.4f);
        var resizer = gripGo.AddComponent<CombatChatPanelResizer>();
        resizer.panel = root;
        resizer.onLayoutEnd = SaveChatGeometry;
    }

    void LoadChatGeometry()
    {
        if (_panelRoot == null) return;
        if (!PlayerPrefs.HasKey("Oracle.CombatChat.W")) return;
        var rt = RT(_panelRoot);
        rt.sizeDelta = new Vector2(
            PlayerPrefs.GetFloat("Oracle.CombatChat.W"),
            PlayerPrefs.GetFloat("Oracle.CombatChat.H"));
        rt.anchoredPosition = new Vector2(
            PlayerPrefs.GetFloat("Oracle.CombatChat.PosX"),
            PlayerPrefs.GetFloat("Oracle.CombatChat.PosY"));
    }

    void SaveChatGeometry()
    {
        if (_panelRoot == null) return;
        var rt = RT(_panelRoot);
        PlayerPrefs.SetFloat("Oracle.CombatChat.PosX", rt.anchoredPosition.x);
        PlayerPrefs.SetFloat("Oracle.CombatChat.PosY", rt.anchoredPosition.y);
        PlayerPrefs.SetFloat("Oracle.CombatChat.W", rt.sizeDelta.x);
        PlayerPrefs.SetFloat("Oracle.CombatChat.H", rt.sizeDelta.y);
        PlayerPrefs.Save();
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    static GameObject UIChild(GameObject parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    static RectTransform RT(GameObject go) => (RectTransform)go.transform;

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}

sealed class CombatChatPanelMover : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RectTransform panel;
    public Action onLayoutEnd;

    Canvas _canvas;

    void Awake() => _canvas = GetComponentInParent<Canvas>();

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (panel == null || _canvas == null) return;
        panel.anchoredPosition += eventData.delta / _canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData) => onLayoutEnd?.Invoke();
}

sealed class CombatChatPanelResizer : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public RectTransform panel;
    public Vector2 minSize = new Vector2(200f, 120f);
    public Vector2 maxSize = new Vector2(720f, 560f);
    public Action onLayoutEnd;

    Canvas _canvas;

    void Awake() => _canvas = GetComponentInParent<Canvas>();

    public void OnDrag(PointerEventData eventData)
    {
        if (panel == null || _canvas == null) return;
        var d = eventData.delta / _canvas.scaleFactor;
        var s = panel.sizeDelta;
        s.x = Mathf.Clamp(s.x + d.x, minSize.x, maxSize.x);
        s.y = Mathf.Clamp(s.y + d.y, minSize.y, maxSize.y);
        panel.sizeDelta = s;
    }

    public void OnEndDrag(PointerEventData eventData) => onLayoutEnd?.Invoke();
}
