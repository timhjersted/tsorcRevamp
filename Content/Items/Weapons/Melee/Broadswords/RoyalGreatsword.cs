using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.Localization;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Broadswords
{
    public class RoyalGreatsword : Greatsword
    {
        public const int BaseDamage = 64;
        public const int SwingTicks = 48;
        public const float FinisherDamageMultiplier = 1.75f;
        public static readonly Vector2 GripPosition = new(16f, 80f);

        // At base speed the cuts last 48/48/57 ticks. Each starts from rest, accelerates, then decelerates
        // into the next windup's entry pose; GreatswordMotion.Live gates damage to the moving part.
        private static readonly GreatswordStep[] RoyalSteps =
        {
            new GreatswordStep
            {
                LoadedAngle = GreatswordMotion.High,
                FinishAngle = GreatswordMotion.Low,
                WindupFraction = 0.30f,
                AccelFraction = 0.22f,
                StartSpeed = 0f,
                DecelPower = 2.5f,
                DurationScale = 1f,
                DamageMultiplier = 1f
            },
            new GreatswordStep
            {
                LoadedAngle = 0.36f,
                FinishAngle = -1.70f,
                WindupFraction = 0.24f,
                AccelFraction = 0.20f,
                StartSpeed = 0f,
                DecelPower = 2.5f,
                DurationScale = 1f,
                DamageMultiplier = 1f
            },
            new GreatswordStep
            {
                LoadedAngle = GreatswordMotion.High,
                FinishAngle = GreatswordMotion.Carry,
                WindupFraction = 0.38f,
                AccelFraction = 0.20f,
                StartSpeed = 0f,
                DecelPower = 4f,
                DurationScale = 1.2f,
                DamageMultiplier = FinisherDamageMultiplier,
                Slam = true
            }
        };

        protected internal override Vector2 Grip => GripPosition;
        protected internal override Vector2 HiltTop => new(20f, 76f);
        protected internal override Vector2 BladeHeel => new(26f, 66f);
        protected internal override Vector2 CuttingEdge => new(56f, 40f);
        protected internal override Vector2 BladeTip => new(92f, 4f);

        public override GreatswordStep[] ComboSteps => RoyalSteps;
        public override bool UsesSlashTrail => true;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(FinisherDamageMultiplier);

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.width = 96;
            Item.height = 96;
            Item.damage = BaseDamage;
            Item.useTime = SwingTicks;
            Item.useAnimation = SwingTicks;
            Item.knockBack = 7f;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.Orange_3;

            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = new Color(145, 168, 196);
            // Fit the driven trail to the 107px span from the middle of the grip to the blade tip.
            instancedGlobal.slashVisualScale = 0.594f;
        }
    }
}
