using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum CharacterState { Idle, Moving, Casting, Dead }

public enum FacingDirection { SouthEast, SouthWest, NorthEast, NorthWest }

[DefaultExecutionOrder(400)]
public class TacticalCharacter : MonoBehaviour
{
    // =========================================================
    // EVENTS
    // =========================================================
    public event System.Action<int, int> OnPAChanged;
    public event System.Action<int, int> OnPMChanged;
    public event System.Action<int, int> OnHPChanged;
    public event System.Action<CharacterState> OnStateChanged;
    public event System.Action<FacingDirection> OnFacingChanged;
    public event System.Action<int>             OnMoveStarted;   // nb de cases parcourues
    public event System.Action OnDeath;
    public event System.Action<SpellData> OnSpellCast;
    public event System.Action OnTurnStart_Passive;
    public event System.Action OnTurnEnd_Passive;

    // =========================================================
    // CONFIGURATION
    // =========================================================
    [Header("Données")]
    public CharacterStats stats;
    public DeckData deck;

    [Tooltip("Rempli au runtime par CombatInitializer si un SpellDeckPool est utilisé ; sinon lecture de deck.Spells.")]
    [SerializeField] List<SpellData> runtimeSpells = new List<SpellData>();

    [Header("Visuel")]
    public SpriteRenderer spriteRenderer;

    [Header("Vitesse de déplacement")]
    [Tooltip("Vitesse pour 3–4 cases (marche normale)")]
    [SerializeField] private float moveSpeedNormal = 6f;
    [Tooltip("Vitesse pour 1–2 cases (marche lente)")]
    [SerializeField] private float moveSpeedSlow   = 3.5f;
    [Tooltip("Vitesse pour plus de 4 cases (sprint)")]
    [SerializeField] private float moveSpeedSprint = 9f;

    // =========================================================
    // ÉTAT EN COMBAT
    // =========================================================
    private int currentHP;
    private int currentPA;
    private int currentPM;
    private int nextTurnBonusPA;
    private int nextTurnBonusPM;
    private int bonusRange;
    private CharacterState _state = CharacterState.Idle;
    private Cell currentCell;
    private FacingDirection facing = FacingDirection.SouthEast;
    private int meleeSpellsCastThisTurn;

    private Dictionary<SpellData, int> spellCooldowns = new Dictionary<SpellData, int>();
    private List<StatusEffect> activeEffects = new List<StatusEffect>();

    struct InvisibilityVisualSnapshot
    {
        public Color Color;
        public bool Enabled;
    }

    readonly Dictionary<SpriteRenderer, InvisibilityVisualSnapshot> _invisibilityVisualRestore = new Dictionary<SpriteRenderer, InvisibilityVisualSnapshot>();
    bool _invisibilityVisualActive;

    [SerializeField, Tooltip("Alpha affichée pour le contrôleur local pendant Invisible (fantôme).")]
    float _localInvisibleSpriteAlpha = 0.5f;

    static readonly SpellData[] NoSpellsFallback = new SpellData[0];

    // =========================================================
    // PROPRIÉTÉS PUBLIQUES
    // =========================================================
    public CharacterState State
    {
        get => _state;
        private set { if (_state == value) return; _state = value; OnStateChanged?.Invoke(_state); }
    }
    public Cell CurrentCell     => currentCell;
    public int CurrentHP          => currentHP;
    public int CurrentPA          => currentPA;
    public int CurrentPM          => currentPM;
    public int NextTurnBonusPA    => nextTurnBonusPA;
    public int NextTurnBonusPM    => nextTurnBonusPM;
    public FacingDirection Facing => facing;
    public bool IsAlive           => currentHP > 0;
    public int ShieldHP           => GetStatusEffectValue(StatusEffectType.Shield);
    public IReadOnlyList<StatusEffect> ActiveStatusEffects => activeEffects;
    /// <summary>Nombre de sorts au corps-à-corps déjà résolus ce tour (avant le sort en cours).</summary>
    public int MeleeSpellsCastThisTurn => meleeSpellsCastThisTurn;

