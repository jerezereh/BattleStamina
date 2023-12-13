using HarmonyLib;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;
using Bannerlord.UIExtenderEx;

namespace BattleStamina
{
    public class BattleStamina : MBSubModuleBase
    {
        public string version;
        public ResourceDepot resourceDepot = new ResourceDepot();
        private UIExtender _uiExtender = new UIExtender("BattleStamina");

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            XmlReader reader = XmlReader.Create("../../Modules/BattleStamina/SubModule.xml");
            reader.ReadToDescendant("Version");
            version = reader.GetAttribute("value");

            InitializeSprites();
            LoadSprites();

            new Harmony("mod.jrzrh.BattleStamina").PatchAll();
            _uiExtender.Register(typeof(BattleStamina).Assembly);
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            InformationManager.DisplayMessage(new InformationMessage("Loaded BattleStamina " + version + ".", Color.FromUint(4282569842U)));
            _uiExtender.Enable();
        }

        private void InitializeSprites()
        {
            resourceDepot.AddLocation("../../Modules/BattleStamina/", "Sprites/");
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
            var texture2D = new Texture(new EngineTexture(texture));
            UIResourceManager.SpriteData.SpriteCategories[spriteSheet].SpriteSheets[sheetId - 1] = texture2D;
        }
    }
}
