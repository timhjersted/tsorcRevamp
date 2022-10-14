using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Throwing
{
    class Firebomb : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Firebomb");
            Tooltip.SetDefault("Explodes, dealing damage in a small area");
        }
        public override void SetDefaults()
        {

            Item.rare = ItemRarityID.Blue;
            Item.width = 22;
            Item.damage = 100;
            Item.height = 24;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 8f;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = 500;
            Item.shoot = ModContent.ItemType<Firebomb>();
            Item.shootSpeed = 6.5f;
            Item.useAnimation = 50;
            Item.useTime = 50;
            Item.UseSound = SoundID.Item1;
            Item.consumable = true;
            Item.maxStack = 999;
            Item.DamageType = DamageClass.Throwing;
        }
    }
}