    /// <summary>Sorts utilisés pour l’UI / l’IA / le réseau : deck runtime si défini, sinon asset <see cref="deck"/>.</summary>
    public IReadOnlyList<SpellData> ActiveSpells =>
        runtimeSpells.Count > 0 ? runtimeSpells : (deck != null ? deck.Spells : NoSpellsFallback);

    public void ClearRuntimeSpellDeck() => runtimeSpells.Clear();

    /// <summary>Jusqu’à <see cref="DeckData.MaxSpells"/> entrées ; ignore les null.</summary>
    public void SetRuntimeSpellDeck(IReadOnlyList<SpellData> picks)
    {
        runtimeSpells.Clear();
        if (picks == null) return;
        for (int i = 0; i < picks.Count && runtimeSpells.Count < DeckData.MaxSpells; i++)
            if (picks[i] != null) runtimeSpells.Add(picks[i]);
    }

    /// <summary>True si ce personnage est celui contrôlé par le client local (hôte = player, invité = opponent en ligne).</summary>
    public bool IsLocallyControlledCharacter()
    {
        var bridge = OracleCombatNetBridge.Instance;
        if (bridge != null)
            return bridge.GetLocalControlledCharacter() == this;
        var ci = CombatInitializer.Instance;
        return ci != null && ci.player == this;
    }

    // =========================================================
    // INITIALISATION
    // =========================================================
    public void Initialize(Cell startCell)
    {
        if (stats == null) { Debug.LogError($"TacticalCharacter '{name}' : stats manquants !"); return; }
        currentHP = stats.maxHP;
        currentPA = stats.maxPA;
        currentPM = stats.maxPM;
        currentCell = startCell;
        startCell.SetOccupant(gameObject);
        transform.position = GridManager.Instance != null
            ? GridManager.Instance.GridToWorldFace(startCell.GridX, startCell.GridY)
            : startCell.WorldPosition;
        UpdateSortingOrder();
        OnHPChanged?.Invoke(currentHP, stats.maxHP);
    }

    // =========================================================
    // GESTION DU TOUR
    // =========================================================
    public void OnTurnStart()
    {
        if (!IsAlive) return;
        currentPA = stats.maxPA + nextTurnBonusPA;
        nextTurnBonusPA = 0;
        currentPM = stats.maxPM + nextTurnBonusPM;
        nextTurnBonusPM = 0;
        meleeSpellsCastThisTurn = 0;
        OnPAChanged?.Invoke(currentPA, stats.maxPA);
        OnPMChanged?.Invoke(currentPM, stats.maxPM);
        TickCooldowns();
        OnTurnStart_Passive?.Invoke();
        ProcessTurnStartEffects();
    }

    public void OnTurnEnd()
    {
        bonusRange = 0;
        OnTurnEnd_Passive?.Invoke();
        ProcessTurnEndEffects();
    }

    public void NotifySpellCast(SpellData spell)
    {
        if (spell != null && spell.isMeleeOnly)
            meleeSpellsCastThisTurn++;
        OnSpellCast?.Invoke(spell);
    }

    private void TickCooldowns()
    {
        var keys = new List<SpellData>(spellCooldowns.Keys);
        foreach (var spell in keys)
            if (--spellCooldowns[spell] <= 0) spellCooldowns.Remove(spell);
    }

    // =========================================================
    // EFFETS DE STATUT
    // =========================================================
    public void AddStatusEffect(StatusEffect effect)
    {
        if (effect.type == StatusEffectType.Bleed)
        {
            // 2e application : upgrade saignement → 35/t (plan balance Hémorragie)
            if (HasStatusEffect(StatusEffectType.Bleed))
            {
                RemoveStatusEffect(StatusEffectType.Bleed);
                activeEffects.Add(new StatusEffect(StatusEffectType.Bleed, 35, effect.turnsRemaining, true));
                return;
            }
            activeEffects.Add(effect);
            return;
        }
        RemoveStatusEffect(effect.type);
        activeEffects.Add(effect);
    }

    public void RemoveStatusEffect(StatusEffectType type)
    {
        activeEffects.RemoveAll(e => e.type == type);
    }

    public bool HasStatusEffect(StatusEffectType type) =>
        activeEffects.Exists(e => e.type == type);

    public bool HasAnyDebuff() =>
        activeEffects.Exists(e => e.isDebuff);

