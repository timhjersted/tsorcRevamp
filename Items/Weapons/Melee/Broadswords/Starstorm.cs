using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class Starstorm : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Causes stars to storm from the sky");
        }
        public override void SetDefaults()
        {
            Item.width = 42;
            Item.height = 42;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.damage = 40;
            Item.knockBack = 6;
            Item.autoReuse = true;
            Item.alpha = 100;
            Item.scale = 1.15f;
            Item.UseSound = SoundID.Item9;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.LightRed_4;
            Item.DamageType = DamageClass.Melee;
        }


        public override bool? UseItem(Player player)
        {
            float x = (float)(Main.mouseX + Main.screenPosition.X);
            float y = (float)(Main.mouseY + Main.screenPosition.Y);
            float speedX = (Main.rand.Next(-20, 20)) / 10f;
            float speedY = 14.9f;
            int type = ModContent.ProjectileType<Projectiles.Melee.StarstormProjectile>();
            int damage = (int)player.GetTotalDamage(DamageClass.Magic).ApplyTo(Item.damage);
            float knockback = 3.0f;
            int owner = player.whoAmI;
            y = player.position.Y - 800f;

            if (player.whoAmI == Main.myPlayer)
            {
                for (int i = 0; i < 5; i++)
                {
                    Projectile.NewProjectile(player.GetSource_ItemUse(Item), x + ((i * 40) - 80), y, speedX, speedY, type, damage, knockback, owner);
                }
            }
            return true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Starfury, 1);
            recipe.AddIngredient(ItemID.FallenStar, 30);
            //recipe.AddIngredient(ItemID.SoulofLight, 3);
            recipe.AddIngredient(ItemID.AdamantiteBar, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 30000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
