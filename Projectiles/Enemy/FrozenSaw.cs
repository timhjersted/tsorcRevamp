using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class FrozenSaw : ModProjectile {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Frozen Orb");

        }
        public override void SetDefaults() {
            projectile.aiStyle = 0;
            projectile.hostile = true;
            projectile.height = 34;
            projectile.tileCollide = false;
            projectile.damage = 40;
            projectile.width = 34;
            projectile.timeLeft = 150;
            projectile.light = .3f;
            Main.projFrames[projectile.type] = 4;
        }

        public override bool PreKill(int timeLeft) {
            projectile.type = 44;
            return true;
        }

        public override void AI() {
            projectile.rotation++;
            if (projectile.velocity.X <= 10 && projectile.velocity.Y <= 10 && projectile.velocity.X >= -10 && projectile.velocity.Y >= -10) {
                projectile.velocity.X *= 1.01f;
                projectile.velocity.Y *= 1.01f;
            }

            if (Main.rand.Next(2) == 0) {

                Lighting.AddLight((int)projectile.position.X / 16, (int)projectile.position.Y / 16, 0f, 0.3f, 0.8f);
                return;

            }

            projectile.frameCounter++;
            if (projectile.frameCounter > 2) {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= 4) {
                projectile.frame = 0;
            }

        }
        public override void OnHitPlayer(Player target, int damage, bool crit) {
            int buffLengthMod = 1;
            if (Main.expertMode)
            {
                buffLengthMod = 2;
            }

            target.AddBuff(BuffID.BrokenArmor, 300 / buffLengthMod);
            if (Main.rand.Next(10) == 0) {
                target.AddBuff(BuffID.Silenced, 180 / buffLengthMod);
                target.AddBuff(BuffID.Slow, 300 / buffLengthMod);
            }
        }
    }
}
