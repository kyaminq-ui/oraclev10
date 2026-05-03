using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;

/// <summary>
/// Contrôleur du joueur local dans le hub.
///
/// • Se place SYNCHRONEMENT dans Start() sur la case centrale marchable.
///   ArenaGenerator s'exécute à l'ordre -5, donc la grille est déjà générée.
/// • Téléporte la caméra IsometricCamera sur le joueur dès le premier frame.
/// • Clic gauche sur une case marchable → MoveFree() (déplacement sans coût PM)
/// • Survol → highlight de la case sous le curseur
///
/// Nécessite TacticalCharacter + PlayerAnimator sur le même GO.
/// </summary>
[RequireComponent(typeof(TacticalCharacter))]
public class HubCharacterController : MonoBehaviour
{
    [Header("Caméra (vide = Camera.main)")]
    public Camera cam;

    [Header("Placement initial")]
    [Tooltip("Rayon de recherche max autour du centre si la case centrale est occupée.")]
    public int placementSearchRadius = 20;

    TacticalCharacter _character;
    Cell              _lastHoveredCell;
    bool              _isLocalPlayer = true;

    // ── Lifecycle ──────────────────────────────────────────────────────

    void Awake()
    {
        _character = GetComponent<TacticalCharacter>();
    }

    void Start()
    {
        if (cam == null) cam = Camera.main;

        // ArenaGenerator [ExecutionOrder -5] a déjà tourné en Start(),
        // la grille et les tiles sont prêts → placement synchrone.
        var pv = GetComponent<PhotonView>();
        _isLocalPlayer = (pv == null || pv.IsMine);

        PlaceCharacter();
    }

    // ── Placement initial ─────────────────────────────────────────────

    void PlaceCharacter()
    {
        var gm = GridManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[HubCharacterController] GridManager introuvable. Vérifie que GridManager est dans la scène.");
            return;
        }
        if (_character == null || _character.stats == null)
        {
            Debug.LogError("[HubCharacterController] TacticalCharacter ou CharacterStats manquant. " +
                           "Lance Oracle > Setup Hub Player pour configurer le personnage.");
            return;
        }

        // Nettoyer les highlights de spawn laissés par ArenaGenerator
        gm.ClearAllHighlights();

        // Case centrale marchable
        int  cx        = gm.GridWidth  / 2;
        int  cy        = gm.GridHeight / 2;
        Cell startCell = FindNearestWalkable(cx, cy, gm);

        if (startCell == null)
        {
            Debug.LogError("[HubCharacterController] Aucune case marchable trouvée pour placer le joueur !");
            return;
        }

        _character.Initialize(startCell);

        // Caméra uniquement pour le joueur local
        if (!_isLocalPlayer) return;

        var isoCam = cam != null ? cam.GetComponent<IsometricCamera>() : null;
        if (isoCam != null)
        {
            isoCam.target = transform;

            float camAbsZ = Mathf.Abs(cam.transform.position.z);
            float yComp   = camAbsZ * Mathf.Tan(isoCam.isometricAngle * Mathf.Deg2Rad);
            isoCam.followOffset = new Vector2(isoCam.followOffset.x, yComp);

            isoCam.TeleportTo(new Vector2(
                transform.position.x + isoCam.followOffset.x,
                transform.position.y + isoCam.followOffset.y));
        }
    }

    Cell FindNearestWalkable(int cx, int cy, GridManager gm)
    {
        // Spirale depuis le centre
        for (int r = 0; r <= placementSearchRadius; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue; // bord du carré seulement
                var c = gm.GetCell(cx + dx, cy + dy);
                if (c != null && c.IsWalkable && !c.IsOccupied) return c;
            }
        }
        return null;
    }

    // ── Update — clic gauche et hover ─────────────────────────────────

    void Update()
    {
        // Ignorer input pour les joueurs distants
        if (!_isLocalPlayer) return;
        if (cam == null || GridManager.Instance == null) return;

        Cell hovered = GridManager.Instance.GetCellAtScreenPosition(cam, Input.mousePosition);

        HandleHover(hovered);
        HandleLeftClick(hovered);

        _lastHoveredCell = hovered;
    }

    void HandleHover(Cell cell)
    {
        if (cell == _lastHoveredCell) return;
        if (cell != null)
            GridManager.Instance.SetHoveredCell(cell.GridX, cell.GridY);
    }

    void HandleLeftClick(Cell cell)
    {
        if (!Input.GetMouseButtonDown(0)) return;

        // Ignorer si le curseur est sur un élément UI (HUD, chat, boutons…)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (cell == null || !cell.IsWalkable) return;
        // Ne pas re-déplacer vers la case déjà occupée par le joueur lui-même
        if (cell.IsOccupied && cell != _character.CurrentCell) return;

        _character.MoveFree(cell);
    }
}
