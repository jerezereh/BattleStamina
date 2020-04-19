using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;

namespace BattleStamina
{
    public class BattleStamina : MBSubModuleBase
    {
        public string version;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            XmlReader reader = XmlReader.Create("../../Modules/BattleStamina/SubModule.xml");
            reader.ReadToDescendant("Version");
            version = reader.GetAttribute("value");

            StaminaProperties.Instance = Helper.Deserialize<StaminaProperties>("../../Modules/BattleStamina/ModuleData/Settings.xml");

            try
            {
                new Harmony("mod.jrzrh.BattleStamina").PatchAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error initializing Harmony patches:\n\n" + ex);
            }
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            InformationManager.DisplayMessage(new InformationMessage("Loaded BattleStamina " + version + ".", Color.FromUint(4282569842U)));
        }
    }
}
