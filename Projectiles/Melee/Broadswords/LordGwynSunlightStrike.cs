using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee.Broadswords
{
    class LordGwynSunlightStrike : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 54;
            Projectile.height = 220;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            for (int i = 0; i < 8; i++)
            {
                Vector2 pos = Projectile.Top + new Vector2(Main.rand.NextFloat(Projectile.width), Main.rand.NextFloat(Projectile.height));
                int type = Main.rand.NextBool() ? DustID.GoldFlame : DustID.Electric;
                int dust = Dust.NewDust(pos, 4, 4, type, 0f, Main.rand.NextFloat(4f, 9f), 40, default, 1.4f);
                Main.dust[dust].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 1f, 0.8f, 0.25f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return projHitbox.Intersects(targetHitbox);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Daybreak, 4 * 60);
            target.AddBuff(BuffID.OnFire3, 4 * 60);
        }
    }
}
