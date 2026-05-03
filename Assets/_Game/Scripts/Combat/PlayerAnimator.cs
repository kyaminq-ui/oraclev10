using System.Collections;
using UnityEngine;

/// <summary>
/// Une animation directionnelle : tableau de sprites + cadence.
/// </summary>
[System.Serializable]
public class DirectionalAnimation
{
    [Tooltip("Frames dans l'ordre de lecture")]
    public Sprite[] frames;

    [Tooltip("Images par seconde (peut être overridé au runtime)")]
    [Min(1f)] public float fps = 8f;

    [Tooltip("Boucle infinie (désactiver pour mort / cast one-shot)")]
    public bool loop = true;
}

/// <summary>
/// Slot de direction : utilisé pour le remappage visuel.
/// </summary>
public enum DirectionSlot { SO, SE, NE, NO }

/// <summary>
/// Gère les animations sprites directionnelles du personnage (Idle / Marche / Sprint / Dégâts / Mort).
///
/// Architecture :
///   Au démarrage, ce script crée un child "SpriteVisual" qui porte le SpriteRenderer.
///   Le parent (TacticalCharacter) garde sa position logique de grille.
///   Le child peut être déplacé/mis à l'échelle librement sans perturber la logique.
///
/// — Remappage de directions —
/// Si une direction affiche le mauvais sprite, change les dropdowns
/// "Quand le perso va vers X, jouer le sprite →" dans l'Inspector.
///
/// — Vitesse de marche —
/// 1–2 cases : marche lente (walkFpsSlow)
/// 3–4 cases : marche normale (walkFpsNormal)
/// 5+ cases  : sprint (sprintFps) si des frames sprint sont disponibles (Resources ou Inspector).
/// </summary>
[RequireComponent(typeof(TacticalCharacter))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimator : MonoBehaviour
{
    // =========================================================
    // TAILLE & POSITION VISUELLE
    // =========================================================
    [Header("Taille & position visuelle")]
    [Tooltip("Facteur d'échelle du sprite (1 = original, 2 = double)")]
    public float spriteScale = 2f;

    [Tooltip("Décalage visuel du sprite par rapport à la position logique de la case.\n" +
             "X : centrage horizontal  |  Y : hauteur sur la case (positif = monter)")]
    public Vector2 visualOffset = new Vector2(0f, 0.25f);

    // =========================================================
    // ANIMATIONS — à remplir via l'outil Editor ou l'Inspector
    // =========================================================
    [Header("Idle (repos)")]
    public DirectionalAnimation idleSO;
    public DirectionalAnimation idleSE;
    public DirectionalAnimation idleNE;
    public DirectionalAnimation idleNO;

    [Header("Walk (marche)")]
    public DirectionalAnimation walkSO;
    public DirectionalAnimation walkSE;
    public DirectionalAnimation walkNE;
    public DirectionalAnimation walkNO;

    [Header("Sprint (5+ cases)")]
    public DirectionalAnimation sprintSO;
    public DirectionalAnimation sprintSE;
    public DirectionalAnimation sprintNE;
    public DirectionalAnimation sprintNO;

    [Header("Dégâts (coup unique, optionnel)")]
    public DirectionalAnimation hurtSO;
    public DirectionalAnimation hurtSE;
    public DirectionalAnimation hurtNE;
    public DirectionalAnimation hurtNO;

    [Header("Death (mort)")]
    public DirectionalAnimation deathSO;
    public DirectionalAnimation deathSE;
    public DirectionalAnimation deathNE;
    public DirectionalAnimation deathNO;

    // =========================================================
    // VITESSE DE MARCHE / SPRINT / COUPS
    // =========================================================
    [Header("Cadences")]
    [Tooltip("FPS pour 3–4 cases (marche normale)")]
    public float walkFpsNormal = 10f;
    [Tooltip("FPS pour 1–2 cases (marche lente)")]
    public float walkFpsSlow   = 5f;
    [Tooltip("FPS sprint (plus de 4 cases)")]
    public float sprintFps     = 12f;
    [Tooltip("FPS animation de dégâts")]
    public float hurtFps       = 12f;
    [Tooltip("FPS animation d'attaque sort (Resources)")]
    public float castFps         = 14f;

    // =========================================================
    // REMAPPAGE DES DIRECTIONS
    // =========================================================
    [Header("Remappage des directions")]
    [Tooltip("Quand le perso se déplace vers SO (bas-gauche), jouer le sprite →")]
    public DirectionSlot mapSO = DirectionSlot.SO;
    [Tooltip("Quand le perso se déplace vers SE (bas-droite), jouer le sprite →")]
    public DirectionSlot mapSE = DirectionSlot.SE;
    [Tooltip("Quand le perso se déplace vers NE (haut-droite), jouer le sprite →")]
    public DirectionSlot mapNE = DirectionSlot.NE;
    [Tooltip("Quand le perso se déplace vers NO (haut-gauche), jouer le sprite →")]
    public DirectionSlot mapNO = DirectionSlot.NO;

    // =========================================================
    // INTERNES
    // =========================================================
    private TacticalCharacter character;
    private SpriteRenderer    spriteRenderer;
    private Transform         spriteRoot;
    private Coroutine         currentAnim;
    private Coroutine         hurtCoroutine;
    private int               lastMoveSteps = 3;
    private int               animLockDepth;

    public bool IsAnimLocked => animLockDepth > 0;

    // =========================================================
    // CYCLE DE VIE
    // =========================================================
    void Awake()
    {
        character      = GetComponent<TacticalCharacter>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        SetupSpriteRoot();
        EnsureSprintHurtFromResources();

        character.OnStateChanged  += HandleStateChanged;
        character.OnFacingChanged += HandleFacingChanged;
        character.OnMoveStarted   += HandleMoveStarted;

        PlayForCurrentState();
    }

    void OnDestroy()
    {
        if (character == null) return;
        character.OnStateChanged  -= HandleStateChanged;
        character.OnFacingChanged -= HandleFacingChanged;
        character.OnMoveStarted   -= HandleMoveStarted;
    }

    // =========================================================
    // RESSOURCES (GIF extraits)
    // =========================================================
    void EnsureSprintHurtFromResources()
    {
        if (!Application.isPlaying) return;

        if (!HasFrames(sprintSO) && !HasFrames(sprintSE) && !HasFrames(sprintNE) && !HasFrames(sprintNO))
        {
            sprintSO = LoadLoopFromResources("sprint_SO", sprintFps);
            sprintSE = LoadLoopFromResources("sprint_SE", sprintFps);
            sprintNE = LoadLoopFromResources("sprint_NE", sprintFps);
            sprintNO = LoadLoopFromResources("sprint_NO", sprintFps);
        }

        if (!HasFrames(hurtSO) && !HasFrames(hurtSE) && !HasFrames(hurtNE) && !HasFrames(hurtNO))
        {
            hurtSO = LoadOnceFromResources("taking_damage_SO", hurtForFps());
            hurtSE = LoadOnceFromResources("taking_damage_SE", hurtForFps());
            hurtNE = LoadOnceFromResources("taking_damage_NE", hurtForFps());
            hurtNO = LoadOnceFromResources("taking_damage_NO", hurtForFps());
        }

        float hurtForFps() => hurtFps > 0 ? hurtFps : 12f;
    }

    static bool HasFrames(DirectionalAnimation a) =>
        a != null && a.frames != null && a.frames.Length > 0;

    static DirectionalAnimation LoadLoopFromResources(string folderName, float fps)
    {
        var frames = CombatAnimationResources.LoadAllSpritesSorted($"CombatAnimations/Frames/{folderName}");
        if (frames.Length == 0) return new DirectionalAnimation { frames = System.Array.Empty<Sprite>(), fps = fps, loop = true };
        return new DirectionalAnimation { frames = frames, fps = Mathf.Max(1f, fps), loop = true };
    }

    static DirectionalAnimation LoadOnceFromResources(string folderName, float fps)
    {
        var frames = CombatAnimationResources.LoadAllSpritesSorted($"CombatAnimations/Frames/{folderName}");
        if (frames.Length == 0) return new DirectionalAnimation { frames = System.Array.Empty<Sprite>(), fps = fps, loop = false };
        return new DirectionalAnimation { frames = frames, fps = Mathf.Max(1f, fps), loop = false };
    }

    bool HasAnySprintFrames() =>
        HasFrames(sprintSO) || HasFrames(sprintSE) || HasFrames(sprintNE) || HasFrames(sprintNO);

    // =========================================================
    // SETUP DU CHILD SPRITE
    // =========================================================
    /// <summary>
    /// Crée un child "SpriteVisual" qui porte le SpriteRenderer visuel.
    /// Le parent garde la position logique de grille ; le child gère
    /// l'échelle et l'offset purement visuels.
    /// </summary>
    private void SetupSpriteRoot()
    {
        var original = GetComponent<SpriteRenderer>();

        var spriteRootGO = new GameObject("SpriteVisual");
        spriteRoot = spriteRootGO.transform;
        spriteRoot.SetParent(transform, worldPositionStays: false);
        spriteRoot.localPosition = new Vector3(visualOffset.x, visualOffset.y, 0f);
        spriteRoot.localScale    = new Vector3(spriteScale, spriteScale, 1f);

        var childRenderer = spriteRootGO.AddComponent<SpriteRenderer>();
        childRenderer.sprite           = original.sprite;
        childRenderer.color            = original.color;
        childRenderer.material         = original.material;
        childRenderer.sortingLayerID   = original.sortingLayerID;
        childRenderer.sortingLayerName = original.sortingLayerName;
        childRenderer.sortingOrder     = original.sortingOrder;

        original.enabled = false;
        spriteRenderer = childRenderer;

        character.spriteRenderer = childRenderer;
    }

    // =========================================================
    // CALLBACKS
    // =========================================================
    private void HandleStateChanged(CharacterState _)
    {
        if (IsAnimLocked) return;
        PlayForCurrentState();
    }

    private void HandleFacingChanged(FacingDirection _)
    {
        if (IsAnimLocked) return;
        if (character.State != CharacterState.Dead)
            PlayForCurrentState();
    }

    private void HandleMoveStarted(int steps)
    {
        lastMoveSteps = steps;
    }

    // =========================================================
    // COUPS / DÉGÂTS (one-shot)
    // =========================================================
    /// <summary>Joue la séquence une fois (sort, sync avec délai de résolution).</summary>
    public IEnumerator PlayOneShotDirectionalRoutine(DirectionalAnimation clip)
    {
        if (clip == null || clip.frames == null || clip.frames.Length == 0) yield break;

        animLockDepth++;
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = null;

        float fps   = clip.fps > 0f ? clip.fps : 12f;
        float delay = 1f / Mathf.Max(0.1f, fps);
        Sprite[] frames = clip.frames;

        if (frames.Length == 1)
        {
            spriteRenderer.sprite = frames[0];
            yield return new WaitForSeconds(delay);
        }
        else
        {
            for (int frame = 0; frame < frames.Length; frame++)
            {
                spriteRenderer.sprite = frames[frame];
                yield return new WaitForSeconds(delay);
            }
        }

        animLockDepth--;
        PlayForCurrentState();
    }

    public void NotifyDamaged()
    {
        if (!isActiveAndEnabled || character == null) return;
        if (character.State == CharacterState.Dead) return;
        if (character.State == CharacterState.Casting) return;

        if (hurtCoroutine != null) StopCoroutine(hurtCoroutine);
        hurtCoroutine = StartCoroutine(HurtRoutine());
    }

    IEnumerator HurtRoutine()
    {
        var hurt = PickDirection(hurtSO, hurtSE, hurtNE, hurtNO);
        if (!HasFrames(hurt))
        {
            hurtCoroutine = null;
            yield break;
        }
        yield return StartCoroutine(PlayOneShotDirectionalRoutine(hurt));
        hurtCoroutine = null;
    }

    // =========================================================
    // LOGIQUE PRINCIPALE
    // =========================================================
    private void PlayForCurrentState()
    {
        if (IsAnimLocked) return;

        var anim = ResolveAnimation();
        if (anim == null || anim.frames == null || anim.frames.Length == 0) return;

        float fpsOverride = -1f;
        if (character.State == CharacterState.Moving)
            fpsOverride = ResolveMoveFps();

        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(RunAnimation(anim, fpsOverride));
    }

    float ResolveMoveFps()
    {
        bool sprinting = lastMoveSteps > 4 && HasAnySprintFrames();
        if (sprinting) return sprintFps > 0f ? sprintFps : walkFpsNormal;
        if (lastMoveSteps <= 2) return walkFpsSlow;
        return walkFpsNormal;
    }

    private DirectionalAnimation ResolveAnimation()
    {
        switch (character.State)
        {
            case CharacterState.Idle:
            case CharacterState.Casting:
                return PickDirection(idleSO, idleSE, idleNE, idleNO);

            case CharacterState.Moving:
                if (lastMoveSteps > 4 && HasAnySprintFrames())
                    return PickDirection(sprintSO, sprintSE, sprintNE, sprintNO);
                return PickDirection(walkSO, walkSE, walkNE, walkNO);

            case CharacterState.Dead:
                return PickDirection(deathSO, deathSE, deathNE, deathNO);

            default:
                return null;
        }
    }

    // =========================================================
    // REMAPPAGE DE DIRECTION
    // =========================================================
    private DirectionalAnimation PickDirection(
        DirectionalAnimation so, DirectionalAnimation se,
        DirectionalAnimation ne, DirectionalAnimation no)
    {
        DirectionSlot slot;
        switch (character.Facing)
        {
            case FacingDirection.SouthWest: slot = mapSO; break;
            case FacingDirection.SouthEast: slot = mapSE; break;
            case FacingDirection.NorthEast: slot = mapNE; break;
            case FacingDirection.NorthWest: slot = mapNO; break;
            default:                        slot = mapSE; break;
        }

        switch (slot)
        {
            case DirectionSlot.SO: return so;
            case DirectionSlot.SE: return se;
            case DirectionSlot.NE: return ne;
            case DirectionSlot.NO: return no;
            default:               return se;
        }
    }

    // =========================================================
    // COROUTINE D'ANIMATION
    // =========================================================
    private IEnumerator RunAnimation(DirectionalAnimation anim, float fpsOverride = -1f)
    {
        Sprite[] frames = anim.frames;
        if (frames == null || frames.Length == 0) yield break;

        if (frames.Length == 1)
        {
            spriteRenderer.sprite = frames[0];
            yield break;
        }

        float fps   = fpsOverride > 0f ? fpsOverride : anim.fps;
        float delay = 1f / Mathf.Max(0.1f, fps);
        int   frame = 0;

        while (true)
        {
            spriteRenderer.sprite = frames[frame];
            frame++;

            if (frame >= frames.Length)
            {
                if (!anim.loop) yield break;
                frame = 0;
            }

            yield return new WaitForSeconds(delay);
        }
    }
}