    public int GetStatusEffectValue(StatusEffectType type)
    {
        int total = 0;
        foreach (var e in activeEffects)
            if (e.type == type) total += e.value;
        return total;
    }

    public void ClearAllDebuffs() =>
        activeEffects.RemoveAll(e => e.isDebuff);

    public void RemoveAllBleedEffects() =>
        activeEffects.RemoveAll(e => e.type == StatusEffectType.Bleed);

    /// <summary>Consume la réduction de première frappe (Vent de Panique). Valeur du statut = % (ex. 20) ; 0 = 20 %.</summary>
    public void ApplyReducedAttackIfAny(ref int outgoingDamage)
    {
        StatusEffect hit = null;
        foreach (var e in activeEffects)
            if (e.type == StatusEffectType.ReducedAttack) { hit = e; break; }
        if (hit == null) return;
        float pct = hit.value > 0 ? hit.value / 100f : 0.20f;
        outgoingDamage = Mathf.RoundToInt(outgoingDamage * (1f - pct));
        RemoveStatusEffect(StatusEffectType.ReducedAttack);
    }

    private void ProcessTurnStartEffects()
    {
        var toRemove = new List<StatusEffect>();
        foreach (var effect in activeEffects)
        {
            if (effect.type == StatusEffectType.Bleed)
            {
                CombatLog.Append($"<b>{name}</b> saigne <color=#FF6B6B>-{effect.value}</color>");
                TakeDamage(effect.value, null);
            }

            // Silence : décompte en fin de tour pour que « N tours » = N fois jouer avec le ban actif
            if (effect.type == StatusEffectType.Silence)
                continue;

            effect.Tick();
            if (effect.IsExpired) toRemove.Add(effect);
        }
        foreach (var e in toRemove) activeEffects.Remove(e);
    }

    private void ProcessTurnEndEffects()
    {
        var toRemove = new List<StatusEffect>();
        foreach (var effect in activeEffects)
        {
            if (effect.type == StatusEffectType.DamageReduction ||
                effect.type == StatusEffectType.Thorns ||
                effect.type == StatusEffectType.Shield ||
                effect.type == StatusEffectType.ReducedAttack ||
                effect.type == StatusEffectType.Silence)
            {
                effect.Tick();
                if (effect.IsExpired) toRemove.Add(effect);
            }
        }
        foreach (var e in toRemove) activeEffects.Remove(e);
    }

    // =========================================================
    // BONUS DE RESSOURCES
    // =========================================================
    public void AddBonusPA(int amount)
    {
        currentPA += amount;
        OnPAChanged?.Invoke(currentPA, stats.maxPA);
    }

    public void AddBonusPM(int amount)
    {
        currentPM += amount;
        OnPMChanged?.Invoke(currentPM, stats.maxPM);
    }

    public void AddNextTurnBonusPA(int amount) => nextTurnBonusPA += amount;

    public void AddNextTurnBonusPM(int amount) => nextTurnBonusPM += amount;

    public void AddBonusRange(int amount, int duration) => bonusRange += amount;

    public int GetBonusRange() => bonusRange;

    public int RemovePM(int amount)
    {
        int actual = Mathf.Min(amount, currentPM);
        currentPM -= actual;
        OnPMChanged?.Invoke(currentPM, stats.maxPM);
        return actual;
    }

    // =========================================================
    // DÉPLACEMENT
    // =========================================================
    public bool CanMoveTo(Cell target)
    {
        if (target == null || !target.IsWalkable || target.IsOccupied) return false;
        if (State != CharacterState.Idle) return false;
        var path = new Pathfinding().FindPath(currentCell, target);
        return path != null && path.Count - 1 <= currentPM;
    }

    public void MoveToCell(Cell target)
    {
        if (!CanMoveTo(target)) return;
        var path = new Pathfinding().FindPath(currentCell, target);
        if (path != null) StartCoroutine(MoveAlongPath(path));
    }

    /// <summary>
    /// Déplacement libre sans coût en PM — usage hub/exploration uniquement.
    /// Utilise le même pathfinding et les mêmes animations que le combat.
    /// </summary>
    public void MoveFree(Cell target)
    {
        if (target == null || !target.IsWalkable || target.IsOccupied) return;
        if (State != CharacterState.Idle) return;
        if (currentCell == null) return;
        var path = new Pathfinding().FindPath(currentCell, target);
        if (path != null && path.Count > 1)
            StartCoroutine(MoveAlongPathFree(path));
    }

