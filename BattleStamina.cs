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
        public ResourceDepot resourceDepot = new ResourceDepot("../../Modules/BattleStamina/");

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            XmlReader reader = XmlReader.Create("../../Modules/BattleStamina/SubModule.xml");
            reader.ReadToDescendant("Version");
            version = reader.GetAttribute("value");
            InitializeSprites();
            LoadSprites();

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

        private void InitializeSprites()
        {
            resourceDepot.AddLocation("Sprites/");
            resourceDepot.CollectResources();
            UIResourceManager.SpriteData.Load(resourceDepot);
        }

        private void LoadSprites()
        {
            AddSprites("BattleStamina");
        }

        public void AddSprites(string spriteSheet, int sheetId = 1)
        {
            SpriteCategory spriteCategory = UIResourceManager.SpriteData.SpriteCategories[spriteSheet];
            spriteCategory.Load(UIResourceManager.ResourceContext, resourceDepot);
            var texture = TaleWorlds.Engine.Texture.LoadTextureFromPath($"{spriteSheet}_{sheetId}.png",
                BasePath.Name + "Modules/BattleStamina/Sprites/SpriteSheets/" + spriteSheet);
            texture.PreloadTexture();
            var texture2D = new TaleWorlds.TwoDimension.Texture(new EngineTexture(texture));
            UIResourceManager.SpriteData.SpriteCategories[spriteSheet].SpriteSheets[sheetId - 1] = texture2D;
        }
    }
}
