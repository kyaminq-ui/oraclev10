#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Oracle Spell Factory — Génère tous les SpellData et PassiveData du jeu.
/// Menu : Oracle > Generate Spells & Passives
/// Valeurs : plan ranked 800 PV (scaling ×20 recommandation simulation).
/// </summary>
public static class OracleSpellFactory
{
    private const string SpellPath   = "Assets/_Game/ScriptableObjects/Spells";
    private const string AttackPath  = "Assets/_Game/ScriptableObjects/Spells/Attaques";
    private const string TactPath    = "Assets/_Game/ScriptableObjects/Spells/Tactiques";
    private const string SurvivePath = "Assets/_Game/ScriptableObjects/Spells/Survie";
    private const string PassivePath = "Assets/_Game/ScriptableObjects/Spells/Passifs";

    [MenuItem("Oracle/Generate Spells & Passives")]
    public static void GenerateAll()
    {
        EnsureFolders();

        int created = 0;
        created += CreateAttaques();
        created += CreateTactiques();
        created += CreateSurvie();
        created += CreatePassives();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Oracle — Factory",
            $"{created} assets créés avec succès !\n\n" +
            "Tu peux maintenant les assigner dans ton DeckData.",
            "Super !");
        Debug.Log($"[OracleSpellFactory] {created} assets générés.");
    }

    static void EnsureFolders()
    {
        CreateFolder("Assets/_Game/ScriptableObjects", "Spells");
        CreateFolder(SpellPath, "Attaques");
        CreateFolder(SpellPath, "Tactiques");
        CreateFolder(SpellPath, "Survie");
        CreateFolder(SpellPath, "Passifs");
    }

    static void CreateFolder(string parent, string name)
    {
        string full = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(full))
            AssetDatabase.CreateFolder(parent, name);
    }

    static int CreateAttaques()
    {
        int n = 0;

        var s = Spell("Couteau dans le dos", 4, 0,
            melee: true, rangeMin: 1, rangeMax: 1,
            zone: ZoneType.SingleTarget, aoe: 0,
            desc: "100 dégâts. +50 depuis le dos.",
            synergy: "Combo Voltige / Maître d'Arme.");
        s.effects.Add(E(SpellEffectType.Damage, 100));
        s.effects.Add(E(SpellEffectType.Damage, 50, cond: SpellCondition.FromBehind));
        n += Save(s, AttackPath);

        s = Spell("Exécution", 4, 0,
            melee: false, rangeMin: 1, rangeMax: 4,
            zone: ZoneType.SingleTarget, aoe: 0,
            desc: "100 dégâts. +36 si cible sous 20 % PV max.",
            synergy: "Après Hémorragie.");
        s.effects.Add(E(SpellEffectType.Damage, 100));
        s.effects.Add(E(SpellEffectType.Damage, 36, cond: SpellCondition.TargetHPBelow, threshold: 20));
        n += Save(s, AttackPath);

        s = Spell("Hémorragie", 3, 0,
            melee: false, rangeMin: 1, rangeMax: 3,
            zone: ZoneType.SingleTarget, aoe: 0,
            desc: "60 dégâts + saignement 20/t (2 t) ; 2e cumul → 35/t.",
            synergy: "Exécution.");
        s.effects.Add(E(SpellEffectType.Damage, 60));
        s.effects.Add(E(SpellEffectType.Bleed, 20, duration: 2));
        n += Save(s, AttackPath);

        s = Spell("Ricochet", 4, 0,
            melee: false, rangeMin: 1, rangeMax: 3,
            zone: ZoneType.Bounce, aoe: 0,
            desc: "100 sur la cible ; rebonds 65 %. +25 si la cible est adjacente à un mur/obstacle.",
            synergy: "Surcharge.");
        s.effects.Add(E(SpellEffectType.Damage, 100));
        s.effects.Add(E(SpellEffectType.Damage, 25, cond: SpellCondition.TargetAdjacentToObstacle));
        n += Save(s, AttackPath);

        s = Spell("Explosion Solaire", 5, 2,
            melee: false, rangeMin: 1, rangeMax: 4,
            zone: ZoneType.Cross, aoe: 1,
            desc: "160 dégâts (croix). LdV requise.",
            synergy: "Vent de Panique.");
        s.requiresLineOfSight = true;
        s.effects.Add(E(SpellEffectType.Damage, 160));
        n += Save(s, AttackPath);

        s = Spell("Patate de forain", 4, 1,
            melee: true, rangeMin: 1, rangeMax: 1,
            zone: ZoneType.SingleTarget, aoe: 0,
            desc: "Retire 1 PM, 120 dégâts, +20 si cible à 0 PM après retrait.",
            synergy: "Épine.");
        s.effects.Add(E(SpellEffectType.RemovePM, 1));
        s.effects.Add(E(SpellEffectType.Damage, 120));
        s.effects.Add(E(SpellEffectType.Damage, 20, cond: SpellCondition.TargetHasNoPM));
        n += Save(s, AttackPath);

        s = Spell("Dague de Verre", 2, 0,
            melee: true, rangeMin: 1, rangeMax: 1,
            zone: ZoneType.SingleTarget, aoe: 0,
            desc: "56 dégâts. +72 si c'est au moins ton 3e sort CàC ce tour.",
            synergy: "Enchaînements CàC.");
        s.effects.Add(E(SpellEffectType.Damage, 56));
        s.effects.Add(E(SpellEffectType.Damage, 72, cond: SpellCondition.ThirdMeleeSpellThisTurn));
        n += Save(s, AttackPath);

        s = Spell("Pluie de Flèches", 4, 0,
            melee: false, rangeMin: 2, rangeMax: 5,
            zone: ZoneType.Circle, aoe: 2,
            desc: "40 dans le cercle + saignement 12/t (2 t). Ignore LdV.",
            synergy: "Pilier.");
        s.ignoresLineOfSight = true;
        s.effects.Add(E(SpellEffectType.Damage, 40));
        s.effects.Add(E(SpellEffectType.Bleed, 12, duration: 2));
        n += Save(s, AttackPath);

        s = Spell("Éclat Arcanique", 3, 0,
            melee: false, rangeMin: 4, rangeMax: 6,
            zone: ZoneType.SingleTarget, aoe: 0,
            desc: "96 dégâts. +12 si la cible est exactement à 6 cases.",
            synergy: "Esprit Clair / Sniper.");
        s.effects.Add(E(SpellEffectType.Damage, 96));
        s.effects.Add(E(SpellEffectType.Damage, 12, cond: SpellCondition.AtExactCastDistance, threshold: 6));
        n += Save(s, AttackPath);

        s = Spell("Silence", 5, 3,
            melee: false, rangeMin: 1, rangeMax: 4,
            zone: ZoneType.SingleTarget, aoe: 0,
            desc: "112 dégâts. Ban 1 sort Attaque 2 tours si la cible a un debuff.",
            synergy: "Après Hémorragie / contrôle.");
        s.effects.Add(E(SpellEffectType.Damage, 112));
        s.effects.Add(E(SpellEffectType.Silence, 0, duration: 2, cond: SpellCondition.TargetHasDebuff));
        n += Save(s, AttackPath);

        return n;
    }

    static int CreateTactiques()
    {
        int n = 0;

        var s = Spell("Voltige", 3, 2,
            melee: false, rangeMin: 1, rangeMax: 3,
            zone: ZoneType.SingleTarget, aoe: 0,
            desc: "20 dégâts puis échange de position avec la cible.",
            synergy: "Dos / Couteau.");
        s.effects.Add(E(SpellEffectType.Damage, 20));
        s.effects.Add(E(SpellEffectType.Swap, 0));
        n += Save(s, TactPath);

        s = Spell("Surcharge", 3, 0,
            melee: false, rangeMin: 0, rangeMax: 0,
            zone: ZoneType.Circle, aoe: 1,
            desc: "36 dégâts en cercle sur ta case (tu ne subis pas les dégâts) puis repousse d'1 case.",
            synergy: "Pilier / Ricochet.");
        s.excludeCasterFromHarmfulAoE = true;
        s.effects.Add(E(SpellEffectType.Damage, 36));
        s.effects.Add(E(SpellEffectType.Push, 1));
        n += Save(s, TactPath);

        s = Spell("Gravité", 4, 2,
            melee: false, rangeMin: 1, rangeMax: 4,
            zone: ZoneType.SingleTarget, aoe: 0,
            desc: "24 dégâts. Gravité 2 tours (téléport / traction / échange bloqués chez la cible).",
            synergy: "Anti-repositionnement.");
        s.effects.Add(E(SpellEffectType.Damage, 24));
        s.effects.Add(E(SpellEffectType.GravityDebuff, 0, duration: 2));
        n += Save(s, TactPath);

        s = Spell("Saut de l'Ange", 3, 2,
            melee: false, rangeMin: 1, rangeMax: 4,
            zone: ZoneType.FreeCell, aoe: 0,
            desc: "24 dégâts aux ennemis à portée 1 autour de toi, puis téléportation (4 cases, ignore obstacles).",
            synergy: "Chip + Couteau.");
        s.ignoresLineOfSight = true;
        s.effects.Add(E(SpellEffectType.ChipDamageAroundCaster, 24, duration: 1));
        s.effects.Add(E(SpellEffectType.Teleport, 0));
        n += Save(s, TactPath);

        s = Spell("Liane de Fer", 3, 2,
            melee: false, rangeMin: 2, rangeMax: 6,
            zone: ZoneType.SingleTarget, aoe: 0,
            desc: "24 dégâts à la cible puis traction vers case adjacente.",
            synergy: "Patate.");
        s.effects.Add(E(SpellEffectType.Damage, 24));
        s.effects.Add(E(SpellEffectType.Pull, 0));
        n += Save(s, TactPath);

        s = Spell("Vent de Panique", 4, 2,
            melee: true, rangeMin: 0, rangeMax: 1,
            zone: ZoneType.Circle, aoe: 1,
            desc: "36 dégâts, repousse d'1 case, −20 % sur la première attaque des cibles.",
            synergy: "Explosion.");
        s.effects.Add(E(SpellEffectType.Damage, 36));
        s.effects.Add(E(SpellEffectType.Push, 1));
        s.effects.Add(E(SpellEffectType.ReduceFirstAttack, 20, duration: 1));
        n += Save(s, TactPath);

        s = Spell("Pilier de Pierre", 4, 2,
            melee: false, rangeMin: 1, rangeMax: 3,
            zone: ZoneType.FreeCell, aoe: 0,
            desc: "Mur 1 case 2 tours. Cd 2.",
            synergy: "Pluie.");
        s.effects.Add(E(SpellEffectType.CreateWall, 2, duration: 2));
        n += Save(s, TactPath);

        s = Spell("Épine", 3, 1,
            melee: false, rangeMin: 0, rangeMax: 0,
            zone: ZoneType.Self, aoe: 0,
            desc: "30 épines pendant 1 tour. Cd 1.",
            synergy: "Toxicité.");
        s.effects.Add(E(SpellEffectType.Thorns, 30, duration: 1));
        n += Save(s, TactPath);

        s = Spell("Sacrifice", 0, 2,
            melee: false, rangeMin: 0, rangeMax: 0,
            zone: ZoneType.Self, aoe: 0,
            desc: "1 PM → 2 PA ce tour.",
            synergy: "Méditation.");
        s.effects.Add(E(SpellEffectType.ConvertPMtoPA, 1));
        n += Save(s, TactPath);

        s = Spell("Siphon", 3, 1,
            melee: false, rangeMin: 1, rangeMax: 3,
            zone: ZoneType.SingleTarget, aoe: 0,
            desc: "Vole 1 PM.",
            synergy: "Patate.");
        s.effects.Add(E(SpellEffectType.StealPM, 1));
        n += Save(s, TactPath);

        s = Spell("Projection", 4, 3,
            melee: true, rangeMin: 0, rangeMax: 1,
            zone: ZoneType.Circle, aoe: 1,
            desc: "180 dégâts aux ennemis adjacents puis repousse de 2 cases.",
            synergy: "Explosion.");
        s.effects.Add(E(SpellEffectType.Damage, 180));
        s.effects.Add(E(SpellEffectType.Push, 2));
        n += Save(s, TactPath);

        s = Spell("Entrave", 4, 2,
            melee: false, rangeMin: 1, rangeMax: 4,
            zone: ZoneType.SingleTarget, aoe: 0,
            desc: "28 dégâts + Gravité 2 tours.",
            synergy: "Explosion.");
        s.effects.Add(E(SpellEffectType.Damage, 28));
        s.effects.Add(E(SpellEffectType.GravityDebuff, 0, duration: 2));
        n += Save(s, TactPath);

        return n;
    }

    static int CreateSurvie()
    {
        int n = 0;

        var s = Spell("Adrénaline", 2, 0,
            melee: false, rangeMin: 0, rangeMax: 0,
            zone: ZoneType.Self, aoe: 0,
            desc: "+2 PM ce tour ; 24 PV (coût).",
            synergy: "Berserker.");
        s.effects.Add(E(SpellEffectType.BonusPM, 2));
        s.effects.Add(E(SpellEffectType.SelfDamage, 24));
        n += Save(s, SurvivePath);

        s = Spell("Méditation", 2, 2,
            melee: false, rangeMin: 0, rangeMax: 0,
            zone: ZoneType.Self, aoe: 0,
            desc: "+2 PA immédiat.",
            synergy: "Sacrifice.");
        s.effects.Add(E(SpellEffectType.BonusPA, 2));
        n += Save(s, SurvivePath);

        s = Spell("Rempart", 4, 2,
            melee: false, rangeMin: 0, rangeMax: 0,
            zone: ZoneType.Self, aoe: 0,
            desc: "Bouclier 96 (durée 2). Cd 2.",
            synergy: "Épine.");
        s.effects.Add(E(SpellEffectType.Shield, 96, duration: 2));
        n += Save(s, SurvivePath);

        s = Spell("Invisibilité", 5, 3,
            melee: false, rangeMin: 0, rangeMax: 0,
            zone: ZoneType.Self, aoe: 0,
            desc: "Invisible 1 tour ou jusqu'à attaque.",
            synergy: "Camouflage.");
        s.effects.Add(E(SpellEffectType.Invisible, 1, duration: 1));
        n += Save(s, SurvivePath);

        s = Spell("Esprit Clair", 2, 2,
            melee: false, rangeMin: 0, rangeMax: 0,
            zone: ZoneType.Self, aoe: 0,
            desc: "+2 portée ce tour.",
            synergy: "Éclat.");
        s.effects.Add(E(SpellEffectType.BonusRange, 2, duration: 1));
        n += Save(s, SurvivePath);

        s = Spell("Balise Statique", 3, 0,
            melee: false, rangeMin: 1, rangeMax: 3,
            zone: ZoneType.FreeCell, aoe: 0,
            desc: "Obstacle 3 tours.",
            synergy: "Couloir.");
        s.effects.Add(E(SpellEffectType.CreateWall, 3, duration: 3));
        n += Save(s, SurvivePath);

        s = Spell("Pansement", 3, 2,
            melee: false, rangeMin: 1, rangeMax: 2,
            zone: ZoneType.SingleTarget, aoe: 0,
            desc: "Soigne 80 + retire saignements.",
            synergy: "Second Souffle.");
        s.effects.Add(E(SpellEffectType.Heal, 80));
        n += Save(s, SurvivePath);

        s = Spell("Peau d'Écorce", 3, 1,
            melee: false, rangeMin: 0, rangeMax: 0,
            zone: ZoneType.Self, aoe: 0,
            desc: "−48 dégâts par coup reçu (1 tour). Cd 1.",
            synergy: "Épine.");
        s.effects.Add(E(SpellEffectType.DamageReduction, 48, duration: 1));
        n += Save(s, SurvivePath);

        s = Spell("Purge", 2, 0,
            melee: false, rangeMin: 1, rangeMax: 2,
            zone: ZoneType.SingleTarget, aoe: 0,
            desc: "Retire les malus.",
            synergy: "Duel long.");
        s.effects.Add(E(SpellEffectType.Cleanse, 0));
        n += Save(s, SurvivePath);

        s = Spell("Second Souffle", 8, 5,
            melee: false, rangeMin: 0, rangeMax: 0,
            zone: ZoneType.Self, aoe: 0,
            desc: "À 0 PV : reste à 1 PV puis soigne ~12 % PV max (ex. 96 à 800 PV).",
            synergy: "Berserker.");
        s.effects.Add(E(SpellEffectType.LastBreath, 0));
        n += Save(s, SurvivePath);

        return n;
    }

    static int CreatePassives()
    {
        int n = 0;

        n += SavePassive(Passive("Berserker",
            PassiveType.Berserker, PassiveTrigger.Permanent,
            effectValue: 0.20f, procChance: 1f, condThreshold: 20,
            desc: "+20 % dégâts sous 20 % PV max."));

        n += SavePassive(Passive("Évasif",
            PassiveType.Evasif, PassiveTrigger.OnDamageReceived,
            effectValue: 0f, procChance: 0.10f, condThreshold: 0,
            desc: "10 % : dégâts distance ÷2."));

        n += SavePassive(Passive("Dernier Rempart",
            PassiveType.DernierRempart, PassiveTrigger.OnTurnStart,
            effectValue: 8f, procChance: 1f, condThreshold: 25,
            desc: "Tour : bouclier 8 % PV max si sous 25 % PV max."));

        n += SavePassive(Passive("Vigilance",
            PassiveType.Vigilance, PassiveTrigger.Permanent,
            effectValue: 0f, procChance: 1f, condThreshold: 0,
            desc: "Début de tour : +1 PM si ennemi adjacent."));

        n += SavePassive(Passive("Masse Critique",
            PassiveType.MasseCritique, PassiveTrigger.OnSpellCast,
            effectValue: 0.15f, procChance: 0.20f, condThreshold: 0,
            desc: "20 % : sort +15 % dégâts."));

        n += SavePassive(Passive("Maître d'Arme",
            PassiveType.MaitreArme, PassiveTrigger.Permanent,
            effectValue: 3f, procChance: 1f, condThreshold: 0,
            desc: "+3 % PV max sur sorts CàC."));

        n += SavePassive(Passive("Camouflage",
            PassiveType.Camouflage, PassiveTrigger.OnTurnEnd,
            effectValue: 0f, procChance: 1f, condThreshold: 0,
            desc: "Sans sort : invisible + décalage +1 PM tour suivant."));

        n += SavePassive(Passive("Sniper",
            PassiveType.Sniper, PassiveTrigger.Permanent,
            effectValue: 3f, procChance: 1f, condThreshold: 5,
            desc: "+3 % PV max si portée > 5 cases."));

        n += SavePassive(Passive("Bouclier Hasardeux",
            PassiveType.BouclierHasardeux, PassiveTrigger.OnDamageReceived,
            effectValue: 0f, procChance: 0f, condThreshold: 0,
            desc: "Chaque 5e coup annulé."));

        n += SavePassive(Passive("Toxicité",
            PassiveType.Toxicite, PassiveTrigger.OnEnemyTurnEnd,
            effectValue: 2f, procChance: 1f, condThreshold: 0,
            desc: "Ennemi adjacent fin de tour : 2 % de tes PV max en dégâts."));

        return n;
    }

    static SpellData Spell(string name, int pa, int cd,
        bool melee, int rangeMin, int rangeMax,
        ZoneType zone, int aoe,
        string desc, string synergy)
    {
        var s = ScriptableObject.CreateInstance<SpellData>();
        s.spellName          = name;
        s.paCost             = pa;
        s.cooldown           = cd;
        s.isMeleeOnly        = melee;
        s.rangeMin           = rangeMin;
        s.rangeMax           = rangeMax;
        s.zoneType           = zone;
        s.aoeRadius          = aoe;
        s.description        = desc;
        s.synergyDescription = synergy;
        return s;
    }

    static SpellEffect E(SpellEffectType type, int value,
        int duration = 0,
        SpellCondition cond = SpellCondition.Always,
        int threshold = 0,
        float mult = 1f)
    {
        return new SpellEffect
        {
            type                = type,
            value               = value,
            duration            = duration,
            condition           = cond,
            conditionThreshold  = threshold,
            conditionMultiplier = mult,
        };
    }

    static PassiveData Passive(string name,
        PassiveType type, PassiveTrigger trigger,
        float effectValue, float procChance, int condThreshold,
        string desc)
    {
        var p = ScriptableObject.CreateInstance<PassiveData>();
        p.passiveName        = name;
        p.passiveType        = type;
        p.trigger            = trigger;
        p.effectValue        = effectValue;
        p.procChance         = procChance;
        p.conditionThreshold = condThreshold;
        p.description        = desc;
        return p;
    }

    static int Save(SpellData s, string folder)
    {
        string path = $"{folder}/{s.spellName}.asset";
        if (AssetDatabase.LoadAssetAtPath<SpellData>(path) != null)
        {
            Debug.Log($"[OracleSpellFactory] Ignoré (déjà existant) : {s.spellName}");
            return 0;
        }
        AssetDatabase.CreateAsset(s, path);
        return 1;
    }

    static int SavePassive(PassiveData p)
    {
        string path = $"{PassivePath}/{p.passiveName}.asset";
        if (AssetDatabase.LoadAssetAtPath<PassiveData>(path) != null)
        {
            Debug.Log($"[OracleSpellFactory] Ignoré (déjà existant) : {p.passiveName}");
            return 0;
        }
        AssetDatabase.CreateAsset(p, path);
        return 1;
    }
}
#endif
