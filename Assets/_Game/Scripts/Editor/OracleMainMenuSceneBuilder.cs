#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// Outil éditeur Oracle — construit la hiérarchie UI du Menu Principal en un clic.
///
/// Menu Unity : Oracle > Build Main Menu Scene
///
/// Ce que ça crée dans la scène MainMenu :
///   [AccountManager]
///   [MainCamera]
///   [EventSystem]
///   Canvas
///     └─ MainPanel          (Login / Paramètres / Quitter)
///     └─ LoginPanel         (username + password + boutons)
///     └─ SettingsPanel      (volumes + déconnexion)
///
/// Après build, ouvrir la scène MainMenu et appuyer sur Play pour tester.
/// </summary>
public static class OracleMainMenuSceneBuilder
{
    // ── Palette Oracle ────────────────────────────────────────────────────
    static readonly Color BgColor      = new Color(0.05f, 0.05f, 0.08f, 1f);
    static readonly Color PanelColor   = new Color(0.08f, 0.08f, 0.12f, 0.96f);
    static readonly Color AccentColor  = new Color(0.85f, 0.70f, 0.25f, 1f);   // or doux
    static readonly Color TextColor    = new Color(0.92f, 0.88f, 0.78f, 1f);
    static readonly Color BtnPrimary   = new Color(0.18f, 0.16f, 0.30f, 1f);
    static readonly Color BtnHover     = new Color(0.28f, 0.24f, 0.46f, 1f);
    static readonly Color BtnDanger    = new Color(0.45f, 0.10f, 0.10f, 1f);
    static readonly Color ErrorColor   = new Color(1.00f, 0.30f, 0.30f, 1f);
    static readonly Color OkColor      = new Color(0.30f, 1.00f, 0.50f, 1f);

    // ── Tailles ───────────────────────────────────────────────────────────
    const float PANEL_W   = 440f;
    const float PANEL_H   = 520f;
    const float BTN_W     = 320f;
    const float BTN_H     = 52f;
    const float INPUT_H   = 44f;
    const float FONT_MAIN = 18f;
    const float FONT_BTN  = 16f;
    const float FONT_SM   = 13f;
    const float GAP       = 14f;

    // ─────────────────────────────────────────────────────────────────────
    [MenuItem("Oracle/Build Main Menu Scene")]
    public static void BuildMainMenuScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // Ouvrir (ou créer) la scène MainMenu
        string scenePath = "Assets/_Game/Scenes/MainMenu.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Nettoyer la scène des anciens GameObjects root
        foreach (var go in scene.GetRootGameObjects())
            Object.DestroyImmediate(go);

        // ── Caméra ───────────────────────────────────────────────────────
        var camGo = new GameObject("MainCamera");
        var cam   = camGo.AddComponent<Camera>();
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = BgColor;
        cam.orthographic     = true;
        cam.orthographicSize = 5f;
        camGo.tag = "MainCamera";

        // ── EventSystem ──────────────────────────────────────────────────
        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();

        // ── AccountManager ───────────────────────────────────────────────
        var amGo = new GameObject("AccountManager");
        amGo.AddComponent<AccountManager>();

