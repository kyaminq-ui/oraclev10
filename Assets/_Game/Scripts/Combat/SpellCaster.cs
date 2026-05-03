using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TacticalCharacter))]
public class SpellCaster : MonoBehaviour
{
    private TacticalCharacter character;
    private SpellData selectedSpell;
    private List<Cell> validTargetCells = new List<Cell>();
    private List<Cell> previewCells = new List<Cell>();

    public SpellData SelectedSpell => selectedSpell;
    public bool HasSpellSelected => selectedSpell != null;
    public IReadOnlyList<Cell> ValidTargetCells => validTargetCells;
    public bool IsValidTarget(Cell cell)  => validTargetCells.Contains(cell);
    public bool IsInAoEPreview(Cell cell) => previewCells.Contains(cell);
    public IReadOnlyList<Cell> AoEPreviewCells => previewCells;

    // SpellCaster local dont un sort est actuellement sélectionné (null sinon)
    public static SpellCaster Active { get; private set; }

    void Awake() => character = GetComponent<TacticalCharacter>();

    // =========================================================
    // SÉLECTION D'UN SORT
    // =========================================================
    public bool SelectSpell(SpellData spell)
    {
        CancelSpell();
        if (!character.CanCastSpell(spell)) return false;

        selectedSpell = spell;

        if (spell.zoneType == ZoneType.Self)
        {
            validTargetCells = new List<Cell> { character.CurrentCell };
            GridManager.Instance.HighlightCells(validTargetCells, HighlightType.Attack);
        }
        else if (spell.zoneType == ZoneType.Boost)
        {
            validTargetCells = new List<Cell> { character.CurrentCell };
            var zone = AoECalculator.GetAffectedCells(
                ZoneType.Boost, character.CurrentCell, character.CurrentCell, spell.aoeRadius);
            GridManager.Instance.HighlightCells(zone, HighlightType.AoE);
        }
        else
        {
            validTargetCells = ComputeValidTargetCells(spell);
            GridManager.Instance.HighlightCells(validTargetCells, HighlightType.Attack);
        }
        Active = this;
        return true;
    }

    public void CancelSpell()
    {
        if (selectedSpell == null) return;
        GridManager.Instance.ClearAllHighlights();
        previewCells.Clear();
        validTargetCells.Clear();
        selectedSpell = null;
        if (Active == this) Active = null;
    }

    // =========================================================
    // PREVIEW AoE AU SURVOL
    // =========================================================
    public void PreviewAoE(Cell hoveredCell)
    {
        if (selectedSpell == null || !validTargetCells.Contains(hoveredCell)) return;
        if (previewCells.Count > 0 && previewCells[0] == hoveredCell) return;

        GridManager.Instance.ClearAllHighlights();
        GridManager.Instance.HighlightCells(validTargetCells, HighlightType.Attack);

        previewCells = AoECalculator.GetAffectedCells(
            selectedSpell.zoneType,
            character.CurrentCell,
            hoveredCell,
            selectedSpell.aoeRadius
        );
        GridManager.Instance.HighlightCells(previewCells, HighlightType.AoE);
    }

    public void ClearPreview()
    {
        if (previewCells.Count == 0) return;
        GridManager.Instance.ClearAllHighlights();
        GridManager.Instance.HighlightCells(validTargetCells, HighlightType.Attack);
        previewCells.Clear();
    }

    // =========================================================
    // LANCER LE SORT
    // =========================================================
    public bool TryCast(Cell targetCell)
    {
        if (selectedSpell == null) return false;
        if (!validTargetCells.Contains(targetCell)) return false;

        StartCoroutine(CastRoutine(targetCell));
        return true;
    }

