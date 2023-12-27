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
        public int BaseStaminaValue { get; set; } = 600;

        [SettingPropertyGroup("Stamina Calculation Variables")]
        [SettingPropertyFloatingInteger("Stamina Gained per Athletics", 0.0f, 10.0f, HintText = "The amount of stamina gained per rank in Athletics")]
        public float StaminaGainedPerAthletics { get; set; } = 3.0f;

        [SettingPropertyGroup("Stamina Calculation Variables")]
        [SettingPropertyFloatingInteger("Stamina Gained per Combat Skill", 0.0f, 10.0f, HintText = "The amount of stamina gained per rank in combat skills (One Handed, Two Handed, Bow, Throwing, etc)")]
        public float StaminaGainedPerCombatSkill { get; set; } = 1.0f;

        [SettingPropertyGroup("Stamina Calculation Variables")]
        [SettingPropertyInteger("Stamina Gained per Level", 0, 50, HintText = "The amount of stamina gained per level")]
        public int StaminaGainedPerLevel { get; set; } = 10;

        [SettingPropertyGroup("Stamina Costs")]
        [SettingPropertyInteger("Stamina Cost of Melee Attacks", 0, 100, HintText = "The amount of stamina depleted per melee attack")]
        public int StaminaCostToMeleeAttack { get; set; } = 40;

        [SettingPropertyGroup("Stamina Costs")]
        [SettingPropertyInteger("Stamina Cost of Ranged Attacks", 0, 100, HintText = "The amount of stamina depleted per ranged attack")]
        public int StaminaCostToRangedAttack { get; set; } = 40;

        [SettingPropertyGroup("Stamina Costs")]
        [SettingPropertyFloatingInteger("Stamina Cost of Blocking Damage", 0.0f, 5.0f, HintText = "The amount of stamina depleted per point of damage blocked")]
        public float StaminaCostPerBlockedDamage { get; set; } = 1.5f;

        [SettingPropertyGroup("Stamina Costs")]
        [SettingPropertyInteger("Stamina Cost of Receiving Damage", 0, 10, HintText = "The amount of stamina depleted per point of damage received")]
        public int StaminaCostPerReceivedDamage { get; set; } = 6;

        //public readonly double MoveCost { get; set; } = 0.5;

        [SettingPropertyGroup("Stamina Debuffs")]
        [SettingPropertyFloatingInteger("Lowest Speed from Stamina Debuff", 0f, 1.0f, HintText = "The minimum speed (percentage) a character will attack at when they run out of stamina")]
        public float LowestSpeedFromStaminaDebuff { get; set; } = 0.5f;

        [SettingPropertyGroup("Stamina Regeneration")]
        [SettingPropertyFloatingInteger("Stamina Recovered per Tick while Moving", 0f, 1f, HintText = "The amount of stamina regained per game tick when above 'Regen Maximum Move Speed' (there are about 60-120 ticks per second)")]
        public float StaminaRecoveredPerTickMoving { get; set; } = 0.05f;

        [SettingPropertyGroup("Stamina Regeneration")]
        [SettingPropertyFloatingInteger("Stamina Recovered per Tick while Resting", 0f, 1f, HintText = "The amount of stamina regained per game tick when below 'Regen Maximum Move Speed' (there are about 60-120 ticks per second)")]
        public float StaminaRecoveredPerTickResting { get; set; } = 0.2f;

        [SettingPropertyGroup("Stamina Regeneration")]
        [SettingPropertyFloatingInteger("Seconds before Stamina Regenerates", 0.0f, 10.0f, HintText = "The estimated amount of seconds before stamina begins to regenerate")]
        public float SecondsBeforeStaminaRegenerates { get; set; } = 6.0f;

        [SettingPropertyGroup("Stamina Regeneration")]
        [SettingPropertyFloatingInteger("Regen Maximum Move Speed", 0.1f, 1f, HintText = "The move speed (percentage) above which stamina regeneration will be reduced (10% requires standing still, 100% will allow full regeneration while moving at any speed)")]
        public float MaximumMoveSpeedPercentStaminaRegenerates { get; set; } = 0.3f;

        public float FullStaminaRemaining { get; set; } = 1.0f;

        public float HighStaminaRemaining { get; set; } = 0.75f;

        public float MediumStaminaRemaining { get; set; } = 0.5f;

        public float LowStaminaRemaining { get; set; } = 0.25f;

        public float NoStaminaRemaining { get; set; } = 0.01f;

        public bool NoStaminaRemainingStopsAttacks { get; set; } = false;

        [SettingPropertyBool("Stamina Affects Crush Through", HintText = "Toggle if stamina affects crush through. If on, characters with less than Low stamina will always have their blocks crushed through while characters with High stamina cannot be crushed though.")]
        public bool StaminaAffectsCrushThrough { get; set; } = true;

        public override IEnumerable<ISettingsPreset> GetBuiltInPresets()
        {
            var basePresets = base.GetBuiltInPresets(); // include the 'Default' preset using above values
            foreach (var preset in basePresets)
                yield return preset;

            yield return new MemorySettingsPreset("Realistic Battles", "Default", "Default", () => new StaminaProperties()
            {
                BaseStaminaValue = 300,
                StaminaGainedPerAthletics = 3.0f,
                StaminaGainedPerCombatSkill = 1.0f,
                StaminaGainedPerLevel = 10,
                StaminaCostToMeleeAttack = 40,
                StaminaCostToRangedAttack = 40,
                StaminaCostPerBlockedDamage = 3.0f,
                StaminaCostPerReceivedDamage = 6,
                LowestSpeedFromStaminaDebuff = 0.5f,
                StaminaRecoveredPerTickMoving = 0.05f,
                StaminaRecoveredPerTickResting = 0.2f,
                SecondsBeforeStaminaRegenerates = 6.0f,
                MaximumMoveSpeedPercentStaminaRegenerates = 0.3f,
                FullStaminaRemaining = 1.00f,
                HighStaminaRemaining = 0.75f,
                MediumStaminaRemaining = 0.5f,
                LowStaminaRemaining = 0.25f,
                NoStaminaRemaining = 0.01f,
                NoStaminaRemainingStopsAttacks = false,
                StaminaAffectsCrushThrough = true,
            });
        }
    }
}
