using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Pets {
    class Miakoda : ModProjectile {

		public override void SetStaticDefaults() {
			DisplayName.SetDefault("Miakoda");
			Main.projFrames[projectile.type] = 4;
			Main.projPet[projectile.type] = true;
		}

        public override void SetDefaults() {
            projectile.CloneDefaults(ProjectileID.BabyHornet);
            aiType = ProjectileID.BabyHornet;
        }

        public override bool PreAI() {
            Player player = Main.player[projectile.owner];
            player.hornet = false;
            return true;
        }
        public override void AI() {
            Player player = Main.player[projectile.owner];
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (player.dead) {
                modPlayer.Miakoda = false;
            }
            if (modPlayer.Miakoda) {
                projectile.timeLeft = 2;
            }
            if (Main.rand.Next(2) == 0) {
                int dust = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 57, projectile.velocity.X, projectile.velocity.Y, 200, Color.White, 1f);
                Main.dust[dust].noGravity = true;
            }

        }

    }
}