    private IEnumerator CastRoutine(Cell targetCell)
    {
        SpellData spell = selectedSpell;
        List<Cell> affectedCells = AoECalculator.GetAffectedCells(
            spell.zoneType,
            character.CurrentCell,
            targetCell,
            spell.aoeRadius
        );

        character.FaceTowardCell(targetCell);

        var playerAnim = character.GetComponent<PlayerAnimator>();
        DirectionalAnimation castClip = null;
        if (playerAnim != null)
        {
            string animBase = SpellCastAnimationCatalog.ResolveAnimBase(spell);
            float cfps = playerAnim.castFps > 0f ? playerAnim.castFps : 14f;
            castClip = CombatAnimationResources.LoadSpellCastClip(animBase, character.Facing, cfps);
        }

        CancelSpell();

        character.SpendPA(spell.paCost);
        character.StartCooldown(spell);
        character.SetCastingState(true);

        if (playerAnim != null && castClip != null && castClip.frames != null && castClip.frames.Length > 0)
            yield return StartCoroutine(playerAnim.PlayOneShotDirectionalRoutine(castClip));
        else
        {
            var legacyVfx = character.GetComponent<SpellAnimator>();
            float t = legacyVfx != null ? Mathf.Max(0f, legacyVfx.resolvedDelaySeconds) : 0f;
            if (t > 0f)
                yield return new WaitForSeconds(t);
        }

        SpellResolver.Resolve(spell, character, affectedCells, targetCell);

        character.SetCastingState(false);
        if (!SpellGrantsInvisible(spell))
            character.RemoveStatusEffect(StatusEffectType.Invisible);
    }

    static bool SpellGrantsInvisible(SpellData spell)
    {
        if (spell?.effects == null) return false;
        foreach (var e in spell.effects)
            if (e.type == SpellEffectType.Invisible)
                return true;
        return false;
    }

    // =========================================================
    // CALCUL DES CASES VALIDES
    // =========================================================

    /// <summary>
    /// Vérifie portée / PA / cooldown / ligne de vue sans modifier la sélection (validation MasterClient réseau).
    /// </summary>
    public bool WouldAcceptCast(SpellData spell, Cell targetCell)
    {
        if (spell == null || targetCell == null) return false;
        if (!character.CanCastSpell(spell)) return false;
        var valid = ComputeValidTargetCells(spell);
        return valid.Contains(targetCell);
    }

    private List<Cell> ComputeValidTargetCells(SpellData spell)
    {
        var cells = new List<Cell>();
        Cell origin = character.CurrentCell;
        if (origin == null) return cells;

        int effectiveMax = spell.rangeMax + character.GetBonusRange();

        if (spell.isMeleeOnly)
        {
            var result = new List<Cell>();
            int effMax = spell.rangeMax + character.GetBonusRange();

            if (spell.rangeMin <= 0)
                result.Add(origin);

            var neighbors = GridManager.Instance.GetNeighbors(origin);
            foreach (Cell n in neighbors)
            {
                if (n == null || !n.IsWalkable) continue;
                const int dist = 1;
                if (dist < spell.rangeMin || dist > effMax) continue;

                if (spell.requiresLineOfSight && !spell.ignoresLineOfSight)
                    if (!AoECalculator.HasLineOfSight(origin, n)) continue;

                result.Add(n);
            }
            return result;
        }

        for (int x = origin.GridX - effectiveMax; x <= origin.GridX + effectiveMax; x++)
            for (int y = origin.GridY - effectiveMax; y <= origin.GridY + effectiveMax; y++)
            {
                int dist = Mathf.Abs(x - origin.GridX) + Mathf.Abs(y - origin.GridY);
                if (dist < spell.rangeMin || dist > effectiveMax) continue;

                Cell cell = GridManager.Instance.GetCell(x, y);
                if (cell == null) continue;

                // Cases obstacle : pas de cible ni de placement de zone (sauf cases déjà filtrées par FreeCell / LOS)
                if (!cell.IsWalkable && !cell.IsOccupied)
                    continue;

                if (spell.zoneType == ZoneType.FreeCell && (cell.IsOccupied || !cell.IsWalkable)) continue;

                if (spell.zoneType == ZoneType.Boost)
                {
                    if (cell != origin) continue;
                }

                if (spell.requiresLineOfSight && !spell.ignoresLineOfSight)
                    if (!AoECalculator.HasLineOfSight(origin, cell)) continue;

                cells.Add(cell);
            }

        return cells;
    }

