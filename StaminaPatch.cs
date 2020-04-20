using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;

namespace BattleStamina.Patches
{
    [HarmonyPatch(typeof(MissionState), "OnTick")]
    class MissionOnTickPatch
    {
        public static Dictionary<Agent, int> AgentRecoveryTimers = new Dictionary<Agent, int>();

        public static void Postfix(MissionState __instance)
        {
            if (__instance.CurrentMission != null && __instance.CurrentMission.CurrentState == Mission.State.Continuing && !__instance.Paused)
            {
                MissionBuildAgentPatch.MaxStaminaPerAgent.Keys.Except(AgentInitializeMissionEquipmentPatch.AgentsToBeUpdated.Select(t => t.Item1).ToList())
                           .Where(x => x.IsActive() && x.Character != null && x.GetCurrentVelocity().Length <= x.GetMaximumSpeedLimit() * StaminaProperties.Instance.MaximumMoveSpeedPercentStaminaRegenerates)
                           .ToList().ForEach(o => AgentRecoveryTimers[o]++);

                // each tick update first X agents that need to be updated
                for (int i = 0; i < 10; i++)
                {
                    if (!AgentInitializeMissionEquipmentPatch.AgentsToBeUpdated.IsEmpty())
                    {
                        Tuple<Agent, double> tuple = AgentInitializeMissionEquipmentPatch.AgentsToBeUpdated.Dequeue();
                        if (tuple.Item1.IsActive())
                        {
                            ChangeWeaponSpeeds(tuple.Item1, tuple.Item2);
                            ChangeMoveSpeed(tuple.Item1, tuple.Item2);
                        }
                    }
                }

                foreach (Agent agent in AgentRecoveryTimers.Keys)
                {
                    if (AgentRecoveryTimers[agent] > 60 * StaminaProperties.Instance.SecondsBeforeStaminaRegenerates
                        && AgentInitializeMissionEquipmentPatch.CurrentStaminaPerAgent[agent] / MissionBuildAgentPatch.MaxStaminaPerAgent[agent] < StaminaProperties.Instance.HighStaminaRemaining)
                    {
                        double newStamina = AgentInitializeMissionEquipmentPatch.CurrentStaminaPerAgent[agent] + StaminaProperties.Instance.StaminaRecoveredPerTick;
                        AgentInitializeMissionEquipmentPatch.UpdateStamina(agent, newStamina, true);
                    }
                }
            }
            else if (__instance.CurrentMission != null && __instance.CurrentMission.CurrentState == Mission.State.Over)
            {
                ClearData();
            }
        }

        public static void ChangeWeaponSpeeds(Agent agent, double speedMultiplier)
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
                        (int)Math.Round(AgentInitializeMissionEquipmentPatch.AgentOriginalWeaponSpeed[agent][i].Item1 * speedMultiplier, MidpointRounding.AwayFromZero),
                        BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);

                    property = typeof(WeaponComponentData).GetProperty("ThrustSpeed");
                    property.DeclaringType.GetProperty("ThrustSpeed");
                    property.SetValue(agent.Equipment[i].PrimaryItem.PrimaryWeapon,
                        (int)Math.Round(AgentInitializeMissionEquipmentPatch.AgentOriginalWeaponSpeed[agent][i].Item2 * speedMultiplier, MidpointRounding.AwayFromZero),
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

        public static void ChangeMoveSpeed(Agent agent, double speedMultiplier)
        {
            agent.SetMaximumSpeedLimit((float)speedMultiplier, true);
        }

        private static void ClearData()
        {
            AgentRecoveryTimers.Clear();
            AgentInitializeMissionEquipmentPatch.AgentOriginalWeaponSpeed.Clear();
            AgentInitializeMissionEquipmentPatch.AgentsToBeUpdated.Clear();
            AgentInitializeMissionEquipmentPatch.CurrentStaminaPerAgent.Clear();
            MissionBuildAgentPatch.MaxStaminaPerAgent.Clear();
        }
    }


