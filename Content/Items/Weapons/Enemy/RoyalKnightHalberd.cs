using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    /// <summary>Royal halberd wielded by the Knight of Gwyn.
    /// 64x64 angled sprite with broad steel axe blade, armor-piercing rear fluke, central thrusting pike, and reinforced shaft.</summary>
    public class RoyalKnightHalberd : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 64;
            Item.height = 64;
            Item.damage = 55;
            Item.knockBack = 6.5f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.DamageType = DamageClass.Melee;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Lime;
        }
    }
}
