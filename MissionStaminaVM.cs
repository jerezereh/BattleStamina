using BattleStamina.Patches;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Multiplayer;

namespace BattleStamina
{
    class MissionStaminaVM : MissionAgentStatusVM
    {
        private double _heroStamina;
        private double _heroStaminaMax;

        public MissionStaminaVM(Mission mission, Camera missionCamera) : base(mission, missionCamera)
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
