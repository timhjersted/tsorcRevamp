using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Archdeacon
{
    // Archdeacon's Glintstone Eruption floor mark. Harmless: it is the telegraph, and it owns the eruption.
    // Spawned by the caster with its Center ON the floor surface under a player (Center.Y = top of the floor tile).
    // TelegraphTicks of starlight across the full eruption width, then the centre lance, then both side lances.
    // The dust spans exactly the lances' reach (SideOffset + half a lance), so stepping off the mark is safe.
    public class ArchdeaconGlintRune : ModProjectile
    {
        public const int TelegraphTicks = 40;
        public const int SideDelayTicks = 12;
        public const float SideOffset = 56f;
        // Half the ArchdeaconGlintLance hitbox width (32px): the outer edge of the side lances.
        public const float HalfWidth = SideOffset + 16f;
        private const int LifetimeTicks = TelegraphTicks + SideDelayTicks + 8;

        private int Timer
        {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = (int)(HalfWidth * 2f);
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = LifetimeTicks;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Timer++;

            if (!Main.dedServ)
            {
                DrawTelegraphDust();
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (Timer == TelegraphTicks)
            {
                SpawnLance(0f);
            }

            if (Timer == TelegraphTicks + SideDelayTicks)
            {
                SpawnLance(-SideOffset);
                SpawnLance(SideOffset);
            }
        }

        // Glints across the mark, denser as the eruption nears; a brighter pillar of sparkles rises at each lance point.
        private void DrawTelegraphDust()
        {
            if (Timer > TelegraphTicks + SideDelayTicks)
            {
                return;
            }

            float progress = MathHelper.Clamp(Timer / (float)TelegraphTicks, 0f, 1f);
            int glintCount = 1 + (int)(progress * 3f);

            for (int i = 0; i < glintCount; i++)
            {
                Vector2 point = Projectile.Center + new Vector2(Main.rand.NextFloat(-HalfWidth, HalfWidth), -2f);
                Vector2 rise = new Vector2(0f, -Main.rand.NextFloat(0.5f, 1.5f) * (0.5f + progress));
                Dust glint = Dust.NewDustPerfect(point, DustID.TintableDustLighted, rise, 40, ArchdeaconGlintStar.GlintColor, 1f);
                glint.noGravity = true;
            }

            if (Timer % 4 == 0)
            {
                for (int i = -1; i <= 1; i++)
                {
                    Vector2 point = Projectile.Center + new Vector2(i * SideOffset, -4f);
                    Dust column = Dust.NewDustPerfect(point, DustID.TintableDustLighted, new Vector2(0f, -2f), 20,
                        ArchdeaconGlintStar.GlintCore, 0.9f + progress * 0.5f);
                    column.noGravity = true;
                }
            }

            if (Timer == 1)
            {
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.5f, Pitch = -0.5f }, Projectile.Center);
            }

            Lighting.AddLight(Projectile.Center, ArchdeaconGlintStar.GlintColor.ToVector3() * (0.2f + progress * 0.4f));
        }

        private void SpawnLance(float offsetX)
        {
            // Start fully buried (centre half a lance below the surface) so it bursts up out of the floor.
            Vector2 buried = Projectile.Center + new Vector2(offsetX, ArchdeaconGlintLance.HalfHeight);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), buried, new Vector2(0f, -ArchdeaconGlintLance.RiseSpeed),
                ModContent.ProjectileType<ArchdeaconGlintLance>(), Projectile.damage, 3f, Main.myPlayer);
        }
    }
}
