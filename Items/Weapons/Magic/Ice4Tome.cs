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
            Item.damage = 140;
            Item.height = 10;
            Item.knockBack = 0.1f;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Lime;
            Item.channel = true;
            Item.autoReuse = true;
            Item.shootSpeed = 45;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.mana = 50;
            storeManaCost4 = Item.mana;
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
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
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<Ice3Tome>());
            recipe.AddIngredient(ItemID.SoulofMight, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 80000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
