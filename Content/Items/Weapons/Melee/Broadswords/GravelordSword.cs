using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.Localization;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Broadswords
{
    public class GravelordSword : Greatsword
    {
        protected internal override Vector2 Grip => new(17f, 121f);
        protected internal override Vector2 HiltTop => new(33f, 105f);
        protected internal override Vector2 BladeHeel => new(47f, 98f);
        protected internal override Vector2 CuttingEdge => new(72f, 85f);
        protected internal override Vector2 BladeTip => new(128f, 1f);

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ComboSteps[2].DamageMultiplier);

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.width = 130;
            Item.height = 138;
            Item.scale = 0.65f;

            Item.damage = 44;
            Item.knockBack = 6f;

            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.Orange_3;
        }
    }
}
