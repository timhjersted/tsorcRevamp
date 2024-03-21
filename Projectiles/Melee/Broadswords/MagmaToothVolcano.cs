using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee.Broadswords
{
    public class MagmaToothVolcano : ModProjectile
    {
        public int Frames = 1;
        public int ProjectileLifetime = 120;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = Frames;
        }
        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 200;
            Projectile.friendly = true;
            Projectile.timeLeft = 120;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 15;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
        }
        public override void OnSpawn(IEntitySource source)
        {
            Projectile.CritChance = (int)Projectile.ai[0];
            Projectile.ai[0] = 0;
            Projectile.velocity = Vector2.Zero;
        }
        public override void AI()
        {
            Projectile.frameCounter++;
            int frameSpeed = ProjectileLifetime / Frames;
            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= Frames) 
                {
                    Projectile.frame = 0;
                }
            }
        }
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Volume = 0.4f, PitchVariance = 0.1f });
            Dust.NewDust(Projectile.Center, 100, 100, DustID.FlameBurst, 0f, 0f, 250, Color.DarkRed, 2.5f);
        }
    }
}