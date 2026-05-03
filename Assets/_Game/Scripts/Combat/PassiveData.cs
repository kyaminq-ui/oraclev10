using UnityEngine;

public enum PassiveType
{
    Berserker,          // +20% dégâts si HP < seuil
    Evasif,             // 10% esquive totale (distance)
    DernierRempart,     // +5 PB au début de tour si HP < seuil
    Vigilance,          // Début de tour : +1 PM si ennemi adjacent
    MasseCritique,      // 20% chance +15% dégâts par sort
    MaitreArme,         // +% max HP dégâts sur sorts CàC (valeur = %)
    Camouflage,         // Invisible + décalage si aucun sort ce tour ; +1 PM tour suivant
    Sniper,             // +% max HP si distance > seuil (cases)
    BouclierHasardeux,  // Chaque 5e coup subi annulé
    Toxicite,           // Dégâts % max HP aux ennemis adjacents fin de tour adverse
    Custom,
}

public enum PassiveTrigger
{
    Permanent,
    OnTurnStart,
    OnTurnEnd,
    OnDamageReceived,
    OnDamageDealt,
    OnKill,
    OnSpellCast,
    OnMove,
    OnEnemyTurnEnd,
}

public enum PassiveEffectType
{
    ModifyDamageDealt,
    ModifyDamageReceived,
    ModifyPA,
    ModifyPM,
    HealOnTrigger,
    Shield,
    Counter,
}

[CreateAssetMenu(fileName = "NewPassive", menuName = "Oracle/Passive Data")]
public class PassiveData : ScriptableObject, IPassive
{
    [Header("Identité")]
    public string passiveName = "Nouveau Passif";
    public Sprite icon;
    [TextArea(2, 4)] public string description;

    [Header("Type")]
    public PassiveType passiveType = PassiveType.Custom;
    public PassiveTrigger trigger   = PassiveTrigger.Permanent;

    [Header("Valeurs")]
    public float effectValue       = 0f;
    public float procChance        = 1.0f;   // 1.0 = toujours, 0.1 = 10%
    public int   conditionThreshold = 0;      // Berserker / Dernier Rempart : % PV max ; Sniper : distance min

    // =========================================================
    // IPassive — délègue au PassiveManager au runtime
    // =========================================================
    public bool CanTrigger(TriggerContext ctx) =>
        ctx.type == trigger || trigger == PassiveTrigger.Permanent;

    public void OnTrigger(TriggerContext ctx)
    {
        // La logique réelle est dans PassiveManager qui a accès à l'état de jeu.
        // Cette méthode est un point d'entrée formel pour l'interface.
    }
}
