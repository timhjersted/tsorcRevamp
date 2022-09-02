using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Shortswords
{
    class ClaiomhSolais : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Seize the day");
        }
        public override void SetDefaults()
        {
            Item.width = 68;
            Item.height = 68;
            Item.useStyle = ItemUseStyleID.Rapier;
            Item.shoot = ModContent.ProjectileType<Projectiles.Shortswords.ClaiomhSolaisProjectile>();
            Item.shootSpeed = 2.1f;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.useAnimation = 18;
            Item.useTime = 18;
            Item.damage = 62;
            Item.knockBack = 6f;
            Item.autoReuse = true;
            Item.scale = 1f;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.Pink_5;
            Item.DamageType = DamageClass.Melee;
        }
        public override bool MeleePrefix()
        {
            return true;
        }
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            //This is the same general effect done with the Fiery Greatsword
            int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 15, player.velocity.X * 0.2f + player.direction * 3, player.velocity.Y * 0.2f, 100, default, 1.0f);
            Main.dust[dust].noGravity = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CobaltBar, 5);
            recipe.AddIngredient(ItemID.MythrilBar, 5);
            recipe.AddIngredient(ItemID.AdamantiteBar, 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
