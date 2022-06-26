using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class Starstorm : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Causes stars to storm from the sky");
        }
        public override void SetDefaults()
        {
            Item.width = 42;
            Item.height = 42;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.damage = 60;
            Item.knockBack = 6;
            Item.autoReuse = true;
            Item.alpha = 100;
            Item.scale = 1.15f;
            Item.UseSound = SoundID.Item9;
            Item.rare = ItemRarityID.LightRed;
            Item.mana = 13;
            Item.value = PriceByRarity.LightRed_4;
            Item.DamageType = DamageClass.Magic;
        }


        public override bool? UseItem(Player player)
        {
            float x = (float)(Main.mouseX + Main.screenPosition.X);
            float y = (float)(Main.mouseY + Main.screenPosition.Y);
            float speedX = (Main.rand.Next(-20, 20)) / 10f;
            float speedY = 14.9f;
            int type = ProjectileID.Starfury;
            int damage = Item.damage;
            float knockback = 3.0f;
            int owner = player.whoAmI;
            y = player.position.Y - 800f;

            for (int i = 0; i < 5; i++)
            {
                Projectile.NewProjectile(player.GetSource_ItemUse(Item), x + ((i * 40) - 80), y, speedX, speedY, type, damage, knockback, owner);
            }
            return true;
        }
        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Starfury, 1);
            recipe.AddIngredient(ItemID.FallenStar, 100);
            recipe.AddIngredient(ItemID.SoulofLight, 20);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 50000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