        // ── Canvas ───────────────────────────────────────────────────────
        var canvasGo = new GameObject("Canvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode      = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder    = 0;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // ── Fond plein écran ──────────────────────────────────────────────
        var bg = MakeImage(canvasGo.transform, "Background", BgColor, stretch: true);

        // ── Panels ───────────────────────────────────────────────────────
        var mainPanel     = BuildMainPanel(canvasGo.transform);
        var loginPanel    = BuildLoginPanel(canvasGo.transform);
        var settingsPanel = BuildSettingsPanel(canvasGo.transform);

        loginPanel.SetActive(false);
        settingsPanel.SetActive(false);

        // ── MainMenuController ────────────────────────────────────────────
        var ctrl = canvasGo.AddComponent<MainMenuController>();
        ctrl.mainPanel     = mainPanel;
        ctrl.loginPanel    = loginPanel;
        ctrl.settingsPanel = settingsPanel;

        // Labels dans le MainPanel
        ctrl.connectedLabel  = mainPanel.transform.Find("ConnectedLabel")?.GetComponent<TextMeshProUGUI>();
        ctrl.loginButtonLabel = mainPanel.transform.Find("Btn_Login/Label")?.GetComponent<TextMeshProUGUI>();

        // Brancher les boutons MainPanel
        WireButton(mainPanel, "Btn_Login",    ctrl, nameof(MainMenuController.OnLoginClicked));
        WireButton(mainPanel, "Btn_Settings", ctrl, nameof(MainMenuController.OnSettingsClicked));
        WireButton(mainPanel, "Btn_Quit",     ctrl, nameof(MainMenuController.OnQuitClicked));

        // LoginPanel → LoginPanelUI
        var loginUI = loginPanel.AddComponent<LoginPanelUI>();
        loginUI.menuController  = ctrl;
        loginUI.usernameField   = loginPanel.transform.Find("UsernameField")?.GetComponent<TMP_InputField>();
        loginUI.passwordField   = loginPanel.transform.Find("PasswordField")?.GetComponent<TMP_InputField>();
        loginUI.statusText      = loginPanel.transform.Find("StatusText")?.GetComponent<TextMeshProUGUI>();

        WireButton(loginPanel, "Btn_Login",         loginUI, nameof(LoginPanelUI.OnLoginClicked));
        WireButton(loginPanel, "Btn_CreateAccount",  loginUI, nameof(LoginPanelUI.OnCreateAccountClicked));
        WireButton(loginPanel, "Btn_Back",           loginUI, nameof(LoginPanelUI.OnBackClicked));

        // SettingsPanel → SettingsPanelUI
        var settingsUI = settingsPanel.AddComponent<SettingsPanelUI>();
        settingsUI.menuController   = ctrl;
        settingsUI.loggedAccountText = settingsPanel.transform.Find("LoggedLabel")?.GetComponent<TextMeshProUGUI>();
        settingsUI.logoutButton      = settingsPanel.transform.Find("Btn_Logout")?.gameObject;

        WireButton(settingsPanel, "Btn_Back",   settingsUI, nameof(SettingsPanelUI.OnBackClicked));
        WireButton(settingsPanel, "Btn_Logout", settingsUI, nameof(SettingsPanelUI.OnLogoutClicked));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[OracleMainMenuBuilder] Scène MainMenu construite — ouvre-la et appuie sur Play !");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Panel builders
    // ─────────────────────────────────────────────────────────────────────

    static GameObject BuildMainPanel(Transform parent)
    {
        var panel = MakePanel(parent, "MainPanel", PANEL_W, PANEL_H);
        float y   = PANEL_H / 2f - 60f;

        // Titre
        MakeTMP(panel.transform, "TitleText", "ORACLE", 34f, AccentColor,
            new Vector2(0f, y), new Vector2(PANEL_W - 40f, 50f), FontStyles.Bold);
        y -= 70f;

        // Sous-titre / statut connexion
        var connLabel = MakeTMP(panel.transform, "ConnectedLabel", "", FONT_SM, OkColor,
            new Vector2(0f, y), new Vector2(PANEL_W - 40f, 26f));
        connLabel.gameObject.SetActive(false);
        y -= 36f;

        // Boutons
        MakeButton(panel.transform, "Btn_Login",    "Login",       new Vector2(0f, y), BTN_W, BTN_H, BtnPrimary);
        y -= BTN_H + GAP;
        MakeButton(panel.transform, "Btn_Settings", "Paramètres",  new Vector2(0f, y), BTN_W, BTN_H, BtnPrimary);
        y -= BTN_H + GAP;
        MakeButton(panel.transform, "Btn_Quit",     "Quitter",     new Vector2(0f, y), BTN_W, BTN_H, BtnDanger);

        return panel;
    }

    static GameObject BuildLoginPanel(Transform parent)
    {
        var panel = MakePanel(parent, "LoginPanel", PANEL_W, PANEL_H);
        float y   = PANEL_H / 2f - 50f;

        MakeTMP(panel.transform, "TitleText", "CONNEXION", 26f, AccentColor,
            new Vector2(0f, y), new Vector2(PANEL_W - 40f, 40f), FontStyles.Bold);
        y -= 60f;

        MakeTMP(panel.transform, "LabelUser", "Nom d'utilisateur", FONT_SM, TextColor,
            new Vector2(0f, y), new Vector2(BTN_W, 22f));
        y -= 28f;
        MakeInputField(panel.transform, "UsernameField", "Entrez votre pseudo…",
            new Vector2(0f, y), BTN_W, INPUT_H, TMP_InputField.ContentType.Standard);
        y -= INPUT_H + GAP;

        MakeTMP(panel.transform, "LabelPass", "Mot de passe", FONT_SM, TextColor,
            new Vector2(0f, y), new Vector2(BTN_W, 22f));
        y -= 28f;
        MakeInputField(panel.transform, "PasswordField", "••••••••",
            new Vector2(0f, y), BTN_W, INPUT_H, TMP_InputField.ContentType.Password);
        y -= INPUT_H + 8f;

        // Feedback
        MakeTMP(panel.transform, "StatusText", "", FONT_SM, ErrorColor,
            new Vector2(0f, y), new Vector2(BTN_W, 24f));
        y -= 32f;

        // Boutons d'action
        float halfW = (BTN_W - 8f) / 2f;
        MakeButton(panel.transform, "Btn_Login",
            "Se connecter", new Vector2(-halfW / 2f - 4f, y),
            halfW, BTN_H, BtnPrimary);
        MakeButton(panel.transform, "Btn_CreateAccount",
            "Créer un compte", new Vector2(halfW / 2f + 4f, y),
            halfW, BTN_H, new Color(0.12f, 0.28f, 0.14f));
        y -= BTN_H + GAP;

        MakeButton(panel.transform, "Btn_Back", "← Retour",
            new Vector2(0f, y), 140f, 36f, new Color(0.15f, 0.15f, 0.15f));

        return panel;
    }

    static GameObject BuildSettingsPanel(Transform parent)
    {
        var panel = MakePanel(parent, "SettingsPanel", PANEL_W, PANEL_H);
        float y   = PANEL_H / 2f - 50f;

        MakeTMP(panel.transform, "TitleText", "PARAMÈTRES", 26f, AccentColor,
            new Vector2(0f, y), new Vector2(PANEL_W - 40f, 40f), FontStyles.Bold);
        y -= 56f;

        // Séparateur audio
        MakeTMP(panel.transform, "AudioLabel", "Audio", FONT_SM, TextColor,
            new Vector2(0f, y), new Vector2(BTN_W, 22f));
        y -= 28f;

        MakeTMP(panel.transform, "LabelMaster", "Volume général", FONT_SM, TextColor,
            new Vector2(0f, y), new Vector2(BTN_W, 20f));
        y -= 24f;
        MakeSlider(panel.transform, "MasterSlider", new Vector2(0f, y), BTN_W, 24f);
        y -= 34f;

        MakeTMP(panel.transform, "LabelMusic", "Musique", FONT_SM, TextColor,
            new Vector2(0f, y), new Vector2(BTN_W, 20f));
        y -= 24f;
        MakeSlider(panel.transform, "MusicSlider", new Vector2(0f, y), BTN_W, 24f);
        y -= 34f;

        MakeTMP(panel.transform, "LabelSfx", "Effets sonores", FONT_SM, TextColor,
            new Vector2(0f, y), new Vector2(BTN_W, 20f));
        y -= 24f;
        MakeSlider(panel.transform, "SfxSlider", new Vector2(0f, y), BTN_W, 24f);
        y -= 44f;

        // Séparateur compte
        var loggedLabel = MakeTMP(panel.transform, "LoggedLabel", "", FONT_SM, OkColor,
            new Vector2(0f, y), new Vector2(BTN_W, 22f));
        loggedLabel.gameObject.SetActive(false);
        y -= 32f;

        var logoutBtn = MakeButton(panel.transform, "Btn_Logout", "Se déconnecter",
            new Vector2(0f, y), BTN_W, BTN_H - 8f, BtnDanger);
        logoutBtn.SetActive(false);
        y -= (BTN_H - 8f) + GAP;

        MakeButton(panel.transform, "Btn_Back", "← Retour",
            new Vector2(0f, y), 140f, 36f, new Color(0.15f, 0.15f, 0.15f));

        return panel;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Factory helpers
    // ─────────────────────────────────────────────────────────────────────

    static GameObject MakePanel(Transform parent, string name, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta    = new Vector2(w, h);
        rt.anchorMin    = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = PanelColor;
        return go;
    }

    static GameObject MakeImage(Transform parent, string name, Color color, bool stretch = false)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        if (stretch)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
        }
        go.AddComponent<Image>().color = color;
        return go;
    }

