using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.Multiplayer;
using UIExtenderLib;
using UIExtenderLib.Prefab;
using UIExtenderLib.ViewModel;

namespace BattleStamina
{
    [PrefabExtension("AgentStatus", "descendant::AgentHealthWidget[@Id='ShieldHealthWidget']")]
    class HeroStaminaBar : PrefabExtensionInsertAsSiblingPatch
    {
        public override InsertType Type => InsertType.Append;
        public override string Name => "HeroStaminaBar";
    }


    [ViewModelMixin]
    public class MissionAgentStatusViewModelMixin : BaseViewModelMixin<MissionAgentStatusVM>
    {
        private double _heroStamina;
        private double _heroStaminaMax;

        public MissionAgentStatusViewModelMixin(MissionAgentStatusVM vm) : base(vm)
        {
        }

        [DataSourceProperty]
        public double HeroStamina
        {
            get
            {
                return this._heroStamina;
            }
            set
            {
                if (value == this._heroStamina)
                    return;
                this._heroStamina = value;
                _vm.OnPropertyChanged(nameof(HeroStamina));
            }
        }

        [DataSourceProperty]
        public double HeroStaminaMax
        {
            get
            {
                return this._heroStaminaMax;
            }
            set
            {
                if (value == this._heroStaminaMax)
                    return;
                this._heroStaminaMax = value;
                _vm.OnPropertyChanged(nameof(HeroStaminaMax));
            }
        }

        //public override void Refresh()
        //{
        //    var horses = MobileParty.MainParty.ItemRoster.Where(i => i.EquipmentElement.Item.ItemCategory.Id == new MBGUID(671088673));
        //    var newTooltip = horses.Aggregate("Horses: ", (s, element) => $"{s}\n{element.EquipmentElement.Item.Name}: {element.Amount}");

        //    if (newTooltip != _horsesTooltip)
        //    {
        //        _horsesAmount = horses.Sum(item => item.Amount);
        //        _horsesTooltip = newTooltip;

        //        _vm.OnPropertyChanged(nameof(HorsesAmount));
        //    }
        //}
    }
}
