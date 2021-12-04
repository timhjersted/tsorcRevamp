using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class EnemyCursedFlames : ModProjectile {

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Okiku/PoisonSmog";

        public override void SetDefaults() {
            projectile.width = 16;
            projectile.height = 16;
            projectile.alpha = 150;
            projectile.aiStyle = 8;
            projectile.timeLeft = 600;
            projectile.damage = 70;
            projectile.light = 0.8f;
            projectile.penetrate = 1;
            projectile.tileCollide = true;
            projectile.hostile = true;
            aiType = 96;
        }

        public override bool PreKill(int timeLeft) {
            projectile.type = 95;
            return true;
        }
        public override void PostAI() {
            projectile.rotation += 3f;

            int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 75, 0, 0, 50, Color.Chartreuse, 3.0f);
            Main.dust[dust].noGravity = true;

            if (projectile.velocity.X <= 4 && projectile.velocity.Y <= 4 && projectile.velocity.X >= -4 && projectile.velocity.Y >= -4) {
                float accel = 2f + (Main.rand.Next(10, 30) * 0.001f);
                projectile.velocity.X *= accel;
                projectile.velocity.Y *= accel;
            }
        }
        public override void OnHitPlayer(Player target, int damage, bool crit) {
            int buffLengthMod = 1;
            if (Main.expertMode)
            {
                buffLengthMod = 2;
            }
            Main.player[Main.myPlayer].AddBuff(BuffID.Slow, 180 / buffLengthMod, false);
            Main.player[Main.myPlayer].AddBuff(BuffID.BrokenArmor, 180 / buffLengthMod, false);
            Main.player[Main.myPlayer].AddBuff(BuffID.Bleeding, 1800 / buffLengthMod, false);
            target.AddBuff(BuffID.Battle, 7600 / buffLengthMod, false);
        }
    }
}
