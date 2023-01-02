using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    public class Ragnarok : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ragnarok");

        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Cyan;
            Item.useTurn = false;
            Item.autoReuse = true;
            Item.damage = 170;
            Item.width = 62;
            Item.height = 62;
            Item.knockBack = 10;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 15;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 21;
            Item.value = PriceByRarity.Cyan_9;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
        }
    }
}
