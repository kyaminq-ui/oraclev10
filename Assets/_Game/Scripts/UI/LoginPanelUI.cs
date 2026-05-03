using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Panel de connexion / création de compte.
/// Deux modes : connexion d'un compte existant, ou création d'un nouveau.
///
/// Brancher dans l'Inspector :
///   • usernameField  → TMP_InputField (nom d'utilisateur)
///   • passwordField  → TMP_InputField (mot de passe, ContentType = Password)
///   • statusText     → TextMeshProUGUI (feedback couleur rouge/vert)
///   • menuController → MainMenuController (pour le bouton Retour)
/// </summary>
public class LoginPanelUI : MonoBehaviour
{
    // ── Champs de saisie ─────────────────────────────────────────────────
    [Header("Champs")]
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;

    // ── Feedback ─────────────────────────────────────────────────────────
    [Header("Feedback")]
    public TextMeshProUGUI statusText;

    // ── Référence contrôleur ──────────────────────────────────────────────
    [Header("Navigation")]
    public MainMenuController menuController;

    [Header("Scène Hub")]
    public string hubSceneName = "Hub";

    // ── Couleurs ─────────────────────────────────────────────────────────
    static readonly UnityEngine.Color ColorOk  = new UnityEngine.Color(0.30f, 1.00f, 0.50f);
    static readonly UnityEngine.Color ColorErr = new UnityEngine.Color(1.00f, 0.30f, 0.30f);

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void OnEnable()
    {
        ClearStatus();
        if (usernameField != null && AccountManager.Instance != null)
            usernameField.text = AccountManager.Instance.LastUsername;
        usernameField?.Select();
    }

    // ── Boutons ───────────────────────────────────────────────────────────

    /// <summary>Bouton "Se connecter" — compte existant.</summary>
    public void OnLoginClicked()
    {
        if (!ValidateInputs(requireMinPassword: false)) return;

        var result = AccountManager.Instance.Login(usernameField.text, passwordField.text);
        switch (result)
        {
            case AccountManager.LoginResult.Success:
                SetStatus("Connexion réussie !", ok: true);
                Invoke(nameof(GoToHub), 0.6f);
                break;
            case AccountManager.LoginResult.WrongPassword:
                SetStatus("Mot de passe incorrect.", ok: false);
                break;
            case AccountManager.LoginResult.UserNotFound:
                SetStatus("Compte introuvable — créez-en un nouveau.", ok: false);
                break;
        }
    }

    /// <summary>Bouton "Créer un compte" — nouveau compte.</summary>
    public void OnCreateAccountClicked()
    {
        if (!ValidateInputs(requireMinPassword: true)) return;

        var result = AccountManager.Instance.CreateAccount(usernameField.text, passwordField.text);
        switch (result)
        {
            case AccountManager.LoginResult.Created:
                SetStatus("Compte créé ! Connexion en cours…", ok: true);
                Invoke(nameof(GoToHub), 0.7f);
                break;
            case AccountManager.LoginResult.UsernameTaken:
                SetStatus("Ce nom est déjà utilisé.", ok: false);
                break;
        }
    }

    /// <summary>Bouton "Retour".</summary>
    public void OnBackClicked()
    {
        CancelInvoke(nameof(GoToHub));
        menuController?.ShowMainPanel();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    bool ValidateInputs(bool requireMinPassword)
    {
        string user = usernameField != null ? usernameField.text.Trim() : "";
        string pass = passwordField != null ? passwordField.text : "";

        if (string.IsNullOrEmpty(user))
        {
            SetStatus("Entrez un nom d'utilisateur.", ok: false);
            return false;
        }

        if (user.Length < 3)
        {
            SetStatus("Le nom doit faire au moins 3 caractères.", ok: false);
            return false;
        }

        if (string.IsNullOrEmpty(pass))
        {
            SetStatus("Entrez un mot de passe.", ok: false);
            return false;
        }

        if (requireMinPassword && pass.Length < 4)
        {
            SetStatus("Mot de passe : 4 caractères minimum.", ok: false);
            return false;
        }

        return true;
    }

    void GoToHub() => SceneManager.LoadScene(hubSceneName);

    void SetStatus(string msg, bool ok)
    {
        if (statusText == null) return;
        statusText.text  = msg;
        statusText.color = ok ? ColorOk : ColorErr;
    }

    void ClearStatus()
    {
        if (statusText != null) statusText.text = "";
    }
}