    static TextMeshProUGUI MakeTMP(Transform parent, string name, string text,
        float fontSize, Color color, Vector2 pos, Vector2 size,
        FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = pos;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text          = text;
        tmp.fontSize      = fontSize;
        tmp.color         = color;
        tmp.fontStyle     = style;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    static GameObject MakeButton(Transform parent, string childName, string label,
        Vector2 pos, float w, float h, Color bgColor)
    {
        var go = new GameObject(childName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(w, h);
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.color = bgColor;

        var btn = go.AddComponent<Button>();
        var cs  = btn.colors;
        cs.normalColor      = bgColor;
        cs.highlightedColor = Color.Lerp(bgColor, Color.white, 0.2f);
        cs.pressedColor     = Color.Lerp(bgColor, Color.black, 0.2f);
        btn.colors = cs;

        // Label
        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);
        var lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.sizeDelta = Vector2.zero;
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text          = label;
        tmp.fontSize      = FONT_BTN;
        tmp.color         = TextColor;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.raycastTarget = false;

        return go;
    }

    static TMP_InputField MakeInputField(Transform parent, string name, string placeholder,
        Vector2 pos, float w, float h, TMP_InputField.ContentType contentType)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(w, h);
        rt.anchoredPosition = pos;

        go.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.22f, 1f);

