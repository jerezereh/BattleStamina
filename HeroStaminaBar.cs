using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Bannerlord.UIExtenderEx.ViewModels;
using BattleStamina.Patches;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.Multiplayer;

namespace BattleStamina
{
    [PrefabExtension("AgentStatus", "descendant::AgentHealthWidget[@Id='HorseHealthWidget']")]
    class HeroStaminaWidgetMounted : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Prepend;

        [PrefabExtensionFileName]
        public string Name => "HeroStaminaBarMounted";
    }

    [PrefabExtension("AgentStatus", "descendant::AgentHealthWidget[@Id='HorseHealthWidget']")]
    class HeroStaminaWidgetDismounted : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Prepend;

        [PrefabExtensionFileName]
        public string Name => "HeroStaminaBarDismounted";
    }


    [ViewModelMixin(nameof(MissionAgentStatusVM.Tick))]
    public class MissionAgentStatusViewModelMixin : BaseViewModelMixin<MissionAgentStatusVM>
    {
        private int _heroStamina = 1;
        private int _heroStaminaMax = 1;
        private readonly MissionAgentStatusVM _vm;

        public MissionAgentStatusViewModelMixin(MissionAgentStatusVM vm) : base(vm)
        {
            _vm = ViewModel;
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
                _vm.OnPropertyChanged(nameof(HeroStamina));
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
                _vm.OnPropertyChanged(nameof(HeroStaminaMax));
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
