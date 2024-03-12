using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Axes;

namespace tsorcRevamp.Items.Weapons.Melee.Axes
{
    public class AncientFireAxe : ModItem
    {
        public override void SetStaticDefaults()
        {

        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Green;
            Item.damage = 23;
            Item.width = 50;
            Item.height = 42;
            Item.knockBack = 10f;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 35;
            Item.useTime = 35;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Green_2;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.OrangeRed;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.GoldAxe);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, 12 * 60, false);
            Projectile Fireball = Projectile.NewProjectileDirect(Projectile.GetSource_None(), player.Center, UsefulFunctions.Aim(player.Center, target.Center, 5f), ModContent.ProjectileType<AncientFireAxeFireball>(), (int)player.GetTotalDamage(DamageClass.Melee).ApplyTo(Item.damage), player.GetTotalKnockback(DamageClass.Melee).ApplyTo(Item.knockBack), Main.myPlayer, player.GetTotalCritChance(DamageClass.Melee) + Item.crit);
        }

        public override void MeleeEffects(Terraria.Player player, Rectangle rectangle)
        {
            int dust = Dust.NewDust(new Vector2((float)rectangle.X, (float)rectangle.Y), rectangle.Width, rectangle.Height, 6, (player.velocity.X * 0.2f) + (player.direction * 3), player.velocity.Y * 0.2f, 100, default, 1.9f);
            Main.dust[dust].noGravity = true;
        }

    }
}
