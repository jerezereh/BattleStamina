using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BattleStamina
{
    public class StaminaProperties
    {
        internal static StaminaProperties Instance { get; set; }

        public int BaseStaminaValue { get; set; } = 100;
        public int StaminaGainedPerAthletics { get; set; } = 5;
        public int StaminaGainedPerCombatSkill { get; set; } = 1;
        public int StaminaGainedPerLevel { get; set; } = 5;

        public int StaminaCostToMeleeAttack { get; set; } = 10;
        public int StaminaCostToRangedAttack { get; set; } = 10;
        public int StaminaCostToBlock { get; set; } = 5;
        public int StaminaCostPerReceivedDamage { get; set; } = 1;
        //public readonly double MoveCost { get; set; } = 0.5;

        public double LowestSpeedFromStaminaDebuff { get; set; } = 0.5;
        public double StaminaRecoveredPerTick { get; set; } = 0.08;
        public double SecondsBeforeStaminaRegenerates { get; set; } = 5;
        public double MaximumMoveSpeedPercentStaminaRegenerates { get; set; } = 0.1;

        public double FullStaminaRemaining { get; set; } = 1.0;
        public double HighStaminaRemaining { get; set; } = 0.75;
        public double MediumStaminaRemaining { get; set; } = 0.5;
        public double LowStaminaRemaining { get; set; } = 0.25;
        public double NoStaminaRemaining { get; set; } = 0.01;

        public bool NoStaminaRemainingStopsAttacks { get; set; } = false;
    }
}
