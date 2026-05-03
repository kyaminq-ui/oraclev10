using System;

/// <summary>
/// Données persistantes d'un compte joueur — sérialisées en JSON via JsonUtility (PlayerPrefs).
/// Tous les champs ajoutés ici sont automatiquement sauvegardés et chargés par AccountManager.
/// </summary>
[Serializable]
public class PlayerAccountData
{
    // ── Identité ─────────────────────────────────────────────────────────
    public string username    = "";   // clé unique (minuscules)
    public string displayName = "";   // affiché en jeu / Photon NickName

    // ── Progression (valeurs de base — à étendre) ────────────────────────
    public int mmr        = 1000;
    public int rank       = 0;        // 0 = non classé
    public int totalGames = 0;
    public int wins       = 0;

    // ── Deck actif ───────────────────────────────────────────────────────
    /// <summary>
    /// Nom du DeckData ScriptableObject actif (lookup via Resources.Load au besoin).
    /// Vide = pas de deck sauvegardé.
    /// </summary>
    public string selectedDeckId = "";

    // ── Métadonnées ──────────────────────────────────────────────────────
    public long createdAt   = 0;   // Unix timestamp UTC (secondes)
    public long lastLoginAt = 0;

    // ── Helpers ──────────────────────────────────────────────────────────
    public int Losses => totalGames - wins;

    public float WinRate => totalGames > 0 ? (float)wins / totalGames : 0f;
}
