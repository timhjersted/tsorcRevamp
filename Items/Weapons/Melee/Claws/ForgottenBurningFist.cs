using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles;

namespace tsorcRevamp.Items.Weapons.Melee.Claws
{
    public class ForgottenBurningFist : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Randomly casts a great fireball explosion.");
        }

        public override void SetDefaults()
        {
            Item.autoReuse = true;
            Item.damage = 62;
            Item.width = 24;
            Item.height = 20;
            Item.knockBack = 3;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 8;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 21;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
            Item.shoot = ModContent.ProjectileType<Nothing>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.AdamantiteBar, 1);
            recipe.AddIngredient(ItemID.Fireblossom, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 30000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override bool? UseItem(Player player)
        {
            if (Main.rand.NextBool(40))
            {
                Projectile.NewProjectile(player.GetSource_ItemUse(Item),
               player.position.X,
               player.position.Y,
               (float)(-40 + Main.rand.Next(80)) / 10,
               14.9f,
               ModContent.ProjectileType<Projectiles.GreatFireballBall>(),
               70,
               2.0f,
               player.whoAmI);
            }
            return true;
        }
    }
}
