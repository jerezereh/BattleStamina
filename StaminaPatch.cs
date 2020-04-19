using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BattleStamina.Patches
{
    [HarmonyPatch(typeof(Mission), "OnTick")]
    class MissionOnTickPatch
    {
        //public static Queue<Agent> AgentsRecoveringStamina = new Queue<Agent>();
        public static Dictionary<Agent, int> AgentRecoveryTimers = new Dictionary<Agent, int>();

        public static void Postfix(Mission __instance)
        {
            AgentInitializeMissionEquipmentPatch.CurrentStaminaPerAgent.Keys.Except(
                       AgentInitializeMissionEquipmentPatch.AgentsToBeUpdated.Select(t => t.Item1).ToList())
                       .Where(x => x.IsActive() && x.Character != null)
                       .ToList().ForEach(o => AgentRecoveryTimers[o]++);

            // each tick update first X agents that need to be updated
            for (int i = 0; i < 5; i++)
            {
                //if (!AgentsRecoveringStamina.IsEmpty())
                //{
                //    Agent agent = AgentsRecoveringStamina.Dequeue();
                //    double newStamina = AgentInitializeMissionEquipmentPatch.CurrentStaminaPerAgent[agent] + StaminaProperties.Instance.StaminaRecoveredPerTick;
                //    AgentInitializeMissionEquipmentPatch.UpdateStamina(agent, newStamina, true);
                //}

                if (!AgentInitializeMissionEquipmentPatch.AgentsToBeUpdated.IsEmpty())
                {
                    Tuple<Agent, double> tuple = AgentInitializeMissionEquipmentPatch.AgentsToBeUpdated.Dequeue();
                    ChangeWeaponSpeeds(tuple.Item1, tuple.Item2);
                }
            }

            foreach (Agent agent in AgentRecoveryTimers.Keys)
            {
                if (AgentRecoveryTimers[agent] > 300 && AgentInitializeMissionEquipmentPatch.CurrentStaminaPerAgent[agent] / MissionBuildAgentPatch.MaxStaminaPerAgent[agent] < 0.75)
                {
                    double newStamina = AgentInitializeMissionEquipmentPatch.CurrentStaminaPerAgent[agent] + StaminaProperties.Instance.StaminaRecoveredPerTick;
                    AgentInitializeMissionEquipmentPatch.UpdateStamina(agent, newStamina, true);
                }
            }
        }

        private static void ChangeWeaponSpeeds(Agent agent, double speedMultiplier)
        {
            speedMultiplier = (speedMultiplier * (1 - StaminaProperties.Instance.LowestSpeedFromStaminaDebuff)) + StaminaProperties.Instance.LowestSpeedFromStaminaDebuff;
            speedMultiplier = speedMultiplier > StaminaProperties.Instance.LowestSpeedFromStaminaDebuff ? speedMultiplier : StaminaProperties.Instance.LowestSpeedFromStaminaDebuff;

            for (int i = 0; i < 4; i++)
            {
                if (!(agent.Equipment[i].CurrentUsageItem == null))
                {
                    PropertyInfo property = typeof(WeaponComponentData).GetProperty("SwingSpeed");
                    property.DeclaringType.GetProperty("SwingSpeed");
                    property.SetValue(agent.Equipment[i].PrimaryItem.PrimaryWeapon,
                        (int)Math.Round(AgentInitializeMissionEquipmentPatch.AgentOriginalWeaponSpeed[agent].Item1 * speedMultiplier, MidpointRounding.AwayFromZero),
                        BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);

                    property = typeof(WeaponComponentData).GetProperty("ThrustSpeed");
                    property.DeclaringType.GetProperty("ThrustSpeed");
                    property.SetValue(agent.Equipment[i].PrimaryItem.PrimaryWeapon,
                        (int)Math.Round(AgentInitializeMissionEquipmentPatch.AgentOriginalWeaponSpeed[agent].Item2 * speedMultiplier, MidpointRounding.AwayFromZero),
                        BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);
                }
            }

            MissionWeapon equipped = agent.WieldedWeapon;
            MissionWeapon offhandEquipped = agent.WieldedOffhandWeapon;
            EquipmentIndex mainIndex = EquipmentIndex.None;
            EquipmentIndex offIndex = EquipmentIndex.None;
            if (equipped.CurrentUsageItem != null)
                mainIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            if (offhandEquipped.CurrentUsageItem != null)
                offIndex = agent.GetWieldedItemIndex(Agent.HandIndex.OffHand);

            agent.EquipItemsFromSpawnEquipment();
            if (mainIndex != EquipmentIndex.None && equipped.CurrentUsageItem != null)
                agent.TryToWieldWeaponInSlot(mainIndex, Agent.WeaponWieldActionType.Instant, true);
            if (offIndex != EquipmentIndex.None && offhandEquipped.CurrentUsageItem != null)
                agent.TryToWieldWeaponInSlot(offIndex, Agent.WeaponWieldActionType.Instant, true);
        }
    }


    [HarmonyPatch(typeof(Mission), "OnAgentHit")]
    class MissionOnAgentHitPatch
    {
        public static void Postfix(Mission __instance,
      Agent affectedAgent,
      Agent affectorAgent,
      int affectorWeaponKind,
      bool isBlocked,
      int damage)
        {
            MissionOnTickPatch.AgentRecoveryTimers[affectedAgent] = 0;
            MissionOnTickPatch.AgentRecoveryTimers[affectorAgent] = 0;

            if (affectorAgent.Character != null)
            {
                ItemObject itemFromWeaponKind = ItemObject.GetItemFromWeaponKind(affectorWeaponKind);
                if (itemFromWeaponKind.PrimaryWeapon.IsConsumable)
                {
                    double newStamina = AgentInitializeMissionEquipmentPatch.CurrentStaminaPerAgent[affectorAgent] - StaminaProperties.Instance.StaminaCostToRangedAttack;
                    AgentInitializeMissionEquipmentPatch.UpdateStamina(affectorAgent, newStamina);
                }
                else
                {
                    double newStamina = AgentInitializeMissionEquipmentPatch.CurrentStaminaPerAgent[affectorAgent] - StaminaProperties.Instance.StaminaCostToMeleeAttack;
                    AgentInitializeMissionEquipmentPatch.UpdateStamina(affectorAgent, newStamina);
                }
            }

            if (affectedAgent.Character != null)
            {
                if (isBlocked)
                {
                    double newStamina = AgentInitializeMissionEquipmentPatch.CurrentStaminaPerAgent[affectedAgent] - StaminaProperties.Instance.StaminaCostToBlock;
                    AgentInitializeMissionEquipmentPatch.UpdateStamina(affectedAgent, newStamina);
                }
                else
                {
                    double newStamina = AgentInitializeMissionEquipmentPatch.CurrentStaminaPerAgent[affectedAgent] - (damage * StaminaProperties.Instance.StaminaCostPerReceivedDamage);
                    AgentInitializeMissionEquipmentPatch.UpdateStamina(affectedAgent, newStamina);
                }
            }
        }
    }


    [HarmonyPatch(typeof(Mission), "BuildAgent")]
    class MissionBuildAgentPatch
    {
        public static Dictionary<Agent, double> MaxStaminaPerAgent = new Dictionary<Agent, double>();

        public static void Postfix(Mission __instance, Agent agent, AgentBuildData agentBuildData)
        {
            if (agentBuildData != null)
            {
                double fullStamina = agentBuildData.AgentCharacter.GetSkillValue(DefaultSkills.Athletics) * StaminaProperties.Instance.StaminaGainedPerAthletics +
                    agentBuildData.AgentCharacter.GetSkillValue(DefaultSkills.OneHanded) * StaminaProperties.Instance.StaminaGainedPerCombatSkill +
                    agentBuildData.AgentCharacter.GetSkillValue(DefaultSkills.TwoHanded) * StaminaProperties.Instance.StaminaGainedPerCombatSkill +
                    agentBuildData.AgentCharacter.GetSkillValue(DefaultSkills.Polearm) * StaminaProperties.Instance.StaminaGainedPerCombatSkill +
                    agentBuildData.AgentCharacter.GetSkillValue(DefaultSkills.Bow) * StaminaProperties.Instance.StaminaGainedPerCombatSkill +
                    agentBuildData.AgentCharacter.GetSkillValue(DefaultSkills.Crossbow) * StaminaProperties.Instance.StaminaGainedPerCombatSkill +
                    agentBuildData.AgentCharacter.GetSkillValue(DefaultSkills.Throwing) * StaminaProperties.Instance.StaminaGainedPerCombatSkill +
                    agentBuildData.AgentCharacter.Level * StaminaProperties.Instance.StaminaGainedPerLevel;

                MaxStaminaPerAgent.Add(agent, fullStamina);
                AgentInitializeMissionEquipmentPatch.CurrentStaminaPerAgent.Add(agent, fullStamina);
            }
            MissionOnTickPatch.AgentRecoveryTimers.Add(agent, 0);
        }
    }


    [HarmonyPatch(typeof(Agent), "InitializeMissionEquipment")]
    class AgentInitializeMissionEquipmentPatch
    {
        public static Dictionary<Agent, Tuple<int, int>> AgentOriginalWeaponSpeed = new Dictionary<Agent, Tuple<int, int>>();
        public static Dictionary<Agent, double> CurrentStaminaPerAgent = new Dictionary<Agent, double>();
        public static Queue<Tuple<Agent, double>> AgentsToBeUpdated = new Queue<Tuple<Agent, double>>();

        public static bool BelowHighStamina = false;
        public static bool BelowMediumStamina = false;
        public static bool BelowLowStamina = false;

        public static void Postfix(Agent __instance)
        {
            // only adds one weapon per agent
            for (int i = 0; i < 4; i++)
                if (!(__instance.Equipment[i].CurrentUsageItem == null) && !AgentOriginalWeaponSpeed.ContainsKey(__instance))
                    AgentOriginalWeaponSpeed.Add(__instance, new Tuple<int, int>(__instance.Equipment[i].PrimaryItem.PrimaryWeapon.SwingSpeed, __instance.Equipment[i].PrimaryItem.PrimaryWeapon.ThrustSpeed));
        }

        public static void UpdateStamina(Agent agent, double newStamina, bool recovering = false)
        {
            double oldStaminaRatio = CurrentStaminaPerAgent[agent] / MissionBuildAgentPatch.MaxStaminaPerAgent[agent];
            double newStaminaRatio;
            bool changedTier = false;

            if (!agent.IsActive())
                return;

            if (recovering)
            {
                CurrentStaminaPerAgent[agent] = newStamina / MissionBuildAgentPatch.MaxStaminaPerAgent[agent] > 0.75 ? (0.75 * MissionBuildAgentPatch.MaxStaminaPerAgent[agent]) : newStamina;
                newStaminaRatio = newStamina / MissionBuildAgentPatch.MaxStaminaPerAgent[agent];
                changedTier = GetTierChanged(oldStaminaRatio, newStaminaRatio, recovering);

                if (agent.IsPlayerControlled)
                {
                    if (newStaminaRatio > StaminaProperties.Instance.LowStaminaRemaining && BelowLowStamina)
                    {
                        Print("You're feeling winded! You're attacks are slowing!", new Color(1.00f, 0.38f, 0.01f)); // orange
                        BelowLowStamina = false;
                    }
                    else if (newStaminaRatio > StaminaProperties.Instance.MediumStaminaRemaining && BelowMediumStamina)
                    {
                        Print("You're feeling warmed up!", new Color(0.91f, 0.84f, 0.42f)); // yellow
                        BelowMediumStamina = false;
                    }
                    else if (newStaminaRatio > StaminaProperties.Instance.HighStaminaRemaining && BelowHighStamina)
                    {
                        Print("You're feeling fresh!", new Color(0.83f, 0.69f, 0.22f)); // gold
                        BelowHighStamina = false;
                    }
                }
            }

            else
            {
                CurrentStaminaPerAgent[agent] = newStamina > 0 ? newStamina : 0;
                newStaminaRatio = newStamina / MissionBuildAgentPatch.MaxStaminaPerAgent[agent];
                changedTier = GetTierChanged(oldStaminaRatio, newStaminaRatio, recovering);

                if (agent.IsPlayerControlled)
                {
                    if (newStaminaRatio <= StaminaProperties.Instance.LowStaminaRemaining && !BelowLowStamina)
                    {
                        Print("You're feeling exhausted! Rest soon!", new Color(0.6f, 0, 0)); // red
                        BelowLowStamina = true;
                    }
                    else if (newStaminaRatio <= StaminaProperties.Instance.MediumStaminaRemaining && !BelowMediumStamina)
                    {
                        Print("You're feeling winded! You're attacks are slowing!", new Color(1.00f, 0.38f, 0.01f)); // orange
                        BelowMediumStamina = true;
                    }
                    else if (newStaminaRatio <= StaminaProperties.Instance.HighStaminaRemaining && !BelowHighStamina)
                    {
                        Print("You're feeling warmed up!", new Color(0.91f, 0.84f, 0.42f)); // yellow
                        BelowHighStamina = true;
                    }
                }
            }

            if (changedTier)
                AgentsToBeUpdated.Enqueue(new Tuple<Agent, double>(agent, newStaminaRatio));
        }

        private static bool GetTierChanged(double oldStaminaRatio, double newStaminaRatio, bool recovering)
        {
            if (recovering)
            {
                if (oldStaminaRatio <= StaminaProperties.Instance.LowStaminaRemaining && newStaminaRatio > StaminaProperties.Instance.LowStaminaRemaining)
                    return true;
                if (oldStaminaRatio <= StaminaProperties.Instance.MediumStaminaRemaining && newStaminaRatio > StaminaProperties.Instance.MediumStaminaRemaining)
                    return true;
                if (oldStaminaRatio <= StaminaProperties.Instance.HighStaminaRemaining && newStaminaRatio > StaminaProperties.Instance.HighStaminaRemaining)
                    return true;
            }

            else
            {
                if (oldStaminaRatio > StaminaProperties.Instance.LowStaminaRemaining && newStaminaRatio <= StaminaProperties.Instance.LowStaminaRemaining)
                    return true;
                if (oldStaminaRatio > StaminaProperties.Instance.MediumStaminaRemaining && newStaminaRatio <= StaminaProperties.Instance.MediumStaminaRemaining)
                    return true;
                if (oldStaminaRatio > StaminaProperties.Instance.HighStaminaRemaining && newStaminaRatio <= StaminaProperties.Instance.HighStaminaRemaining)
                    return true;
            }
            return false;
        }

        private static void Print(string message, Color color)
        {
            InformationManager.DisplayMessage(new InformationMessage(message, color, "Combat"));
        }
    }
}
