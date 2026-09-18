using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>
    /// Harmless 30-tick aim line for Soul of Cinder's Azure Wisp Fan. The stored velocity is
    /// released as a real CinderBlueWisp when the line expires.
    /// </summary>
    class CinderWispTelegraph : ModProjectile
    {
        const int TelegraphTicks = 30;
        const float LineLength = 420f;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = TelegraphTicks;
            Projectile.hide = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (Main.dedServ)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float progress = 1f - Projectile.timeLeft / (float)TelegraphTicks;
            int motes = 2 + (int)(progress * 2f);
            for (int i = 0; i < motes; i++)
            {
                float distance = Main.rand.NextFloat(18f, LineLength);
                Vector2 sideways = new Vector2(-direction.Y, direction.X) * Main.rand.NextFloat(-1.5f, 1.5f);
                Dust line = Dust.NewDustPerfect(Projectile.Center + direction * distance + sideways,
                    DustID.Clentaminator_Cyan, Vector2.Zero, 120,
                    new Color(90, 195, 255), Main.rand.NextFloat(0.42f, 0.72f));
                line.noGravity = true;
            }

            Dust gather = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                DustID.PurpleTorch, -Main.rand.NextVector2Circular(0.7f, 0.7f), 110,
                new Color(185, 90, 255), Main.rand.NextFloat(0.5f, 0.85f));
            gather.noGravity = true;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity,
                    ModContent.ProjectileType<CinderBlueWisp>(), Projectile.damage, Projectile.knockBack, Main.myPlayer);
            }

            if (Main.dedServ)
                return;

            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Clentaminator_Cyan,
                    Main.rand.NextVector2CircularEdge(3.5f, 3.5f), 90,
                    new Color(100, 205, 255), Main.rand.NextFloat(0.5f, 0.9f));
                dust.noGravity = true;
            }
        }
    }
}
