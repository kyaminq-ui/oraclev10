using UnityEngine;
using System.Collections.Generic;

public enum ZoneType
{
    SingleTarget,
    Cross,       // Centre + 4 adjacentes (Explosion Solaire)
    Line,        // Ligne dans une direction
    Circle,      // Cercle rayon R (Pluie de Flèches, Surcharge)
    Bounce,      // Rebond sur ennemis adjacents à la cible (Ricochet)
    Self,        // Soi-même uniquement
    FreeCell,    // Case libre dans la portée (Saut de l'Ange, Pilier de Pierre)
    Cone,        // Cône devant le lanceur (axe vers la case visée)
    Boost,       // Zone autour du lanceur (rayon = aoeRadius)
}

public enum SpellEffectType
{
    Damage,
    Heal,
    SelfDamage,          // Coût en PV (Adrénaline)
    Push,                // Repousser de N cases (Surcharge, Vent de Panique)
    Pull,                // Attirer le lanceur vers la cible (Liane de Fer)
    Swap,                // Échanger positions (Voltige)
    Teleport,            // Se téléporter sur case libre (Saut de l'Ange)
    Shield,              // Bouclier de N PB (Rempart)
    DamageReduction,     // Réduire dégâts reçus de N par hit (Peau d'Écorce)
    Thorns,              // Renvoyer N dégâts à l'attaquant (Épine)
    Invisible,           // Devenir invisible
    Silence,             // Bannit 1 sort Attaque aléatoire du deck cible (durée = tours du porteur)
    BonusPA,             // Gagner N PA ce tour
    BonusPM,             // Gagner N PM ce tour
    RemovePM,            // Retirer N PM à la cible (Patate de forain)
    StealPM,             // Voler N PM à la cible (Siphon)
    BonusPANextTurn,     // +N PA au début du prochain tour
    BonusRange,          // +N portée sur tous les sorts ce tour (Esprit Clair)
    Bleed,               // N dégâts par tour pendant D tours (Hémorragie)
    Cleanse,             // Retirer tous les malus (Purge)
    CreateWall,          // Créer obstacle temporaire (Pilier de Pierre, Balise Statique)
    LastBreath,          // Survivre à 1 PV (Second Souffle)
    ConvertPMtoPA,       // 1 PM → 2 PA (Sacrifice)
    GravityDebuff,       // Interdit téléportation/dash (Gravité)
    ReduceFirstAttack,   // Premier sort réduit de 20% (Vent de Panique)
    /// <summary>Dégâts aux ennemis dans un cercle Manhattan autour du lanceur (rayon = <see cref="SpellEffect.duration"/>, défaut 1). Saut de l'Ange.</summary>
    ChipDamageAroundCaster,
}

public enum SpellCondition
{
    Always,
    /// <summary>Seuil = pourcentage des PV max de la cible (ex. 20 = moins de 20 % des PV max).</summary>
    TargetHPBelow,
    FromBehind,      // Dégâts bonus si l'attaquant est derrière la cible (valeur = bonus plat, voir SpellResolver.IsAttackerBehindTarget)
    SelfHPBelow,
    /// <summary>Patate de forain : bonus si la cible n'a plus de PM (après un RemovePM dans le même sort si listé avant).</summary>
    TargetHasNoPM,
    /// <summary>Éclat Arcanique : bonus si la case cible est exactement à la portée max effective (rangeMax + bonus portée).</summary>
    AtMaxCastRange,
    /// <summary>Ricochet : case de la cible adjacente à un obstacle / mur / case non marchable.</summary>
    TargetAdjacentToObstacle,
    /// <summary>Dague de Verre : ce sort est au moins la 3e attaque au CàC lancée ce tour par le lanceur.</summary>
    ThirdMeleeSpellThisTurn,
    /// <summary>Silence : n'applique l'effet Silence que si la cible a au moins un debuff.</summary>
    TargetHasDebuff,
    /// <summary>Bonus si distance de lancer = <see cref="SpellEffect.conditionThreshold"/> (Manhattan).</summary>
    AtExactCastDistance,
}

/// <summary>Catégorie pour la constitution du deck (tirage 2+2+2 depuis le pool de 30 sorts).</summary>
public enum SpellDeckCategory
{
    Attack,
    Survival,
    Tactic
}

[System.Serializable]
public class SpellEffect
{
    public SpellEffectType type = SpellEffectType.Damage;
    public int value = 0;
    public int duration = 0;
    public SpellCondition condition = SpellCondition.Always;
    public int conditionThreshold = 0;
    public float conditionMultiplier = 1f;
}

[CreateAssetMenu(fileName = "NewSpell", menuName = "Oracle/Spell Data")]
public class SpellData : ScriptableObject
{
    [Header("Identité")]
    public string spellName = "Nouveau Sort";
    [Tooltip("Attaques / Survie / Tactiques — rempli auto par le menu Oracle « Spell Deck Pool ».")]
    public SpellDeckCategory deckCategory = SpellDeckCategory.Attack;
    public Sprite icon;
    [TextArea(2, 4)] public string description;
    [TextArea(2, 4)] public string synergyDescription;

    [Header("Coût & Cooldown")]
    public int paCost = 2;
    public int cooldown = 0;

    [Header("Portée")]
    public bool isMeleeOnly = false;
    public int rangeMin = 1;
    public int rangeMax = 3;
    public bool requiresLineOfSight = false;
    public bool ignoresLineOfSight = false;

    [Header("Zone d'effet")]
    public ZoneType zoneType = ZoneType.SingleTarget;
    public int aoeRadius = 1;
    [Tooltip("Si vrai, Damage et Push ne s'appliquent pas au lanceur (ex. Surcharge centrée sur soi).")]
    public bool excludeCasterFromHarmfulAoE = false;

    [Header("Effets")]
    public List<SpellEffect> effects = new List<SpellEffect>();

    [Header("Animation combat")]
    [Tooltip("Dossier Resources CombatAnimations/Frames/<id>_NE — ex. attack_dague_de_verre. Laisser vide pour le mapping automatique par nom de sort.")]
    public string castAnimationBaseId;
}
