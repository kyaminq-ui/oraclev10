using UnityEngine;

/// <summary>
/// Résout le préfixe de dossiers d’animation de cast pour un <see cref="SpellData"/>.
/// Utilise <see cref="SpellCombatAnimationStems"/> (attack_ / survival_ / tactic_).
/// </summary>
public static class SpellCastAnimationCatalog
{
    public const string DefaultCastAnimBase = "attack_eclat_arcanique";

    public static string ResolveAnimBase(SpellData spell)
    {
        if (spell == null) return DefaultCastAnimBase;
        if (!string.IsNullOrWhiteSpace(spell.castAnimationBaseId))
            return spell.castAnimationBaseId.Trim();
        if (!string.IsNullOrEmpty(spell.spellName) &&
            SpellCombatAnimationStems.TryGetStem(spell.spellName, out string stem))
            return stem;
        return DefaultCastAnimBase;
    }
}
