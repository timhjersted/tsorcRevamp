using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Magic.Tomes
{
    class Ice2Tome : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Ice 2 Tome");
            /* Tooltip.SetDefault("A lost tome for artisans, with a high rate of casting." +
                                "\nSlows and occasionally freezes enemies" +
                                "\nCan be upgraded"); */
        }

        //This stores the original, true mana cost of the item. We have to change item.mana later to cause it to use less/none while it's not actually firing
        int storeManaCost2;
        public override void SetDefaults()
        {
            Item.damage = 15;
            Item.height = 10;
            Item.knockBack = 0.1f;
            Item.rare = ItemRarityID.Orange;
            Item.channel = true;
            Item.autoReuse = true;
            Item.shootSpeed = 13;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.mana = 14;
            storeManaCost2 = Item.mana;
            Item.useAnimation = 19;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 19;
            Item.value = PriceByRarity.Orange_3;
            Item.width = 34;
            Item.shoot = ModContent.ProjectileType<Projectiles.Ice2Ball>();
        }

        public override bool CanUseItem(Player player)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Ice2Ball>()] < 5 && player == Main.LocalPlayer)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<Ice1Tome>(), 1);
            recipe.AddIngredient(ItemID.JungleSpores, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 8000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
