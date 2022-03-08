using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace  tsorcRevamp.Projectiles.Enemy.Gwyn {
    class TackleProjectile : ModProjectile {

        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Gwyn");
        }
        public override void SetDefaults() {
            projectile.height = 58;
            projectile.width = 58;
            projectile.light = 0.8f;
            projectile.penetrate = 99999;
            projectile.tileCollide = false;
            projectile.timeLeft = 160;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.damage = 40;
        }

        internal float AI_Timer {
            get => projectile.ai[0];
            set => projectile.ai[0] = value;
        }
        internal float AI_Owner {
            get => projectile.ai[1];
            set => projectile.ai[1] = value;
        }

        
        public override void AI() {
            NPC owner = Main.npc[(int)AI_Owner];
            projectile.Center = owner.Center;
            projectile.velocity = owner.velocity;
            if (projectile.velocity.Length() > 30 && Main.netMode != NetmodeID.Server) {
                for (int i = 0; i < 4; i++) {
                    Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.RuneWizard);
                    dust.noGravity = true;
                }
            }
        }
    }
}
