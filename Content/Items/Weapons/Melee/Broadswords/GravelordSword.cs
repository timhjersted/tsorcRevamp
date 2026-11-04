using Microsoft.Xna.Framework;
using Terraria.ID;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Broadswords
{
    public class GravelordSword : Greatsword
    {
        protected internal override Vector2 Grip => new(28f, 113f);
        protected internal override Vector2 BladeHeel => new(47f, 98f);
        protected internal override Vector2 CuttingEdge => new(72f, 85f);
        protected internal override Vector2 BladeTip => new(128f, 1f);

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