    [HarmonyPatch(typeof(Agent), "OnItemPickup")]
    class AgentOnItemPickupPatch
    {
        public static void Postfix(Agent __instance,
      SpawnedItemEntity spawnedItemEntity,
      EquipmentIndex weaponPickUpSlotIndex,
      bool removeWeapon)
        {
            AgentInitializeMissionEquipmentPatch.AgentOriginalWeaponSpeed[__instance][(int)weaponPickUpSlotIndex] = new Tuple<int, int>(spawnedItemEntity.WeaponCopy.PrimaryItem.PrimaryWeapon.SwingSpeed, spawnedItemEntity.WeaponCopy.PrimaryItem.PrimaryWeapon.ThrustSpeed);
            MissionOnTickPatch.ChangeWeaponSpeeds(__instance, AgentInitializeMissionEquipmentPatch.CurrentStaminaPerAgent[__instance] / MissionBuildAgentPatch.MaxStaminaPerAgent[__instance]);
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
                if (itemFromWeaponKind != null && itemFromWeaponKind.PrimaryWeapon.IsConsumable)
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


    [HarmonyPatch(typeof(Mission), "SpawnAgent")]
    class MissionBuildAgentPatch
    {
        public static Dictionary<Agent, double> MaxStaminaPerAgent = new Dictionary<Agent, double>();
        public static Agent heroAgent;

        public static void Postfix(Mission __instance,
      AgentBuildData agentBuildData,
      bool spawnFromAgentVisuals,
      int formationTroopCount,
      ref Agent __result)
        {
            if (agentBuildData != null)
            {
                double fullStamina = StaminaProperties.Instance.BaseStaminaValue +
                    agentBuildData.AgentCharacter.GetSkillValue(DefaultSkills.Athletics) * StaminaProperties.Instance.StaminaGainedPerAthletics +
                    agentBuildData.AgentCharacter.GetSkillValue(DefaultSkills.OneHanded) * StaminaProperties.Instance.StaminaGainedPerCombatSkill +
                    agentBuildData.AgentCharacter.GetSkillValue(DefaultSkills.TwoHanded) * StaminaProperties.Instance.StaminaGainedPerCombatSkill +
                    agentBuildData.AgentCharacter.GetSkillValue(DefaultSkills.Polearm) * StaminaProperties.Instance.StaminaGainedPerCombatSkill +
                    agentBuildData.AgentCharacter.GetSkillValue(DefaultSkills.Bow) * StaminaProperties.Instance.StaminaGainedPerCombatSkill +
                    agentBuildData.AgentCharacter.GetSkillValue(DefaultSkills.Crossbow) * StaminaProperties.Instance.StaminaGainedPerCombatSkill +
                    agentBuildData.AgentCharacter.GetSkillValue(DefaultSkills.Throwing) * StaminaProperties.Instance.StaminaGainedPerCombatSkill +
                    agentBuildData.AgentCharacter.Level * StaminaProperties.Instance.StaminaGainedPerLevel;

                MaxStaminaPerAgent.Add(__result, fullStamina);
                AgentInitializeMissionEquipmentPatch.CurrentStaminaPerAgent.Add(__result, fullStamina);
            }
            MissionOnTickPatch.AgentRecoveryTimers.Add(__result, 0);

            if (__result.IsPlayerControlled)
                heroAgent = __result;
        }
    }


    [HarmonyPatch(typeof(Agent), "InitializeMissionEquipment")]
    class AgentInitializeMissionEquipmentPatch
    {
        public static Dictionary<Agent, List<Tuple<int, int>>> AgentOriginalWeaponSpeed = new Dictionary<Agent, List<Tuple<int, int>>>();
        public static Dictionary<Agent, double> CurrentStaminaPerAgent = new Dictionary<Agent, double>();
        public static Queue<Tuple<Agent, double>> AgentsToBeUpdated = new Queue<Tuple<Agent, double>>();

        public static bool BelowFullStamina = false;
        public static bool BelowHighStamina = false;
        public static bool BelowMediumStamina = false;
        public static bool BelowLowStamina = false;
        public static bool NoStamina = false;

        public static void Postfix(Agent __instance)
        {
            List<Tuple<int, int>> equipmentList = new List<Tuple<int, int>>();
            // only adds one weapon per agent to AgentOriginalWeaponSpeed dictionary
            for (int i = 0; i < 4; i++)
            {
                if (!(__instance.Equipment[i].CurrentUsageItem == null))
                    equipmentList.Add(new Tuple<int, int>(__instance.Equipment[i].PrimaryItem.PrimaryWeapon.SwingSpeed, __instance.Equipment[i].PrimaryItem.PrimaryWeapon.ThrustSpeed));
                else
                    equipmentList.Add(new Tuple<int, int>(0, 0));
            }

            AgentOriginalWeaponSpeed.Add(__instance, equipmentList);
        }

        public static void UpdateStamina(Agent agent, double newStamina, bool recovering = false)
        {
            double oldStaminaRatio = CurrentStaminaPerAgent[agent] / MissionBuildAgentPatch.MaxStaminaPerAgent[agent];
            double newStaminaRatio;
            bool changedTier;

            if (!agent.IsActive())
                return;

            if (recovering)
            {
                CurrentStaminaPerAgent[agent] = newStamina / MissionBuildAgentPatch.MaxStaminaPerAgent[agent] > StaminaProperties.Instance.HighStaminaRemaining ? 
                    (StaminaProperties.Instance.HighStaminaRemaining * MissionBuildAgentPatch.MaxStaminaPerAgent[agent]) : newStamina;
                newStaminaRatio = newStamina / MissionBuildAgentPatch.MaxStaminaPerAgent[agent];
                changedTier = GetTierChanged(oldStaminaRatio, newStaminaRatio, recovering);

                if (agent.IsPlayerControlled)
                {
                    if (newStaminaRatio > StaminaProperties.Instance.NoStaminaRemaining && NoStamina)
                    {
                        Print("You manage to catch your breath! You're feeling tired!", new Color(1.00f, 0.38f, 0.01f)); // orange
                        NoStamina = false;
                    }
                    else if (newStaminaRatio > StaminaProperties.Instance.LowStaminaRemaining && BelowLowStamina)
                    {
                        Print("Your heart calms! You're feeling winded!", new Color(1.0f, 0.83f, 0.0f)); // yellow
                        BelowLowStamina = false;
                    }
                    else if (newStaminaRatio > StaminaProperties.Instance.MediumStaminaRemaining && BelowMediumStamina)
                    {
                        Print("You're feeling warmed up!", new Color(0.91f, 0.84f, 0.42f)); // pale yellow
                        BelowMediumStamina = false;
                    }
                    else if (newStaminaRatio > StaminaProperties.Instance.HighStaminaRemaining && BelowHighStamina)
                    {
                        Print("You're feeling fresh!", new Color(0.64f, 0.78f, 0.22f)); // green
                        BelowHighStamina = false;
                    }
                }
            }

            else
            {
                CurrentStaminaPerAgent[agent] = newStamina > StaminaProperties.Instance.NoStaminaRemaining ? newStamina : StaminaProperties.Instance.NoStaminaRemaining;
                newStaminaRatio = newStamina / MissionBuildAgentPatch.MaxStaminaPerAgent[agent];
                changedTier = GetTierChanged(oldStaminaRatio, newStaminaRatio, recovering);

                if (agent.IsPlayerControlled)
                {
                    if (newStaminaRatio <= StaminaProperties.Instance.NoStaminaRemaining && !NoStamina)
                    {
                        Print("You're completely exhausted! Rest now!", new Color(0.55f, 0, 0)); // red
                        NoStamina = true;
                    }
                    else if (newStaminaRatio <= StaminaProperties.Instance.LowStaminaRemaining && !BelowLowStamina)
                    {
                        Print("You're feeling tired! Rest soon!", new Color(1.00f, 0.38f, 0.01f)); // orange
                        BelowLowStamina = true;
                    }
                    else if (newStaminaRatio <= StaminaProperties.Instance.MediumStaminaRemaining && !BelowMediumStamina)
                    {
                        Print("You're feeling winded! You're attacks are slowing!", new Color(1.0f, 0.83f, 0.0f)); // yellow
                        BelowMediumStamina = true;
                    }
                    else if (newStaminaRatio <= StaminaProperties.Instance.HighStaminaRemaining && !BelowHighStamina)
                    {
                        Print("You're feeling warmed up!", new Color(0.91f, 0.84f, 0.42f)); // pale yellow
                        BelowHighStamina = true;
                    }
                    else if (newStaminaRatio <= StaminaProperties.Instance.FullStaminaRemaining && !BelowFullStamina)
                    {
                        Print("You're feeling fresh!", new Color(0.64f, 0.78f, 0.22f)); // green
                        BelowFullStamina = true;
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
                if (oldStaminaRatio <= StaminaProperties.Instance.NoStaminaRemaining && newStaminaRatio > StaminaProperties.Instance.NoStaminaRemaining)
                    return true;
                if (oldStaminaRatio <= StaminaProperties.Instance.LowStaminaRemaining && newStaminaRatio > StaminaProperties.Instance.LowStaminaRemaining)
                    return true;
                if (oldStaminaRatio <= StaminaProperties.Instance.MediumStaminaRemaining && newStaminaRatio > StaminaProperties.Instance.MediumStaminaRemaining)
                    return true;
                if (oldStaminaRatio <= StaminaProperties.Instance.HighStaminaRemaining && newStaminaRatio > StaminaProperties.Instance.HighStaminaRemaining)
                    return true;
            }

            else
            {
                if (oldStaminaRatio > StaminaProperties.Instance.NoStaminaRemaining && newStaminaRatio <= StaminaProperties.Instance.NoStaminaRemaining)
                    return true;
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


    [HarmonyPatch(typeof(Mission), "OnAgentRemoved")]
    class MissionOnAgentRemovedPatch
    {
        public static void Postfix(Mission __instance,
      Agent affectedAgent,
      Agent affectorAgent,
      AgentState agentState,
      KillingBlow killingBlow)
        {
            MissionOnTickPatch.AgentRecoveryTimers.Remove(affectedAgent);
            AgentInitializeMissionEquipmentPatch.AgentOriginalWeaponSpeed.Remove(affectedAgent);
            AgentInitializeMissionEquipmentPatch.CurrentStaminaPerAgent.Remove(affectedAgent);
            MissionBuildAgentPatch.MaxStaminaPerAgent.Remove(affectedAgent);
        }
    }

    
    [HarmonyPatch(typeof(BrushFactory), MethodType.Constructor, new Type[] { typeof(ResourceDepot), typeof(string), typeof(SpriteData), typeof(FontFactory) })]
    class DebugPatch
    {
        public static void Postfix(BrushFactory __instance,
      ResourceDepot resourceDepot,
      string resourceFolder,
      SpriteData spriteData,
      FontFactory fontFactory)
        {
            Console.WriteLine("");
        }
    }
}
