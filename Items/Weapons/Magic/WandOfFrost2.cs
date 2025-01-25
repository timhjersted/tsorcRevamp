using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class WandOfFrost2 : ModItem
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Wand of Frost II");
            /* Tooltip.SetDefault("Reforged to reveal the full power of this ancient ice spell" +
                                "\nCan pass through walls"); */
            Item.staff[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.damage = 52;
            Item.height = 30;
            Item.knockBack = 6;
            Item.rare = ItemRarityID.Pink; //yes, despite not taking any mech boss items
            Item.shootSpeed = 13f;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 22;
            Item.useAnimation = 24;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item28;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 24;
            Item.value = PriceByRarity.Pink_5;
            Item.width = 30;
            Item.shoot = ModContent.ProjectileType<Projectiles.Icicle2>();
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(); ;
            recipe.AddIngredient(ModContent.ItemType<WandOfFrost>(), 1);
            recipe.AddIngredient(ItemID.CrystalShard, 15);
            recipe.AddIngredient(ItemID.SoulofFlight, 9);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 25000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
