using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Shortswords
{
    class Laevateinn : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Strike true." + "\nWielded like a shortsword");
        }

        public override void SetDefaults()
        {
            Item.width = 60;
            Item.height = 60;
            Item.useStyle = ItemUseStyleID.Rapier;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<Projectiles.Shortswords.LaevateinnProjectile>(); // The projectile is what makes a shortsword work
            Item.shootSpeed = 2.1f; // This value bleeds into the behavior of the projectile as velocity, keep that in mind when tweaking values
            Item.useAnimation = 9;
            Item.useTime = 9;
            Item.damage = 50;
            Item.knockBack = 3.8f;
            Item.autoReuse = true;
            Item.useTurn = false;
            Item.scale = 0.95f;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.Pink_5;
            Item.DamageType = DamageClass.Melee;
        }
        public override bool MeleePrefix()
        {
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            //recipe.AddIngredient(ItemID.CobaltBar, 5);
            //recipe.AddIngredient(ItemID.MythrilBar, 5);
            recipe.AddIngredient(ItemID.AdamantiteBar, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 15, player.velocity.X * 0.2f + player.direction * 3, player.velocity.Y * 0.2f, 100, default, 1.0f);
            Main.dust[dust].noGravity = true;
        }
    }
}
