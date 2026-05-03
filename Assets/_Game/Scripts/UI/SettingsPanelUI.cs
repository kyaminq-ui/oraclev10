using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel Paramètres (placeholder extensible).
/// Contient les réglages audio de base — à compléter au fil du projet.
///
/// Brancher dans l'Inspector :
///   • masterVolumeSlider / musicVolumeSlider / sfxVolumeSlider (optionnels)
///   • menuController → MainMenuController (bouton Retour)
/// </summary>
public class SettingsPanelUI : MonoBehaviour
{
    [Header("Navigation")]
    public MainMenuController menuController;

    [Header("Audio (optionnels)")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Compte")]
    [Tooltip("Affiché si un compte est connecté.")]
    public TextMeshProUGUI loggedAccountText;
    [Tooltip("Bouton Se déconnecter — affiché uniquement si connecté.")]
    public GameObject logoutButton;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void OnEnable()
    {
        // Restaurer les valeurs audio
        masterVolumeSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat("vol_master", 1f));
        musicVolumeSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat("vol_music", 0.7f));
        sfxVolumeSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat("vol_sfx", 1f));

        // État du compte
        bool logged = AccountManager.Instance != null && AccountManager.Instance.IsLoggedIn;
        if (loggedAccountText != null)
        {
            loggedAccountText.gameObject.SetActive(logged);
            if (logged)
                loggedAccountText.text = $"Compte : <b>{AccountManager.Instance.CurrentAccount.displayName}</b>";
        }
        if (logoutButton != null)
            logoutButton.SetActive(logged);
    }

    // ── Boutons / Sliders ─────────────────────────────────────────────────

    public void OnMasterVolumeChanged(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat("vol_master", v);
    }

    public void OnMusicVolumeChanged(float v)
    {
        PlayerPrefs.SetFloat("vol_music", v);
        // TODO : appliquer au AudioSource musique de fond quand il existera
    }

    public void OnSfxVolumeChanged(float v)
    {
        PlayerPrefs.SetFloat("vol_sfx", v);
    }

    /// <summary>Bouton "Se déconnecter".</summary>
    public void OnLogoutClicked()
    {
        AccountManager.Instance?.Logout();
        menuController?.ShowMainPanel();
    }

    /// <summary>Bouton "Retour".</summary>
    public void OnBackClicked()
    {
        PlayerPrefs.Save();
        menuController?.ShowMainPanel();
    }
}
