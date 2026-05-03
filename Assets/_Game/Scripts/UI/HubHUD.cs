using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD du hub : chat global, barre de navigation, modal d'arène et deck builder.
/// Se construit entièrement en runtime depuis Awake (même pattern que CombatChatUI).
/// Créer la scène Hub via Oracle > Create Hub Scene.
/// </summary>
public class HubHUD : MonoBehaviour
{
    // ── Palette Oracle ──────────────────────────────────────────────────
    static readonly Color BgDark      = new Color(0.05f, 0.05f, 0.08f, 0.88f);
    static readonly Color BgMedium    = new Color(0.10f, 0.10f, 0.14f, 0.95f);
    static readonly Color BgField     = new Color(0.14f, 0.14f, 0.18f, 1.00f);
    static readonly Color AccentGold  = new Color(0.788f, 0.659f, 0.298f, 1f);
    static readonly Color AccentGoldH = new Color(0.88f,  0.75f,  0.38f, 1f);
    static readonly Color AccentGoldP = new Color(0.60f,  0.50f,  0.22f, 1f);
    static readonly Color TextWhite   = Color.white;
    static readonly Color TextGray    = new Color(0.78f, 0.78f, 0.78f, 1f);
    static readonly Color TextDim     = new Color(0.42f, 0.42f, 0.45f, 1f);
    static readonly Color Overlay     = new Color(0f, 0f, 0f, 0.65f);
    static readonly Color BtnDisabled = new Color(0.22f, 0.22f, 0.26f, 1f);
    static readonly Color TextDisabled= new Color(0.45f, 0.45f, 0.48f, 1f);
    static readonly Color NavBtnNorm  = new Color(0.12f, 0.12f, 0.17f, 0.95f);
    static readonly Color NavBtnHov   = new Color(0.18f, 0.18f, 0.24f, 1f);
    static readonly Color NavBtnPrs   = new Color(0.07f, 0.07f, 0.10f, 1f);

    // ── Palette deck builder ─────────────────────────────────────────────
    static readonly Color CardBg       = new Color(0.12f, 0.12f, 0.17f, 1f);
    static readonly Color CardBgSel    = new Color(0.20f, 0.16f, 0.07f, 1f);
    static readonly Color CardBgDim    = new Color(0.08f, 0.08f, 0.10f, 0.65f);
    static readonly Color TabActive    = new Color(0.22f, 0.18f, 0.08f, 1f);
    static readonly Color TabNorm      = new Color(0.10f, 0.10f, 0.14f, 1f);
    // catégories
    static readonly Color CatAtk  = new Color(0.75f, 0.25f, 0.20f, 1f);
    static readonly Color CatTac  = new Color(0.25f, 0.45f, 0.75f, 1f);
    static readonly Color CatSur  = new Color(0.25f, 0.65f, 0.30f, 1f);

    // ── Paramètres chat ─────────────────────────────────────────────────
    [Header("Chat")]
    public float chatWidth  = 320f;
    public float chatHeight = 210f;
    public int   maxMessages = 60;

    RectTransform  _chatContent;
    ScrollRect     _chatScroll;
    TMP_InputField _chatInput;
    readonly List<GameObject> _chatMessages = new();

    // ── Modal arène ─────────────────────────────────────────────────────
    GameObject      _arenaModal;
    GameObject      _searchingPanel;
    TextMeshProUGUI _searchingLabel;
    Button          _cancelSearchBtn;

    // ── Deck builder ─────────────────────────────────────────────────────
    GameObject        _deckModal;
    RectTransform     _deckScrollContent;
    TextMeshProUGUI   _deckCounter;

    SpellDeckCategory _currentTab     = SpellDeckCategory.Attack;
    readonly List<SpellData> _allSpells      = new();
    readonly List<SpellData> _selectedSpells = new();

    // Références aux cartes visibles (onglet courant)
    struct SpellCardView
    {
        public SpellData spell;
        public Image     bg;
        public GameObject selMark;
    }
    readonly List<SpellCardView> _currentCards = new();

    // Références aux boutons d'onglet (0=Attack,1=Tactic,2=Survival)
    static readonly (SpellDeckCategory cat, string label, Color color)[] Tabs =
    {
        (SpellDeckCategory.Attack,   "Attaques", CatAtk),
        (SpellDeckCategory.Tactic,   "Tactique", CatTac),
        (SpellDeckCategory.Survival, "Survie",   CatSur),
    };
    readonly Image[] _tabImages = new Image[3];

    // Caméra isométrique — pour bloquer le zoom quand le deck builder est ouvert
    IsometricCamera _isoCam;

    // Label PA/PM (mis à jour à l'ouverture du deck builder)
    TextMeshProUGUI _statsLabel;

    // ── Lifecycle ───────────────────────────────────────────────────────
    void Awake() => BuildAll();

