using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Ammo;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Ranged.Ammo
{
    public class AbyssArrowProjectile : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.aiStyle = 1;
            Projectile.friendly = true;
            Projectile.height = 18;
            Projectile.damage = 25;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.width = 16;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 1200;
            Projectile.light = 0.5f;
            Projectile.penetrate = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<AbyssInferno>(), 3 * 60);
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.8f, 0.1f, 0.7f);

            int dust = Dust.NewDust(
                Projectile.Center - new Vector2(4, 4),
                8, 
                8, 
                DustID.PinkTorch, 
                Projectile.velocity.X * 0.2f, 
                Projectile.velocity.Y * 0.2f,
                0, 
                Color.Pink, 
                1.2f 
            );
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity *= 0.2f;
        }

		public override void OnKill(int timeLeft)
        {
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
            for (int i = 0; i < 5; i++)
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, 223, 1f);
                dust.noGravity = true;
            }
        }
    }
}