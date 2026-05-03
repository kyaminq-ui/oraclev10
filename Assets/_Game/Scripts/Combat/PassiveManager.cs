using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(TacticalCharacter))]
public class PassiveManager : MonoBehaviour
{
    private TacticalCharacter character;
    public PassiveData activePassive;

    private bool castSpellThisTurn  = false;
    private bool masseCritiqueBonus = false;
    private int bouclierHitsReceived = 0;

    void Awake()
    {
        character = GetComponent<TacticalCharacter>();
    }

    void Start()
    {
        character.OnSpellCast += HandleSpellCast;
        character.OnTurnStart_Passive += HandleTurnStart;
        character.OnTurnEnd_Passive   += HandleTurnEnd;

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnEnd += HandleAnyTurnEnd;
    }

    void OnDestroy()
    {
        character.OnSpellCast         -= HandleSpellCast;
        character.OnTurnStart_Passive -= HandleTurnStart;
        character.OnTurnEnd_Passive   -= HandleTurnEnd;

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnEnd -= HandleAnyTurnEnd;
    }

    public void SetPassive(PassiveData passive) => activePassive = passive;

    public int ModifyOutgoingDamage(int damage, SpellData spell, TacticalCharacter target, int distance)
    {
        if (activePassive == null || character.stats == null) return damage;

        switch (activePassive.passiveType)
        {
            case PassiveType.Berserker:
            {
                int th = Mathf.Max(1, Mathf.RoundToInt(character.stats.maxHP * (activePassive.conditionThreshold / 100f)));
                if (character.CurrentHP <= th)
                    damage = Mathf.RoundToInt(damage * (1f + activePassive.effectValue));
                break;
            }

            case PassiveType.MaitreArme:
                if (spell != null && spell.isMeleeOnly)
                    damage += Mathf.Max(1, Mathf.RoundToInt(character.stats.maxHP * (activePassive.effectValue / 100f)));
                break;

            case PassiveType.Sniper:
                if (spell != null && !spell.isMeleeOnly && distance > activePassive.conditionThreshold)
                    damage += Mathf.Max(1, Mathf.RoundToInt(character.stats.maxHP * (activePassive.effectValue / 100f)));
                break;

            case PassiveType.MasseCritique:
                if (masseCritiqueBonus)
                {
                    damage = Mathf.RoundToInt(damage * (1f + activePassive.effectValue));
                    masseCritiqueBonus = false;
                }
                break;
        }
        return damage;
    }

    public int ModifyIncomingDamage(int damage, TacticalCharacter attacker)
    {
        if (activePassive == null || damage <= 0) return damage;

        switch (activePassive.passiveType)
        {
            case PassiveType.Evasif:
            {
                bool isRanged = attacker != null && !IsAdjacent(attacker);
                if (isRanged && Random.value < activePassive.procChance)
                    damage = Mathf.RoundToInt(damage * 0.5f);
                break;
            }

            case PassiveType.BouclierHasardeux:
                bouclierHitsReceived++;
                if (bouclierHitsReceived % 5 == 0)
                    return 0;
                break;
        }
        return damage;
    }

    private void HandleSpellCast(SpellData spell)
    {
        castSpellThisTurn = true;

        if (activePassive?.passiveType == PassiveType.MasseCritique)
            if (Random.value < activePassive.procChance)
                masseCritiqueBonus = true;
    }

    private void HandleTurnStart()
    {
        castSpellThisTurn = false;

        if (character.stats == null || activePassive == null) return;

        if (activePassive.passiveType == PassiveType.DernierRempart)
        {
            int th = Mathf.Max(1, Mathf.RoundToInt(character.stats.maxHP * (activePassive.conditionThreshold / 100f)));
            if (character.CurrentHP <= th)
            {
                int sh = Mathf.Max(1, Mathf.RoundToInt(character.stats.maxHP * (activePassive.effectValue / 100f)));
                character.AddStatusEffect(new StatusEffect(StatusEffectType.Shield, sh, 1));
            }
        }

        if (activePassive.passiveType == PassiveType.Vigilance && HasAdjacentEnemy())
            character.AddBonusPM(1);
    }

    private void HandleTurnEnd()
    {
        if (activePassive?.passiveType == PassiveType.Camouflage && !castSpellThisTurn)
        {
            character.AddStatusEffect(new StatusEffect(StatusEffectType.Invisible, 0, 1));
            character.AddNextTurnBonusPM(1);
            ShiftOneRandomCell();
        }
    }

    private void HandleAnyTurnEnd(TacticalCharacter whoJustPlayed)
    {
        if (activePassive?.passiveType != PassiveType.Toxicite) return;
        if (whoJustPlayed == character) return;
        if (character.stats == null) return;
        if (IsAdjacent(whoJustPlayed))
        {
            int d = Mathf.Max(1, Mathf.RoundToInt(character.stats.maxHP * (activePassive.effectValue / 100f)));
            whoJustPlayed.TakeDamage(d, character);
        }
    }

    private bool HasAdjacentEnemy()
    {
        if (character.CurrentCell == null || GridManager.Instance == null) return false;
        foreach (var n in GridManager.Instance.GetNeighbors(character.CurrentCell))
        {
            if (n == null || !n.IsOccupied) continue;
            var other = n.Occupant?.GetComponent<TacticalCharacter>();
            if (other != null && other != character && other.IsAlive) return true;
        }
        return false;
    }

    private bool IsAdjacent(TacticalCharacter other)
    {
        if (character.CurrentCell == null || other.CurrentCell == null) return false;
        int dx = Mathf.Abs(character.CurrentCell.GridX - other.CurrentCell.GridX);
        int dy = Mathf.Abs(character.CurrentCell.GridY - other.CurrentCell.GridY);
        return dx + dy == 1;
    }

    private void ShiftOneRandomCell()
    {
        if (character.CurrentCell == null) return;
        var neighbors = GridManager.Instance.GetNeighbors(character.CurrentCell);
        var free = neighbors.FindAll(c => c.IsWalkable && !c.IsOccupied);
        if (free.Count == 0) return;

        Cell dest = free[Random.Range(0, free.Count)];
        character.CurrentCell.ClearOccupant();
        dest.SetOccupant(character.gameObject);
        character.transform.position = dest.WorldPosition;
        character.ForceSetCell(dest);
    }
}
