using SandBox.GauntletUI;
using System.Collections.Generic;
using TaleWorlds.Engine.Screens;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.ExtraWidgets;
using TaleWorlds.MountAndBlade.View.Screen;
using TaleWorlds.TwoDimension;

namespace BattleStamina
{
    class AgentStaminaWidget : Widget
    {
        private float AnimationDelay = 0.2f;
        private float AnimationDuration = 0.8f;
        private int _prevHealth = -1;
        private List<AgentStaminaWidget.HealthDropData> _healthDrops;
        private int _health;
        private int _maxHealth;
        private bool _showHealthBar;
        private FillBarWidget _healthBar;
        private Widget _healthDropContainer;
        private Brush _healthDropBrush;

        public AgentStaminaWidget(UIContext context) : base(context)
        {
            this._healthDrops = new List<AgentStaminaWidget.HealthDropData>();
            this.CheckVisibility();
        }

        private void CreateHealthDrop(Widget container, int preHealth, int currentHealth)
        {
            double num1 = (double)(container.Size.X) / (double)this.Context.Scale;
            float num2 = Mathf.Ceil((float)(num1 * ((double)(preHealth - currentHealth) / (double)this._maxHealth)));
            float num3 = Mathf.Floor((float)(num1 * ((double)currentHealth / (double)this._maxHealth)));
            Widget widget = new Widget(this.Context)
            {
                WidthSizePolicy = SizePolicy.Fixed,
                HeightSizePolicy = SizePolicy.Fixed,
                Brush = this.HealthDropBrush,
                SuggestedWidth = num2
            };
            widget.SuggestedHeight = (float)widget.Brush.Sprite.Height;
            widget.HorizontalAlignment = HorizontalAlignment.Left;
            widget.VerticalAlignment = VerticalAlignment.Center;
            widget.PositionXOffset = num3;
            widget.ParentWidget = container;
            this._healthDrops.Add(new AgentStaminaWidget.HealthDropData(widget, this.AnimationDelay + this.AnimationDuration));
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            if (this.HealthBar != null && this.HealthBar.IsVisible)
            {
                for (int index = this._healthDrops.Count - 1; index >= 0; --index)
                {
                    AgentStaminaWidget.HealthDropData healtDrop = this._healthDrops[index];
                    healtDrop.lifeTime -= dt;
                    if ((double)healtDrop.lifeTime <= 0.0)
                    {
                        this.HealthDropContainer.RemoveChild(healtDrop.widget);
                        this._healthDrops.RemoveAt(index);
                    }
                    else
                    {
                        float num = Mathf.Min(1f, healtDrop.lifeTime / this.AnimationDuration);
                        healtDrop.widget.Brush.AlphaFactor = num;
                    }
                }
            }
            this.CheckVisibility();
        }

        private void HealthChanged(bool createDropVisual = true)
        {
            this.HealthBar.MaxAmount = this._maxHealth;
            this.HealthBar.InitialAmount = this.Health;
            if (this._prevHealth <= this.Health)
                return;
            this.CreateHealthDrop(this.HealthDropContainer, this._prevHealth, this.Health);
        }

        private void CheckVisibility()
        {
            bool flag = this.ShowHealthBar;
            if (flag)
                flag = (double)this._health > 0.0 || this._healthDrops.Count > 0;
            this.IsVisible = flag;
        }

        [Editor(false)]
        public int Health
        {
            get
            {
                return this._health;
            }
            set
            {
                if (this._health == value)
                    return;
                this._prevHealth = this._health;
                this._health = value;
                this.HealthChanged(true);
                this.OnPropertyChanged((object)value, nameof(Health));
            }
        }

        [Editor(false)]
        public int MaxHealth
        {
            get
            {
                return this._maxHealth;
            }
            set
            {
                if (this._maxHealth == value)
                    return;
                this._maxHealth = value;
                this.HealthChanged(false);
                this.OnPropertyChanged((object)value, nameof(MaxHealth));
            }
        }

        [Editor(false)]
        public FillBarWidget HealthBar
        {
            get
            {
                return this._healthBar;
            }
            set
            {
                if (this._healthBar == value)
                    return;
                this._healthBar = value;
                this.OnPropertyChanged((object)value, nameof(HealthBar));
            }
        }

        [Editor(false)]
        public Widget HealthDropContainer
        {
            get
            {
                return this._healthDropContainer;
            }
            set
            {
                if (this._healthDropContainer == value)
                    return;
                this._healthDropContainer = value;
                this.OnPropertyChanged((object)value, nameof(HealthDropContainer));
            }
        }

        [Editor(false)]
        public Brush HealthDropBrush
        {
            get
            {
                return this._healthDropBrush;
            }
            set
            {
                if (this._healthDropBrush == value)
                    return;
                this._healthDropBrush = value;
                this.OnPropertyChanged((object)value, nameof(HealthDropBrush));
            }
        }

        [Editor(false)]
        public bool ShowHealthBar
        {
            get
            {
                return this._showHealthBar;
            }
            set
            {
                if (this._showHealthBar == value)
                    return;
                this._showHealthBar = value;
                this.OnPropertyChanged((object)value, nameof(ShowHealthBar));
            }
        }

        private class HealthDropData
        {
            public Widget widget;
            public float lifeTime;

            public HealthDropData(Widget widget, float lifeTime)
            {
                this.widget = widget;
                this.lifeTime = lifeTime;
            }
        }
    }
}
