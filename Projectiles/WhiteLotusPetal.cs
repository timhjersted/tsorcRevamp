using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class WhiteLotusPetal : ModProjectile {

        public override void SetDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.timeLeft = 200;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = true;
            Projectile.knockBack = 2;
            Projectile.damage = 4;
        }

        public int ownerWAI;
        public float dir;
        public int count;
        public override void AI() {
            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.NorthPole, Vector2.Zero);
            dust.noGravity = true;
            Projectile owner = Main.projectile[ownerWAI];
            if (!owner.active || owner.type != ModContent.ProjectileType<WhiteLotusCore>())
                Projectile.Kill();
            Projectile.ai[0] += 0.15f;

            Vector2 offset;
            if (dir == 1) {
                offset = new((float)(18 * Math.Cos(Projectile.ai[0])), (float)(18 * Math.Sin(Projectile.ai[0])));
            }
            else {
                offset = new((float)(18 * Math.Sin(Projectile.ai[0])), (float)(18 * Math.Cos(Projectile.ai[0])));
            }
            offset = offset.RotatedBy(MathHelper.ToRadians(Projectile.ai[1] * (360 / count)));
            Projectile.Center = owner.Center + offset;
            Projectile.Center += new Vector2(1.5f, 0.5f); //theyre off center otherwise
            Projectile.rotation = (offset.ToRotation() + MathHelper.ToRadians(90));
        }

        public override void OnKill(int timeLeft) {
            for (int i = 0; i < 10; i++) {
                Dust dust = Dust.NewDustDirect(Projectile.Center, 1, 1, DustID.BubbleBurst_White);
                dust.noGravity = true;
            }
        }
    }
}