    void BuildAll()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[HubHUD] Aucun Canvas dans la scène — HUD non construit.");
            return;
        }

        BuildChatPanel(canvas);
        BuildNavBar(canvas);
        BuildArenaModal(canvas);
        BuildDeckModal(canvas);
    }

    // ════════════════════════════════════════════════════════════════════
    // CHAT PANEL  — bas gauche
    // ════════════════════════════════════════════════════════════════════

    void BuildChatPanel(Canvas canvas)
    {
        const float INPUT_H  = 28f;
        const float SEND_W   = 58f;
        const float PAD      = 4f;
        const float OFFSET_X = 12f;
        const float OFFSET_Y = 60f;   // au-dessus de la barre de navigation

        var root   = MakeChild(canvas.gameObject, "HubChatPanel");
        var rootRt = RT(root);
        rootRt.anchorMin = rootRt.anchorMax = Vector2.zero;
        rootRt.pivot     = Vector2.zero;
        rootRt.sizeDelta = new Vector2(chatWidth, chatHeight);
        rootRt.anchoredPosition = new Vector2(OFFSET_X, OFFSET_Y);
        root.AddComponent<Image>().color = BgDark;

        // ── Zone scrollable ──
        var scrollGo = MakeChild(root, "Scroll");
        var scrollRt = RT(scrollGo);
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(0f, INPUT_H);
        scrollRt.offsetMax = Vector2.zero;

        _chatScroll = scrollGo.AddComponent<ScrollRect>();
        _chatScroll.horizontal        = false;
        _chatScroll.vertical          = true;
        _chatScroll.movementType      = ScrollRect.MovementType.Clamped;
        _chatScroll.scrollSensitivity = 20f;
        _chatScroll.verticalScrollbar = null;

        var vpGo = MakeChild(scrollGo, "Viewport");
        Stretch(RT(vpGo));
        vpGo.AddComponent<RectMask2D>();
        _chatScroll.viewport = RT(vpGo);

        var contentGo = MakeChild(vpGo, "Content");
        _chatContent = RT(contentGo);
        _chatContent.anchorMin        = new Vector2(0f, 1f);
        _chatContent.anchorMax        = new Vector2(1f, 1f);
        _chatContent.pivot            = new Vector2(0.5f, 1f);
        _chatContent.sizeDelta        = Vector2.zero;
        _chatContent.anchoredPosition = Vector2.zero;

        var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 2f;
        vlg.padding = new RectOffset(6, 6, 4, 4);
        contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _chatScroll.content = _chatContent;

        // ── Ligne d'input ──
        var rowGo = MakeChild(root, "InputRow");
        var rowRt = RT(rowGo);
        rowRt.anchorMin = Vector2.zero;
        rowRt.anchorMax = new Vector2(1f, 0f);
        rowRt.pivot     = Vector2.zero;
        rowRt.sizeDelta = new Vector2(0f, INPUT_H);
        rowRt.anchoredPosition = Vector2.zero;
        rowGo.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 1f);

        var ifGo = MakeChild(rowGo, "Field");
        var ifRt = RT(ifGo);
        ifRt.anchorMin = Vector2.zero; ifRt.anchorMax = Vector2.one;
        ifRt.offsetMin = new Vector2(PAD, 3f);
        ifRt.offsetMax = new Vector2(-(SEND_W + PAD + 2f), -3f);
        ifGo.AddComponent<Image>().color = BgField;

        _chatInput = ifGo.AddComponent<TMP_InputField>();
        _chatInput.lineType = TMP_InputField.LineType.SingleLine;

        var taGo = MakeChild(ifGo, "TextArea");
        var taRt = RT(taGo);
        taRt.anchorMin = Vector2.zero; taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(5f, 1f); taRt.offsetMax = new Vector2(-5f, -1f);
        taGo.AddComponent<RectMask2D>();
        _chatInput.textViewport = taRt;

        var phTmp = MakeLabel(MakeChild(taGo, "Placeholder"), "Envoyer un message…", 10f, TextDim);
        phTmp.fontStyle          = FontStyles.Italic;
        phTmp.enableWordWrapping = false;
        phTmp.raycastTarget      = false;

        var txtTmp = MakeLabel(MakeChild(taGo, "Text"), string.Empty, 10f, TextWhite);
        txtTmp.enableWordWrapping = false;
        txtTmp.raycastTarget      = false;

        _chatInput.textComponent = txtTmp;
        _chatInput.placeholder   = phTmp;
        _chatInput.onSubmit.AddListener(SubmitChat);

        var sendGo  = MakeChild(rowGo, "Send");
        var sendRt  = RT(sendGo);
        sendRt.anchorMin = new Vector2(1f, 0f); sendRt.anchorMax = new Vector2(1f, 1f);
        sendRt.pivot     = new Vector2(1f, 0.5f);
        sendRt.sizeDelta = new Vector2(SEND_W, -6f);
        sendRt.anchoredPosition = new Vector2(-PAD, 0f);
        var sendImg = sendGo.AddComponent<Image>(); sendImg.color = AccentGold;
        var sendBtn = sendGo.AddComponent<Button>(); sendBtn.targetGraphic = sendImg;
        ApplyColors(sendBtn, AccentGold, AccentGoldH, AccentGoldP);
        sendBtn.onClick.AddListener(OnSendClicked);

        var sendLbl = MakeLabel(MakeChild(sendGo, "Lbl"), "Envoyer", 8.5f, new Color(0.10f, 0.07f, 0.03f, 1f));
        Stretch(RT(sendLbl.gameObject));

        AppendChat("Bienvenue dans le hub Oracle !", AccentGold);
    }

    void OnSendClicked() => SubmitChat(_chatInput != null ? _chatInput.text : string.Empty);

    void SubmitChat(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        string msg = text.Trim();
        if (_chatInput != null) _chatInput.text = string.Empty;
        AppendChat($"<b>Vous</b> : {msg}", TextWhite);

        // Afficher la bulle au-dessus du personnage
        FindObjectOfType<HubChatBubble>()?.ShowMessage(msg);
    }

    void AppendChat(string text, Color color)
    {
        if (_chatContent == null) return;

        if (_chatMessages.Count >= maxMessages)
        {
            Destroy(_chatMessages[0]);
            _chatMessages.RemoveAt(0);
        }

        var go  = MakeChild(_chatContent.gameObject, "Msg");
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text               = text;
        tmp.fontSize           = 11f;
        tmp.color              = color;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget      = false;
        OracleUIImportantFont.Apply(tmp);

        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;

        _chatMessages.Add(go);
        StartCoroutine(ScrollToBottom());
    }

    IEnumerator ScrollToBottom()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (_chatScroll != null)
            _chatScroll.verticalNormalizedPosition = 0f;
    }

    // ════════════════════════════════════════════════════════════════════
    // BARRE DE NAVIGATION  — bas droite (5 boutons : +Deck)
    // ════════════════════════════════════════════════════════════════════

    void BuildNavBar(Canvas canvas)
    {
        const float BAR_H    = 50f;
        const float BTN_W    = 110f;
        const float BTN_H    = 36f;
        const float SPACING  = 6f;
        const float PAD_H    = 10f;
        const float PAD_V    = 7f;
        const float OFFSET_X = -10f;
        const float OFFSET_Y = 8f;

        int   btnCount = 5;
        float totalW   = btnCount * BTN_W + (btnCount - 1) * SPACING + 2f * PAD_H;

        var bar   = MakeChild(canvas.gameObject, "HubNavBar");
        var barRt = RT(bar);
        barRt.anchorMin = barRt.anchorMax = new Vector2(1f, 0f);
        barRt.pivot     = new Vector2(1f, 0f);
        barRt.sizeDelta = new Vector2(totalW, BAR_H);
        barRt.anchoredPosition = new Vector2(OFFSET_X, OFFSET_Y);
        bar.AddComponent<Image>().color = BgDark;

        var hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment      = TextAnchor.MiddleCenter;
        hlg.spacing             = SPACING;
        hlg.padding             = new RectOffset((int)PAD_H, (int)PAD_H, (int)PAD_V, (int)PAD_V);
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;

        MakeNavBtn(bar, "Classement", BTN_W, BTN_H, false,
            () => AppendChat("Classement — bientôt disponible.", TextGray));
        MakeNavBtn(bar, "Boutique",   BTN_W, BTN_H, false,
            () => AppendChat("Boutique — bientôt disponible.", TextGray));
        MakeNavBtn(bar, "Paramètres", BTN_W, BTN_H, false,
            () => AppendChat("Paramètres — bientôt disponible.", TextGray));
        MakeNavBtn(bar, "Deck",       BTN_W, BTN_H, false, OpenDeckModal);
        MakeNavBtn(bar, "Arène",      BTN_W, BTN_H, true,  OpenArenaMenu);
    }

    void MakeNavBtn(GameObject parent, string label, float w, float h, bool isAccent, System.Action onClick)
    {
        var go  = MakeChild(parent, label + "Btn");
        var le  = go.AddComponent<LayoutElement>();
        le.preferredWidth  = w;
        le.preferredHeight = h;

        var img = go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        if (isAccent)
        {
            img.color = AccentGold;
            ApplyColors(btn, AccentGold, AccentGoldH, AccentGoldP);
            var lbl = MakeLabel(MakeChild(go, "Lbl"), label, 13f, new Color(0.10f, 0.07f, 0.03f, 1f));
            lbl.fontStyle = FontStyles.Bold;
            Stretch(RT(lbl.gameObject));
        }
        else
        {
            img.color = NavBtnNorm;
            ApplyColors(btn, NavBtnNorm, NavBtnHov, NavBtnPrs);
            var lbl = MakeLabel(MakeChild(go, "Lbl"), label, 12f, AccentGold);
            Stretch(RT(lbl.gameObject));
        }

        btn.onClick.AddListener(() => onClick());
    }

    // ════════════════════════════════════════════════════════════════════
    // MODAL ARÈNE  — centré, overlay semi-transparent
    // ════════════════════════════════════════════════════════════════════

    void BuildArenaModal(Canvas canvas)
    {
        const float PANEL_W = 440f;
        const float PANEL_H = 290f;

        _arenaModal = MakeChild(canvas.gameObject, "ArenaModal");
        Stretch(RT(_arenaModal));
        var overlayImg = _arenaModal.AddComponent<Image>();
        overlayImg.color = Overlay;
        var overlayBtn = _arenaModal.AddComponent<Button>();
        overlayBtn.targetGraphic = overlayImg;
        overlayBtn.onClick.AddListener(CloseArenaMenu);
        _arenaModal.SetActive(false);

        var panel   = MakeChild(_arenaModal, "Panel");
        var panelRt = RT(panel);
        panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot     = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(PANEL_W, PANEL_H);
        panelRt.anchoredPosition = Vector2.zero;
        panel.AddComponent<Image>().color = BgMedium;
        panel.AddComponent<GraphicRaycaster>();

        var titleArea = MakeChild(panel, "TitleArea");
        var titleRt   = RT(titleArea);
        titleRt.anchorMin = new Vector2(0f, 1f); titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot     = new Vector2(0.5f, 1f);
        titleRt.sizeDelta = new Vector2(0f, 52f);
        titleRt.anchoredPosition = Vector2.zero;

        var titleTmp = MakeLabel(MakeChild(titleArea, "Title"), "ARÈNE", 22f, AccentGold);
        titleTmp.fontStyle = FontStyles.Bold;
        Stretch(RT(titleTmp.gameObject));

        var sep   = MakeChild(panel, "TitleSep");
        var sepRt = RT(sep);
        sepRt.anchorMin = new Vector2(0f, 1f); sepRt.anchorMax = new Vector2(1f, 1f);
        sepRt.pivot     = new Vector2(0.5f, 1f);
        sepRt.sizeDelta = new Vector2(-24f, 1f);
        sepRt.anchoredPosition = new Vector2(0f, -50f);
        sep.AddComponent<Image>().color = AccentGold;

        var grid   = MakeChild(panel, "ModeGrid");
        var gridRt = RT(grid);
        gridRt.anchorMin = new Vector2(0.05f, 0.12f);
        gridRt.anchorMax = new Vector2(0.95f, 0.76f);
        gridRt.offsetMin = gridRt.offsetMax = Vector2.zero;

        var glg = grid.AddComponent<GridLayoutGroup>();
        glg.cellSize       = new Vector2(180f, 68f);
        glg.spacing        = new Vector2(12f, 10f);
        glg.startCorner    = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis      = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment = TextAnchor.MiddleCenter;

        BuildArenaMode(grid, "Entraînement", active: true,  () => OnTrainingSelected());
        BuildArenaMode(grid, "1 VS 1",       active: true,  () => OnVS1Selected());
        BuildArenaMode(grid, "2 VS 2",       active: false, null);
        BuildArenaMode(grid, "3 VS 3",       active: false, null);

        var closeGo  = MakeChild(panel, "CloseBtn");
        var closeRt  = RT(closeGo);
        closeRt.anchorMin = new Vector2(1f, 0f); closeRt.anchorMax = new Vector2(1f, 0f);
        closeRt.pivot     = new Vector2(1f, 0f);
        closeRt.sizeDelta = new Vector2(95f, 34f);
        closeRt.anchoredPosition = new Vector2(-14f, 12f);

        var closeImg = closeGo.AddComponent<Image>(); closeImg.color = BgDark;
        var closeBtn = closeGo.AddComponent<Button>(); closeBtn.targetGraphic = closeImg;
        ApplyColors(closeBtn, BgDark, NavBtnHov, NavBtnPrs);
        closeBtn.onClick.AddListener(CloseArenaMenu);
        var closeLbl = MakeLabel(MakeChild(closeGo, "Lbl"), "Fermer", 12f, TextGray);
        Stretch(RT(closeLbl.gameObject));

        // ── Panel "Recherche en cours" (superposé, caché par défaut) ─────
        BuildSearchingPanel(panel);
    }

    void BuildSearchingPanel(GameObject parent)
    {
        _searchingPanel = MakeChild(parent, "SearchingPanel");
        var spRt = RT(_searchingPanel);
        spRt.anchorMin = Vector2.zero; spRt.anchorMax = Vector2.one;
        spRt.offsetMin = spRt.offsetMax = Vector2.zero;
        _searchingPanel.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.09f, 0.96f);
        _searchingPanel.SetActive(false);

        // Label "Recherche..."
        _searchingLabel = MakeLabel(MakeChild(_searchingPanel, "SearchLbl"),
            "Recherche d'un adversaire…\n<size=28><color=#C8A84B>0 / 2</color></size>",
            18f, TextWhite);
        _searchingLabel.enableWordWrapping = true;
        var slRt = RT(_searchingLabel.gameObject);
        slRt.anchorMin = new Vector2(0.1f, 0.45f); slRt.anchorMax = new Vector2(0.9f, 0.85f);
        slRt.offsetMin = slRt.offsetMax = Vector2.zero;

        // Bouton Annuler
        var cancelGo = MakeChild(_searchingPanel, "CancelBtn");
        var cancelRt = RT(cancelGo);
        cancelRt.anchorMin = new Vector2(0.3f, 0.2f); cancelRt.anchorMax = new Vector2(0.7f, 0.38f);
        cancelRt.offsetMin = cancelRt.offsetMax = Vector2.zero;
        var cancelImg = cancelGo.AddComponent<Image>(); cancelImg.color = BtnDisabled;
        _cancelSearchBtn = cancelGo.AddComponent<Button>(); _cancelSearchBtn.targetGraphic = cancelImg;
        ApplyColors(_cancelSearchBtn, BtnDisabled, NavBtnHov, NavBtnPrs);
        _cancelSearchBtn.onClick.AddListener(OnCancelSearchClicked);
        var cancelLbl = MakeLabel(MakeChild(cancelGo, "Lbl"), "Annuler", 14f, TextGray);
        Stretch(RT(cancelLbl.gameObject));
    }

    void BuildArenaMode(GameObject parent, string label, bool active, System.Action onClick)
    {
        var go  = MakeChild(parent, label.Replace(" ", "") + "Mode");
        var img = go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.interactable  = active;

        if (active)
        {
            img.color = BgDark;
            ApplyColors(btn, BgDark, new Color(0.14f, 0.14f, 0.20f, 1f), new Color(0.03f, 0.03f, 0.05f, 1f));
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            var lbl = MakeLabel(MakeChild(go, "Lbl"), label, 14f, AccentGold);
            lbl.fontStyle = FontStyles.Bold;
            Stretch(RT(lbl.gameObject));

            var badge   = MakeChild(go, "Badge");
            var badgeRt = RT(badge);
            badgeRt.anchorMin = new Vector2(0f, 0f); badgeRt.anchorMax = new Vector2(0f, 0f);
            badgeRt.pivot     = new Vector2(0f, 0f);
            badgeRt.sizeDelta = new Vector2(60f, 14f);
            badgeRt.anchoredPosition = new Vector2(6f, 6f);
            badge.AddComponent<Image>().color = new Color(0.20f, 0.58f, 0.25f, 0.85f);
            var badgeTmp = MakeLabel(MakeChild(badge, "T"), "disponible", 8f, TextWhite);
            Stretch(RT(badgeTmp.gameObject));
        }
        else
        {
            img.color = BtnDisabled;
            var cs = btn.colors;
            cs.disabledColor = new Color(0.35f, 0.35f, 0.38f, 0.6f);
            btn.colors = cs;

            var lbl = MakeLabel(MakeChild(go, "Lbl"), label, 14f, TextDisabled);
            lbl.fontStyle = FontStyles.Italic;
            Stretch(RT(lbl.gameObject));

            var badge   = MakeChild(go, "Badge");
            var badgeRt = RT(badge);
            badgeRt.anchorMin = new Vector2(0f, 0f); badgeRt.anchorMax = new Vector2(0f, 0f);
            badgeRt.pivot     = new Vector2(0f, 0f);
            badgeRt.sizeDelta = new Vector2(50f, 14f);
            badgeRt.anchoredPosition = new Vector2(6f, 6f);
            badge.AddComponent<Image>().color = new Color(0.35f, 0.30f, 0.20f, 0.70f);
            var badgeTmp = MakeLabel(MakeChild(badge, "T"), "à venir", 8f, new Color(0.65f, 0.55f, 0.25f, 1f));
            Stretch(RT(badgeTmp.gameObject));
        }
    }

    void OpenArenaMenu()
    {
        if (_arenaModal != null) _arenaModal.SetActive(true);
    }

    void CloseArenaMenu()
    {
        if (_arenaModal != null) _arenaModal.SetActive(false);
    }

    void OnTrainingSelected()
    {
        CloseArenaMenu();
        if (HubManager.Instance != null)
            HubManager.Instance.LaunchTraining();
        else
        {
            Debug.LogWarning("[HubHUD] HubManager introuvable — chargement direct par nom.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Monjeu");
        }
    }

    void OnVS1Selected()
    {
        var mm = HubMatchmaker.Instance;
        if (mm == null)
        {
            AppendChat("HubMatchmaker introuvable — ajoute-le sur NetworkRoot.", new Color(1f, 0.4f, 0.4f));
            return;
        }

        // Afficher le panel de recherche
        if (_searchingPanel != null) _searchingPanel.SetActive(true);

        // S'abonner aux events du matchmaker
        mm.OnPlayerCountUpdated += UpdateSearchingLabel;
        mm.OnMatchFound         += OnMatchFound;
        mm.OnSearchCancelled    += OnSearchCancelled;

        mm.StartSearch1v1();
        AppendChat("Recherche 1v1 en cours…", AccentGold);
    }

    void OnCancelSearchClicked()
    {
        HubMatchmaker.Instance?.CancelSearch();
    }

    void UpdateSearchingLabel(int current, int max)
    {
        if (_searchingLabel != null)
            _searchingLabel.text =
                $"Recherche d'un adversaire…\n<size=28><color=#C8A84B>{current} / {max}</color></size>";
    }

    void OnMatchFound()
    {
        if (_searchingPanel != null) _searchingPanel.SetActive(false);
        UnsubscribeMatchmaker();
        AppendChat("Adversaire trouvé ! Chargement du combat…", AccentGold);
    }

    void OnSearchCancelled()
    {
        if (_searchingPanel != null) _searchingPanel.SetActive(false);
        UnsubscribeMatchmaker();
        AppendChat("Recherche annulée.", TextGray);
    }

    void UnsubscribeMatchmaker()
    {
        var mm = HubMatchmaker.Instance;
        if (mm == null) return;
        mm.OnPlayerCountUpdated -= UpdateSearchingLabel;
        mm.OnMatchFound         -= OnMatchFound;
        mm.OnSearchCancelled    -= OnSearchCancelled;
    }

    // ════════════════════════════════════════════════════════════════════
    // DECK BUILDER MODAL  — centré, onglets par catégorie
    // ════════════════════════════════════════════════════════════════════

    void BuildDeckModal(Canvas canvas)
    {
        // Hauteurs fixes (en unités canvas 1920×1080)
        const float HEADER_H = 76f;
        const float TABS_H   = 52f;
        const float FOOTER_H = 68f;

        // ── Overlay ──
        _deckModal = MakeChild(canvas.gameObject, "DeckModal");
        Stretch(RT(_deckModal));
        var olImg = _deckModal.AddComponent<Image>();
        olImg.color = Overlay;
        var olBtn = _deckModal.AddComponent<Button>();
        olBtn.targetGraphic = olImg;
        olBtn.onClick.AddListener(CloseDeckModal);
        _deckModal.SetActive(false);

        // ── Panel — remplit 92 % × 94 % de l'écran (anchor-based) ──
        var panel   = MakeChild(_deckModal, "Panel");
        var panelRt = RT(panel);
        panelRt.anchorMin        = new Vector2(0.04f, 0.03f);
        panelRt.anchorMax        = new Vector2(0.96f, 0.97f);
        panelRt.pivot            = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta        = Vector2.zero;
        panelRt.anchoredPosition = Vector2.zero;
        panel.AddComponent<Image>().color = BgMedium;
        panel.AddComponent<GraphicRaycaster>();

        // ── Header ──
        var header   = MakeChild(panel, "Header");
        var headerRt = RT(header);
        headerRt.anchorMin        = new Vector2(0f, 1f);
        headerRt.anchorMax        = new Vector2(1f, 1f);
        headerRt.pivot            = new Vector2(0.5f, 1f);
        headerRt.sizeDelta        = new Vector2(0f, HEADER_H);
        headerRt.anchoredPosition = Vector2.zero;
        header.AddComponent<Image>().color = BgDark;

        var titleTmp = MakeLabel(MakeChild(header, "Title"), "DECK BUILDER", 30f, AccentGold);
        titleTmp.fontStyle = FontStyles.Bold;
        var titleRt = RT(titleTmp.gameObject);
        titleRt.anchorMin = new Vector2(0f, 0f); titleRt.anchorMax = new Vector2(0.48f, 1f);
        titleRt.offsetMin = new Vector2(24f, 0f); titleRt.offsetMax = Vector2.zero;
        titleTmp.alignment = TextAlignmentOptions.MidlineLeft;

        _statsLabel = MakeLabel(MakeChild(header, "Stats"), "— PA / — PM", 20f, TextGray);
        var statsRt = RT(_statsLabel.gameObject);
        statsRt.anchorMin = new Vector2(0.48f, 0f); statsRt.anchorMax = new Vector2(0.74f, 1f);
        statsRt.offsetMin = statsRt.offsetMax = Vector2.zero;
        _statsLabel.alignment = TextAlignmentOptions.Center;

        _deckCounter = MakeLabel(MakeChild(header, "Counter"), "0 / 6 sorts", 20f, TextGray);
        var cntRt = RT(_deckCounter.gameObject);
        cntRt.anchorMin = new Vector2(0.74f, 0f); cntRt.anchorMax = new Vector2(1f, 1f);
        cntRt.offsetMin = Vector2.zero; cntRt.offsetMax = new Vector2(-18f, 0f);
        _deckCounter.alignment = TextAlignmentOptions.MidlineRight;

        // Séparateur doré
        var sep   = MakeChild(panel, "Sep");
        var sepRt = RT(sep);
        sepRt.anchorMin        = new Vector2(0f, 1f);
        sepRt.anchorMax        = new Vector2(1f, 1f);
        sepRt.pivot            = new Vector2(0.5f, 1f);
        sepRt.sizeDelta        = new Vector2(-20f, 2f);
        sepRt.anchoredPosition = new Vector2(0f, -HEADER_H);
        sep.AddComponent<Image>().color = AccentGold;

        // ── Onglets ──
        var tabBar   = MakeChild(panel, "TabBar");
        var tabBarRt = RT(tabBar);
        tabBarRt.anchorMin        = new Vector2(0f, 1f);
        tabBarRt.anchorMax        = new Vector2(1f, 1f);
        tabBarRt.pivot            = new Vector2(0.5f, 1f);
        tabBarRt.sizeDelta        = new Vector2(0f, TABS_H);
        tabBarRt.anchoredPosition = new Vector2(0f, -(HEADER_H + 2f));
        tabBar.AddComponent<Image>().color = BgDark;

        var tabHlg = tabBar.AddComponent<HorizontalLayoutGroup>();
        tabHlg.childAlignment      = TextAnchor.MiddleLeft;
        tabHlg.spacing             = 4f;
        tabHlg.padding             = new RectOffset(16, 16, 6, 6);
        tabHlg.childForceExpandWidth  = false;
        tabHlg.childForceExpandHeight = true;
        tabHlg.childControlWidth      = false;
        tabHlg.childControlHeight     = true;

        for (int i = 0; i < Tabs.Length; i++)
        {
            var (cat, lbl, col) = Tabs[i];

            var tabGo  = MakeChild(tabBar, lbl + "Tab");
            var tabImg = tabGo.AddComponent<Image>();
            var tabBtn = tabGo.AddComponent<Button>();
            tabBtn.targetGraphic = tabImg;

            var le = tabGo.AddComponent<LayoutElement>();
            le.preferredWidth  = 220f;
            le.preferredHeight = 40f;

            _tabImages[i] = tabImg;
            tabImg.color   = TabNorm;

            var stripe   = MakeChild(tabGo, "Stripe");
            var stripeRt = RT(stripe);
            stripeRt.anchorMin        = new Vector2(0f, 0f);
            stripeRt.anchorMax        = new Vector2(1f, 0f);
            stripeRt.pivot            = new Vector2(0.5f, 0f);
            stripeRt.sizeDelta        = new Vector2(0f, 4f);
            stripeRt.anchoredPosition = Vector2.zero;
            stripe.AddComponent<Image>().color = col;

            var tabLbl = MakeLabel(MakeChild(tabGo, "Lbl"), lbl, 17f, TextGray);
            Stretch(RT(tabLbl.gameObject));

            tabBtn.onClick.AddListener(() => SelectDeckTab(cat));
        }

        // ── Zone de défilement ──
        float scrollTopOffset = HEADER_H + 2f + TABS_H;

        var scrollGo = MakeChild(panel, "DeckScroll");
        var scrollRt = RT(scrollGo);
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.offsetMin = new Vector2(0f, FOOTER_H);
        scrollRt.offsetMax = new Vector2(0f, -scrollTopOffset);

        var sr = scrollGo.AddComponent<ScrollRect>();
        sr.horizontal        = false;
        sr.vertical          = true;
        sr.movementType      = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 50f;
        sr.verticalScrollbar = null;

        var vpGo = MakeChild(scrollGo, "Viewport");
        Stretch(RT(vpGo));
        vpGo.AddComponent<RectMask2D>();
        sr.viewport = RT(vpGo);

        var contentGo = MakeChild(vpGo, "Content");
        _deckScrollContent                    = RT(contentGo);
        _deckScrollContent.anchorMin          = new Vector2(0f, 1f);
        _deckScrollContent.anchorMax          = new Vector2(1f, 1f);
        _deckScrollContent.pivot              = new Vector2(0.5f, 1f);
        _deckScrollContent.anchoredPosition   = Vector2.zero;
        _deckScrollContent.sizeDelta          = Vector2.zero;

        var glg = contentGo.AddComponent<GridLayoutGroup>();
        glg.cellSize        = new Vector2(390f, 270f);
        glg.spacing         = new Vector2(16f, 16f);
        glg.padding         = new RectOffset(20, 20, 18, 18);
        glg.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis       = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment  = TextAnchor.UpperLeft;
        glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 4;

        contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = _deckScrollContent;

        // ── Footer : Sauvegarder + Fermer ──
        var footer   = MakeChild(panel, "Footer");
        var footerRt = RT(footer);
        footerRt.anchorMin        = new Vector2(0f, 0f);
        footerRt.anchorMax        = new Vector2(1f, 0f);
        footerRt.pivot            = new Vector2(0.5f, 0f);
        footerRt.sizeDelta        = new Vector2(0f, FOOTER_H);
        footerRt.anchoredPosition = Vector2.zero;
        footer.AddComponent<Image>().color = BgDark;

        var saveGo = MakeChild(footer, "SaveBtn");
        var saveRt = RT(saveGo);
        saveRt.anchorMin        = new Vector2(0.5f, 0.5f);
        saveRt.anchorMax        = new Vector2(0.5f, 0.5f);
        saveRt.pivot            = new Vector2(1f, 0.5f);
        saveRt.sizeDelta        = new Vector2(240f, 44f);
        saveRt.anchoredPosition = new Vector2(-8f, 0f);
        var saveImg = saveGo.AddComponent<Image>(); saveImg.color = AccentGold;
        var saveBtn = saveGo.AddComponent<Button>(); saveBtn.targetGraphic = saveImg;
        ApplyColors(saveBtn, AccentGold, AccentGoldH, AccentGoldP);
        saveBtn.onClick.AddListener(SaveAndCloseDeck);
        var saveLbl = MakeLabel(MakeChild(saveGo, "Lbl"), "Sauvegarder le deck", 16f, new Color(0.10f, 0.07f, 0.03f, 1f));
        saveLbl.fontStyle = FontStyles.Bold;
        Stretch(RT(saveLbl.gameObject));

        var closeGo  = MakeChild(footer, "CloseBtn");
        var closeRt  = RT(closeGo);
        closeRt.anchorMin        = new Vector2(0.5f, 0.5f);
        closeRt.anchorMax        = new Vector2(0.5f, 0.5f);
        closeRt.pivot            = new Vector2(0f, 0.5f);
        closeRt.sizeDelta        = new Vector2(140f, 44f);
        closeRt.anchoredPosition = new Vector2(8f, 0f);
        var closeImg = closeGo.AddComponent<Image>(); closeImg.color = BgMedium;
        var closeBtn = closeGo.AddComponent<Button>(); closeBtn.targetGraphic = closeImg;
        ApplyColors(closeBtn, BgMedium, NavBtnHov, NavBtnPrs);
        closeBtn.onClick.AddListener(CloseDeckModal);
        var closeLbl = MakeLabel(MakeChild(closeGo, "Lbl"), "Fermer", 16f, TextGray);
        Stretch(RT(closeLbl.gameObject));
    }

    // ── Deck builder — logique ────────────────────────────────────────────

    void OpenDeckModal()
    {
        // Charger les sorts depuis le pool si pas encore fait
        if (_allSpells.Count == 0)
        {
            var pool = Resources.Load<SpellDeckPool>("OracleSpellPools/AllCombatSpellsPool");
            if (pool != null)
                foreach (var s in pool.candidates)
                    if (s != null) _allSpells.Add(s);

            if (_allSpells.Count == 0)
            {
                AppendChat("[Deck Builder] Pool de sorts introuvable. Oracle → Spell Deck Pool.", new Color(1f, 0.4f, 0.4f, 1f));
                return;
            }
        }

        // Restaurer la sélection précédente (deck hub sauvegardé)
        _selectedSpells.Clear();
        var saved = HubManager.Instance?.SelectedDeck;
        if (saved != null) _selectedSpells.AddRange(saved);

        _currentTab = SpellDeckCategory.Attack;
        _deckModal.SetActive(true);
        RefreshTabVisuals();
        PopulateSpellCards();
        UpdateDeckCounter();
        UpdateStatsLabel();

        // Bloquer le zoom caméra pour que la molette ne serve qu'au scroll du deck builder
        SetCameraZoom(false);
    }

    void CloseDeckModal()
    {
        if (_deckModal != null) _deckModal.SetActive(false);
        SetCameraZoom(true);
    }

    void SetCameraZoom(bool enabled)
    {
        if (_isoCam == null && Camera.main != null)
            _isoCam = Camera.main.GetComponent<IsometricCamera>();
        if (_isoCam != null) _isoCam.zoomEnabled = enabled;
    }

    void UpdateStatsLabel()
    {
        if (_statsLabel == null) return;
        var tc    = FindObjectOfType<HubCharacterController>()?.GetComponent<TacticalCharacter>();
        var stats = tc?.stats;
        _statsLabel.text  = stats != null ? $"{stats.maxPA} PA  /  {stats.maxPM} PM" : "— PA / — PM";
        _statsLabel.color = AccentGold;
    }

    void SelectDeckTab(SpellDeckCategory cat)
    {
        if (_currentTab == cat) return;
        _currentTab = cat;
        RefreshTabVisuals();
        PopulateSpellCards();
    }

    void RefreshTabVisuals()
    {
        for (int i = 0; i < Tabs.Length; i++)
            _tabImages[i].color = (Tabs[i].cat == _currentTab) ? TabActive : TabNorm;
    }

    void PopulateSpellCards()
    {
        // Vider le contenu précédent
        foreach (Transform child in _deckScrollContent)
            Destroy(child.gameObject);
        _currentCards.Clear();

        foreach (var spell in _allSpells)
        {
            if (spell.deckCategory != _currentTab) continue;
            MakeSpellCard(spell);
        }

        RefreshCardVisuals();
    }

    void MakeSpellCard(SpellData spell)
    {
        // Card = 390 × 270 (cellSize du GridLayoutGroup)
        // Layout top → bottom (ancré en haut) :
        //   8px gap | 80px icon | 4px gap | 1px sep | 4px gap | 38px name | 4px gap | 100px desc
        //   bottom : 6px gap | 26px PA badge

        var go  = MakeChild(_deckScrollContent.gameObject, spell.spellName);
        var bg  = go.AddComponent<Image>();
        bg.color = CardBg;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;

        // ── Icône ──
        var iconGo = MakeChild(go, "Icon");
        var iconRt = RT(iconGo);
        iconRt.anchorMin        = new Vector2(0.10f, 1f);
        iconRt.anchorMax        = new Vector2(0.90f, 1f);
        iconRt.pivot            = new Vector2(0.5f, 1f);
        iconRt.sizeDelta        = new Vector2(0f, 80f);
        iconRt.anchoredPosition = new Vector2(0f, -8f);
        var iconImg = iconGo.AddComponent<Image>();
        if (spell.icon != null) { iconImg.sprite = spell.icon; iconImg.preserveAspect = true; }
        else                    { iconImg.color  = CategoryColor(spell.deckCategory) * new Color(1f, 1f, 1f, 0.45f); }

        // ── Séparateur ──
        var sepGo = MakeChild(go, "Sep");
        var sepRt = RT(sepGo);
        sepRt.anchorMin        = new Vector2(0.04f, 1f);
        sepRt.anchorMax        = new Vector2(0.96f, 1f);
        sepRt.pivot            = new Vector2(0.5f, 1f);
        sepRt.sizeDelta        = new Vector2(0f, 1f);
        sepRt.anchoredPosition = new Vector2(0f, -92f);  // 8+80+4
        sepGo.AddComponent<Image>().color = new Color(0.38f, 0.38f, 0.44f, 0.55f);

        // ── Nom ──
        var nameGo = MakeChild(go, "Name");
        var nameRt = RT(nameGo);
        nameRt.anchorMin        = new Vector2(0f, 1f);
        nameRt.anchorMax        = new Vector2(1f, 1f);
        nameRt.pivot            = new Vector2(0.5f, 1f);
        nameRt.sizeDelta        = new Vector2(-10f, 38f);
        nameRt.anchoredPosition = new Vector2(0f, -97f);  // 92+1+4
        var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
        nameTmp.text               = spell.spellName;
        nameTmp.fontSize           = 15f;
        nameTmp.color              = TextWhite;
        nameTmp.fontStyle          = FontStyles.Bold;
        nameTmp.alignment          = TextAlignmentOptions.Center;
        nameTmp.enableWordWrapping = true;
        nameTmp.raycastTarget      = false;
        OracleUIImportantFont.Apply(nameTmp);

        // ── Description ──
        var descGo = MakeChild(go, "Desc");
        var descRt = RT(descGo);
        descRt.anchorMin        = new Vector2(0f, 1f);
        descRt.anchorMax        = new Vector2(1f, 1f);
        descRt.pivot            = new Vector2(0.5f, 1f);
        descRt.sizeDelta        = new Vector2(-14f, 100f);
        descRt.anchoredPosition = new Vector2(0f, -139f);  // 97+38+4
        string descText = !string.IsNullOrEmpty(spell.description)
            ? spell.description : BuildEffectSummary(spell);
        var descTmp = descGo.AddComponent<TextMeshProUGUI>();
        descTmp.text               = descText;
        descTmp.fontSize           = 12f;
        descTmp.color              = TextGray;
        descTmp.alignment          = TextAlignmentOptions.Center;
        descTmp.enableWordWrapping = true;
        descTmp.raycastTarget      = false;
        OracleUIImportantFont.Apply(descTmp);

        // ── Badge PA (coin inférieur gauche) ──
        var paGo  = MakeChild(go, "PA");
        var paRt  = RT(paGo);
        paRt.anchorMin        = new Vector2(0f, 0f);
        paRt.anchorMax        = new Vector2(0f, 0f);
        paRt.pivot            = new Vector2(0f, 0f);
        paRt.sizeDelta        = new Vector2(56f, 26f);
        paRt.anchoredPosition = new Vector2(8f, 8f);
        paGo.AddComponent<Image>().color = new Color(0.13f, 0.13f, 0.20f, 1f);
        var paTxtGo = MakeChild(paGo, "T");
        Stretch(RT(paTxtGo));
        var paTmp = paTxtGo.AddComponent<TextMeshProUGUI>();
        paTmp.text          = $"{spell.paCost} PA";
        paTmp.fontSize      = 13f;
        paTmp.color         = AccentGold;
        paTmp.alignment     = TextAlignmentOptions.Center;
        paTmp.raycastTarget = false;
        OracleUIImportantFont.Apply(paTmp);

        // ── Indicateur de sélection (coin supérieur droit) ──
        var selGo  = MakeChild(go, "SelMark");
        var selRt  = RT(selGo);
        selRt.anchorMin        = new Vector2(1f, 1f);
        selRt.anchorMax        = new Vector2(1f, 1f);
        selRt.pivot            = new Vector2(1f, 1f);
        selRt.sizeDelta        = new Vector2(26f, 26f);
        selRt.anchoredPosition = new Vector2(-6f, -6f);
        selGo.AddComponent<Image>().color = AccentGold;
        selGo.SetActive(false);

        var captured = spell;
        btn.onClick.AddListener(() => ToggleSpell(captured));
        _currentCards.Add(new SpellCardView { spell = spell, bg = bg, selMark = selGo });
    }

    /// <summary>Résumé lisible des effets si la description est vide.</summary>
    static string BuildEffectSummary(SpellData s)
    {
        if (s.effects == null || s.effects.Count == 0) return "Aucun effet décrit.";
        var sb = new System.Text.StringBuilder();
        foreach (var e in s.effects)
        {
            if (e.value != 0)
                sb.Append($"{e.type}: {e.value}");
            else
                sb.Append(e.type.ToString());
            if (e.duration > 0) sb.Append($" ({e.duration}t)");
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    void ToggleSpell(SpellData spell)
    {
        if (_selectedSpells.Contains(spell))
            _selectedSpells.Remove(spell);
        else if (_selectedSpells.Count < DeckData.MaxSpells)
            _selectedSpells.Add(spell);
        // Si déjà 6 sélectionnés et ce sort ne l'est pas : ignorer

        RefreshCardVisuals();
        UpdateDeckCounter();
    }

    void RefreshCardVisuals()
    {
        bool maxReached = _selectedSpells.Count >= DeckData.MaxSpells;
        foreach (var card in _currentCards)
        {
            bool selected = _selectedSpells.Contains(card.spell);
            bool dim      = maxReached && !selected;
            card.bg.color = selected ? CardBgSel : dim ? CardBgDim : CardBg;
            card.selMark.SetActive(selected);
        }
    }

    void UpdateDeckCounter()
    {
        if (_deckCounter == null) return;
        int n = _selectedSpells.Count;
        _deckCounter.text  = $"{n} / {DeckData.MaxSpells} sorts";
        _deckCounter.color = (n == DeckData.MaxSpells) ? AccentGold : TextGray;
    }

    void SaveAndCloseDeck()
    {
        if (HubManager.Instance != null)
            HubManager.Instance.SelectedDeck = new List<SpellData>(_selectedSpells);

        string info = _selectedSpells.Count == DeckData.MaxSpells
            ? $"Deck sauvegardé ({DeckData.MaxSpells} sorts)."
            : $"Deck partiel sauvegardé ({_selectedSpells.Count} / {DeckData.MaxSpells} sorts). Complète-le avant le combat !";
        AppendChat(info, AccentGold);

        CloseDeckModal();
    }

    static Color CategoryColor(SpellDeckCategory cat)
    {
        switch (cat)
        {
            case SpellDeckCategory.Attack:   return CatAtk;
            case SpellDeckCategory.Tactic:   return CatTac;
            case SpellDeckCategory.Survival: return CatSur;
            default: return Color.gray;
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // HELPERS UI
    // ════════════════════════════════════════════════════════════════════

    static GameObject MakeChild(GameObject parent, string name)
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

    static void ApplyColors(Button btn, Color normal, Color highlighted, Color pressed)
    {
        var cs = btn.colors;
        cs.normalColor      = normal;
        cs.highlightedColor = highlighted;
        cs.pressedColor     = pressed;
        btn.colors = cs;
    }

    static TextMeshProUGUI MakeLabel(GameObject go, string text, float fontSize, Color color)
    {
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        OracleUIImportantFont.Apply(tmp);
        return tmp;
    }
}