    private IEnumerator MoveAlongPathFree(List<Cell> path)
    {
        int   steps = path.Count - 1;
        float speed = ResolvePathSpeed(steps);

        OnMoveStarted?.Invoke(steps);
        State = CharacterState.Moving;

        for (int i = 1; i < path.Count; i++)
        {
            Cell next = path[i];
            UpdateFacing(currentCell, next);
            currentCell.ClearOccupant();

            Vector3 start    = transform.position;
            Vector3 end      = GridManager.Instance != null
                ? GridManager.Instance.GridToWorldFace(next.GridX, next.GridY)
                : next.WorldPosition;
            float elapsed = 0f, duration = 1f / speed;

            while (elapsed < duration)
            {
                elapsed            += Time.deltaTime;
                transform.position  = Vector3.Lerp(start, end, elapsed / duration);
                UpdateSortingOrder();
                yield return null;
            }
            transform.position = end;
            currentCell        = next;
            currentCell.SetOccupant(gameObject);
            UpdateSortingOrder();
        }

        State = CharacterState.Idle;
    }

    public void ForceSetCell(Cell cell)
    {
        currentCell = cell;
        UpdateSortingOrder();
    }

    /// <summary>
    /// Applique l'état visuel reçu du réseau (Hub uniquement).
    /// Ne modifie pas la grille ni les ressources — uniquement les événements visuels.
    /// </summary>
    public void ApplyNetworkVisualState(CharacterState state, FacingDirection dir)
    {
        if (_state != state)
        {
            _state = state;
            OnStateChanged?.Invoke(_state);
        }
        if (facing != dir)
        {
            facing = dir;
            OnFacingChanged?.Invoke(facing);
        }
    }

