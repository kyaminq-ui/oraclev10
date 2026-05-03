using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

/// <summary>
/// Affiche le nom du joueur au-dessus de son personnage dans le hub,
/// UNIQUEMENT quand la souris survole le personnage.
/// Placer sur LocalPlayer (même GO que HubCharacterController).
/// </summary>
public class HubPlayerLabel : MonoBehaviour
{
    [Header("Position")]
    [Tooltip("Décalage monde au-dessus du sprite.")]
    public Vector3 worldOffset = new Vector3(0f, 1.85f, 0f);

    [Header("Hover")]
    [Tooltip("Rayon en pixels écran autour du personnage pour déclencher l'affichage.")]
    public float hoverPixelRadius = 52f;

    [Header("Nom de secours")]
    public string fallbackName = "Joueur";

    // ── Palette ──────────────────────────────────────────────────────────
    static readonly Color LabelBg   = new Color(0f,    0f,    0f,    0.62f);
    static readonly Color LabelText = new Color(0.95f, 0.87f, 0.60f, 1f);

    // ── Refs ─────────────────────────────────────────────────────────────
    Camera        _cam;
    Canvas        _canvas;
    RectTransform _root;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Start()
    {
        _cam    = Camera.main;
        _canvas = FindObjectOfType<Canvas>();
        if (_canvas != null) CreateLabel();
    }

    void Update()
    {
        if (_root == null || _cam == null) return;

        // Vérifier si la souris est dans le rayon autour du personnage
        Vector3 sp   = _cam.WorldToScreenPoint(transform.position);
        float   dist = Vector2.Distance(Input.mousePosition, new Vector2(sp.x, sp.y));
        bool    over = dist <= hoverPixelRadius;

        if (_root.gameObject.activeSelf != over)
            _root.gameObject.SetActive(over);
    }

    void LateUpdate()
    {
        if (_root == null || !_root.gameObject.activeSelf || _cam == null) return;
        Vector3 sp     = _cam.WorldToScreenPoint(transform.position + worldOffset);
        _root.position = new Vector3(sp.x, sp.y, 0f);
    }

    // ── Construction UI ───────────────────────────────────────────────────

    void CreateLabel()
    {
        // Joueur distant → NickName du propriétaire Photon
        // Joueur local   → AccountManager > PhotonNetwork.NickName > fallback
        var pv = GetComponent<PhotonView>();
        string name;
        if (pv != null && !pv.IsMine)
        {
            name = pv.Owner?.NickName ?? "";
            if (string.IsNullOrEmpty(name)) name = fallbackName;
        }
        else
        {
            name = AccountManager.Instance != null && AccountManager.Instance.IsLoggedIn
                ? AccountManager.Instance.CurrentAccount.displayName
                : string.IsNullOrEmpty(PhotonNetwork.NickName)
                    ? fallbackName
                    : PhotonNetwork.NickName;
        }

        var go = new GameObject("PlayerNameLabel", typeof(RectTransform));
        go.transform.SetParent(_canvas.transform, false);

        _root           = (RectTransform)go.transform;
        _root.sizeDelta = new Vector2(130f, 20f);
        _root.pivot     = new Vector2(0.5f, 0f);
        go.SetActive(false);    // caché au départ — visible au survol uniquement

        go.AddComponent<Image>().color = LabelBg;

        var tGo = new GameObject("T", typeof(RectTransform));
        tGo.transform.SetParent(go.transform, false);
        var tRt = (RectTransform)tGo.transform;
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = new Vector2(4f, 1f);
        tRt.offsetMax = new Vector2(-4f, -1f);

        var tmp = tGo.AddComponent<TextMeshProUGUI>();
        tmp.text          = name;
        tmp.fontSize      = 10.5f;
        tmp.color         = LabelText;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.raycastTarget = false;
    }

    /// <summary>Met à jour le nom affiché (utile si le NickName change après Start).</summary>
    public void SetName(string name)
    {
        if (_root == null) return;
        var tmp = _root.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = name;
    }
}
