using MCM.Abstractions;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using System.Collections.Generic;

namespace BattleStamina
{
    public class StaminaProperties : AttributeGlobalSettings<StaminaProperties>
    {
        public override string DisplayName => "BattleStamina";
        public override string FormatType => "xml";
        public override string Id { get; } = "BattleStaminaProperties";

        [SettingPropertyGroup("Stamina Calculation Variables")]
        [SettingPropertyInteger("Base Stamina Value", 0, 1000, HintText = "The base amount of stamina each character gets")]
        public int BaseStaminaValue
        {
            get => StaminaSettings.Instance.BaseStaminaValue;
            set { StaminaSettings.Instance.BaseStaminaValue = value; StaminaSettings.Save(); }
        }

        [SettingPropertyGroup("Stamina Calculation Variables")]
        [SettingPropertyFloatingInteger("Stamina Gained per Athletics", 0.0f, 10.0f, HintText = "The amount of stamina gained per rank in Athletics")]
        public float StaminaGainedPerAthletics
        {
            get => StaminaSettings.Instance.StaminaGainedPerAthletics;
            set { StaminaSettings.Instance.StaminaGainedPerAthletics = value; StaminaSettings.Save(); }
        }

        [SettingPropertyGroup("Stamina Calculation Variables")]
        [SettingPropertyFloatingInteger("Stamina Gained per Combat Skill", 0.0f, 10.0f, HintText = "The amount of stamina gained per rank in combat skills (One Handed, Two Handed, Bow, Throwing, etc)")]
        public float StaminaGainedPerCombatSkill
        {
            get => StaminaSettings.Instance.StaminaGainedPerCombatSkill;
            set { StaminaSettings.Instance.StaminaGainedPerCombatSkill = value; StaminaSettings.Save(); }
        }

        [SettingPropertyGroup("Stamina Calculation Variables")]
        [SettingPropertyInteger("Stamina Gained per Level", 0, 50, HintText = "The amount of stamina gained per level")]
        public int StaminaGainedPerLevel
        {
            get => StaminaSettings.Instance.StaminaGainedPerLevel;
            set { StaminaSettings.Instance.StaminaGainedPerLevel = value; StaminaSettings.Save(); }
        }

        [SettingPropertyGroup("Stamina Costs")]
        [SettingPropertyInteger("Stamina Cost of Melee Attacks", 0, 100, HintText = "The amount of stamina depleted per melee attack")]
        public int StaminaCostToMeleeAttack
        {
            get => StaminaSettings.Instance.StaminaCostToMeleeAttack;
            set { StaminaSettings.Instance.StaminaCostToMeleeAttack = value; StaminaSettings.Save(); }
        }

        [SettingPropertyGroup("Stamina Costs")]
        [SettingPropertyInteger("Stamina Cost of Ranged Attacks", 0, 100, HintText = "The amount of stamina depleted per ranged attack")]
        public int StaminaCostToRangedAttack
        {
            get => StaminaSettings.Instance.StaminaCostToRangedAttack;
            set { StaminaSettings.Instance.StaminaCostToRangedAttack = value; StaminaSettings.Save(); }
        }

        [SettingPropertyGroup("Stamina Costs")]
        [SettingPropertyFloatingInteger("Stamina Cost of Blocking Damage", 0.0f, 5.0f, HintText = "The amount of stamina depleted per point of damage blocked")]
        public float StaminaCostPerBlockedDamage
        {
            get => StaminaSettings.Instance.StaminaCostPerBlockedDamage;
            set { StaminaSettings.Instance.StaminaCostPerBlockedDamage = value; StaminaSettings.Save(); }
        }

        [SettingPropertyGroup("Stamina Costs")]
        [SettingPropertyInteger("Stamina Cost of Receiving Damage", 0, 10, HintText = "The amount of stamina depleted per point of damage received")]
        public int StaminaCostPerReceivedDamage
        {
            get => StaminaSettings.Instance.StaminaCostPerReceivedDamage;
            set { StaminaSettings.Instance.StaminaCostPerReceivedDamage = value; StaminaSettings.Save(); }
        }

        [SettingPropertyGroup("Stamina Debuffs")]
        [SettingPropertyFloatingInteger("Lowest Speed from Stamina Debuff", 0f, 1.0f, HintText = "The minimum speed (percentage) a character will attack at when they run out of stamina")]
        public float LowestSpeedFromStaminaDebuff
        {
            get => StaminaSettings.Instance.LowestSpeedFromStaminaDebuff;
            set { StaminaSettings.Instance.LowestSpeedFromStaminaDebuff = value; StaminaSettings.Save(); }
        }

        [SettingPropertyGroup("Stamina Regeneration")]
        [SettingPropertyFloatingInteger("Stamina Recovered per Tick while Moving", 0f, 1f, HintText = "The amount of stamina regained per game tick when above 'Regen Maximum Move Speed'")]
        public float StaminaRecoveredPerTickMoving
        {
            get => StaminaSettings.Instance.StaminaRecoveredPerTickMoving;
            set { StaminaSettings.Instance.StaminaRecoveredPerTickMoving = value; StaminaSettings.Save(); }
        }

        [SettingPropertyGroup("Stamina Regeneration")]
        [SettingPropertyFloatingInteger("Stamina Recovered per Tick while Resting", 0f, 1f, HintText = "The amount of stamina regained per game tick when below 'Regen Maximum Move Speed'")]
        public float StaminaRecoveredPerTickResting
        {
            get => StaminaSettings.Instance.StaminaRecoveredPerTickResting;
            set { StaminaSettings.Instance.StaminaRecoveredPerTickResting = value; StaminaSettings.Save(); }
        }

        [SettingPropertyGroup("Stamina Regeneration")]
        [SettingPropertyFloatingInteger("Seconds before Stamina Regenerates", 0.0f, 10.0f, HintText = "The estimated amount of seconds before stamina begins to regenerate")]
        public float SecondsBeforeStaminaRegenerates
        {
            get => StaminaSettings.Instance.SecondsBeforeStaminaRegenerates;
            set { StaminaSettings.Instance.SecondsBeforeStaminaRegenerates = value; StaminaSettings.Save(); }
        }

        [SettingPropertyGroup("Stamina Regeneration")]
        [SettingPropertyFloatingInteger("Regen Maximum Move Speed", 0.1f, 1f, HintText = "The move speed percentage above which stamina regeneration will be reduced")]
        public float MaximumMoveSpeedPercentStaminaRegenerates
        {
            get => StaminaSettings.Instance.MaximumMoveSpeedPercentStaminaRegenerates;
            set { StaminaSettings.Instance.MaximumMoveSpeedPercentStaminaRegenerates = value; StaminaSettings.Save(); }
        }

        [SettingPropertyBool("Stamina Affects Crush Through", HintText = "Toggle if stamina affects crush through.")]
        public bool StaminaAffectsCrushThrough
        {
            get => StaminaSettings.Instance.StaminaAffectsCrushThrough;
            set { StaminaSettings.Instance.StaminaAffectsCrushThrough = value; StaminaSettings.Save(); }
        }

        public override IEnumerable<ISettingsPreset> GetBuiltInPresets()
        {
            foreach (var preset in base.GetBuiltInPresets())
            {
                yield return preset;
            }
        }
    }
}
