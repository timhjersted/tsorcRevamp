using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class ModDD2ExplosiveTrapT2Explosion : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }
        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
            Projectile.alpha = 255;
            Projectile.width = 144;
            Projectile.height = 144;
        }
        public override void AI()
        {
            int num = Main.projFrames[Projectile.type];
            int num2 = 3;
            Projectile.alpha -= 25;
            if (Projectile.alpha < 0)
            {
                Projectile.alpha = 0;
            }
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
            }
            if (++Projectile.frameCounter >= num2)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame >= num)
                {
                    Projectile.Kill();
                    return;
                }
            }
            DelegateMethods.v3_1 = new Vector3(1.3f, 0.9f, 0.2f);
            Utils.PlotTileLine(Projectile.Top, Projectile.Bottom, 2f, DelegateMethods.CastLightOpen);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            if (player.setHuntressT2)
            {
                target.AddBuff(BuffID.Oiled, Main.rand.Next(8, 18) * 30);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = new Color(255, 255, 255, 127) * (1f - (float)Projectile.alpha / 255f);
            return true;
        }
    }

}
