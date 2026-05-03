using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gère la présence multijoueur dans le Hub.
///
/// Logique :
///   1. Rejoint (ou crée) la room permanente "HUB_GLOBAL" (maxPlayers = 0 = illimité).
///   2. Instancie "HubPlayer" (prefab dans Resources/) via PhotonNetwork.Instantiate.
///
/// Ce composant est sur NetworkRoot (DontDestroyOnLoad) → il persiste en scènes de combat.
/// Toutes les actions de spawn/join sont gardées par IsInHubScene pour éviter tout effet
/// de bord en dehors du Hub.
/// </summary>
public class HubNetworkSpawner : MonoBehaviourPunCallbacks
{
    // ── Config ────────────────────────────────────────────────────────────
    [Header("Prefab (doit être dans Assets/_Game/Resources/)")]
    public string avatarPrefabName = "HubPlayer";

    [Header("Room Hub")]
    [Tooltip("Nom de la room Photon permanente du Hub. Identique pour tous les joueurs.")]
    public string hubRoomName = "HUB_GLOBAL";

    // ── Ref interne ───────────────────────────────────────────────────────
    GameObject _myPlayer;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        if (!PhotonNetwork.IsConnected) return;
        TryInitForHub();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        // NetworkRoot est DontDestroyOnLoad et ne se détruit jamais en pratique,
        // mais par sécurité on détruit le HubPlayer si on est encore dans la room Hub.
        if (_myPlayer != null && PhotonNetwork.InRoom)
            PhotonNetwork.Destroy(_myPlayer);
        _myPlayer = null;
    }

    /// <summary>Appelé par Unity à chaque chargement de scène. Re-initialise le Hub si on revient dedans.</summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Détruire le HubPlayer s'il a survécu au changement de scène (ex. via Photon AutomaticallySyncScene).
        if (_myPlayer != null)
        {
            Destroy(_myPlayer);
            _myPlayer = null;
        }

        if (scene.name != "Hub") return;

        TryInitForHub();
    }

    // ── Photon Callbacks ──────────────────────────────────────────────────

    public override void OnConnectedToMaster()
    {
        if (IsInHubScene) JoinHubRoom();
    }

    /// <summary>
    /// Appelé avant de quitter une room pour détruire proprement le HubPlayer
    /// via le réseau (doit être appelé pendant qu'on est encore dans la room).
    /// </summary>
    public void DestroyPlayerBeforeLeave()
    {
        if (_myPlayer == null) return;
        if (PhotonNetwork.InRoom)
            PhotonNetwork.Destroy(_myPlayer);
        else
            Destroy(_myPlayer);
        _myPlayer = null;
    }

    /// <summary>
    /// Déclenché après avoir quitté une room (Hub ou combat).
    /// On s'assure que le HubPlayer est bien nettoyé localement (filet de sécurité).
    /// </summary>
    public override void OnLeftRoom()
    {
        // Filet de sécurité : si DestroyPlayerBeforeLeave n'a pas été appelé,
        // on détruit l'objet localement pour éviter qu'il soit re-instancié
        // par Photon lors du prochain LoadLevel.
        if (_myPlayer != null)
        {
            Destroy(_myPlayer);
            _myPlayer = null;
        }

        if (HubMatchmaker.Instance != null && HubMatchmaker.Instance.IsSearching) return;
        if (IsInHubScene) JoinHubRoom();
        // Hors Hub (ex. fin de combat) : on laisse OnSceneLoaded gérer le retour.
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.CurrentRoom.Name != hubRoomName) return;
        if (!IsInHubScene) return; // ne pas spawner si on n'est pas dans la scène Hub

        Debug.Log($"[HubNetworkSpawner] Room Hub rejointe : {PhotonNetwork.CurrentRoom.Name} " +
                  $"({PhotonNetwork.CurrentRoom.PlayerCount} joueur(s))");
        SpawnPlayer();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[HubNetworkSpawner] JoinRoom échoué ({returnCode} {message}) — nouvelle tentative.");
        if (IsInHubScene) JoinHubRoom();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) =>
        Debug.Log($"[HubNetworkSpawner] {newPlayer.NickName} a rejoint le hub.");

    public override void OnPlayerLeftRoom(Player otherPlayer) =>
        Debug.Log($"[HubNetworkSpawner] {otherPlayer.NickName} a quitté le hub.");

    // ── Helpers ───────────────────────────────────────────────────────────

    bool IsInHubScene => SceneManager.GetActiveScene().name == "Hub";

    /// <summary>Tente de rejoindre ou créer HUB_GLOBAL, puis spawner le joueur local.</summary>
    void TryInitForHub()
    {
        if (!IsInHubScene) return;
        if (!PhotonNetwork.IsConnected) return;

        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.CurrentRoom.Name == hubRoomName)
                SpawnPlayer();
            else
                PhotonNetwork.LeaveRoom(); // retour depuis combat → quitter la room de combat
        }
        else if (PhotonNetwork.IsConnectedAndReady)
        {
            JoinHubRoom();
        }
    }

    void JoinHubRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady) return;
        if (!IsInHubScene) return;

        var opts = new RoomOptions
        {
            MaxPlayers          = 0,
            IsVisible           = false,
            IsOpen              = true,
            CleanupCacheOnLeave = true
        };
        PhotonNetwork.JoinOrCreateRoom(hubRoomName, opts, TypedLobby.Default);
    }

    void SpawnPlayer()
    {
        if (_myPlayer != null) return;
        if (!IsInHubScene) return;
        // Vérification supplémentaire : être dans la bonne room avant de spawner.
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom.Name != hubRoomName) return;

        _myPlayer = PhotonNetwork.Instantiate(avatarPrefabName, Vector3.zero, Quaternion.identity);
    }
}
