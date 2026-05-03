using Photon.Pun;
using UnityEngine;
using TMPro;

/// <summary>
/// Avatar réseau d'un joueur dans le Hub.
///
/// • Si photonView.IsMine  : invisible, suit la position de LocalPlayer et la broadcast.
/// • Si !photonView.IsMine : affiche un sprite + label avec le nom du joueur distant,
///                           se déplace par interpolation vers la position reçue.
///
/// Nécessite : PhotonView + PhotonTransformView sur le même GameObject.
/// Ce prefab doit être dans Assets/_Game/Resources/HubPlayerAvatar.prefab
/// (requis par PhotonNetwork.Instantiate).
/// </summary>
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(PhotonTransformView))]
public class HubPlayerAvatar : MonoBehaviourPun
{
    // ── Références visuelles ──────────────────────────────────────────────
    [Header("Visuel")]
    [Tooltip("SpriteRenderer de l'avatar (affiché uniquement pour les joueurs distants).")]
    public SpriteRenderer avatarSprite;

    [Tooltip("Sprite par défaut si aucun sprite personnalisé n'est assigné.")]
    public Sprite fallbackSprite;

    [Header("Label")]
    [Tooltip("Décalage monde au-dessus du sprite pour le label de nom.")]
    public Vector3 labelOffset = new Vector3(0f, 0.7f, 0f);

    // ── État interne ──────────────────────────────────────────────────────
    Transform          _localPlayer;   // référence au LocalPlayer (uniquement si IsMine)
    TextMeshProUGUI    _nameLabel;
    Camera             _cam;

    // ── Couleurs du label ─────────────────────────────────────────────────
    static readonly Color LabelBg   = new Color(0f, 0f, 0f, 0.62f);
    static readonly Color LabelText = new Color(0.95f, 0.87f, 0.60f, 1f);

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Awake()
    {
        _cam = Camera.main;

        if (photonView.IsMine)
        {
            // Avatar local : invisible — sert uniquement à broadcaster la position
            if (avatarSprite != null) avatarSprite.enabled = false;
        }
        else
        {
            // Avatar distant : afficher sprite + label
            if (avatarSprite != null)
            {
                avatarSprite.enabled = true;
                if (avatarSprite.sprite == null && fallbackSprite != null)
                    avatarSprite.sprite = fallbackSprite;
            }

            BuildNameLabel();
        }
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        // Suivre le LocalPlayer → PhotonTransformView broadcast automatiquement cette position
        if (_localPlayer != null)
            transform.position = _localPlayer.position;
    }

    void LateUpdate()
    {
        UpdateLabelPosition();
    }

    // ── API publique ──────────────────────────────────────────────────────

    /// <summary>Appelé par HubNetworkSpawner juste après l'instantiation locale.</summary>
    public void LinkToLocalPlayer(Transform localPlayerTransform)
    {
        _localPlayer = localPlayerTransform;
    }

    // ── Construction du label ─────────────────────────────────────────────

    void BuildNameLabel()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        string playerName = photonView.Owner?.NickName;
        if (string.IsNullOrEmpty(playerName)) playerName = "???";

        // Conteneur
        var go = new GameObject($"Label_{playerName}", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);

        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(130f, 22f);
        rt.pivot     = new Vector2(0.5f, 0f);

        go.AddComponent<UnityEngine.UI.Image>().color = LabelBg;

        // Texte
        var textGo = new GameObject("T", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var tRt = (RectTransform)textGo.transform;
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = new Vector2(4f, 1f);
        tRt.offsetMax = new Vector2(-4f, -1f);

        _nameLabel               = textGo.AddComponent<TextMeshProUGUI>();
        _nameLabel.text          = playerName;
        _nameLabel.fontSize      = 10.5f;
        _nameLabel.color         = LabelText;
        _nameLabel.alignment     = TextAlignmentOptions.Center;
        _nameLabel.fontStyle     = FontStyles.Bold;
        _nameLabel.raycastTarget = false;
    }

    void UpdateLabelPosition()
    {
        if (_nameLabel == null || _cam == null) return;

        var rt     = _nameLabel.transform.parent.GetComponent<RectTransform>();
        if (rt == null) return;

        Vector3 sp = _cam.WorldToScreenPoint(transform.position + labelOffset);
        rt.position = new Vector3(sp.x, sp.y, 0f);

        // Cacher si derrière la caméra
        bool visible = sp.z > 0f;
        if (rt.gameObject.activeSelf != visible)
            rt.gameObject.SetActive(visible);
    }

    void OnDestroy()
    {
        // Nettoyer le label si le joueur quitte
        if (_nameLabel != null)
            Destroy(_nameLabel.transform.parent.gameObject);
    }
}
