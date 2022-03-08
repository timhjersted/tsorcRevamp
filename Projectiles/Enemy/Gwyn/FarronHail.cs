using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Gwyn {
    class FarronHail : ModProjectile {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Gwyn/Petal";
        public override void SetDefaults() {
            projectile.height = 16;
            projectile.width = 16;
            projectile.light = 0.8f;
            projectile.penetrate = 99999;
            projectile.tileCollide = false;
            projectile.timeLeft = 120;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.damage = 40;
        }

        internal float AI_Owner {
            get => projectile.ai[1];
            set => projectile.ai[1] = value;
        }

        public override void AI() {

            if (projectile.damage == 0) {
                projectile.alpha = 255;
                Dust h = Dust.NewDustPerfect(projectile.Center, DustID.Clentaminator_Cyan);
                h.noGravity = true;
                h.velocity = Vector2.Zero;
                projectile.extraUpdates = 60;
                projectile.timeLeft--;
            }

        }

        public override bool PreKill(int timeLeft) {
            if (projectile.damage != 0) {
                for (int i = 0; i < 5; i++) {
                    Dust.NewDustPerfect(projectile.Center, DustID.Clentaminator_Cyan);
                } 
            }
            return true;
        }
    }
}
