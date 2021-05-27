using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Pets;

namespace tsorcRevamp.Projectiles
{
    class tsorcGlobalProjectile : GlobalProjectile
    {
        public override bool PreAI(Projectile projectile) {
            Player player = Main.player[projectile.owner];
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();


            if (projectile.owner == Main.myPlayer && !projectile.hostile && modPlayer.MiakodaCrescentBoost && !(projectile.type == (int)ModContent.ProjectileType<MiakodaCrescent>() || projectile.type == (int)ModContent.ProjectileType<ShulletBellDark>() || projectile.type == (int)ModContent.ProjectileType<ShulletBellLight>() || projectile.type == (int)ModContent.ProjectileType<Bloodsign>())) {
                if (Main.rand.Next(2) == 0) {
                    int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 164, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = false;
                }
            }

            if (projectile.owner == Main.myPlayer && !projectile.hostile && modPlayer.MiakodaNewBoost && !(projectile.type == (int)ModContent.ProjectileType<MiakodaNew>() || projectile.type == (int)ModContent.ProjectileType<ShulletBellDark>() || projectile.type == (int)ModContent.ProjectileType<ShulletBellLight>() || projectile.type == (int)ModContent.ProjectileType<Bloodsign>())) {
                if (Main.rand.Next(2) == 0) {
                    int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 57, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 130, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                }
            }
            return true;
        }
        public override void OnHitNPC(Projectile projectile, NPC target, int damage, float knockback, bool crit) {
            Player player = Main.player[projectile.owner];
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            if (projectile.owner == Main.myPlayer && !projectile.hostile && modPlayer.MiakodaCrescentBoost && projectile.type != (int)ModContent.ProjectileType<MiakodaCrescent>()) {
                target.AddBuff(ModContent.BuffType<Buffs.CrescentMoonlight>(), 180); // Adds the ExampleJavelin debuff for a very small DoT
            }

            if (projectile.owner == Main.myPlayer && !projectile.hostile && modPlayer.MiakodaNewBoost && projectile.type != (int)ModContent.ProjectileType<MiakodaNew>()) {
                target.AddBuff(BuffID.Midas, 300);
            }
        }
    }
}
