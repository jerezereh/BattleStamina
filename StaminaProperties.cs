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
        [SettingPropertyFloatingInteger("Stamina Cost per Weapon Weight", 0.0f, 10.0f, HintText = "Extra stamina depleted per point of melee or thrown weapon weight")]
        public float StaminaCostPerWeaponWeight
        {
            get => StaminaSettings.Instance.StaminaCostPerWeaponWeight;
            set { StaminaSettings.Instance.StaminaCostPerWeaponWeight = value; StaminaSettings.Save(); }
        }

        [SettingPropertyGroup("Stamina Costs")]
        [SettingPropertyFloatingInteger("Stamina Cost per Missile Speed", 0.0f, 0.5f, HintText = "Extra stamina depleted by bows and crossbows per point of missile speed")]
        public float StaminaCostPerRangedMissileSpeed
        {
            get => StaminaSettings.Instance.StaminaCostPerRangedMissileSpeed;
            set { StaminaSettings.Instance.StaminaCostPerRangedMissileSpeed = value; StaminaSettings.Save(); }
        }

        [SettingPropertyGroup("Stamina Costs")]
        [SettingPropertyFloatingInteger("Stamina Cost of Melee Hits", 0.0f, 20.0f, HintText = "Extra stamina depleted when a melee attack hits an agent")]
        public float StaminaCostToMeleeHit
        {
            get => StaminaSettings.Instance.StaminaCostToMeleeHit;
            set { StaminaSettings.Instance.StaminaCostToMeleeHit = value; StaminaSettings.Save(); }
        }

        [SettingPropertyGroup("Stamina Costs")]
        [SettingPropertyFloatingInteger("Melee Hit Speed Cost Modifier", 0.0f, 20.0f, HintText = "Extra melee hit stamina cost per point of movement speed damage modifier")]
        public float StaminaCostPerMeleeHitSpeedModifier
        {
            get => StaminaSettings.Instance.StaminaCostPerMeleeHitSpeedModifier;
            set { StaminaSettings.Instance.StaminaCostPerMeleeHitSpeedModifier = value; StaminaSettings.Save(); }
        }

        [SettingPropertyGroup("Stamina Costs")]
        [SettingPropertyFloatingInteger("Stamina Cost of Shield Impact", 0.0f, 20.0f, HintText = "Extra stamina depleted when a melee attack hits a shield")]
        public float StaminaCostToMeleeShieldImpact
        {
            get => StaminaSettings.Instance.StaminaCostToMeleeShieldImpact;
            set { StaminaSettings.Instance.StaminaCostToMeleeShieldImpact = value; StaminaSettings.Save(); }
        }

        [SettingPropertyGroup("Stamina Costs")]
        [SettingPropertyFloatingInteger("Stamina Cost of Blocking Damage", 0.0f, 5.0f, HintText = "The amount of stamina depleted per point of damage blocked")]
        public float StaminaCostPerBlockedDamage
        {
            get => StaminaSettings.Instance.StaminaCostPerBlockedDamage;
            set { StaminaSettings.Instance.StaminaCostPerBlockedDamage = value; StaminaSettings.Save(); }
        }

        [SettingPropertyGroup("Stamina Costs")]
        [SettingPropertyFloatingInteger("Stamina Cost of Receiving Damage", 0.0f, 10.0f, HintText = "The amount of stamina depleted per point of damage received")]
        public float StaminaCostPerReceivedDamage
        {
            get => StaminaSettings.Instance.StaminaCostPerReceivedDamage;
            set { StaminaSettings.Instance.StaminaCostPerReceivedDamage = value; StaminaSettings.Save(); }
        }

        [SettingPropertyGroup("Stamina Costs")]
        [SettingPropertyFloatingInteger("Stamina Cost per Second Sprinting", 0.0f, 20.0f, HintText = "Base stamina depleted per second while moving at sprint speed, unless mounted")]
        public float StaminaCostPerSecondSprinting
        {
            get => StaminaSettings.Instance.StaminaCostPerSecondSprinting;
            set { StaminaSettings.Instance.StaminaCostPerSecondSprinting = value; StaminaSettings.Save(); }
        }

        [SettingPropertyGroup("Stamina Costs")]
        [SettingPropertyFloatingInteger("Sprinting Cost per Carried Weight", 0.0f, 1.0f, HintText = "Extra stamina depleted per second while sprinting, per point of carried equipment weight")]
        public float StaminaCostPerSecondSprintingPerCarriedWeight
        {
            get => StaminaSettings.Instance.StaminaCostPerSecondSprintingPerCarriedWeight;
            set { StaminaSettings.Instance.StaminaCostPerSecondSprintingPerCarriedWeight = value; StaminaSettings.Save(); }
        }

        [SettingPropertyGroup("Stamina Costs")]
        [SettingPropertyFloatingInteger("Sprint Stamina Drain Move Speed", 0.1f, 1f, HintText = "The move speed percentage above which unmounted agents lose stamina from sprinting")]
        public float MinimumMoveSpeedPercentStaminaDrains
        {
            get => StaminaSettings.Instance.MinimumMoveSpeedPercentStaminaDrains;
            set { StaminaSettings.Instance.MinimumMoveSpeedPercentStaminaDrains = value; StaminaSettings.Save(); }
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
