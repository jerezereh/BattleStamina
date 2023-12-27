﻿#undef DEBUG

using HarmonyLib;
using SandBox.GameComponents;
using SandBox.Missions.MissionLogics.Arena;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Engine.Options;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BattleStamina.Patches
{
    [HarmonyPatch(typeof(MissionState), "TickMission")]
    class MissionOnTickPatch
    {
        public static Dictionary<Agent, int> AgentRecoveryTimers = new Dictionary<Agent, int>();

        public static void Postfix(MissionState __instance)
        {
            if (__instance.CurrentMission != null && __instance.CurrentMission.CurrentState == Mission.State.Continuing && !__instance.Paused)
            {
                __instance.CurrentMission.Agents.Except(MissionSpawnAgentPatch.AgentsToBeUpdated.Select(t => t.Item1).ToList())
                    .Where(x => x.IsActive() && x.Character != null)
                    .ToList().ForEach(o => AgentRecoveryTimers[o]++);

                if (!MissionSpawnAgentPatch.AgentsToBeUpdated.IsEmpty())
                {
                    Tuple<Agent, double> tuple = MissionSpawnAgentPatch.AgentsToBeUpdated.Dequeue();

                    if (tuple.Item1.IsActive())
                    {
                        ChangeWeaponSpeedsHandler(tuple.Item1, tuple.Item2);
                        //ChangeMoveSpeed(tuple.Item1, tuple.Item2);
                    }
                }

                foreach (Agent agent in AgentRecoveryTimers.Keys.ToList())
                {
                    // frame rate dependent, might be bugged
                    if (AgentRecoveryTimers[agent] > AgentInitializeMissionEquipmentPatch.frameRate * StaminaProperties.Instance.SecondsBeforeStaminaRegenerates)
                    {
                        //AgentRecoveryTimers[agent] = 0;

                        if (agent.GetCurrentVelocity().Length > agent.MaximumForwardUnlimitedSpeed * StaminaProperties.Instance.MaximumMoveSpeedPercentStaminaRegenerates)
                            MissionSpawnAgentPatch.UpdateStaminaHandler(agent, StaminaProperties.Instance.StaminaRecoveredPerTickMoving, true);
                        else
                            MissionSpawnAgentPatch.UpdateStaminaHandler(agent, StaminaProperties.Instance.StaminaRecoveredPerTickResting, true);
                    }
                }
            }
        }

        public static void ChangeWeaponSpeedsHandler(Agent agent, double speedMultiplier, bool requip = true)
        {
            // get currently wielded weapon(s) and index(es)
            MissionWeapon equipped = agent.WieldedWeapon;
            MissionWeapon offhandEquipped = agent.WieldedOffhandWeapon;
            EquipmentIndex mainIndex = EquipmentIndex.None;
            EquipmentIndex offIndex = EquipmentIndex.None;
            if (equipped.CurrentUsageItem != null)
                mainIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            if (offhandEquipped.CurrentUsageItem != null)
                offIndex = agent.GetWieldedItemIndex(Agent.HandIndex.OffHand);

            for (int i = 0; i < 4; i++)
            {
                MissionWeapon currentWeapon = agent.Equipment[i];

                if (currentWeapon.CurrentUsageItem != null && !currentWeapon.CurrentUsageItem.IsRangedWeapon) // && !currentWeapon.CurrentUsageItem.IsRangedWeapon -- checking to ignore ranged weapons?
                {
                    // normalize speed multiplier between 100% and minimum set in properties and ensure speed multiplier above minimum
                    speedMultiplier = (speedMultiplier * (1 - StaminaProperties.Instance.LowestSpeedFromStaminaDebuff)) + StaminaProperties.Instance.LowestSpeedFromStaminaDebuff;
                    double newSwingSpeed = AgentInitializeMissionEquipmentPatch.AgentOriginalWeaponSpeed[agent.Index][i].Item1 * speedMultiplier;
                    double newThrustSpeed = AgentInitializeMissionEquipmentPatch.AgentOriginalWeaponSpeed[agent.Index][i].Item2 * speedMultiplier;

                    ChangeWeaponSpeed(agent, i, newSwingSpeed, newThrustSpeed, currentWeapon);
                    if (requip)
                    {
                        RequipWeapon(agent, i, currentWeapon, mainIndex, offIndex);
                    }
                }
            }
            
        }

        public static void ChangeWeaponSpeed(Agent agent, int i, double newSwingSpeed, double newThrustSpeed, MissionWeapon weapon)
        {
            if (agent != null && weapon.CurrentUsageItem != null)
            {
                try
                {
                    PropertyInfo property = typeof(WeaponComponentData).GetProperty("SwingSpeed");
                    property.DeclaringType.GetProperty("SwingSpeed");
                    property.SetValue(weapon.CurrentUsageItem,
                        (int)Math.Round(newSwingSpeed, MidpointRounding.AwayFromZero),
                        BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);
#if DEBUG
                    //InformationManager.DisplayMessage(new InformationMessage("Actor: " + agent.Name + "; Weapon: " + weapon.Item.Name + "; New speed: " + property.GetValue(weapon.CurrentUsageItem), new Color(1.00f, 0.38f, 0.01f), "Debug"));
#endif
                    property = typeof(WeaponComponentData).GetProperty("ThrustSpeed");
                    property.DeclaringType.GetProperty("ThrustSpeed");
                    property.SetValue(weapon.CurrentUsageItem,
                        (int)Math.Round(newThrustSpeed, MidpointRounding.AwayFromZero),
                        BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);

                }
                catch (InvalidOperationException)
                {
#if DEBUG
                    InformationManager.DisplayMessage(new InformationMessage("Caught InvalidOperationException in ChangeWeaponSpeeds!", new Color(1.00f, 0.38f, 0.01f), "Debug"));
#endif
                }
                catch (KeyNotFoundException)
                {
#if DEBUG
                    InformationManager.DisplayMessage(new InformationMessage("Caught KeyNotFoundException in ChangeWeaponSpeeds!", new Color(1.00f, 0.38f, 0.01f), "Debug"));
#endif
                }
                catch (Exception)
                {
#if DEBUG
                    InformationManager.DisplayMessage(new InformationMessage("Unexpected exception" + e + " in ChangeWeaponSpeeds!", new Color(1.00f, 0.38f, 0.01f), "Debug"));
#endif
                }
            }
        }

        public static void RequipWeapon(Agent agent, int index, MissionWeapon weapon, EquipmentIndex mainIndex, EquipmentIndex offIndex)
        {
            if (agent != null && agent.Health > 0 && weapon.Item != null)
            {
                // reset weapon to set new speed
                if (weapon.Item != null)
                {
                    agent.EquipWeaponWithNewEntity((EquipmentIndex)index, ref weapon);
                }

                // if weapon was currently equipped, re-equip it
                if (mainIndex != EquipmentIndex.None && index == (int)mainIndex)
                {
                    agent.TryToWieldWeaponInSlot(mainIndex, Agent.WeaponWieldActionType.Instant, true);
                }
                if (offIndex != EquipmentIndex.None && index == (int)offIndex)
                {
                    agent.TryToWieldWeaponInSlot(offIndex, Agent.WeaponWieldActionType.Instant, true);
                }
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
            Agent agent = MissionSpawnAgentPatch.heroAgent;

            if (agent != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    MissionWeapon currentWeapon = agent.Equipment[i];

                    if (currentWeapon.Item != null)
                    {
                        // normalize speed multiplier between 100% and minimum set in properties and ensure speed multiplier above minimum
                        double newSwingSpeed = AgentInitializeMissionEquipmentPatch.AgentOriginalWeaponSpeed[agent.Index][i].Item1;
                        double newThrustSpeed = AgentInitializeMissionEquipmentPatch.AgentOriginalWeaponSpeed[agent.Index][i].Item2;

                        ChangeWeaponSpeed(agent, i, newSwingSpeed, newThrustSpeed, currentWeapon);
                    }
                }

                //ChangeMoveSpeed(MissionSpawnAgentPatch.heroAgent, MissionSpawnAgentPatch.heroAgent.MaximumForwardUnlimitedSpeed, false);
            }

            AgentRecoveryTimers.Clear();
            AgentInitializeMissionEquipmentPatch.AgentOriginalWeaponSpeed.Clear();
            MissionSpawnAgentPatch.AgentsToBeUpdated.Clear();
            MissionSpawnAgentPatch.CurrentStaminaPerAgent.Clear();
            MissionSpawnAgentPatch.CurrentMaxStaminaPerAgent.Clear();
            MissionSpawnAgentPatch.OriginalMaxStaminaPerAgent.Clear();
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

    [HarmonyPatch(typeof(Agent), "OnItemPickup")]
    class AgentOnItemPickupPatch
    {
        public static void Postfix(Agent __instance,
      SpawnedItemEntity spawnedItemEntity,
      EquipmentIndex weaponPickUpSlotIndex,
      bool removeWeapon)
        {
            try
            {
                AgentInitializeMissionEquipmentPatch.AgentOriginalWeaponSpeed[__instance.Index][(int)weaponPickUpSlotIndex] = 
                    new Tuple<int, int>(spawnedItemEntity.WeaponCopy.CurrentUsageItem.SwingSpeed, spawnedItemEntity.WeaponCopy.CurrentUsageItem.ThrustSpeed);

                MissionOnTickPatch.ChangeWeaponSpeedsHandler(__instance, MissionSpawnAgentPatch.GetCurrentStaminaRatio(__instance));
            }
            catch (KeyNotFoundException)
            {
#if DEBUG
                InformationManager.DisplayMessage(new InformationMessage("Caught KeyNotFoundException in AgentOnItemPickupPatch!", new Color(1.00f, 0.38f, 0.01f), "Debug"));
#endif
            }
        }
    }

    [HarmonyPatch(typeof(Mission), "OnAgentHit")]
    class MissionOnAgentHitPatch
    {
        public static void Postfix(Mission __instance,
      Agent affectedAgent,
      Agent affectorAgent,
      in Blow b,
      in AttackCollisionData collisionData,
      bool isBlocked,
      float damagedHp)
        {
            if (affectorAgent.Character != null && affectorAgent.IsActive())
            {
                MissionOnTickPatch.AgentRecoveryTimers[affectorAgent] = 0;

                if (b.IsMissile)
                {
                    MissionSpawnAgentPatch.UpdateStaminaHandler(affectorAgent, StaminaProperties.Instance.StaminaCostToRangedAttack);
                }
                else
                {
                    MissionSpawnAgentPatch.UpdateStaminaHandler(affectorAgent, StaminaProperties.Instance.StaminaCostToMeleeAttack);
                }
            }

            if (affectedAgent.Character != null && affectedAgent.IsActive() && affectedAgent.Health - damagedHp > 0)
            {
                MissionOnTickPatch.AgentRecoveryTimers[affectedAgent] = 0;

                if (isBlocked)
                {

                    MissionSpawnAgentPatch.UpdateStaminaHandler(affectedAgent, damagedHp * StaminaProperties.Instance.StaminaCostPerBlockedDamage);
                }
                else
                {
                    MissionSpawnAgentPatch.UpdateStaminaHandler(affectedAgent, damagedHp * StaminaProperties.Instance.StaminaCostPerReceivedDamage);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Mission), "SpawnAgent")]
    class MissionSpawnAgentPatch
    {
        public static Queue<Tuple<Agent, double>> AgentsToBeUpdated = new Queue<Tuple<Agent, double>>();
        public static Dictionary<int, double> OriginalMaxStaminaPerAgent = new Dictionary<int, double>();
        public static Dictionary<int, double> CurrentMaxStaminaPerAgent = new Dictionary<int, double>();
        public static Dictionary<int, double> CurrentStaminaPerAgent = new Dictionary<int, double>();
        public static Agent heroAgent;

        public static bool FullStaminaTier = true;
        public static bool HighStaminaTier = false;
        public static bool MediumStaminaTier = false;
        public static bool LowStaminaTier = false;
        public static bool NoStaminaTier = false;

        public static void Postfix(Mission __instance,
      AgentBuildData agentBuildData,
      bool spawnFromAgentVisuals,
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

                OriginalMaxStaminaPerAgent.Add(__result.Index, fullStamina);
                CurrentMaxStaminaPerAgent.Add(__result.Index, fullStamina);
                CurrentStaminaPerAgent.Add(__result.Index, fullStamina);
            }
#if DEBUG
                InformationManager.DisplayMessage(new InformationMessage("Agent " + __result.Name + " has " + CurrentStaminaPerAgent[__result.Index] + " stamina" , new Color(1.00f, 0.38f, 0.01f), "Debug"));
#endif

            MissionOnTickPatch.AgentRecoveryTimers.Add(__result, 0);

            if (__result.IsPlayerUnit)
            {
                heroAgent = __result;
            }
        }

        public static double GetCurrentStaminaRatio(Agent agent)
        {
            try
            {
                return CurrentStaminaPerAgent[agent.Index] / OriginalMaxStaminaPerAgent[agent.Index];
            }
            catch (KeyNotFoundException)
            {
#if DEBUG
                InformationManager.DisplayMessage(new InformationMessage("Caught KeyNotFoundException in GetCurrentStaminaRatio!", new Color(1.00f, 0.38f, 0.01f), "Debug"));
#endif
                return 0.0;
            }
        }

        public static void UpdateStaminaHandler(Agent agent, double staminaDelta, bool recovering = false)
        {
            try
            {
                double newStamina = recovering ? CurrentStaminaPerAgent[agent.Index] + staminaDelta : CurrentStaminaPerAgent[agent.Index] - staminaDelta;
                UpdateStamina(agent, newStamina < 0 ? 0 : newStamina, recovering);
            }
            catch (KeyNotFoundException)
            {
#if DEBUG
                InformationManager.DisplayMessage(new InformationMessage("Caught KeyNotFoundException in UpdateStaminaHandler!", new Color(1.00f, 0.38f, 0.01f), "Debug"));
#endif
            }
        }

        private static void UpdateStamina(Agent agent, double newStamina, bool recovering = false)
        {
            //double oldStaminaRatio = GetCurrentStaminaRatio(agent);
            double newStaminaRatio = newStamina / OriginalMaxStaminaPerAgent[agent.Index];
            bool changedTier = GetTierChanged(newStaminaRatio, recovering);

            if (!agent.IsActive())
                return;

            if (agent.IsPlayerUnit)
                InformationManager.DisplayMessage(new InformationMessage("Agent " + agent.Name + " has " + CurrentStaminaPerAgent[agent.Index] + " stamina", new Color(1.00f, 0.38f, 0.01f), "Debug"));

            if (recovering)
            {
                // if stamina has fallen below High Level (75% default) and recovery would set it above High Level, set to High Level instead
                if (CurrentStaminaPerAgent[agent.Index] < StaminaProperties.Instance.HighStaminaRemaining * OriginalMaxStaminaPerAgent[agent.Index] &&
                    newStaminaRatio > StaminaProperties.Instance.HighStaminaRemaining)
                {
                    CurrentStaminaPerAgent[agent.Index] = StaminaProperties.Instance.HighStaminaRemaining * OriginalMaxStaminaPerAgent[agent.Index];
                }
                else
                {
                    CurrentStaminaPerAgent[agent.Index] = newStamina > OriginalMaxStaminaPerAgent[agent.Index] ? OriginalMaxStaminaPerAgent[agent.Index] : newStamina;
                }
            }

            else
            {
                CurrentStaminaPerAgent[agent.Index] = newStamina > StaminaProperties.Instance.NoStaminaRemaining ? newStamina : StaminaProperties.Instance.NoStaminaRemaining;

                if (newStaminaRatio <= StaminaProperties.Instance.LowStaminaRemaining && !LowStaminaTier)
                {
                    CurrentMaxStaminaPerAgent[agent.Index] -= OriginalMaxStaminaPerAgent[agent.Index] * .1; // lose 10% of max stamina whenever stamina falls below Low
                }
            }

            if (changedTier)
            {
                AgentsToBeUpdated.Enqueue(new Tuple<Agent, double>(agent, newStaminaRatio)); // update weapon speeds and move speed when stamina tier changes

                if (agent.IsPlayerUnit)
                {
                    PrintTierChangedMessage(recovering);
                }
            }
        }

        private static bool GetTierChanged(double newStaminaRatio, bool recovering)
        {
            if (recovering)
            {
                if (NoStaminaTier && newStaminaRatio > StaminaProperties.Instance.NoStaminaRemaining)
                {
                    NoStaminaTier = false;
                    LowStaminaTier = true;
                    return true;
                }
                else if (LowStaminaTier && newStaminaRatio > StaminaProperties.Instance.LowStaminaRemaining)
                {
                    LowStaminaTier = false;
                    MediumStaminaTier = true;
                    return true;
                }
                else if (MediumStaminaTier && newStaminaRatio > StaminaProperties.Instance.MediumStaminaRemaining)
                {
                    MediumStaminaTier = false;
                    HighStaminaTier = true;
                    return true;
                }
                else if (HighStaminaTier && newStaminaRatio > StaminaProperties.Instance.HighStaminaRemaining)
                {
                    HighStaminaTier = false;
                    FullStaminaTier = true;
                    return true;
                }
            }

            else
            {
                if (LowStaminaTier && newStaminaRatio < StaminaProperties.Instance.LowStaminaRemaining)
                {
                    LowStaminaTier = false;
                    NoStaminaTier = true;
                    return true;
                }
                else if (MediumStaminaTier && newStaminaRatio < StaminaProperties.Instance.MediumStaminaRemaining)
                {
                    MediumStaminaTier = false;
                    LowStaminaTier = true;
                    return true;
                }
                else if (HighStaminaTier && newStaminaRatio < StaminaProperties.Instance.HighStaminaRemaining)
                {
                    HighStaminaTier = false;
                    MediumStaminaTier = true;
                    return true;
                }
                else if (FullStaminaTier && newStaminaRatio < StaminaProperties.Instance.FullStaminaRemaining)
                {
                    FullStaminaTier = false;
                    HighStaminaTier = true;
                    return true;
                }
            }

            return false;
        }

        public static void PrintTierChangedMessage(bool recovering)
        {
            if (recovering)
            {
                if (LowStaminaTier)
                {
                    Print("You manage to catch your breath! You're feeling tired!", new Color(1.00f, 0.38f, 0.01f)); // orange
                }
                else if (MediumStaminaTier)
                {
                    Print("Your heart calms! You're feeling winded!", new Color(1.0f, 0.83f, 0.0f)); // yellow
                }
                else if (HighStaminaTier)
                {
                    Print("You're feeling warmed up!", new Color(0.91f, 0.84f, 0.42f)); // pale yellow
                }
                else if (FullStaminaTier)
                {
                    Print("You're feeling fresh!", new Color(0.64f, 0.78f, 0.22f)); // green
                }
            }

            else
            {
                if (NoStaminaTier)
                {
                    Print("You're completely exhausted! Rest now!", new Color(0.55f, 0, 0)); // red
                }
                else if (LowStaminaTier)
                {
                    Print("You're feeling tired! Rest soon!", new Color(1.00f, 0.38f, 0.01f)); // orange
                }
                else if (MediumStaminaTier)
                {
                    Print("You're feeling winded! You're attacks are slowing!", new Color(1.0f, 0.83f, 0.0f)); // yellow
                }
                else if (HighStaminaTier)
                {
                    Print("You're feeling warmed up!", new Color(0.91f, 0.84f, 0.42f)); // pale yellow
                }
            }
        }

        public static void Print(string message, Color color)
        {
            InformationManager.DisplayMessage(new InformationMessage(message, color, "Combat"));
        }
    }

    [HarmonyPatch(typeof(Mission), "OnAgentRemoved")]
    class MissionOnAgentRemovedPatch
    {
        public static void Postfix(Mission __instance, Agent affectedAgent)
        {
            if (affectedAgent != MissionSpawnAgentPatch.heroAgent)
            {
                MissionOnTickPatch.AgentRecoveryTimers.Remove(affectedAgent);
                MissionSpawnAgentPatch.CurrentStaminaPerAgent.Remove(affectedAgent.Index);
                MissionSpawnAgentPatch.OriginalMaxStaminaPerAgent.Remove(affectedAgent.Index);
            }
        }
    }

    [HarmonyPatch(typeof(Agent), "InitializeMissionEquipment")]
    class AgentInitializeMissionEquipmentPatch
    {
        public static Dictionary<int, List<Tuple<int, int>>> AgentOriginalWeaponSpeed = new Dictionary<int, List<Tuple<int, int>>>();
        public static float frameRate = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.FrameLimiter);

        public static void Postfix(Agent __instance)
        {
            List<Tuple<int, int>> equipmentList = new List<Tuple<int, int>>();
            for (int i = 0; i < 4; i++)
            {
                if (!(__instance.Equipment[i].CurrentUsageItem == null))
                    equipmentList.Add(new Tuple<int, int>(__instance.Equipment[i].CurrentUsageItem.SwingSpeed, __instance.Equipment[i].CurrentUsageItem.ThrustSpeed));
                else
                    equipmentList.Add(new Tuple<int, int>(0, 0));
            }

            AgentOriginalWeaponSpeed.Add(__instance.Index, equipmentList);
        }
    }

    [HarmonyPatch(typeof(ArenaPracticeFightMissionController), "OnAgentRemoved")]
    class ArenaPracticeFightMissionControllerOnAgentRemovedPatch
    {
        public static void Postfix(ArenaPracticeFightMissionController __instance, 
            Agent affectedAgent,
            Agent affectorAgent,
            AgentState agentState,
            KillingBlow killingBlow)
        {
            if (affectedAgent != MissionSpawnAgentPatch.heroAgent)
            {
                AgentInitializeMissionEquipmentPatch.AgentOriginalWeaponSpeed.Remove(affectedAgent.Index);
                MissionOnTickPatch.AgentRecoveryTimers.Remove(affectedAgent);
                MissionSpawnAgentPatch.CurrentStaminaPerAgent.Remove(affectedAgent.Index);
                MissionSpawnAgentPatch.OriginalMaxStaminaPerAgent.Remove(affectedAgent.Index);
            }
        }
    }

    [HarmonyPatch(typeof(ArenaPracticeFightMissionController), "CheckPracticeEndedForPlayer")]
    class ArenaPracticeFightMissionControllerCheckPracticeEndedForPlayerPatch
    {
        public static void Postfix(ref bool __result)
        {
            if (__result)
            {
                MissionOnTickPatch.Cleanup();
            }
        }
    }

    [HarmonyPatch(typeof(Mission), "SpawnAttachedWeaponOnCorpse")]
    class AgentGetEquipmentPatch
    {
        public static bool Prefix(Mission __instance,
      Agent agent,
      int attachedWeaponIndex,
      int forcedSpawnIndex)
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(SandboxAgentApplyDamageModel), "DecideCrushedThrough")]
    class DecideCrushedThroughPatch
    {
        public static void Postfix(
            Agent attackerAgent, 
            Agent defenderAgent, float totalAttackEnergy, 
            Agent.UsageDirection attackDirection, 
            StrikeType strikeType, 
            WeaponComponentData defendItem, 
            bool isPassiveUsage,
            ref bool __result)
        {
            if (MissionSpawnAgentPatch.CurrentStaminaPerAgent.Count > 0 && StaminaProperties.Instance.StaminaAffectsCrushThrough)
            {
                double attackerStaminaRatio = MissionSpawnAgentPatch.CurrentStaminaPerAgent[attackerAgent.Index] / MissionSpawnAgentPatch.OriginalMaxStaminaPerAgent[attackerAgent.Index];
                double defenderStaminaRatio = MissionSpawnAgentPatch.CurrentStaminaPerAgent[defenderAgent.Index] / MissionSpawnAgentPatch.OriginalMaxStaminaPerAgent[defenderAgent.Index];

                // if defender stamina low and attack wasn't crushed through, let it through
                if (defenderStaminaRatio < StaminaProperties.Instance.LowStaminaRemaining)
                {
                    __result = true;
                }

                // if defender stamina high and attack crushed through, block it
                if (defenderStaminaRatio > StaminaProperties.Instance.HighStaminaRemaining || attackerStaminaRatio <= StaminaProperties.Instance.LowStaminaRemaining)
                {
                    __result = false;
                }
            }
        }
    }
}