    private IEnumerator MoveAlongPath(List<Cell> path)
    {
        int steps = path.Count - 1;
        float speed = ResolvePathSpeed(steps);

        OnMoveStarted?.Invoke(steps);
        State = CharacterState.Moving;

        for (int i = 1; i < path.Count; i++)
        {
            Cell next = path[i];
            currentPM--;
            OnPMChanged?.Invoke(currentPM, stats.maxPM);
            UpdateFacing(currentCell, next);
            currentCell.ClearOccupant();

            Vector3 start = transform.position;
            Vector3 end = GridManager.Instance != null
                ? GridManager.Instance.GridToWorldFace(next.GridX, next.GridY)
                : next.WorldPosition;
            float elapsed = 0f, duration = 1f / speed;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(start, end, elapsed / duration);
                UpdateSortingOrder();
                yield return null;
            }
            transform.position = end;
            currentCell = next;
            currentCell.SetOccupant(gameObject);
            UpdateSortingOrder();
        }
        State = CharacterState.Idle;
    }

    private void UpdateFacing(Cell from, Cell to)
    {
        // Grille iso (GridManager.GridToWorld) : un pas (+1,0) ou (0,+1) n'a pas le même
        // vecteur monde. Classer par signe de Δworld pour aligner NE/SE/NO/SO avec les sprites _NE/_SE/_NO/_SO.
        Vector3 a = GridManager.Instance != null
            ? GridManager.Instance.GridToWorld(from.GridX, from.GridY)
            : from.WorldPosition;
        Vector3 b = GridManager.Instance != null
            ? GridManager.Instance.GridToWorld(to.GridX, to.GridY)
            : to.WorldPosition;
        Vector3 d = b - a;
        if (d.sqrMagnitude < 1e-10f) return;

        FacingDirection dir;
        if      (d.x >= 0f && d.y >= 0f) dir = FacingDirection.NorthEast;
        else if (d.x <  0f && d.y >= 0f) dir = FacingDirection.NorthWest;
        else if (d.x >= 0f && d.y <  0f) dir = FacingDirection.SouthEast;
        else                               dir = FacingDirection.SouthWest;
        SetFacing(dir);
    }

    private void SetFacing(FacingDirection dir)
    {
        if (facing == dir) return;
        facing = dir;
        OnFacingChanged?.Invoke(dir);
    }

    /// <summary>Oriente le personnage sans déplacement (ex. idle au placement).</summary>
    public void SetFacingDirection(FacingDirection dir) => SetFacing(dir);

    // =========================================================
    // SORTS
    // =========================================================
    public bool CanCastSpell(SpellData spell)
    {
        if (spell == null || State != CharacterState.Idle) return false;
        if (currentPA < spell.paCost) return false;
        if (spellCooldowns.ContainsKey(spell)) return false;
        // Silence : bannit un sort Attaque aléatoire du deck (pas les Tactiques / Survie)
        if (spell.deckCategory == SpellDeckCategory.Attack)
        {
            foreach (var e in activeEffects)
            {
                if (e.type != StatusEffectType.Silence || e.bannedSpell == null) continue;
                if (e.bannedSpell == spell) return false;
            }
        }
        return true;
    }

    /// <summary>Sort Attaque actuellement banni par Silence (null si aucun ou deck sans Attaque).</summary>
    public SpellData GetSilenceBannedAttackSpell()
    {
        foreach (var e in activeEffects)
            if (e.type == StatusEffectType.Silence && e.bannedSpell != null)
                return e.bannedSpell;
        return null;
    }

    public void SpendPA(int amount)
    {
        currentPA = Mathf.Max(0, currentPA - amount);
        OnPAChanged?.Invoke(currentPA, stats.maxPA);
    }

    public void StartCooldown(SpellData spell)
    {
        if (spell.cooldown > 0) spellCooldowns[spell] = spell.cooldown;
    }

    public int GetCooldown(SpellData spell) =>
        spellCooldowns.TryGetValue(spell, out int cd) ? cd : 0;

    public void SetCastingState(bool casting) =>
        State = casting ? CharacterState.Casting : CharacterState.Idle;

    // =========================================================
    // DÉGÂTS / SOINS
    // =========================================================
    /// <returns>PV réellement retirés (après bouclier / réduction), 0 si aucun mal aux PV.</returns>
    public int TakeDamage(int amount, TacticalCharacter attacker, bool suppressFloatingText = false)
    {
        if (!IsAlive) return 0;

        // Passifs défensifs (Évasif, Bouclier Hasardeux)
        var pm = GetComponent<PassiveManager>();
        if (pm != null) amount = pm.ModifyIncomingDamage(amount, attacker);
        if (amount <= 0) return 0;

        // Réduction de dégâts (Peau d'Écorce)
        int reduction = GetStatusEffectValue(StatusEffectType.DamageReduction);
        amount = Mathf.Max(0, amount - reduction);

        // Bouclier (Rempart)
        int shield = GetStatusEffectValue(StatusEffectType.Shield);
        if (shield > 0)
        {
            int absorbed = Mathf.Min(shield, amount);
            amount -= absorbed;
            // Réduire le shield restant
            foreach (var e in activeEffects)
                if (e.type == StatusEffectType.Shield) { e.value -= absorbed; break; }
            if (GetStatusEffectValue(StatusEffectType.Shield) <= 0)
                RemoveStatusEffect(StatusEffectType.Shield);
        }

        // Renvoi de dégâts (Épine) — toujours un texte flottant dédié, hors batch des sorts
        int thorns = GetStatusEffectValue(StatusEffectType.Thorns);
        if (thorns > 0 && attacker != null)
        {
            CombatLog.Append($"<b>{name}</b> : <color=#88FF44>Épines</color> → <b>{attacker.name}</b> <color=#FF6B6B>-{thorns}</color>");
            attacker.TakeDamage(thorns, null, false);
        }

        if (amount <= 0) return 0;

        // LastBreath (Second Souffle)
        if (HasStatusEffect(StatusEffectType.LastBreath) && currentHP - amount <= 0)
        {
            currentHP = 1;
            RemoveStatusEffect(StatusEffectType.LastBreath);
            int recover = Mathf.RoundToInt(stats.maxHP * 0.12f);
            if (recover > 0)
                Heal(recover);
            OnHPChanged?.Invoke(currentHP, stats.maxHP);
            return 0;
        }

        int hpBefore = currentHP;
        currentHP = Mathf.Max(0, currentHP - amount);
        int lost = hpBefore - currentHP;

        if (!suppressFloatingText && lost > 0)
            FloatingDamageText.SpawnDamage(lost, transform.position);

        OnHPChanged?.Invoke(currentHP, stats.maxHP);
        if (currentHP <= 0)
            Die();
        else
            GetComponent<PlayerAnimator>()?.NotifyDamaged();

        return lost;
    }

    /// <summary>Oriente le personnage vers une case cible (pour sorts / visuel).</summary>
    public void FaceTowardCell(Cell targetCell)
    {
        if (currentCell == null || targetCell == null) return;
        UpdateFacing(currentCell, targetCell);
    }

    float ResolvePathSpeed(int steps)
    {
        if (steps > 4) return moveSpeedSprint > 0f ? moveSpeedSprint : moveSpeedNormal;
        if (steps >= 3) return moveSpeedNormal;
        return moveSpeedSlow;
    }

    public void Heal(int amount)
    {
        if (!IsAlive) return;
        int actualHeal = Mathf.Min(amount, stats.maxHP - currentHP);
        currentHP = Mathf.Min(stats.maxHP, currentHP + amount);
        if (actualHeal > 0)
            FloatingDamageText.SpawnHeal(actualHeal, transform.position);
        OnHPChanged?.Invoke(currentHP, stats.maxHP);
    }

    private void Die()
    {
        RestoreInvisibilityVisuals();
        State = CharacterState.Dead;
        currentCell?.ClearOccupant();
        OnDeath?.Invoke();
    }

    void LateUpdate()
    {
        UpdateInvisibilityVisuals();
    }

    void UpdateInvisibilityVisuals()
    {
        bool shouldShowInvisEffect = IsAlive && HasStatusEffect(StatusEffectType.Invisible);

        if (!shouldShowInvisEffect)
        {
            if (_invisibilityVisualActive)
                RestoreInvisibilityVisuals();
            return;
        }

        _invisibilityVisualActive = true;
        bool localGhost = IsLocallyControlledCharacter();

        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null) continue;
            if (!_invisibilityVisualRestore.ContainsKey(sr))
                _invisibilityVisualRestore[sr] = new InvisibilityVisualSnapshot { Color = sr.color, Enabled = sr.enabled };

            if (localGhost)
            {
                sr.enabled = true;
                Color c = sr.color;
                c.a = _localInvisibleSpriteAlpha;
                sr.color = c;
            }
            else
                sr.enabled = false;
        }
    }

    void RestoreInvisibilityVisuals()
    {
        foreach (var kvp in _invisibilityVisualRestore)
        {
            var sr = kvp.Key;
            if (sr == null) continue;
            sr.enabled = kvp.Value.Enabled;
            sr.color = kvp.Value.Color;
        }
        _invisibilityVisualRestore.Clear();
        _invisibilityVisualActive = false;
    }

    // =========================================================
    // SORTING ORDER ISOMÉTRIQUE
    // =========================================================
    private void UpdateSortingOrder()
    {
        if (spriteRenderer == null) return;
        var gm = GridManager.Instance;
        if (gm == null) return;

        int w         = gm.GridWidth;
        int h         = gm.GridHeight;
        int orderBias = gm.config.arenaTileSortingOrderBias;

        // Recover the logical grid position from world space.
        // characterWorldOffset shifts the visual position; subtract it to get the cell centre.
        var     cfg        = gm.config;
        Vector3 logicalPos = transform.position
                           - new Vector3(cfg.characterWorldOffset.x, cfg.characterWorldOffset.y, 0f);
        Vector2Int g  = gm.WorldToGrid(logicalPos);
        int        gx = Mathf.Clamp(g.x, 0, w - 1);
        int        gy = Mathf.Clamp(g.y, 0, h - 1);

        // Miroir exact de la formule obstacle (ArenaGenerator) : baseOrder + w*h.
        // Preuve : pour des cases adjacentes (|Δrow| = 1), le personnage est correctement
        // devant ou derrière l'obstacle selon qui est le plus proche du spectateur.
        spriteRenderer.sortingOrder = orderBias + (-(gy * w + gx)) + w * h;
    }
}