        var field = go.AddComponent<TMP_InputField>();
        field.contentType = contentType;

        // Viewport
        var vpGo = new GameObject("Viewport", typeof(RectTransform));
        vpGo.transform.SetParent(go.transform, false);
        var vpRt = vpGo.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
        vpRt.sizeDelta = new Vector2(-8f, -4f);
        vpGo.AddComponent<RectMask2D>();

        // Placeholder
        var phGo = new GameObject("Placeholder", typeof(RectTransform));
        phGo.transform.SetParent(vpGo.transform, false);
        var phRt = phGo.GetComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one; phRt.sizeDelta = Vector2.zero;
        var phTmp = phGo.AddComponent<TextMeshProUGUI>();
        phTmp.text      = placeholder;
        phTmp.fontSize  = FONT_MAIN;
        phTmp.color     = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        phTmp.alignment = TextAlignmentOptions.MidlineLeft;
        phTmp.fontStyle = FontStyles.Italic;

        // Text
        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(vpGo.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one; textRt.sizeDelta = Vector2.zero;
        var textTmp = textGo.AddComponent<TextMeshProUGUI>();
        textTmp.fontSize  = FONT_MAIN;
        textTmp.color     = TextColor;
        textTmp.alignment = TextAlignmentOptions.MidlineLeft;

        field.textViewport   = vpRt;
        field.textComponent  = textTmp;
        field.placeholder    = phTmp;
        field.caretColor     = AccentColor;
        field.caretWidth     = 2;
        field.selectionColor = new Color(0.85f, 0.70f, 0.25f, 0.35f);

        return field;
    }

    static Slider MakeSlider(Transform parent, string name, Vector2 pos, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(w, h);
        rt.anchoredPosition = pos;

        // Background
        var bg = new GameObject("Background", typeof(RectTransform));
        bg.transform.SetParent(go.transform, false);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.25f); bgRt.anchorMax = new Vector2(1f, 0.75f);
        bgRt.sizeDelta = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.22f, 1f);

        // Fill Area
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        var faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0f, 0.25f); faRt.anchorMax = new Vector2(1f, 0.75f);
        faRt.sizeDelta = new Vector2(-10f, 0f);
        var fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(fillArea.transform, false);
        fill.AddComponent<Image>().color = AccentColor;
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.sizeDelta = Vector2.zero;

        // Handle
        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        var haRt = handleArea.GetComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one;
        haRt.sizeDelta = new Vector2(-10f, 0f);
        var handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(handleArea.transform, false);
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        var handleRt = handle.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(18f, 18f);

        var slider = go.AddComponent<Slider>();
        slider.fillRect   = fill.GetComponent<RectTransform>();
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.direction  = Slider.Direction.LeftToRight;
        slider.minValue   = 0f;
        slider.maxValue   = 1f;
        slider.value      = 1f;

        return slider;
    }

    // Raccourci : brancher l'UnityEvent d'un bouton sur un composant existant
    static void WireButton(GameObject panel, string btnName,
        UnityEngine.Object target, string methodName)
    {
        var btnGo = panel.transform.Find(btnName)?.gameObject;
        if (btnGo == null)
        {
            Debug.LogWarning($"[OracleMainMenuBuilder] Bouton introuvable : {panel.name}/{btnName}");
            return;
        }
        var btn = btnGo.GetComponent<Button>();
        if (btn == null) return;

        btn.onClick.RemoveAllListeners();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            btn.onClick,
            (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                typeof(UnityEngine.Events.UnityAction), target, methodName));
    }
}
#endif
