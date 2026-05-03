using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// Singleton persistant (DontDestroyOnLoad) — gère les comptes joueurs locaux.
///
/// Stockage : PlayerPrefs (JSON + hash SHA-256 du mot de passe).
/// Clés PlayerPrefs :
///   oracle_account_{username}  → JSON PlayerAccountData
///   oracle_pass_{username}     → SHA-256 base64 du mot de passe
///   oracle_last_user           → dernier nom d'utilisateur (pour pré-remplir le champ)
///
/// À placer sur un GameObject "AccountManager" dans la scène MainMenu.
/// Il survivra à tous les changements de scène.
/// </summary>
[DisallowMultipleComponent]
public class AccountManager : MonoBehaviour
{
    public static AccountManager Instance { get; private set; }

    // ── État courant ──────────────────────────────────────────────────────
    public PlayerAccountData CurrentAccount { get; private set; }
    public bool IsLoggedIn => CurrentAccount != null;

    /// <summary>Dernier nom d'utilisateur utilisé (pour pré-remplir le champ de connexion).</summary>
    public string LastUsername => PlayerPrefs.GetString(KeyLastUser, "");

    // ── Résultat des opérations de login ─────────────────────────────────
    public enum LoginResult
    {
        Success,
        WrongPassword,
        UserNotFound,
        Created,
        UsernameTaken
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── API publique ──────────────────────────────────────────────────────

    /// <summary>Tente de connecter un compte existant.</summary>
    public LoginResult Login(string username, string password)
    {
        string user = username.Trim().ToLowerInvariant();
        if (!AccountExists(user)) return LoginResult.UserNotFound;

        string stored = PlayerPrefs.GetString(PassKey(user), "");
        if (stored != HashPassword(password)) return LoginResult.WrongPassword;

        string json = PlayerPrefs.GetString(AccountKey(user), "{}");
        CurrentAccount = JsonUtility.FromJson<PlayerAccountData>(json);
        CurrentAccount.lastLoginAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        SaveCurrentAccount();

        PlayerPrefs.SetString(KeyLastUser, CurrentAccount.displayName);
        PlayerPrefs.Save();

        ApplyToPhoton();
        Debug.Log($"[AccountManager] Connecté : {CurrentAccount.displayName} — MMR {CurrentAccount.mmr}");
        return LoginResult.Success;
    }

    /// <summary>Crée un nouveau compte et le connecte immédiatement.</summary>
    public LoginResult CreateAccount(string username, string password)
    {
        string user = username.Trim().ToLowerInvariant();
        if (AccountExists(user)) return LoginResult.UsernameTaken;

        var account = new PlayerAccountData
        {
            username    = user,
            displayName = username.Trim(),   // respecte la casse d'origine
            mmr         = 1000,
            rank        = 0,
            createdAt   = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            lastLoginAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        CurrentAccount = account;
        PlayerPrefs.SetString(PassKey(user), HashPassword(password));
        SaveCurrentAccount();

        PlayerPrefs.SetString(KeyLastUser, account.displayName);
        PlayerPrefs.Save();

        ApplyToPhoton();
        Debug.Log($"[AccountManager] Compte créé : {account.displayName}");
        return LoginResult.Created;
    }

    /// <summary>Vérifie si un compte avec ce nom existe déjà.</summary>
    public bool AccountExists(string username) =>
        PlayerPrefs.HasKey(AccountKey(username.Trim().ToLowerInvariant()));

    /// <summary>Sauvegarde les données du compte courant (appeler après chaque modification).</summary>
    public void SaveCurrentAccount()
    {
        if (CurrentAccount == null) return;
        PlayerPrefs.SetString(AccountKey(CurrentAccount.username), JsonUtility.ToJson(CurrentAccount));
        PlayerPrefs.Save();
    }

    /// <summary>Déconnecte le joueur (les données restent sauvegardées sur disque).</summary>
    public void Logout()
    {
        Debug.Log($"[AccountManager] Déconnexion : {CurrentAccount?.displayName}");
        CurrentAccount = null;
        PhotonNetwork.NickName = "";
    }

    // ── Helpers internes ──────────────────────────────────────────────────

    void ApplyToPhoton()
    {
        if (CurrentAccount == null) return;
        PhotonNetwork.NickName = CurrentAccount.displayName;
    }

    static string AccountKey(string user) => $"oracle_account_{user}";
    static string PassKey(string user)    => $"oracle_pass_{user}";
    const  string KeyLastUser             = "oracle_last_user";

    static string HashPassword(string password)
    {
        byte[] bytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(password + "oracle_v4_salt"));
        return Convert.ToBase64String(bytes);
    }
}
