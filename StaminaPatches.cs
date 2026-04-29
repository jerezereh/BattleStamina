#define DEBUG

using HarmonyLib;
using SandBox.GameComponents;
using SandBox.Missions.MissionLogics.Arena;
using System;
using System.Collections.Generic;
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
        public static Dictionary<Agent, int> AgentMovementFatigueTimers = new Dictionary<Agent, int>();
        public static float FrameRate = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.FrameLimiter);

        public static void Postfix(MissionState __instance)
        {
            if (__instance.CurrentMission != null && __instance.CurrentMission.CurrentState == Mission.State.Continuing && !__instance.Paused)
            {
                foreach (Agent agent in new List<Agent>(AgentRecoveryTimers.Keys))
                {
                    if (agent == null || !agent.IsActive() || agent.Character == null)
                        continue;

                    AgentAttackStaminaPatch.UpdateAttackReadying(agent);

                    if (!MissionSpawnAgentPatch.IsAgentQueuedForUpdate(agent))
                    {
                        AgentRecoveryTimers[agent]++;
                    }
                }

                if (!MissionSpawnAgentPatch.AgentsToBeUpdated.IsEmpty())
                {
                    Tuple<Agent, double> tuple = MissionSpawnAgentPatch.AgentsToBeUpdated.Dequeue();
                    double staminaRatio = MissionSpawnAgentPatch.GetQueuedAgentUpdateRatio(tuple.Item1, tuple.Item2);
                    MissionSpawnAgentPatch.MarkAgentUpdateDequeued(tuple.Item1);

                    if (tuple.Item1.IsActive())
                    {
                        UpdateAgentFatigueProperties(tuple.Item1, staminaRatio);
                    }
                }

                foreach (Agent agent in new List<Agent>(AgentRecoveryTimers.Keys))
                {
                    if (agent == null || !agent.IsActive() || agent.Character == null)
                        continue;

                    bool drainingMovementStamina = TryApplyMovementFatigue(agent);

                    // frame rate dependent, might be bugged
                    if (!drainingMovementStamina && AgentRecoveryTimers[agent] > GetTicksPerSecond() * StaminaSettings.Instance.SecondsBeforeStaminaRegenerates)
                    {
                        //AgentRecoveryTimers[agent] = 0;

                        if (agent.GetCurrentVelocity().Length > agent.GetMaximumForwardUnlimitedSpeed() * StaminaSettings.Instance.MaximumMoveSpeedPercentStaminaRegenerates)
                            MissionSpawnAgentPatch.UpdateStaminaHandler(agent, StaminaSettings.Instance.StaminaRecoveredPerTickMoving, true, "mov");
                        else
                            MissionSpawnAgentPatch.UpdateStaminaHandler(agent, StaminaSettings.Instance.StaminaRecoveredPerTickResting, true, "rst");
                    }
                }
            }
        }

        public static void Cleanup()
        {
            AgentRecoveryTimers.Clear();
            AgentMovementFatigueTimers.Clear();
            MissionSpawnAgentPatch.AgentsToBeUpdated.Clear();
            MissionSpawnAgentPatch.CurrentStaminaPerAgent.Clear();
            MissionSpawnAgentPatch.CurrentMaxStaminaPerAgent.Clear();
            MissionSpawnAgentPatch.OriginalMaxStaminaPerAgent.Clear();
            MissionSpawnAgentPatch.CurrentTierPerAgent.Clear();
            MissionSpawnAgentPatch.QueuedAgentUpdates.Clear();
            MissionSpawnAgentPatch.QueuedAgentUpdateRatios.Clear();
            AgentAttackStaminaPatch.LastChargedMeleeAttackTimePerAgent.Clear();
            AgentAttackStaminaPatch.LastChargedRangedAttackTimePerAgent.Clear();
            AgentAttackStaminaPatch.PreviousActionTypePerAgentChannel.Clear();
            AgentAttackStaminaPatch.PreviousActionStagePerAgentChannel.Clear();
#if DEBUG
            MissionSpawnAgentPatch.PendingDebugRegenPerAgent.Clear();
#endif
            AgentStatCalculateModelPatch.LastAppliedFatigueRatioPerAgent.Clear();
            MissionSpawnAgentPatch.heroAgent = null;
            MissionSpawnAgentPatch.debugAllyAgent = null;
        }

        public static void UpdateAgentFatigueProperties(Agent agent, double staminaRatio)
        {
            if (agent == null || !agent.IsActive())
                return;

            AgentStatCalculateModelPatch.LastAppliedFatigueRatioPerAgent[agent.Index] = staminaRatio;
            agent.UpdateAgentProperties();
        }

        private static bool TryApplyMovementFatigue(Agent agent)
        {
            if (agent == null || agent.IsMount || agent.HasMount)
                return false;

            float maximumSpeed = agent.GetMaximumForwardUnlimitedSpeed();
            if (maximumSpeed <= 0)
                return false;

            float speedRatio = agent.GetCurrentVelocity().Length / maximumSpeed;
            if (speedRatio < StaminaSettings.Instance.MinimumMoveSpeedPercentStaminaDrains)
            {
                AgentMovementFatigueTimers[agent] = 0;
                return false;
            }

            int timer;
            AgentMovementFatigueTimers.TryGetValue(agent, out timer);
            timer++;

            int ticksPerSecond = GetTicksPerSecond();
            if (timer >= ticksPerSecond)
            {
                timer = 0;
                MissionSpawnAgentPatch.UpdateStaminaHandler(agent, StaminaCostCalculator.GetMovementCostPerSecond(agent), false, "run");
            }

            AgentMovementFatigueTimers[agent] = timer;
            return true;
        }

        private static int GetTicksPerSecond()
        {
            return Math.Max(1, (int)Math.Round(FrameRate));
        }
    }

    enum StaminaTier
    {
        No,
        Low,
        Medium,
        High,
        Full
    }

    static class StaminaFatigueModel
    {
        public static float GetSpeedMultiplier(double staminaRatio)
        {
            double clampedRatio = Clamp(staminaRatio, StaminaSettings.Instance.NoStaminaRemaining, 1.0);
            double minimumMultiplier = Clamp(StaminaSettings.Instance.LowestSpeedFromStaminaDebuff, 0.05, 1.0);
            double normalMultiplier = minimumMultiplier + clampedRatio * (1.0 - minimumMultiplier);
            double lowThreshold = Math.Max(StaminaSettings.Instance.LowStaminaRemaining, StaminaSettings.Instance.NoStaminaRemaining);

            if (clampedRatio >= lowThreshold)
                return (float)Clamp(normalMultiplier, minimumMultiplier, 1.0);

            double lowMultiplier = minimumMultiplier + lowThreshold * (1.0 - minimumMultiplier);
            double lowPenalty = 1.0 - lowMultiplier;
            double exhaustionProgress = (lowThreshold - clampedRatio) / (lowThreshold - StaminaSettings.Instance.NoStaminaRemaining);
            double exhaustionPenalty = lowPenalty * exhaustionProgress;
            double exhaustedMultiplier = lowMultiplier - exhaustionPenalty;
            return (float)Clamp(exhaustedMultiplier, minimumMultiplier, 1.0);
        }

        public static float GetAiMultiplier(double staminaRatio)
        {
            float speedMultiplier = GetSpeedMultiplier(staminaRatio);
            return Math.Max(0.15f, speedMultiplier);
        }

        public static float GetInaccuracyMultiplier(double staminaRatio)
        {
            double fatigue = 1.0 - GetSpeedMultiplier(staminaRatio);
            return (float)(1.0 + fatigue * 2.0);
        }

        public static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }
    }

    class AgentStatCalculateModelPatch
    {
        public static Dictionary<int, double> LastAppliedFatigueRatioPerAgent = new Dictionary<int, double>();
        private const double FatigueRefreshRatioStep = 0.025;

        public static void ApplyStaminaFatigue(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            if (agent == null || agentDrivenProperties == null || !MissionSpawnAgentPatch.CurrentStaminaPerAgent.ContainsKey(agent.Index))
                return;

            double staminaRatio = MissionSpawnAgentPatch.GetCurrentStaminaRatio(agent);
            float speedMultiplier = StaminaFatigueModel.GetSpeedMultiplier(staminaRatio);
            float aiMultiplier = StaminaFatigueModel.GetAiMultiplier(staminaRatio);
            float inaccuracyMultiplier = StaminaFatigueModel.GetInaccuracyMultiplier(staminaRatio);
            float fatigue = 1f - speedMultiplier;

            agentDrivenProperties.SwingSpeedMultiplier *= speedMultiplier;
            agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier *= speedMultiplier;
            agentDrivenProperties.BipedalRangedReadySpeedMultiplier *= speedMultiplier;
            agentDrivenProperties.ReloadSpeed *= speedMultiplier;
            agentDrivenProperties.BipedalRangedReloadSpeedMultiplier *= speedMultiplier;
            agentDrivenProperties.MaxSpeedMultiplier *= speedMultiplier;
            agentDrivenProperties.CombatMaxSpeedMultiplier *= speedMultiplier;
            agentDrivenProperties.CrouchedSpeedMultiplier *= speedMultiplier;
            agentDrivenProperties.WeaponInaccuracy *= inaccuracyMultiplier;
            agentDrivenProperties.WeaponInaccuracy += fatigue * 0.15f;
            agentDrivenProperties.AiShooterError *= inaccuracyMultiplier;
            agentDrivenProperties.AiShooterError += fatigue * 2f;
            agentDrivenProperties.AiShooterErrorWoRangeUpdate *= inaccuracyMultiplier;
            agentDrivenProperties.AiShooterErrorWoRangeUpdate += fatigue * 2f;
            agentDrivenProperties.AiRangerHorizontalErrorMultiplier *= inaccuracyMultiplier;
            agentDrivenProperties.AiRangerVerticalErrorMultiplier *= inaccuracyMultiplier;
            agentDrivenProperties.AIAttackOnDecideChance *= aiMultiplier;
            agentDrivenProperties.AIDecideOnAttackChance *= aiMultiplier;
            agentDrivenProperties.AIAttackOnParryChance *= aiMultiplier;
            agentDrivenProperties.AiTryChamberAttackOnDecide *= aiMultiplier;
            agentDrivenProperties.AIParryOnAttackAbility *= aiMultiplier;
            agentDrivenProperties.AIParryOnAttackingContinueAbility *= aiMultiplier;
            agentDrivenProperties.AIBlockOnDecideAbility *= aiMultiplier;
            agentDrivenProperties.AIParryOnDecideAbility *= aiMultiplier;
            agentDrivenProperties.AiShootFreq *= aiMultiplier;
            agentDrivenProperties.AiWaitBeforeShootFactor /= Math.Max(aiMultiplier, 0.15f);
            agentDrivenProperties.AiAttackCalculationMaxTimeFactor /= Math.Max(aiMultiplier, 0.15f);
        }

        public static bool ShouldRefreshAgentProperties(Agent agent, double staminaRatio, bool changedTier, bool recovering)
        {
            if (agent == null)
                return false;

            if (changedTier || !recovering)
                return true;

            double lastAppliedRatio;
            return !LastAppliedFatigueRatioPerAgent.TryGetValue(agent.Index, out lastAppliedRatio) ||
                Math.Abs(lastAppliedRatio - staminaRatio) >= FatigueRefreshRatioStep;
        }
    }

    [HarmonyPatch(typeof(CustomBattleAgentStatCalculateModel), "UpdateAgentStats")]
    class CustomBattleAgentStatCalculateModelPatch
    {
        public static void Postfix(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            AgentStatCalculateModelPatch.ApplyStaminaFatigue(agent, agentDrivenProperties);
        }
    }

    [HarmonyPatch(typeof(SandboxAgentStatCalculateModel), "UpdateAgentStats")]
    class SandboxAgentStatCalculateModelPatch
    {
        public static void Postfix(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            AgentStatCalculateModelPatch.ApplyStaminaFatigue(agent, agentDrivenProperties);
        }
    }

    [HarmonyPatch(typeof(MultiplayerAgentStatCalculateModel), "UpdateAgentStats")]
    class MultiplayerAgentStatCalculateModelPatch
    {
        public static void Postfix(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            AgentStatCalculateModelPatch.ApplyStaminaFatigue(agent, agentDrivenProperties);
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
                MissionOnTickPatch.UpdateAgentFatigueProperties(__instance, MissionSpawnAgentPatch.GetCurrentStaminaRatio(__instance));
            }
            catch (KeyNotFoundException)
            {
#if DEBUG
                InformationManager.DisplayMessage(new InformationMessage("Caught KeyNotFoundException in AgentOnItemPickupPatch!", new Color(1.00f, 0.38f, 0.01f), "Debug"));
#endif
            }
        }
    }

    class AgentAttackStaminaPatch
    {
        public static Dictionary<int, float> LastChargedMeleeAttackTimePerAgent = new Dictionary<int, float>();
        public static Dictionary<int, float> LastChargedRangedAttackTimePerAgent = new Dictionary<int, float>();
        public static Dictionary<int, Agent.ActionCodeType> PreviousActionTypePerAgentChannel = new Dictionary<int, Agent.ActionCodeType>();
        public static Dictionary<int, Agent.ActionStage> PreviousActionStagePerAgentChannel = new Dictionary<int, Agent.ActionStage>();

        public static void ApplyAttackStaminaCost(Agent agent, bool isRangedAttack)
        {
            if (agent == null || agent.Character == null || !agent.IsActive())
                return;

            MissionOnTickPatch.AgentRecoveryTimers[agent] = 0;

            MissionSpawnAgentPatch.UpdateStaminaHandler(
                agent,
                StaminaCostCalculator.GetAttackCost(agent, isRangedAttack),
                false,
                isRangedAttack ? "rng" : "atk");
        }

        public static bool ShouldChargeAttack(Dictionary<int, float> lastChargedAttackTimePerAgent, Agent agent, float attackTime)
        {
            if (agent == null)
                return false;

            float lastChargedAttackTime;
            if (lastChargedAttackTimePerAgent.TryGetValue(agent.Index, out lastChargedAttackTime) &&
                Math.Abs(lastChargedAttackTime - attackTime) < 0.0001f)
            {
                return false;
            }

            lastChargedAttackTimePerAgent[agent.Index] = attackTime;
            return true;
        }

        public static int GetActionChannelKey(Agent agent, int channelNo)
        {
            return agent.Index * 16 + channelNo;
        }

        public static bool IsAttackStartAction(Agent.ActionCodeType actionType, Agent.ActionStage actionStage)
        {
            return actionType == Agent.ActionCodeType.ReadyMelee ||
                actionType == Agent.ActionCodeType.ReadyRanged ||
                actionType == Agent.ActionCodeType.ReleaseThrowing ||
                (actionType == Agent.ActionCodeType.ReleaseRanged && actionStage == Agent.ActionStage.AttackRelease);
        }

        public static bool IsRangedAttackAction(Agent.ActionCodeType actionType)
        {
            return actionType == Agent.ActionCodeType.ReadyRanged ||
                actionType == Agent.ActionCodeType.ReleaseRanged ||
                actionType == Agent.ActionCodeType.ReleaseThrowing;
        }

        public static void ClearActionChannelCache(Agent agent)
        {
            if (agent == null)
                return;

            for (int channelNo = 0; channelNo < 8; channelNo++)
            {
                int key = GetActionChannelKey(agent, channelNo);
                PreviousActionTypePerAgentChannel.Remove(key);
                PreviousActionStagePerAgentChannel.Remove(key);
            }
        }

        public static void UpdateAttackReadying(Agent agent)
        {
            if (agent == null || agent.Character == null || !agent.IsActive())
                return;

            for (int channelNo = 0; channelNo < 2; channelNo++)
            {
                Agent.ActionCodeType newActionType = agent.GetCurrentActionType(channelNo);
                Agent.ActionStage newActionStage = agent.GetCurrentActionStage(channelNo);
                int key = GetActionChannelKey(agent, channelNo);

                Agent.ActionCodeType oldActionType;
                Agent.ActionStage oldActionStage;
                PreviousActionTypePerAgentChannel.TryGetValue(key, out oldActionType);
                PreviousActionStagePerAgentChannel.TryGetValue(key, out oldActionStage);

                if (IsAttackStartAction(newActionType, newActionStage) &&
                    !IsAttackStartAction(oldActionType, oldActionStage))
                {
                    ApplyAttackStaminaCost(agent, IsRangedAttackAction(newActionType));
                }

                PreviousActionTypePerAgentChannel[key] = newActionType;
                PreviousActionStagePerAgentChannel[key] = newActionStage;
            }
        }
    }

    [HarmonyPatch(typeof(Agent), "SetActionChannel")]
    class AgentSetActionChannelPatch
    {
        public static void Prefix(Agent __instance, int channelNo)
        {
            if (__instance == null)
                return;

            int key = AgentAttackStaminaPatch.GetActionChannelKey(__instance, channelNo);
            AgentAttackStaminaPatch.PreviousActionTypePerAgentChannel[key] = __instance.GetCurrentActionType(channelNo);
            AgentAttackStaminaPatch.PreviousActionStagePerAgentChannel[key] = __instance.GetCurrentActionStage(channelNo);
        }

        public static void Postfix(Agent __instance, int channelNo, bool __result)
        {
            if (!__result || __instance == null || __instance.Character == null || !__instance.IsActive())
                return;

            Agent.ActionCodeType newActionType = __instance.GetCurrentActionType(channelNo);
            Agent.ActionStage newActionStage = __instance.GetCurrentActionStage(channelNo);

            if (!AgentAttackStaminaPatch.IsAttackStartAction(newActionType, newActionStage))
                return;

            int key = AgentAttackStaminaPatch.GetActionChannelKey(__instance, channelNo);
            Agent.ActionCodeType oldActionType;
            Agent.ActionStage oldActionStage;
            AgentAttackStaminaPatch.PreviousActionTypePerAgentChannel.TryGetValue(key, out oldActionType);
            AgentAttackStaminaPatch.PreviousActionStagePerAgentChannel.TryGetValue(key, out oldActionStage);

            if (AgentAttackStaminaPatch.IsAttackStartAction(oldActionType, oldActionStage))
            {
                return;
            }

            AgentAttackStaminaPatch.ApplyAttackStaminaCost(
                __instance,
                AgentAttackStaminaPatch.IsRangedAttackAction(newActionType));
        }
    }

    [HarmonyPatch(typeof(Agent), "set_LastMeleeAttackTime")]
    class AgentSetLastMeleeAttackTimePatch
    {
        public static void Prefix(Agent __instance, float value)
        {
            AgentAttackStaminaPatch.ShouldChargeAttack(AgentAttackStaminaPatch.LastChargedMeleeAttackTimePerAgent, __instance, value);
        }
    }

    [HarmonyPatch(typeof(Agent), "UpdateLastRangedAttackTimeDueToAnAttack")]
    class AgentUpdateLastRangedAttackTimeDueToAnAttackPatch
    {
        public static void Prefix(Agent __instance, float newTime)
        {
            AgentAttackStaminaPatch.ShouldChargeAttack(AgentAttackStaminaPatch.LastChargedRangedAttackTimePerAgent, __instance, newTime);
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
            if (affectedAgent.Character != null && affectedAgent.IsActive() && affectedAgent.Health - damagedHp > 0)
            {
                MissionOnTickPatch.AgentRecoveryTimers[affectedAgent] = 0;
                StaminaCostCalculator.ApplyMeleeImpactCost(affectorAgent, collisionData);

                if (isBlocked)
                {
                    MissionSpawnAgentPatch.UpdateStaminaHandler(affectedAgent, StaminaDamageCalculator.GetBlockedDamageForStamina(b, collisionData) * StaminaSettings.Instance.StaminaCostPerBlockedDamage, false, "blk");
                }
                else
                {
                    MissionSpawnAgentPatch.UpdateStaminaHandler(affectedAgent, damagedHp * StaminaSettings.Instance.StaminaCostPerReceivedDamage, false, "hit");
                }
            }
        }
    }

    static class StaminaDamageCalculator
    {
        public static float GetBlockedDamageForStamina(in Blow blow, in AttackCollisionData collisionData)
        {
            if (collisionData.InflictedDamage > 0)
                return collisionData.InflictedDamage;

            if (blow.InflictedDamage > 0)
                return blow.InflictedDamage;

            if (collisionData.AbsorbedByArmor > 0)
                return collisionData.AbsorbedByArmor;

            if (blow.AbsorbedByArmor > 0)
                return blow.AbsorbedByArmor;

            if (collisionData.BaseMagnitude > 0)
                return collisionData.BaseMagnitude;

            return blow.BaseMagnitude > 0 ? blow.BaseMagnitude : 0;
        }
    }

    static class StaminaCostCalculator
    {
        public static double GetAttackCost(Agent agent, bool isRangedAttack)
        {
            MissionWeapon weapon = agent.WieldedWeapon;
            WeaponComponentData usageItem = weapon.CurrentUsageItem;
            double baseCost = isRangedAttack ? StaminaSettings.Instance.StaminaCostToRangedAttack : StaminaSettings.Instance.StaminaCostToMeleeAttack;

            if (usageItem == null)
                return baseCost;

            if (isRangedAttack && (usageItem.IsBow || usageItem.IsCrossBow))
            {
                return baseCost + Math.Max(0, usageItem.MissileSpeed) * StaminaSettings.Instance.StaminaCostPerRangedMissileSpeed;
            }

            return baseCost + GetItemWeight(weapon) * StaminaSettings.Instance.StaminaCostPerWeaponWeight;
        }

        public static double GetMovementCostPerSecond(Agent agent)
        {
            return StaminaSettings.Instance.StaminaCostPerSecondSprinting +
                GetCarriedWeight(agent) * StaminaSettings.Instance.StaminaCostPerSecondSprintingPerCarriedWeight;
        }

        public static void ApplyMeleeImpactCost(Agent agent, in AttackCollisionData collisionData)
        {
            if (agent == null || agent.Character == null || !agent.IsActive())
                return;

            MissionWeapon weapon = agent.WieldedWeapon;
            WeaponComponentData usageItem = weapon.CurrentUsageItem;
            if (usageItem == null || !usageItem.IsMeleeWeapon)
                return;

            double impactCost;
            string reason;
            if (collisionData.AttackBlockedWithShield || collisionData.CollidedWithShieldOnBack)
            {
                impactCost = StaminaSettings.Instance.StaminaCostToMeleeShieldImpact;
                reason = "shd";
            }
            else
            {
                impactCost = GetMeleeHitCost(collisionData);
                reason = "imp";
            }

            if (impactCost <= 0)
                return;

            MissionOnTickPatch.AgentRecoveryTimers[agent] = 0;
            MissionSpawnAgentPatch.UpdateStaminaHandler(agent, impactCost, false, reason);
        }

        private static double GetMeleeHitCost(in AttackCollisionData collisionData)
        {
            double speedModifier = Math.Max(0, collisionData.MovementSpeedDamageModifier);
            return StaminaSettings.Instance.StaminaCostToMeleeHit +
                speedModifier * StaminaSettings.Instance.StaminaCostPerMeleeHitSpeedModifier;
        }

        private static float GetCarriedWeight(Agent agent)
        {
            float weight = 0f;

            try
            {
                if (agent.SpawnEquipment != null)
                    weight += agent.SpawnEquipment.GetTotalWeightOfArmor(true);
            }
            catch
            {
            }

            try
            {
                if (agent.Equipment != null)
                    weight += agent.Equipment.GetTotalWeightOfWeapons();
            }
            catch
            {
            }

            return weight;
        }

        private static float GetItemWeight(MissionWeapon weapon)
        {
            return weapon.Item != null ? weapon.Item.Weight : 0f;
        }
    }

    [HarmonyPatch(typeof(Mission), "SpawnAgent")]
    class MissionSpawnAgentPatch
    {
        public static Queue<Tuple<Agent, double>> AgentsToBeUpdated = new Queue<Tuple<Agent, double>>();
        public static Dictionary<int, double> OriginalMaxStaminaPerAgent = new Dictionary<int, double>();
        public static Dictionary<int, double> CurrentMaxStaminaPerAgent = new Dictionary<int, double>();
        public static Dictionary<int, double> CurrentStaminaPerAgent = new Dictionary<int, double>();
        public static Dictionary<int, StaminaTier> CurrentTierPerAgent = new Dictionary<int, StaminaTier>();
        public static HashSet<int> QueuedAgentUpdates = new HashSet<int>();
        public static Dictionary<int, double> QueuedAgentUpdateRatios = new Dictionary<int, double>();
        public static Agent heroAgent;
        public static Agent debugAllyAgent;
#if DEBUG
        public static Dictionary<int, double> PendingDebugRegenPerAgent = new Dictionary<int, double>();
#endif

        public static void Postfix(Mission __instance,
      AgentBuildData agentBuildData,
      bool spawnFromAgentVisuals,
      ref Agent __result)
        {
            if (agentBuildData != null)
            {
                double fullStamina = StaminaSettings.Instance.BaseStaminaValue +
                    agentBuildData.AgentCharacter.GetSkillValue(DefaultSkills.Athletics) * StaminaSettings.Instance.StaminaGainedPerAthletics +
                    agentBuildData.AgentCharacter.GetSkillValue(DefaultSkills.OneHanded) * StaminaSettings.Instance.StaminaGainedPerCombatSkill +
                    agentBuildData.AgentCharacter.GetSkillValue(DefaultSkills.TwoHanded) * StaminaSettings.Instance.StaminaGainedPerCombatSkill +
                    agentBuildData.AgentCharacter.GetSkillValue(DefaultSkills.Polearm) * StaminaSettings.Instance.StaminaGainedPerCombatSkill +
                    agentBuildData.AgentCharacter.GetSkillValue(DefaultSkills.Bow) * StaminaSettings.Instance.StaminaGainedPerCombatSkill +
                    agentBuildData.AgentCharacter.GetSkillValue(DefaultSkills.Crossbow) * StaminaSettings.Instance.StaminaGainedPerCombatSkill +
                    agentBuildData.AgentCharacter.GetSkillValue(DefaultSkills.Throwing) * StaminaSettings.Instance.StaminaGainedPerCombatSkill +
                    agentBuildData.AgentCharacter.Level * StaminaSettings.Instance.StaminaGainedPerLevel;

                OriginalMaxStaminaPerAgent.Add(__result.Index, fullStamina);
                CurrentMaxStaminaPerAgent.Add(__result.Index, fullStamina);
                CurrentStaminaPerAgent.Add(__result.Index, fullStamina);
                CurrentTierPerAgent.Add(__result.Index, StaminaTier.Full);
            }
            MissionOnTickPatch.AgentRecoveryTimers.Add(__result, 0);

            if (__result.IsPlayerUnit)
            {
                heroAgent = __result;
#if DEBUG
                TrySetDebugAllyAgent();
#endif
            }

#if DEBUG
            TrySetDebugAllyAgent(__result);
#endif
        }

        public static bool IsAgentQueuedForUpdate(Agent agent)
        {
            return agent != null && QueuedAgentUpdates.Contains(agent.Index);
        }

        public static void MarkAgentUpdateDequeued(Agent agent)
        {
            if (agent != null)
            {
                QueuedAgentUpdates.Remove(agent.Index);
                QueuedAgentUpdateRatios.Remove(agent.Index);
            }
        }

        public static double GetQueuedAgentUpdateRatio(Agent agent, double fallbackStaminaRatio)
        {
            if (agent == null)
                return fallbackStaminaRatio;

            double staminaRatio;
            return QueuedAgentUpdateRatios.TryGetValue(agent.Index, out staminaRatio) ? staminaRatio : fallbackStaminaRatio;
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

        public static void UpdateStaminaHandler(Agent agent, double staminaDelta, bool recovering = false, string reason = "sta")
        {
            try
            {
                double newStamina = recovering ? CurrentStaminaPerAgent[agent.Index] + staminaDelta : CurrentStaminaPerAgent[agent.Index] - staminaDelta;
                UpdateStamina(agent, newStamina < 0 ? 0 : newStamina, recovering, reason);
            }
            catch (KeyNotFoundException)
            {
#if DEBUG
                InformationManager.DisplayMessage(new InformationMessage("Caught KeyNotFoundException in UpdateStaminaHandler!", new Color(1.00f, 0.38f, 0.01f), "Debug"));
#endif
            }
        }

        private static void UpdateStamina(Agent agent, double newStamina, bool recovering = false, string reason = "sta")
        {
            //double oldStaminaRatio = GetCurrentStaminaRatio(agent);
            double oldStamina = CurrentStaminaPerAgent[agent.Index];
            double newStaminaRatio = newStamina / OriginalMaxStaminaPerAgent[agent.Index];
            StaminaTier oldTier = CurrentTierPerAgent[agent.Index];

            if (!agent.IsActive())
                return;

            if (recovering)
            {
                // if stamina has fallen below High Level (75% default) and recovery would set it above High Level, set to High Level instead
                if (CurrentStaminaPerAgent[agent.Index] < StaminaSettings.Instance.HighStaminaRemaining * OriginalMaxStaminaPerAgent[agent.Index] &&
                    newStaminaRatio > StaminaSettings.Instance.HighStaminaRemaining)
                {
                    CurrentStaminaPerAgent[agent.Index] = StaminaSettings.Instance.HighStaminaRemaining * OriginalMaxStaminaPerAgent[agent.Index];
                }
                else
                {
                    CurrentStaminaPerAgent[agent.Index] = newStamina > OriginalMaxStaminaPerAgent[agent.Index] ? OriginalMaxStaminaPerAgent[agent.Index] : newStamina;
                }
            }

            else
            {
                CurrentStaminaPerAgent[agent.Index] = newStamina > StaminaSettings.Instance.NoStaminaRemaining ? newStamina : StaminaSettings.Instance.NoStaminaRemaining;

                if (newStaminaRatio <= StaminaSettings.Instance.LowStaminaRemaining && oldTier > StaminaTier.Low)
                {
                    CurrentMaxStaminaPerAgent[agent.Index] -= OriginalMaxStaminaPerAgent[agent.Index] * .1; // lose 10% of max stamina whenever stamina falls below Low
                }
            }

            newStaminaRatio = CurrentStaminaPerAgent[agent.Index] / OriginalMaxStaminaPerAgent[agent.Index];
            StaminaTier newTier = GetTier(newStaminaRatio);
            bool changedTier = oldTier != newTier;
            bool refreshAgentProperties = AgentStatCalculateModelPatch.ShouldRefreshAgentProperties(agent, newStaminaRatio, changedTier, recovering);

#if DEBUG
            PrintStaminaDebug(agent, reason, CurrentStaminaPerAgent[agent.Index] - oldStamina, newTier, changedTier);
#endif

            if (refreshAgentProperties)
            {
                EnqueueAgentUpdate(agent, newStaminaRatio);
            }

            if (changedTier)
            {
                CurrentTierPerAgent[agent.Index] = newTier;

                if (agent.IsPlayerUnit)
                {
                    PrintTierChangedMessage(newTier, recovering);
                }
            }
        }

        private static void EnqueueAgentUpdate(Agent agent, double staminaRatio)
        {
            QueuedAgentUpdateRatios[agent.Index] = staminaRatio;

            if (QueuedAgentUpdates.Add(agent.Index))
            {
                AgentsToBeUpdated.Enqueue(new Tuple<Agent, double>(agent, staminaRatio));
            }
        }

        private static StaminaTier GetTier(double staminaRatio)
        {
            if (staminaRatio <= StaminaSettings.Instance.NoStaminaRemaining)
                return StaminaTier.No;

            if (staminaRatio <= StaminaSettings.Instance.LowStaminaRemaining)
                return StaminaTier.Low;

            if (staminaRatio < StaminaSettings.Instance.MediumStaminaRemaining)
                return StaminaTier.Medium;

            if (staminaRatio < StaminaSettings.Instance.HighStaminaRemaining)
                return StaminaTier.High;

            return StaminaTier.Full;
        }

        public static void PrintTierChangedMessage(StaminaTier tier, bool recovering)
        {
            if (recovering)
            {
                if (tier == StaminaTier.Low)
                {
                    Print("You manage to catch your breath! You're feeling tired!", new Color(1.00f, 0.38f, 0.01f)); // orange
                }
                else if (tier == StaminaTier.Medium)
                {
                    Print("Your heart calms! You're feeling winded!", new Color(1.0f, 0.83f, 0.0f)); // yellow
                }
                else if (tier == StaminaTier.High)
                {
                    Print("You're feeling warmed up!", new Color(0.91f, 0.84f, 0.42f)); // pale yellow
                }
                else if (tier == StaminaTier.Full)
                {
                    Print("You're feeling fresh!", new Color(0.64f, 0.78f, 0.22f)); // green
                }
            }

            else
            {
                if (tier == StaminaTier.No)
                {
                    Print("You're completely exhausted! Rest now!", new Color(0.55f, 0, 0)); // red
                }
                else if (tier == StaminaTier.Low)
                {
                    Print("You're feeling tired! Rest soon!", new Color(1.00f, 0.38f, 0.01f)); // orange
                }
                else if (tier == StaminaTier.Medium)
                {
                    Print("You're feeling winded! You're attacks are slowing!", new Color(1.0f, 0.83f, 0.0f)); // yellow
                }
                else if (tier == StaminaTier.High)
                {
                    Print("You're feeling warmed up!", new Color(0.91f, 0.84f, 0.42f)); // pale yellow
                }
            }
        }

        public static void Print(string message, Color color)
        {
            InformationManager.DisplayMessage(new InformationMessage(message, color, "Combat"));
        }

#if DEBUG
        private static void TrySetDebugAllyAgent(Agent candidate = null)
        {
            try
            {
                if (debugAllyAgent != null && debugAllyAgent.IsActive())
                    return;

                if (heroAgent == null || !heroAgent.IsActive())
                    return;

                if (IsTrackableDebugAlly(candidate))
                {
                    SetDebugAllyAgent(candidate);
                    return;
                }

                foreach (Agent agent in new List<Agent>(MissionOnTickPatch.AgentRecoveryTimers.Keys))
                {
                    if (IsTrackableDebugAlly(agent))
                    {
                        SetDebugAllyAgent(agent);
                        return;
                    }
                }
            }
            catch (Exception)
            {
                debugAllyAgent = null;
            }
        }

        private static void SetDebugAllyAgent(Agent agent)
        {
            debugAllyAgent = agent;
            Print("STA ally " + GetDebugName(agent), new Color(0.64f, 0.78f, 0.22f));
        }

        private static bool IsTrackableDebugAlly(Agent agent)
        {
            try
            {
                return agent != null &&
                    agent != heroAgent &&
                    agent.IsActive() &&
                    agent.Character != null &&
                    heroAgent != null &&
                    heroAgent.IsActive() &&
                    agent.Team != null &&
                    heroAgent.Team != null &&
                    agent.Team == heroAgent.Team;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void PrintStaminaDebug(Agent agent, string reason, double delta, StaminaTier tier, bool changedTier)
        {
            try
            {
                TrySetDebugAllyAgent(agent);

                string label;
                if (agent == heroAgent)
                {
                    label = "P";
                }
                else if (agent == debugAllyAgent)
                {
                    label = "A";
                }
                else
                {
                    return;
                }

                if (Math.Abs(delta) < 0.01)
                    return;

                bool isRegen = reason == "mov" || reason == "rst";
                if (isRegen)
                {
                    double pendingDelta;
                    PendingDebugRegenPerAgent.TryGetValue(agent.Index, out pendingDelta);
                    pendingDelta += delta;

                    if (Math.Abs(pendingDelta) < 5 && !changedTier)
                    {
                        PendingDebugRegenPerAgent[agent.Index] = pendingDelta;
                        return;
                    }

                    PendingDebugRegenPerAgent.Remove(agent.Index);
                    delta = pendingDelta;
                }

                int current = (int)Math.Round(CurrentStaminaPerAgent[agent.Index]);
                int max = (int)Math.Round(OriginalMaxStaminaPerAgent[agent.Index]);
                int percent = max > 0 ? (int)Math.Round(CurrentStaminaPerAgent[agent.Index] / OriginalMaxStaminaPerAgent[agent.Index] * 100) : 0;
                int speedPercent = (int)Math.Round(GetSpeedMultiplier(CurrentStaminaPerAgent[agent.Index] / OriginalMaxStaminaPerAgent[agent.Index]) * 100);
                int roundedDelta = (int)Math.Round(delta);
                if (roundedDelta == 0 && !changedTier)
                    return;

                string deltaText = roundedDelta > 0 ? "+" + roundedDelta.ToString() : roundedDelta.ToString();
                string tierText = changedTier ? TierCode(tier) + "*" : TierCode(tier);

                Print("STA " + label + " " + reason + " " + deltaText + " " + current + "/" + max + " " + percent + "% " + tierText + " spd" + speedPercent, new Color(0.64f, 0.78f, 0.22f));
            }
            catch (Exception)
            {
            }
        }

        private static double GetSpeedMultiplier(double staminaRatio)
        {
            return StaminaFatigueModel.GetSpeedMultiplier(staminaRatio);
        }

        private static string TierCode(StaminaTier tier)
        {
            switch (tier)
            {
                case StaminaTier.No:
                    return "N";
                case StaminaTier.Low:
                    return "L";
                case StaminaTier.Medium:
                    return "M";
                case StaminaTier.High:
                    return "H";
                default:
                    return "F";
            }
        }

        private static string GetDebugName(Agent agent)
        {
            try
            {
                string name = agent != null && agent.Name != null ? agent.Name.ToString() : "NPC";
                return name.Length > 16 ? name.Substring(0, 16) : name;
            }
            catch (Exception)
            {
                return "NPC";
            }
        }
#endif
    }

    [HarmonyPatch(typeof(Mission), "OnAgentRemoved")]
    class MissionOnAgentRemovedPatch
    {
        public static void Postfix(Mission __instance, Agent affectedAgent)
        {
            if (affectedAgent != MissionSpawnAgentPatch.heroAgent)
            {
                MissionOnTickPatch.AgentRecoveryTimers.Remove(affectedAgent);
                MissionOnTickPatch.AgentMovementFatigueTimers.Remove(affectedAgent);
                MissionSpawnAgentPatch.CurrentStaminaPerAgent.Remove(affectedAgent.Index);
                MissionSpawnAgentPatch.CurrentMaxStaminaPerAgent.Remove(affectedAgent.Index);
                MissionSpawnAgentPatch.OriginalMaxStaminaPerAgent.Remove(affectedAgent.Index);
                MissionSpawnAgentPatch.CurrentTierPerAgent.Remove(affectedAgent.Index);
                MissionSpawnAgentPatch.QueuedAgentUpdates.Remove(affectedAgent.Index);
                MissionSpawnAgentPatch.QueuedAgentUpdateRatios.Remove(affectedAgent.Index);
                AgentAttackStaminaPatch.LastChargedMeleeAttackTimePerAgent.Remove(affectedAgent.Index);
                AgentAttackStaminaPatch.LastChargedRangedAttackTimePerAgent.Remove(affectedAgent.Index);
                AgentAttackStaminaPatch.ClearActionChannelCache(affectedAgent);
#if DEBUG
                MissionSpawnAgentPatch.PendingDebugRegenPerAgent.Remove(affectedAgent.Index);
#endif

                if (affectedAgent == MissionSpawnAgentPatch.debugAllyAgent)
                {
                    MissionSpawnAgentPatch.debugAllyAgent = null;
                }
            }
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
                MissionOnTickPatch.AgentRecoveryTimers.Remove(affectedAgent);
                MissionOnTickPatch.AgentMovementFatigueTimers.Remove(affectedAgent);
                MissionSpawnAgentPatch.CurrentStaminaPerAgent.Remove(affectedAgent.Index);
                MissionSpawnAgentPatch.CurrentMaxStaminaPerAgent.Remove(affectedAgent.Index);
                MissionSpawnAgentPatch.OriginalMaxStaminaPerAgent.Remove(affectedAgent.Index);
                MissionSpawnAgentPatch.CurrentTierPerAgent.Remove(affectedAgent.Index);
                MissionSpawnAgentPatch.QueuedAgentUpdates.Remove(affectedAgent.Index);
                MissionSpawnAgentPatch.QueuedAgentUpdateRatios.Remove(affectedAgent.Index);
                AgentAttackStaminaPatch.LastChargedMeleeAttackTimePerAgent.Remove(affectedAgent.Index);
                AgentAttackStaminaPatch.LastChargedRangedAttackTimePerAgent.Remove(affectedAgent.Index);
                AgentAttackStaminaPatch.ClearActionChannelCache(affectedAgent);
#if DEBUG
                MissionSpawnAgentPatch.PendingDebugRegenPerAgent.Remove(affectedAgent.Index);
#endif

                if (affectedAgent == MissionSpawnAgentPatch.debugAllyAgent)
                {
                    MissionSpawnAgentPatch.debugAllyAgent = null;
                }
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
            if (MissionSpawnAgentPatch.CurrentStaminaPerAgent.Count > 0 && StaminaSettings.Instance.StaminaAffectsCrushThrough)
            {
                double attackerStaminaRatio = MissionSpawnAgentPatch.CurrentStaminaPerAgent[attackerAgent.Index] / MissionSpawnAgentPatch.OriginalMaxStaminaPerAgent[attackerAgent.Index];
                double defenderStaminaRatio = MissionSpawnAgentPatch.CurrentStaminaPerAgent[defenderAgent.Index] / MissionSpawnAgentPatch.OriginalMaxStaminaPerAgent[defenderAgent.Index];

                // if defender stamina low and attack wasn't crushed through, let it through
                if (defenderStaminaRatio < StaminaSettings.Instance.LowStaminaRemaining)
                {
                    __result = true;
                }

                // if defender stamina high and attack crushed through, block it
                if (defenderStaminaRatio > StaminaSettings.Instance.HighStaminaRemaining || attackerStaminaRatio <= StaminaSettings.Instance.LowStaminaRemaining)
                {
                    __result = false;
                }
            }
        }
    }
}
