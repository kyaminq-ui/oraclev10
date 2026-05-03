using System;
using UnityEngine;

public enum StatusEffectType
{
    Bleed,               // Dégâts par tour (Hémorragie)
    /// <summary>Interdit uniquement le sort Attaque référencé par <see cref="StatusEffect.bannedSpell"/> (Silence).</summary>
    Silence,
    DamageReduction,     // Réduit dégâts reçus par hit (Peau d'Écorce)
    Thorns,              // Renvoie dégâts à l'attaquant (Épine)
    Invisible,           // Pas ciblable, se brise au 1er sort (Invisibilité)
    Shield,              // Absorbe X dégâts (Rempart, Balise)
    BonusPANextTurn,     // +X PA au début du prochain tour (effet générique si utilisé)
    GravityDebuff,       // Pas de téléportation/dash (Gravité)
    LastBreath,          // Survit à 1 PV une fois (Second Souffle)
    ReducedAttack,       // Prochaine attaque sortante : dégâts réduits (Vent de Panique ; valeur = % ou 0 → 20 %)
    ReducedPM,           // PM réduit (Patate de forain)
}

[Serializable]
public class StatusEffect
{
    public StatusEffectType type;
    public int value;
    public int turnsRemaining;
    public bool isDebuff;
    /// <summary>Pour <see cref="StatusEffectType.Silence"/> : sort <see cref="SpellDeckCategory.Attack"/> tiré du deck adverse.</summary>
    public SpellData bannedSpell;

    public StatusEffect(StatusEffectType type, int value, int duration, bool isDebuff = false, SpellData bannedSpell = null)
    {
        this.type = type;
        this.value = value;
        turnsRemaining = duration;
        this.isDebuff = isDebuff;
        this.bannedSpell = bannedSpell;
    }

    public bool IsExpired => turnsRemaining <= 0;

    public void Tick() => turnsRemaining = Math.Max(0, turnsRemaining - 1);
}
