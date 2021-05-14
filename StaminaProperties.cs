using ModLib;
using ModLib.Attributes;
using System.Xml;
using System.Xml.Serialization;

namespace BattleStamina
{
    public class StaminaProperties : SettingsBase
    {
        public const string InstanceID = "BattleStaminaProperties";

        public override string ModName => "BattleStamina";
        public override string ModuleFolderName => "BattleStamina";

        [XmlElement]
        public override string ID { get; set; } = InstanceID;

        [XmlElement]
        [SettingPropertyGroup("Stamina Calculation Variables")]
        [SettingProperty("Base Stamina Value", 0, 1000, "The base amount of stamina each character gets")]
        public int BaseStaminaValue { get; set; } = 600;
        [XmlElement]
        [SettingPropertyGroup("Stamina Calculation Variables")]
        [SettingProperty("Stamina Gained per Athletics", 0, 10, "The amount of stamina gained per rank in Athletics")]
        public float StaminaGainedPerAthletics { get; set; } = 3.0f;
        [XmlElement]
        [SettingPropertyGroup("Stamina Calculation Variables")]
        [SettingProperty("Stamina Gained per Combat Skill", 0, 10, "The amount of stamina gained per rank in combat skills (One Handed, Two Handed, Bow, Throwing, etc)")]
        public float StaminaGainedPerCombatSkill { get; set; } = 1.0f;
        [XmlElement]
        [SettingPropertyGroup("Stamina Calculation Variables")]
        [SettingProperty("Stamina Gained per Level", 0, 50, "The amount of stamina gained per level")]
        public int StaminaGainedPerLevel { get; set; } = 10;

        [XmlElement]
        [SettingPropertyGroup("Stamina Costs")]
        [SettingProperty("Stamina Cost of Melee Attacks", 0, 100, "The amount of stamina depleted per melee attack")]
        public int StaminaCostToMeleeAttack { get; set; } = 40;
        [XmlElement]
        [SettingPropertyGroup("Stamina Costs")]
        [SettingProperty("Stamina Cost of Ranged Attacks", 0, 100, "The amount of stamina depleted per ranged attack")]
        public int StaminaCostToRangedAttack { get; set; } = 40;
        [XmlElement]
        [SettingPropertyGroup("Stamina Costs")]
        [SettingProperty("Stamina Cost of Blocking Damage", 0, 5, "The amount of stamina depleted per point of damage blocked")]
        public float StaminaCostPerBlockedDamage { get; set; } = 1.5f;
        [XmlElement]
        [SettingPropertyGroup("Stamina Costs")]
        [SettingProperty("Stamina Cost of Receiving Damage", 0, 10, "The amount of stamina depleted per point of damage received")]
        public int StaminaCostPerReceivedDamage { get; set; } = 6;
        //public readonly double MoveCost { get; set; } = 0.5;

        [XmlElement]
        [SettingProperty("Lowest Speed from Stamina Debuff", 0f, 1.0f, "The minimum speed (percentage) a character will attack at when they run out of stamina")]
        public float LowestSpeedFromStaminaDebuff { get; set; } = 0.5f;

        [XmlElement]
        [SettingPropertyGroup("Stamina Regeneration")]
        [SettingProperty("Stamina Recovered per Tick while Moving", 0f, 1f, "The amount of stamina regained per game tick when above 'Regen Maximum Move Speed' (there are about 60-120 ticks per second)")]
        public float StaminaRecoveredPerTickMoving { get; set; } = 0.2f;
        [XmlElement]
        [SettingPropertyGroup("Stamina Regeneration")]
        [SettingProperty("Stamina Recovered per Tick while Resting", 0f, 1f, "The amount of stamina regained per game tick when below 'Regen Maximum Move Speed' (there are about 60-120 ticks per second)")]
        public float StaminaRecoveredPerTickResting { get; set; } = 0.5f;
        [XmlElement]
        [SettingPropertyGroup("Stamina Regeneration")]
        [SettingProperty("Seconds before Stamina Regenerates", 0, 10, "The estimated amount of seconds before stamina begins to regenerate")]
        public float SecondsBeforeStaminaRegenerates { get; set; } = 2.5f;
        [XmlElement]
        [SettingPropertyGroup("Stamina Regeneration")]
        [SettingProperty("Regen Maximum Move Speed", 0.1f, 1f, "The move speed (percentage) above which stamina regeneration will be reduced (10% requires standing still, 100% will allow full regeneration while moving at any speed)")]
        public float MaximumMoveSpeedPercentStaminaRegenerates { get; set; } = 0.5f;

        [XmlElement]
        public float FullStaminaRemaining { get; set; } = 1.0f;
        [XmlElement]
        public float HighStaminaRemaining { get; set; } = 0.75f;
        [XmlElement]
        public float MediumStaminaRemaining { get; set; } = 0.5f;
        [XmlElement]
        public float LowStaminaRemaining { get; set; } = 0.25f;
        [XmlElement]
        public float NoStaminaRemaining { get; set; } = 0.01f;

        [XmlElement]
        public bool NoStaminaRemainingStopsAttacks { get; set; } = false;

        [XmlElement]
        [SettingProperty("Stamina Affects Crush Through", "Toggle if stamina affects crush through. If on, characters with less than Low stamina will always have their blocks crushed through while characters with High stamina cannot be crushed though.")]
        public bool StaminaAffectsCrushThrough { get; set; } = true;

        public static StaminaProperties Instance
        {
            get
            {
                return (StaminaProperties)SettingsDatabase.GetSettings(InstanceID);
            }
        }
    }
}
