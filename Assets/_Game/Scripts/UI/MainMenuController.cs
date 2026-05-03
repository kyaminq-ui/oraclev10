using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Contrôleur de la scène MainMenu.
/// Gère les trois boutons principaux (Login, Paramètres, Quitter)
/// et la navigation entre les panneaux.
///
/// Dépend de AccountManager (DontDestroyOnLoad) qui doit exister
/// sur un GameObject "AccountManager" dans cette scène.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    // ── Panels ────────────────────────────────────────────────────────────
    [Header("Panels")]
    [Tooltip("Panel principal avec les trois boutons.")]
    public GameObject mainPanel;
    [Tooltip("Panel de connexion / création de compte.")]
    public GameObject loginPanel;
    [Tooltip("Panel des paramètres.")]
    public GameObject settingsPanel;

    // ── UI d'état ─────────────────────────────────────────────────────────
    [Header("État de connexion")]
    [Tooltip("Label affiché sous les boutons quand un compte est déjà connecté.")]
    public TextMeshProUGUI connectedLabel;
    [Tooltip("Objet bouton 'Login' — son texte change si déjà connecté.")]
    public TextMeshProUGUI loginButtonLabel;

    // ── Config ────────────────────────────────────────────────────────────
    [Header("Navigation")]
    public string hubSceneName = "Hub";

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Start()
    {
        ShowMainPanel();
        RefreshLoginState();
    }

    void OnEnable()  => RefreshLoginState();

    // ── Boutons principaux ────────────────────────────────────────────────

    /// <summary>Bouton "Login" : ouvre le panel de connexion ou va directement au Hub.</summary>
    public void OnLoginClicked()
    {
        if (AccountManager.Instance != null && AccountManager.Instance.IsLoggedIn)
        {
            GoToHub();
            return;
        }
        ShowPanel(loginPanel);
    }

    /// <summary>Bouton "Paramètres".</summary>
    public void OnSettingsClicked() => ShowPanel(settingsPanel);

    /// <summary>Bouton "Quitter".</summary>
    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Navigation interne ────────────────────────────────────────────────

    public void ShowMainPanel()
    {
        mainPanel?.SetActive(true);
        loginPanel?.SetActive(false);
        settingsPanel?.SetActive(false);
        RefreshLoginState();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    void ShowPanel(GameObject panel)
    {
        mainPanel?.SetActive(false);
        loginPanel?.SetActive(false);
        settingsPanel?.SetActive(false);
        panel?.SetActive(true);
    }

    void RefreshLoginState()
    {
        bool logged = AccountManager.Instance != null && AccountManager.Instance.IsLoggedIn;

        if (connectedLabel != null)
        {
            connectedLabel.gameObject.SetActive(logged);
            if (logged)
                connectedLabel.text = $"Connecté : <b>{AccountManager.Instance.CurrentAccount.displayName}</b>";
        }

        if (loginButtonLabel != null)
            loginButtonLabel.text = logged ? "Entrer dans le Hub" : "Login";
    }

    void GoToHub() => SceneManager.LoadScene(hubSceneName);
}
