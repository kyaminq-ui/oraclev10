using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Affiche les infos du compte connecté dans la scène Hub (bandeau supérieur).
/// Redirige vers MainMenu si aucun compte n'est connecté.
///
/// Brancher dans l'Inspector :
///   • displayNameText → TextMeshProUGUI  (ex: "Bienvenue, LordOracle")
///   • mmrText         → TextMeshProUGUI  (ex: "MMR : 1 000")
///   • statsText       → TextMeshProUGUI  (ex: "15V / 7D")  (optionnel)
/// </summary>
public class HubAccountHeader : MonoBehaviour
{
    [Header("Labels")]
    public TextMeshProUGUI displayNameText;
    public TextMeshProUGUI mmrText;
    public TextMeshProUGUI statsText;

    [Header("Déconnexion")]
    [Tooltip("Bouton optionnel 'Se déconnecter' dans le Hub.")]
    public GameObject logoutButton;

    [Header("Navigation")]
    public string mainMenuSceneName = "MainMenu";

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Start()
    {
        // Sécurité : si on arrive dans le Hub sans être connecté, retour au menu
        if (AccountManager.Instance == null || !AccountManager.Instance.IsLoggedIn)
        {
            Debug.LogWarning("[HubAccountHeader] Aucun compte connecté — retour au menu principal.");
            SceneManager.LoadScene(mainMenuSceneName);
            return;
        }

        Refresh();
    }

    // ── API publique ──────────────────────────────────────────────────────

    /// <summary>Met à jour tous les labels (appeler après modification du compte).</summary>
    public void Refresh()
    {
        if (AccountManager.Instance == null || !AccountManager.Instance.IsLoggedIn)
            return;

        var acc = AccountManager.Instance.CurrentAccount;

        if (displayNameText != null)
            displayNameText.text = $"Bienvenue, <b>{acc.displayName}</b>";

        if (mmrText != null)
            mmrText.text = $"MMR : {acc.mmr:N0}";

        if (statsText != null)
            statsText.text = acc.totalGames > 0
                ? $"{acc.wins}V / {acc.Losses}D ({acc.WinRate:P0})"
                : "Aucune partie jouée";

        if (logoutButton != null)
            logoutButton.SetActive(true);
    }

    // ── Bouton Se déconnecter ─────────────────────────────────────────────

    public void OnLogoutClicked()
    {
        AccountManager.Instance?.Logout();
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
