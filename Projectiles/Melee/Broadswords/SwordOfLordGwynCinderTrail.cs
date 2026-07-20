using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee.Broadswords
{
    class SwordOfLordGwynCinderTrail : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 54;
            Projectile.height = 42;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 70;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override void AI()
        {
            float fade = Projectile.timeLeft / 70f;
            if (Projectile.timeLeft < 20)
            {
                Projectile.damage = 0;
            }

            for (int i = 0; i < 3; i++)
            {
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, type, 0f, -1.4f, 80, default, MathHelper.Lerp(0.7f, 1.8f, fade));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity.X *= 0.35f;
                Main.dust[dust].velocity.Y -= 0.6f;
            }
            Lighting.AddLight(Projectile.Center, 0.55f * fade, 0.25f * fade, 0.08f * fade);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 4 * 60);
        }
    }
}
