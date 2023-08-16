using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Pets
{
    class Miakoda : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Miakoda");
            Main.projFrames[Projectile.type] = 4;
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.CharacterPreviewAnimations[Projectile.type] = ProjectileID.Sets.SimpleLoop(0, Main.projFrames[Projectile.type], 4)
				.WithOffset(10, -36f)
				.WithSpriteDirection(-1)
				.WithCode(DelegateMethods.CharacterPreview.Float);
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.BabyHornet);
            AIType = ProjectileID.BabyHornet;
        }

        public override bool PreAI()
        {
            Player player = Main.player[Projectile.owner];
            player.hornet = false;
            return true;
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (player.dead)
            {
                modPlayer.Miakoda = false;
            }
            if (modPlayer.Miakoda)
            {
                Projectile.timeLeft = 2;
            }
            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 57, Projectile.velocity.X, Projectile.velocity.Y, 200, Color.White, 1f);
                Main.dust[dust].noGravity = true;
            }

        }

    }
}
