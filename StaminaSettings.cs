using System;
using System.IO;
using System.Xml.Serialization;
using TaleWorlds.Library;

namespace BattleStamina
{
    public class StaminaSettings
    {
        private const string SettingsFileName = "BattleStaminaSettings.xml";

        public static StaminaSettings Instance { get; private set; } = new StaminaSettings();

        public int BaseStaminaValue { get; set; } = 600;
        public float StaminaGainedPerAthletics { get; set; } = 3.0f;
        public float StaminaGainedPerCombatSkill { get; set; } = 1.0f;
        public int StaminaGainedPerLevel { get; set; } = 10;
        public int StaminaCostToMeleeAttack { get; set; } = 8;
        public int StaminaCostToRangedAttack { get; set; } = 8;
        public float StaminaCostPerWeaponWeight { get; set; } = 3.0f;
        public float StaminaCostPerRangedMissileSpeed { get; set; } = 0.08f;
        public float StaminaCostToMeleeHit { get; set; } = 3.0f;
        public float StaminaCostPerMeleeHitSpeedModifier { get; set; } = 6.0f;
        public float StaminaCostToMeleeShieldImpact { get; set; } = 4.0f;
        public float StaminaCostPerBlockedDamage { get; set; } = 0.15f;
        public float StaminaCostPerReceivedDamage { get; set; } = 1.5f;
        public float StaminaCostPerSecondSprinting { get; set; } = 1.0f;
        public float StaminaCostPerSecondSprintingPerCarriedWeight { get; set; } = 0.08f;
        public float LowestSpeedFromStaminaDebuff { get; set; } = 0.35f;
        public float StaminaRecoveredPerTickMoving { get; set; } = 0.015f;
        public float StaminaRecoveredPerTickResting { get; set; } = 0.04f;
        public float SecondsBeforeStaminaRegenerates { get; set; } = 6.0f;
        public float MaximumMoveSpeedPercentStaminaRegenerates { get; set; } = 0.3f;
        public float MinimumMoveSpeedPercentStaminaDrains { get; set; } = 0.75f;
        public float FullStaminaRemaining { get; set; } = 1.0f;
        public float HighStaminaRemaining { get; set; } = 0.75f;
        public float MediumStaminaRemaining { get; set; } = 0.5f;
        public float LowStaminaRemaining { get; set; } = 0.10f;
        public float NoStaminaRemaining { get; set; } = 0.01f;
        public bool NoStaminaRemainingStopsAttacks { get; set; } = false;
        public bool StaminaAffectsCrushThrough { get; set; } = true;

        public static void Load()
        {
            string path = GetSettingsPath();

            if (!File.Exists(path))
            {
                Instance = new StaminaSettings();
                Save();
                return;
            }

            try
            {
                using (var stream = File.OpenRead(path))
                {
                    var serializer = new XmlSerializer(typeof(StaminaSettings));
                    Instance = (StaminaSettings)serializer.Deserialize(stream);
                }
            }
            catch (Exception exception)
            {
                Instance = new StaminaSettings();
                InformationManager.DisplayMessage(new InformationMessage(
                    "BattleStamina failed to read settings XML; using defaults. " + exception.Message,
                    Color.FromUint(0xDD5454FF)));
            }
        }

        public static void Save()
        {
            string path = GetSettingsPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (var stream = File.Create(path))
            {
                var serializer = new XmlSerializer(typeof(StaminaSettings));
                serializer.Serialize(stream, Instance);
            }
        }

        private static string GetSettingsPath()
        {
            return Path.Combine(BasePath.Name, "Modules", "BattleStamina", "ModuleData", SettingsFileName);
        }
    }
}
