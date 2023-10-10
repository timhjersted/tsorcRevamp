using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class MagmaTooth : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Chance to light enemies on hellish fire.");
        }
        public override void SetDefaults()
        {
            Item.width = 58;
            Item.height = 58;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 32;
            Item.scale = 1.2f;
            Item.useTime = 32;
            Item.maxStack = 1;
            Item.damage = 44;
            Item.knockBack = 8;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.Orange_3;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.OrangeRed;
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.NewProjectile(Projectile.GetSource_None(), target.Center, Vector2.Zero, ProjectileID.Volcano, (int)player.GetTotalDamage(DamageClass.Melee).ApplyTo(Item.damage), hit.Knockback, Main.myPlayer);
            target.AddBuff(BuffID.OnFire3, 300, false);
        }
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            int dust = Dust.NewDust(new Vector2((float)hitbox.X, (float)hitbox.Y), hitbox.Width, hitbox.Height, 6, player.velocity.X, player.velocity.Y, 100, default, 2f);
            Main.dust[dust].noGravity = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.FieryGreatsword, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