    // =========================================================
    // PRÉVISUALISATION (sans modifier l'état)
    // =========================================================

    /// <summary>
    /// Calcule les dégâts/soins de base que <paramref name="spell"/> infligerait à <paramref name="target"/>.
    /// Ne tient pas compte des passifs (estimation indicative).
    /// </summary>
    public static (int dmg, int heal) ComputePreview(SpellData spell, TacticalCharacter caster, TacticalCharacter target)
    {
        int dmg = 0, heal = 0;
        if (spell == null || caster == null || target == null) return (0, 0);

        foreach (SpellEffect effect in spell.effects)
        {
            switch (effect.type)
            {
                case SpellEffectType.Damage:
                    if (spell.excludeCasterFromHarmfulAoE && ReferenceEquals(target, caster))
                        break;
                    int d = effect.value;
                    if (effect.condition == SpellCondition.TargetHPBelow)
                    {
                        if (target.stats == null) continue;
                        int th = Mathf.Max(1, Mathf.RoundToInt(target.stats.maxHP * (effect.conditionThreshold / 100f)));
                        if (target.CurrentHP < th) d = Mathf.RoundToInt(d * effect.conditionMultiplier);
                        else continue;
                    }
                    else if (effect.condition == SpellCondition.FromBehind)
                    {
                        if (!SpellResolver.IsAttackerBehindTarget(caster, target)) continue;
                    }
                    else if (effect.condition == SpellCondition.TargetHasNoPM && target.CurrentPM > 0)
                        continue;
                    else if (effect.condition == SpellCondition.AtMaxCastRange)
                    {
                        int effMax = spell.rangeMax + caster.GetBonusRange();
                        var tc = target.CurrentCell;
                        var cc = caster.CurrentCell;
                        if (cc == null || tc == null) continue;
                        int dist = Mathf.Abs(cc.GridX - tc.GridX) + Mathf.Abs(cc.GridY - tc.GridY);
                        if (dist != effMax) continue;
                    }
                    else if (effect.condition == SpellCondition.TargetAdjacentToObstacle)
                    {
                        if (target.CurrentCell == null || !SpellResolver.IsCellAdjacentToObstacle(target.CurrentCell)) continue;
                    }
                    else if (effect.condition == SpellCondition.ThirdMeleeSpellThisTurn)
                    {
                        if (!spell.isMeleeOnly || caster.MeleeSpellsCastThisTurn < 2) continue;
                    }
                    else if (effect.condition == SpellCondition.AtExactCastDistance)
                    {
                        var tc = target.CurrentCell;
                        var cc = caster.CurrentCell;
                        if (cc == null || tc == null) continue;
                        int dist = Mathf.Abs(cc.GridX - tc.GridX) + Mathf.Abs(cc.GridY - tc.GridY);
                        if (dist != effect.conditionThreshold) continue;
                    }
                    dmg += d;
                    break;
                case SpellEffectType.ChipDamageAroundCaster:
                {
                    Cell cc = caster.CurrentCell, tc = target.CurrentCell;
                    if (cc != null && tc != null && tc != cc)
                    {
                        int r = effect.duration > 0 ? effect.duration : 1;
                        int dist = Mathf.Abs(cc.GridX - tc.GridX) + Mathf.Abs(cc.GridY - tc.GridY);
                        if (dist <= r)
                            dmg += effect.value;
                    }
                    break;
                }
                case SpellEffectType.Heal:
                    heal += effect.value;
                    break;
            }
        }
        return (dmg, heal);
    }
}
