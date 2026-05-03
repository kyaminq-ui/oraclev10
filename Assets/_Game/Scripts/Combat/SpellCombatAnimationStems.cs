using System;
using System.Collections.Generic;

/// <summary>
/// Préfixes partagés pour les animations de cast (<c>Resources/CombatAnimations/Frames/{stem}_NE</c>…)
/// et pour les noms de fichiers d’illustration carte (dossier CARTE_SORT). Doivent correspondre aux GIF
/// <c>attack_*</c>, <c>survival_*</c>, <c>tactic_*</c>.
/// </summary>
public static class SpellCombatAnimationStems
{
    /// <summary>Clé = <see cref="SpellData.spellName"/> (identique aux assets).</summary>
    public static readonly Dictionary<string, string> BySpellName =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Attaques
            ["Couteau dans le dos"] = "attack_couteau_dans_le_dos",
            ["Dague de Verre"]      = "attack_dague_de_verre",
            ["Éclat Arcanique"]     = "attack_eclat_arcanique",
            ["Exécution"]           = "attack_execution",
            ["Explosion Solaire"]   = "attack_explosion_solaire",
            ["Hémorragie"]          = "attack_hemorragie",
            ["Patate de forain"]    = "attack_patate_de_forain",
            ["Pluie de Flèches"]    = "attack_pluie_de_fleches",
            ["Ricochet"]            = "attack_ricochet",
            ["Silence"]             = "attack_silence",

            // Survie
            ["Adrénaline"]          = "survival_adrenaline",
            ["Balise Statique"]     = "survival_balise_statique",
            ["Esprit Clair"]        = "survival_esprit_clair",
            ["Invisibilité"]        = "survival_invisibilite",
            ["Méditation"]          = "survival_meditation",
            ["Pansement"]          = "survival_pansement",
            ["Peau d'Écorce"]       = "survival_peau_decorce",
            ["Purge"]               = "survival_purge",
            ["Rempart"]             = "survival_rempart",
            ["Second Souffle"]      = "survival_second_souffle",

            // Tactiques
            ["Épine"]               = "tactic_epine",
            ["Gravité"]             = "tactic_gravite",
            ["Liane de Fer"]       = "tactic_liane_de_fer",
            ["Pilier de Pierre"]   = "tactic_pilier_de_pierre",
            ["Sacrifice"]           = "tactic_sacrifice",
            ["Saut de l'Ange"]     = "tactic_saut_de_l_ange",
            ["Siphon"]              = "tactic_siphon",
            ["Surcharge"]           = "tactic_surcharge",
            ["Vent de Panique"]    = "tactic_vent_de_panique",
            ["Voltige"]             = "tactic_voltige",
        };

    public static bool TryGetStem(string spellName, out string stem) =>
        BySpellName.TryGetValue(spellName ?? string.Empty, out stem);
}
