using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Ammo;

namespace tsorcRevamp.Projectiles.Ranged.Ammo
{
    public class AbyssArrowProjectile : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.aiStyle = 1;
            Projectile.friendly = true;
            Projectile.height = 10;
            Projectile.damage = 25;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.width = 5;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 1200;
            Projectile.light = 0.5f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Daybreak, 3 * 60);
        }

        public override void AI()
        {
            int dust = Dust.NewDust(Projectile.position, 1, 1, DustID.SolarFlare, Projectile.velocity.X, Projectile.velocity.Y, 0, default, 1f);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity *= 0.2f;
            Projectile.localAI[0] = 0;
            Lighting.AddLight(Projectile.Center, 0.9f, 0.5f, 0f);
        }

		public override void OnKill(int timeLeft) 
        {
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
            for (int i = 0; i < 5; i++)
			{
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.SolarFlare, 1f);
				dust.noGravity = true;
            }
        }
    }
}