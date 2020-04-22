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
        public int BaseStaminaValue { get; set; } = 200;
        [XmlElement]
        [SettingPropertyGroup("Stamina Calculation Variables")]
        [SettingProperty("Stamina Gained per Athletics", 0, 10, "The amount of stamina gained per rank in Athletics")]
        public int StaminaGainedPerAthletics { get; set; } = 2;
        [XmlElement]
        [SettingPropertyGroup("Stamina Calculation Variables")]
        [SettingProperty("Stamina Gained per Combat Skill", 0, 10, "The amount of stamina gained per rank in combat skills (One Handed, Two Handed, Bow, Throwing, etc)")]
        public int StaminaGainedPerCombatSkill { get; set; } = 1;
        [XmlElement]
        [SettingPropertyGroup("Stamina Calculation Variables")]
        [SettingProperty("Stamina Gained per Level", 0, 50, "The amount of stamina gained per level")]
        public int StaminaGainedPerLevel { get; set; } = 5;

        [XmlElement]
        [SettingPropertyGroup("Stamina Costs")]
        [SettingProperty("Stamina Cost of Melee Attacks", 0, 100, "The amount of stamina depleted per melee attack")]
        public int StaminaCostToMeleeAttack { get; set; } = 20;
        [XmlElement]
        [SettingPropertyGroup("Stamina Costs")]
        [SettingProperty("Stamina Cost of Ranged Attacks", 0, 100, "The amount of stamina depleted per ranged attack")]
        public int StaminaCostToRangedAttack { get; set; } = 20;
        [XmlElement]
        [SettingPropertyGroup("Stamina Costs")]
        [SettingProperty("Stamina Cost of Blocking", 0, 100, "The amount of stamina depleted per blocked attack")]
        public int StaminaCostToBlock { get; set; } = 10;
        [XmlElement]
        [SettingPropertyGroup("Stamina Costs")]
        [SettingProperty("Stamina Cost of Receiving Damage", 0, 10, "The amount of stamina depleted per point of damage received")]
        public int StaminaCostPerReceivedDamage { get; set; } = 1;
        //public readonly double MoveCost { get; set; } = 0.5;

        [XmlElement]
        [SettingProperty("Lowest Speed from Stamina Debuff", 0f, 1.0f, "The maximum speed loss when a character runs out of stamina (0% is no debuff, 100% is all speed lost)")]
        public float LowestSpeedFromStaminaDebuff { get; set; } = 0.5f;

        [XmlElement]
        [SettingPropertyGroup("Stamina Regeneration")]
        [SettingProperty("Stamina Recovered per Tick while Moving", 0f, 1f, "The amount of stamina regained per game tick when above 'Regen Maximum Move Speed' (there are about 60 ticks per second)")]
        public float StaminaRecoveredPerTickMoving { get; set; } = 0.1f;
        [XmlElement]
        [SettingPropertyGroup("Stamina Regeneration")]
        [SettingProperty("Stamina Recovered per Tick while Resting", 0f, 1f, "The amount of stamina regained per game tick when below 'Regen Maximum Move Speed' (there are about 60 ticks per second)")]
        public float StaminaRecoveredPerTickResting { get; set; } = 0.2f;
        [XmlElement]
        [SettingPropertyGroup("Stamina Regeneration")]
        [SettingProperty("Seconds before Stamina Regenerates", 0, 10, "The estimated amount of time before stamina begins to regenerate")]
        public int SecondsBeforeStaminaRegenerates { get; set; } = 5;
        [XmlElement]
        [SettingPropertyGroup("Stamina Regeneration")]
        [SettingProperty("Regen Maximum Move Speed", 0.1f, 1f, "The move speed percent above which stamina regeneration will be reduced (10% requires standing still, 100% will allow full regeneration while moving at any speed)")]
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

        public static StaminaProperties Instance
        {
            get
            {
                return (StaminaProperties)SettingsDatabase.GetSettings(InstanceID);
            }
        }
    }
}
