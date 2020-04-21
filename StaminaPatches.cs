using HarmonyLib;
using SandBox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem.SandBox.Source.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

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
                MissionSpawnAgentPatch.MaxStaminaPerAgent.Keys.Except(MissionSpawnAgentPatch.AgentsToBeUpdated.Select(t => t.Item1).ToList())
                           .Where(x => x.IsActive() && x.Character != null)
                           .ToList().ForEach(o => AgentRecoveryTimers[o]++);

                //if (MissionSpawnAgentPatch.heroAgent != null && MissionSpawnAgentPatch.heroAgent.IsActive())
                //{

                //}

                if (!MissionSpawnAgentPatch.AgentsToBeUpdated.IsEmpty())
                {
                    Tuple<Agent, double> tuple = MissionSpawnAgentPatch.AgentsToBeUpdated.Dequeue();
                    if (tuple.Item1.IsActive())
                    {
                        ChangeWeaponSpeeds(tuple.Item1, tuple.Item2);
                        ChangeMoveSpeed(tuple.Item1, tuple.Item2);
                    }
                }

                foreach (Agent agent in AgentRecoveryTimers.Keys.ToList())
                {
                    if (agent.GetCurrentVelocity().Length > agent.MaximumForwardUnlimitedSpeed * StaminaProperties.Instance.MaximumMoveSpeedPercentStaminaRegenerates)
                    {
                        AgentRecoveryTimers[agent] = 0;
                    }

                    if (AgentRecoveryTimers[agent] > 60 * StaminaProperties.Instance.SecondsBeforeStaminaRegenerates
                        && MissionSpawnAgentPatch.GetCurrentStaminaRatio(agent) < StaminaProperties.Instance.HighStaminaRemaining)
                    {
                        MissionSpawnAgentPatch.UpdateStaminaHandler(agent, StaminaProperties.Instance.StaminaRecoveredPerTick, true);
                    }
                }
            }
        }

        public static void ChangeWeaponSpeeds(Agent agent, double speedMultiplier)
        {
            if (agent != null && agent.IsActive())
            {
                speedMultiplier = (speedMultiplier * (1 - StaminaProperties.Instance.LowestSpeedFromStaminaDebuff)) + StaminaProperties.Instance.LowestSpeedFromStaminaDebuff;
                speedMultiplier = speedMultiplier > StaminaProperties.Instance.LowestSpeedFromStaminaDebuff ? speedMultiplier : StaminaProperties.Instance.LowestSpeedFromStaminaDebuff;

                for (int i = 0; i < 4; i++)
                {
                    if (agent.Equipment[i].CurrentUsageItem != null)
                    {
                        try
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
                        catch (InvalidOperationException)
                        {

                        }
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

        public static void ChangeMoveSpeed(Agent agent, double speedMultiplier, bool multiplier = false)
        {
            float newSpeed = agent.GetMaximumSpeedLimit() * (float)speedMultiplier;
            newSpeed = newSpeed > 0 ? newSpeed : 0;
            agent.SetMaximumSpeedLimit(newSpeed, multiplier);
        }

        public static void Cleanup()
        {
            if (MissionSpawnAgentPatch.heroAgent != null)
            {
                ChangeWeaponSpeeds(MissionSpawnAgentPatch.heroAgent, 1.0);
                ChangeMoveSpeed(MissionSpawnAgentPatch.heroAgent, MissionSpawnAgentPatch.heroAgent.MaximumForwardUnlimitedSpeed, false);
            }

            AgentRecoveryTimers.Clear();
            AgentInitializeMissionEquipmentPatch.AgentOriginalWeaponSpeed.Clear();
            MissionSpawnAgentPatch.AgentsToBeUpdated.Clear();
            MissionSpawnAgentPatch.CurrentStaminaPerAgent.Clear();
            MissionSpawnAgentPatch.MaxStaminaPerAgent.Clear();
            MissionSpawnAgentPatch.heroAgent = null;
        }
    }

    [HarmonyPatch(typeof(Mission), "EndMission")]
    class MissionOnEndMissionRequestPatch
    {
        public static void Postfix(Mission __instance)
        {
            MissionOnTickPatch.Cleanup();
        }
    }

    [HarmonyPatch(typeof(TournamentRound), "EndMatch")]
    class TournamentRoundEndMatchPatch
    {
        public static void Postfix(TournamentRound __instance)
        {
            MissionOnTickPatch.Cleanup();
        }
    }

    [HarmonyPatch(typeof(ArenaPracticeFightMissionController), "StartPlayerPractice")]
    class ArenaPracticeFightMissionControllerStartPlayerPracticePatch
    {
        public static void Postfix(ArenaPracticeFightMissionController __instance)
        {
            MissionOnTickPatch.Cleanup();
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
            MissionOnTickPatch.ChangeWeaponSpeeds(__instance, MissionSpawnAgentPatch.GetCurrentStaminaRatio(__instance));
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

            if (affectorAgent.Character != null && affectorAgent.IsActive())
            {
                ItemObject itemFromWeaponKind = ItemObject.GetItemFromWeaponKind(affectorWeaponKind);
                if (itemFromWeaponKind != null && itemFromWeaponKind.PrimaryWeapon.IsConsumable)
                {
                    MissionSpawnAgentPatch.UpdateStaminaHandler(affectedAgent, StaminaProperties.Instance.StaminaCostToRangedAttack);
                }
                else
                {
                    MissionSpawnAgentPatch.UpdateStaminaHandler(affectedAgent, StaminaProperties.Instance.StaminaCostToMeleeAttack);
                }
            }

            if (affectedAgent.Character != null && affectedAgent.IsActive())
            {
                if (isBlocked)
                {

                    MissionSpawnAgentPatch.UpdateStaminaHandler(affectedAgent, StaminaProperties.Instance.StaminaCostToBlock);
                }
                else
                {
                    MissionSpawnAgentPatch.UpdateStaminaHandler(affectedAgent, damage * StaminaProperties.Instance.StaminaCostPerReceivedDamage);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Mission), "SpawnAgent")]
    class MissionSpawnAgentPatch
    {
        public static Queue<Tuple<Agent, double>> AgentsToBeUpdated = new Queue<Tuple<Agent, double>>();
        public static Dictionary<Agent, double> MaxStaminaPerAgent = new Dictionary<Agent, double>();
        public static Dictionary<Agent, double> CurrentStaminaPerAgent = new Dictionary<Agent, double>();
        public static Agent heroAgent;

        public static bool BelowFullStamina = false;
        public static bool BelowHighStamina = false;
        public static bool BelowMediumStamina = false;
        public static bool BelowLowStamina = false;
        public static bool NoStamina = false;

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
                CurrentStaminaPerAgent.Add(__result, fullStamina);
            }
            MissionOnTickPatch.AgentRecoveryTimers.Add(__result, 0);

            if (__result.IsPlayerControlled)
            {
                heroAgent = __result;
            }
        }

        public static double GetCurrentStaminaRatio(Agent agent)
        {
            try
            {
                return CurrentStaminaPerAgent[agent] / MaxStaminaPerAgent[agent];
            }
            catch
            {
                return 0.0;
            }
        }

        public static void UpdateStaminaHandler(Agent agent, double staminaDelta, bool recovering = false)
        {
            try
            {
                double newStamina = recovering ? CurrentStaminaPerAgent[agent] + staminaDelta : CurrentStaminaPerAgent[agent] - staminaDelta;
                UpdateStamina(agent, newStamina, recovering);
            }
            catch (KeyNotFoundException)
            {

            }
        }

        private static void UpdateStamina(Agent agent, double newStamina, bool recovering = false)
        {
            double oldStaminaRatio = CurrentStaminaPerAgent[agent] / MaxStaminaPerAgent[agent];
            double newStaminaRatio;
            bool changedTier;

            if (!agent.IsActive())
                return;

            if (recovering)
            {
                CurrentStaminaPerAgent[agent] = newStamina / MaxStaminaPerAgent[agent] > StaminaProperties.Instance.HighStaminaRemaining ?
                    (StaminaProperties.Instance.HighStaminaRemaining * MaxStaminaPerAgent[agent]) : newStamina;
                newStaminaRatio = newStamina / MaxStaminaPerAgent[agent];
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
                newStaminaRatio = newStamina / MissionSpawnAgentPatch.MaxStaminaPerAgent[agent];
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

    [HarmonyPatch(typeof(Agent), "InitializeMissionEquipment")]
    class AgentInitializeMissionEquipmentPatch
    {
        public static Dictionary<Agent, List<Tuple<int, int>>> AgentOriginalWeaponSpeed = new Dictionary<Agent, List<Tuple<int, int>>>();

        public static void Postfix(Agent __instance)
        {
            List<Tuple<int, int>> equipmentList = new List<Tuple<int, int>>();
            for (int i = 0; i < 4; i++)
            {
                if (!(__instance.Equipment[i].CurrentUsageItem == null))
                    equipmentList.Add(new Tuple<int, int>(__instance.Equipment[i].PrimaryItem.PrimaryWeapon.SwingSpeed, __instance.Equipment[i].PrimaryItem.PrimaryWeapon.ThrustSpeed));
                else
                    equipmentList.Add(new Tuple<int, int>(0, 0));
            }

            AgentOriginalWeaponSpeed.Add(__instance, equipmentList);
        }
    }

    [HarmonyPatch(typeof(Mission), "OnAgentAddedAsCorpse")]
    class MissionOnAgentAddedAsCorpsePatch
    {
        public static void Postfix(Mission __instance, Agent affectedAgent)
        {
            // need to reset weapons' speeds on agent death
            if (affectedAgent != MissionSpawnAgentPatch.heroAgent)
            {
                AgentInitializeMissionEquipmentPatch.AgentOriginalWeaponSpeed.Remove(affectedAgent);
                MissionOnTickPatch.AgentRecoveryTimers.Remove(affectedAgent);
                MissionSpawnAgentPatch.CurrentStaminaPerAgent.Remove(affectedAgent);
                MissionSpawnAgentPatch.MaxStaminaPerAgent.Remove(affectedAgent);
            }
        }
    }
}
