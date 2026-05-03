using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// Gère la recherche de match (matchmaking) depuis le Hub.
///
/// Flux 1v1 :
///   1. StartSearch1v1()  → quitte la room Hub, tente JoinRandomRoom (maxPlayers=2).
///   2. Si aucune room    → CreateRoom avec nom aléatoire, attente du 2e joueur.
///   3. Dès que 2 joueurs → MasterClient charge la scène de combat.
///   4. CancelSearch()    → quitte la room de matchmaking, retour au Hub.
///
/// Placer sur NetworkRoot aux côtés de HubNetworkSpawner.
/// </summary>
public class HubMatchmaker : MonoBehaviourPunCallbacks
{
    public static HubMatchmaker Instance { get; private set; }

    [Header("Config")]
    public string combatSceneName       = "Ranked1v1";
    public string matchmakingRoomPrefix = "oracle_1v1_";
    public byte   maxPlayersFor1v1      = 2;

    // ── État ──────────────────────────────────────────────────────────────
    public bool IsSearching { get; private set; }

    // ── Events (abonnés par HubHUD pour mettre à jour l'UI) ──────────────
    public event System.Action          OnSearchStarted;
    public event System.Action          OnSearchCancelled;
    public event System.Action          OnMatchFound;
    /// <summary>(joueurs actuels, joueurs requis)</summary>
    public event System.Action<int,int> OnPlayerCountUpdated;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── API publique ──────────────────────────────────────────────────────

    /// <summary>Lance la recherche d'un match 1v1.</summary>
    public void StartSearch1v1()
    {
        if (IsSearching) return;
        IsSearching = true;

        // DOIT être activé sur tous les clients AVANT d'entrer dans la room de combat.
        // Si seulement le MasterClient l'a à true, lui seul chargera la scène.
        PhotonNetwork.AutomaticallySyncScene = true;

        OnSearchStarted?.Invoke();
        OnPlayerCountUpdated?.Invoke(0, maxPlayersFor1v1);

        Debug.Log("[HubMatchmaker] Recherche 1v1 démarrée.");

        if (PhotonNetwork.InRoom)
        {
            // Détruire le HubPlayer AVANT de quitter la room — s'il est encore en vie
            // et non détruit via PhotonNetwork.Destroy(), Photon le ré-instancierait
            // dans la scène de combat via AutomaticallySyncScene.
            GetComponent<HubNetworkSpawner>()?.DestroyPlayerBeforeLeave();
            PhotonNetwork.LeaveRoom();
        }
        else
            JoinOrCreateMatchRoom();
    }

    /// <summary>Annule la recherche en cours et retourne au Hub.</summary>
    public void CancelSearch()
    {
        if (!IsSearching) return;
        IsSearching = false;
        PhotonNetwork.AutomaticallySyncScene = false; // remettre à false pour le Hub
        OnSearchCancelled?.Invoke();

        Debug.Log("[HubMatchmaker] Recherche annulée.");

        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
        // HubNetworkSpawner.OnLeftRoom() prendra le relais pour rejoindre HUB_GLOBAL
    }

    // ── Photon Callbacks ──────────────────────────────────────────────────

    public override void OnLeftRoom()
    {
        if (!IsSearching) return;
        JoinOrCreateMatchRoom();
    }

    public override void OnJoinedRoom()
    {
        if (!IsSearching) return;
        int count = PhotonNetwork.CurrentRoom.PlayerCount;
        OnPlayerCountUpdated?.Invoke(count, maxPlayersFor1v1);
        Debug.Log($"[HubMatchmaker] Dans la room de match ({count}/{maxPlayersFor1v1}).");
        TryStartCombat();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!IsSearching) return;
        int count = PhotonNetwork.CurrentRoom.PlayerCount;
        OnPlayerCountUpdated?.Invoke(count, maxPlayersFor1v1);
        Debug.Log($"[HubMatchmaker] {newPlayer.NickName} a rejoint ({count}/{maxPlayersFor1v1}).");
        TryStartCombat();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        if (!IsSearching) return;
        Debug.LogWarning($"[HubMatchmaker] Création de room échouée ({returnCode}) — nouvelle tentative.");
        JoinOrCreateMatchRoom();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    void JoinOrCreateMatchRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady) return;

        // JoinRandomOrCreateRoom est atomique : évite la race condition où deux clients
        // échouent à JoinRandom simultanément et créent chacun leur propre room.
        string newRoomName = matchmakingRoomPrefix + System.Guid.NewGuid().ToString("N")[..8];
        var opts = new RoomOptions
        {
            MaxPlayers          = maxPlayersFor1v1,
            IsVisible           = true,
            IsOpen              = true,
            CleanupCacheOnLeave = true
        };
        PhotonNetwork.JoinRandomOrCreateRoom(
            null,
            maxPlayersFor1v1,
            MatchmakingMode.FillRoom,
            TypedLobby.Default,
            null,
            newRoomName,
            opts
        );
    }

    void TryStartCombat()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount < maxPlayersFor1v1) return;

        // Tous les clients marquent la recherche comme terminée dès que la room est pleine.
        // Sans ça, le client non-master garde IsSearching = true indéfiniment.
        OnMatchFound?.Invoke();
        IsSearching = false;

        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log($"[HubMatchmaker] Match trouvé ! Chargement → {combatSceneName}");
        PhotonNetwork.LoadLevel(combatSceneName);
    }
}
