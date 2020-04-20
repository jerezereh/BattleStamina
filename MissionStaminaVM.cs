using BattleStamina.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BattleStamina
{
    class MissionStaminaVM : ViewModel
    {
        private double _heroStamina;
        private double _heroStaminaMax;

        public MissionStaminaVM()
        {
            _heroStamina = MissionBuildAgentPatch.MaxStaminaPerAgent[MissionBuildAgentPatch.heroAgent];
            _heroStaminaMax = MissionBuildAgentPatch.MaxStaminaPerAgent[MissionBuildAgentPatch.heroAgent];
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
                this.OnPropertyChanged(nameof(HeroStamina));
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
                this.OnPropertyChanged(nameof(HeroStaminaMax));
            }
        }
    }
}
