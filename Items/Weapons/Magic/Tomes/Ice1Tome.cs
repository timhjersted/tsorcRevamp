using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Magic.Tomes
{
    class Ice1Tome : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Ice 1 Tome");
            /* Tooltip.SetDefault("A lost beginner's tome" +
                "\nMultiple shots can be controlled and stacked" +
                "\nallowing for high burst damage" +
                "\nDrops a small icicle upon collision" +
                "\nSlows enemies" +
                "\nCan be upgraded"); */
        }

        //This stores the original, true mana cost of the item. We have to change item.mana later to cause it to use less/none while it's not actually firing
        int storeManaCost;
        public override void SetDefaults()
        {
            Item.damage = 11;
            Item.height = 10;
            Item.knockBack = 0.1f;
            Item.channel = true;
            Item.autoReuse = true;
            Item.rare = ItemRarityID.Green;
            Item.shootSpeed = 9;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.mana = 8;
            storeManaCost = Item.mana;
            Item.useAnimation = 19;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 19;
            Item.value = PriceByRarity.Green_2;
            Item.width = 34;
            Item.shoot = ModContent.ProjectileType<Projectiles.Ice1Ball>();
        }

        public override bool CanUseItem(Player player)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Ice1Ball>()] < 5)
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
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
