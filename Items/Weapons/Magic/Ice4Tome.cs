using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class Ice4Tome : ModItem
    {


        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ice 4 Tome");
            Tooltip.SetDefault("A lost legendary tome. Shatter your enemies with a freezing hailstorm." +
                "\nConstant hits are capable of completely imprisoning weaker foes.");
        }

        //This stores the original, true mana cost of the item. We have to change item.mana later to cause it to use less/none while it's not actually firing
        int storeManaCost4;
        public override void SetDefaults()
        {
            Item.damage = 120;
            Item.height = 10;
            Item.knockBack = 0.1f;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Lime;
            Item.channel = true;
            Item.autoReuse = true;
            Item.shootSpeed = 45;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.mana = 45;
            storeManaCost4 = Item.mana;
            Item.useAnimation = 30;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 30;
            Item.value = PriceByRarity.Lime_7;
            Item.width = 34;
            Item.shoot = ModContent.ProjectileType<Projectiles.Ice4Ball>();
        }

        public override bool CanUseItem(Player player)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Ice4Ball>()] < 5)
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
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("Ice3Tome").Type, 1);
            recipe.AddIngredient(ItemID.LunarTabletFragment, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 80000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
