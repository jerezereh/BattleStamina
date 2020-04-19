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

        public int StaminaGainedPerAthletics { get; set; } = 10;
        public int StaminaGainedPerCombatSkill { get; set; } = 5;
        public int StaminaGainedPerLevel { get; set; } = 10;

        public int StaminaCostToMeleeAttack { get; set; } = 10;
        public int StaminaCostToRangedAttack { get; set; } = 10;
        public int StaminaCostToBlock { get; set; } = 5;
        public int StaminaCostPerReceivedDamage { get; set; } = 1;
        //public readonly double MoveCost { get; set; } = 0.5;

        public double LowestSpeedFromStaminaDebuff { get; set; } = 0.5;
        public double StaminaRecoveredPerTick { get; set; } = 0.1;
        
        public double HighStaminaRemaining { get; set; } = 0.75;
        public double MediumStaminaRemaining { get; set; } = 0.5;
        public double LowStaminaRemaining { get; set; } = 0.25;
    }
}
