using BattleStamina.Patches;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Multiplayer;
using UIExtenderLib.Interface;

namespace BattleStamina
{
    [PrefabExtension("AgentStatus", "descendant::AgentHealthWidget[@Id='HorseHealthWidget']")]
    class HeroStaminaBar : PrefabExtensionInsertAsSiblingPatch
    {
        public override InsertType Type => InsertType.Prepend;
        public override string Name => "HeroStaminaBar";
    }


    [ViewModelMixin]
    public class MissionAgentStatusViewModelMixin : BaseViewModelMixin<MissionAgentStatusVM>
    {
        private int _heroStamina = 1;
        private int _heroStaminaMax = 1;

        public MissionAgentStatusViewModelMixin(MissionAgentStatusVM vm) : base(vm)
        {
        }

        [DataSourceProperty] public int HeroStamina
        {
            get
            {
                return (int)this._heroStamina;
            }
            set
            {
                if (value == this._heroStamina)
                    return;
                this._heroStamina = value;
                _vm.TryGetTarget(out MissionAgentStatusVM target);
                target.OnPropertyChanged(nameof(HeroStamina));
            }
        }

        [DataSourceProperty] public int HeroStaminaMax
        {
            get
            {
                return (int)this._heroStaminaMax;
            }
            set
            {
                if (value == this._heroStaminaMax)
                    return;
                this._heroStaminaMax = value;
                _vm.TryGetTarget(out MissionAgentStatusVM target);
                target.OnPropertyChanged(nameof(HeroStaminaMax));
            }
        }

        public override void OnRefresh()
        {
            if (MissionSpawnAgentPatch.heroAgent != null)
            {
                int currentStamina = (int)MissionSpawnAgentPatch.CurrentStaminaPerAgent[MissionSpawnAgentPatch.heroAgent];
                HeroStamina = currentStamina > 0 ? currentStamina : 1;
                HeroStaminaMax = (int)MissionSpawnAgentPatch.MaxStaminaPerAgent[MissionSpawnAgentPatch.heroAgent];
            }
        }
    }
}
