//#undef DEBUG

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
    [HarmonyPatch(typeof(MissionState), "TickMission")]
    class MissionOnTickPatch
    {
        public static Dictionary<Agent, int> AgentRecoveryTimers = new Dictionary<Agent, int>();

        public static void Postfix(MissionState __instance, float realDt)
        {
            if (__instance.CurrentMission != null && __instance.CurrentMission.CurrentState == Mission.State.Continuing && !__instance.Paused)
            {
                MissionSpawnAgentPatch.MaxStaminaPerAgent.Keys.Except(MissionSpawnAgentPatch.AgentsToBeUpdated.Select(t => t.Item1).ToList())
                           .Where(x => x.IsActive() && x.Character != null)
                           .ToList().ForEach(o => AgentRecoveryTimers[o]++);

                if (!MissionSpawnAgentPatch.AgentsToBeUpdated.IsEmpty())
                {
                    Tuple<Agent, double> tuple = MissionSpawnAgentPatch.AgentsToBeUpdated.Dequeue();

                    // if Agent is currently not attacking, update weapon speed
                    //if (tuple.Item1.AttackDirection == Agent.UsageDirection.None) {
                        if (tuple.Item1.IsActive())
                        {
                            ChangeWeaponSpeedsHandler(tuple.Item1, tuple.Item2);
                            //ChangeMoveSpeed(tuple.Item1, tuple.Item2);
                        }
                    //}
                    //else
                    //{
                    //    // else add them to end of queue so they don't block
                    //    MissionSpawnAgentPatch.AgentsToBeUpdated.Enqueue(tuple);
                    //}
                }

                foreach (Agent agent in AgentRecoveryTimers.Keys.ToList())
                {
                    if (AgentRecoveryTimers[agent] > 120 * StaminaProperties.Instance.SecondsBeforeStaminaRegenerates
                        && MissionSpawnAgentPatch.GetCurrentStaminaRatio(agent) < StaminaProperties.Instance.HighStaminaRemaining)
                    {
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

                ChangeWeaponSpeed(agent, i, speedMultiplier, currentWeapon);
                if (requip)
                    RequipWeapon(agent, i, currentWeapon, mainIndex, offIndex);
            }
        }

        public static void ChangeWeaponSpeed(Agent agent, int i, double speedMultiplier, MissionWeapon weapon)
        {
            // normalize speed multiplier between 100% and minimum set in properties and ensure speed multiplier above minimum
            speedMultiplier = (speedMultiplier * (1 - StaminaProperties.Instance.LowestSpeedFromStaminaDebuff)) + StaminaProperties.Instance.LowestSpeedFromStaminaDebuff;
            speedMultiplier = speedMultiplier > StaminaProperties.Instance.LowestSpeedFromStaminaDebuff ? speedMultiplier : StaminaProperties.Instance.LowestSpeedFromStaminaDebuff;

            if (agent != null && weapon.CurrentUsageItem != null)
            {
                try
                {
                    // change speed value by multiplier
                    PropertyInfo property = typeof(WeaponComponentData).GetProperty("SwingSpeed");
                    property.DeclaringType.GetProperty("SwingSpeed");
                    property.SetValue(weapon.CurrentUsageItem,
                        (int)Math.Round(AgentInitializeMissionEquipmentPatch.AgentOriginalWeaponSpeed[agent][i].Item1 * speedMultiplier, MidpointRounding.AwayFromZero),
                        BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);

                    property = typeof(WeaponComponentData).GetProperty("ThrustSpeed");
                    property.DeclaringType.GetProperty("ThrustSpeed");
                    property.SetValue(weapon.CurrentUsageItem,
                        (int)Math.Round(AgentInitializeMissionEquipmentPatch.AgentOriginalWeaponSpeed[agent][i].Item2 * speedMultiplier, MidpointRounding.AwayFromZero),
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
            }
        }

        public static void RequipWeapon(Agent agent, int index, MissionWeapon weapon, EquipmentIndex mainIndex, EquipmentIndex offIndex)
        {
            // reset weapon to set new speed
            agent.EquipWeaponWithNewEntity((EquipmentIndex)index, ref weapon);

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
                ChangeWeaponSpeedsHandler(MissionSpawnAgentPatch.heroAgent, 1.0, false);
                //ChangeMoveSpeed(MissionSpawnAgentPatch.heroAgent, MissionSpawnAgentPatch.heroAgent.MaximumForwardUnlimitedSpeed, false);
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
            try
            {
                AgentInitializeMissionEquipmentPatch.AgentOriginalWeaponSpeed[__instance][(int)weaponPickUpSlotIndex] = 
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
      int affectorWeaponSlotOrMissileIndex,
      bool isMissile,
      bool isBlocked,
      int damage,
      float movementSpeedDamageModifier,
      float hitDistance,
      AgentAttackType attackType,
      BoneBodyPartType victimHitBodyPart)
        {
            if (MissionOnTickPatch.AgentRecoveryTimers.ContainsKey(affectorAgent) && MissionOnTickPatch.AgentRecoveryTimers.ContainsKey(affectedAgent) && affectorAgent != affectedAgent)
            {
                MissionOnTickPatch.AgentRecoveryTimers[affectorAgent] = 0;
                MissionOnTickPatch.AgentRecoveryTimers[affectedAgent] = 0;

                if (affectorAgent.Character != null && affectorAgent.IsActive())
                {
                    if (isMissile)
                    {
#if DEBUG
                        InformationManager.DisplayMessage(new InformationMessage(((Dictionary<int, Mission.Missile>)Helper.GetField(__instance, "_missiles"))[affectorWeaponSlotOrMissileIndex].Weapon.Item.Name.ToString(), new Color(1.00f, 0.38f, 0.01f), "Debug"));
#endif
                        MissionSpawnAgentPatch.UpdateStaminaHandler(affectorAgent, StaminaProperties.Instance.StaminaCostToRangedAttack);
                    }
                    else
                    {
                        MissionSpawnAgentPatch.UpdateStaminaHandler(affectorAgent, StaminaProperties.Instance.StaminaCostToMeleeAttack);
                    }
                }

                if (affectedAgent.Character != null && affectedAgent.IsActive())
                {
                    if (isBlocked)
                    {

                        MissionSpawnAgentPatch.UpdateStaminaHandler(affectedAgent, damage * StaminaProperties.Instance.StaminaCostPerBlockedDamage);
                    }
                    else
                    {
                        MissionSpawnAgentPatch.UpdateStaminaHandler(affectedAgent, damage * StaminaProperties.Instance.StaminaCostPerReceivedDamage);
                    }
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
                double newStamina = recovering ? CurrentStaminaPerAgent[agent] + staminaDelta : CurrentStaminaPerAgent[agent] - staminaDelta;
                UpdateStamina(agent, newStamina, recovering);
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
            double oldStaminaRatio = GetCurrentStaminaRatio(agent);
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

        public static void Print(string message, Color color)
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
                    equipmentList.Add(new Tuple<int, int>(__instance.Equipment[i].CurrentUsageItem.SwingSpeed, __instance.Equipment[i].CurrentUsageItem.ThrustSpeed));
                else
                    equipmentList.Add(new Tuple<int, int>(0, 0));
            }

            AgentOriginalWeaponSpeed.Add(__instance, equipmentList);
        }
    }

    [HarmonyPatch(typeof(BattleAgentLogic), "OnAgentRemoved")]
    class MissionOnAgentRemovedPatch
    {
        public static void Postfix(BattleAgentLogic __instance, Agent affectedAgent,
      Agent affectorAgent,
      AgentState agentState,
      KillingBlow killingBlow)
        {
            if (affectedAgent != MissionSpawnAgentPatch.heroAgent)
            {
                AgentInitializeMissionEquipmentPatch.AgentOriginalWeaponSpeed.Remove(affectedAgent);
                MissionOnTickPatch.AgentRecoveryTimers.Remove(affectedAgent);
                MissionSpawnAgentPatch.CurrentStaminaPerAgent.Remove(affectedAgent);
                MissionSpawnAgentPatch.MaxStaminaPerAgent.Remove(affectedAgent);
            }
            else
            {
                // need to reset weapons' speeds on agent death
                MissionOnTickPatch.ChangeWeaponSpeedsHandler(affectedAgent, 1.0, false);
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
}
