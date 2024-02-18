using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee.Axes
{
    class AncientFireAxeFireball : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.aiStyle = 8;
            Projectile.friendly = true;
            Projectile.light = 0.8f;
            Projectile.alpha = 100;
            Projectile.DamageType = DamageClass.Melee;
        }
        public override void OnSpawn(IEntitySource source)
        {
            Projectile.CritChance = (int)Projectile.ai[0];
            Projectile.ai[0] = 0;
        }
        public override void AI()
        {
            for (int num88 = 0; num88 < 2; num88++)
            {
                int num89 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 6, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default(Color), 2f);
                Main.dust[num89].noGravity = true;
                Main.dust[num89].velocity.X *= 0.3f;
                Main.dust[num89].velocity.Y *= 0.3f;
            }
            Projectile.ai[1] += 1f;
            if (Projectile.ai[1] >= 20f)
            {
                Projectile.velocity.Y += 0.2f;
            }
            Projectile.rotation += 0.3f * (float)Projectile.direction;
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
            }
            Lighting.AddLight(Projectile.Center, Projectile.light * 0.4f, Projectile.light * 0.1f, Projectile.light * 1f);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.OnFire, 5 * 60);
            }
        }
    }

}
