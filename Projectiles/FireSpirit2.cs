using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class FireSpirit2 : ModProjectile {

        public override void SetDefaults() {
            projectile.width = 20;
            projectile.height = 20;
            projectile.scale = 1.1f;
            projectile.timeLeft = 120;
            projectile.hostile = false;
            projectile.tileCollide = true;
            projectile.ignoreWater = false;
            projectile.friendly = true;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = 60;
            projectile.magic = true;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit) {
            if (Main.rand.Next(8) == 0) {
                target.AddBuff(BuffID.OnFire, 300);
            }
        }

        public override void AI() {
            Vector2 arg_23A6_0 = new Vector2(projectile.position.X, projectile.position.Y - projectile.height / 2);
            int arg_23A6_1 = projectile.width;
            int arg_23A6_2 = projectile.height;
            int arg_23A6_3 = 6;
            float arg_23A6_4 = projectile.velocity.X * 0.2f;
            float arg_23A6_5 = projectile.velocity.Y * 0.2f;
            int arg_23A6_6 = 100;
            if (projectile.timeLeft % 3 == 0) { 
                int num44 = Dust.NewDust(arg_23A6_0, arg_23A6_1, arg_23A6_2, arg_23A6_3, arg_23A6_4, arg_23A6_5, arg_23A6_6, default, 3.5f);
                Main.dust[num44].noGravity = true;
            }

            if (projectile.wet) {
                projectile.Kill();
            }
        }
    }
}
