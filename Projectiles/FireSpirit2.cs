using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class FireSpirit2 : ModProjectile {

        public override void SetDefaults() {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.scale = 1.1f;
            Projectile.timeLeft = 120;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.friendly = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit) {
            if (Main.rand.Next(8) == 0) {
                target.AddBuff(BuffID.OnFire, 300);
            }
        }

        public override void AI() {
            Projectile.rotation += 0.25f;
            Vector2 arg_23A6_0 = new Vector2(Projectile.position.X, Projectile.position.Y - Projectile.height / 2);
            int arg_23A6_1 = Projectile.width;
            int arg_23A6_2 = Projectile.height;
            int arg_23A6_3 = 6;
            float arg_23A6_4 = Projectile.velocity.X * 0.2f;
            float arg_23A6_5 = Projectile.velocity.Y * 0.2f;
            int arg_23A6_6 = 100;
            if (Projectile.timeLeft % 3 == 0) { 
                int num44 = Dust.NewDust(arg_23A6_0, arg_23A6_1, arg_23A6_2, arg_23A6_3, arg_23A6_4, arg_23A6_5, arg_23A6_6, default, 3.5f);
                Main.dust[num44].noGravity = true;
            }

            if (Projectile.wet) {
                Projectile.Kill();
            }
        }
    }
}
