using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    /// <summary>Royal halberd wielded by the Royal Knight (presentation and swing clock only; the puppet deals the damage).
    /// 62x62 angled sprite, point (61, 0.5) top-right, butt (0.5, 61.5) bottom-left: broad steel axe blade, rear fluke,
    /// central thrusting pike and reinforced shaft. useAnimation 27 matches the Pilgrim Spontoon the combos were tuned on.</summary>
    public class RoyalKnightHalberd : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 62;
            Item.height = 62;
            Item.damage = 55;
            Item.knockBack = 6.5f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 27;
            Item.useTime = 27;
            Item.DamageType = DamageClass.Melee;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarityID.Lime;
        }
    }
}
