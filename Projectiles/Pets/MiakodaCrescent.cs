using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Pets
{
    class MiakodaCrescent : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Crescent Moon Miakoda");
            Main.projFrames[Projectile.type] = 8;
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.CharacterPreviewAnimations[Projectile.type] = ProjectileID.Sets.SimpleLoop(0, Main.projFrames[Projectile.type], 8)
				.WithOffset(10, -36f)
				.WithSpriteDirection(-1)
				.WithCode(DelegateMethods.CharacterPreview.Float);
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.BabyHornet);
            Projectile.width = 18;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            AIType = ProjectileID.BabyHornet;
            Projectile.scale = 0.85f;
            DrawOffsetX = -8;
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
            float MiakodaVol = ModContent.GetInstance<tsorcRevampConfig>().MiakodaVolume / 100f;
            Lighting.AddLight(Projectile.position, .6f, .45f, .6f);


            Vector2 idlePosition = player.Center;
            Vector2 vectorToIdlePosition = idlePosition - Projectile.Center;
            float distanceToIdlePosition = vectorToIdlePosition.Length();


            if (Main.myPlayer == player.whoAmI && distanceToIdlePosition > 1500f)
            {
                // Whenever you deal with non-regular events that change the behavior or position drastically, make sure to only run the code on the owner of the projectile,
                // and then set netUpdate to true
                Projectile.position = idlePosition;
                Projectile.velocity *= 0.1f;
                Projectile.netUpdate = true;
            }

            if (player.dead)
            {
                modPlayer.MiakodaCrescent = false;
            }
            if (modPlayer.MiakodaCrescent)
            {
                Projectile.timeLeft = 2;
            }

            if (modPlayer.MiakodaEffectsTimer > 720)
            {
                Lighting.AddLight(Projectile.position, .8f, .65f, .8f);

                if (Main.rand.NextBool(3))
                {
                    if (Projectile.direction == 1)
                    {
                        int dust = Dust.NewDust(new Vector2(Projectile.position.X + 6, Projectile.position.Y), Projectile.width - 6, Projectile.height - 6, 234, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 30, default(Color), 1f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].noLight = true;
                    }
                    if (Projectile.direction == -1)
                    {
                        int dust = Dust.NewDust(new Vector2(Projectile.position.X - 6, Projectile.position.Y), Projectile.width - 6, Projectile.height - 6, 234, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 30, default(Color), 1f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].noLight = true;
                    }
                }
            }

            if (modPlayer.MiakodaEffectsTimer == 720 && MiakodaVol != 0) //sound effect the moment the timer reaches 420, to signal pet ability ready.
            {
                string[] ReadySoundChoices = new string[] { "tsorcRevamp/Sounds/Custom/MiakodaChaaa", "tsorcRevamp/Sounds/Custom/MiakodaChao", "tsorcRevamp/Sounds/Custom/MiakodaDootdoot", "tsorcRevamp/Sounds/Custom/MiakodaHi", "tsorcRevamp/Sounds/Custom/MiakodaOuuee" };
                string ReadySound = Main.rand.Next(ReadySoundChoices);
                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle(ReadySound) with { Volume = 0.4f * MiakodaVol, PitchVariance = 0.2f }, Projectile.Center);
            }

            if (modPlayer.MiakodaCrescentDust2) //splash effect and sound once player gets crit+heal.
            {
                if (MiakodaVol != 0)
                {
                    string[] AmgerySoundChoices = new string[] { "tsorcRevamp/Sounds/Custom/MiakodaScream", "tsorcRevamp/Sounds/Custom/MiakodaChaoExcl", "tsorcRevamp/Sounds/Custom/MiakodaUwuu" };
                    string AmgerySound = Main.rand.Next(AmgerySoundChoices);
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle(AmgerySound) with { Volume = 0.6f * MiakodaVol, PitchVariance = 0.2f }, Projectile.Center);
                }

                for (int d = 0; d < 90; d++)
                {
                    if (Projectile.direction == 1)
                    {
                        int dust = Dust.NewDust(new Vector2(Projectile.position.X - 4, Projectile.position.Y + 2), Projectile.width - 6, Projectile.height - 6, 164, 0f, 0f, 30, default(Color), 1.2f);
                        Main.dust[dust].velocity *= Main.rand.NextFloat(1f, 4f);
                        Main.dust[dust].noGravity = true;
                    }
                    if (Projectile.direction == -1)
                    {
                        int dust = Dust.NewDust(new Vector2(Projectile.position.X + 4, Projectile.position.Y + 2), Projectile.width - 6, Projectile.height - 6, 164, 0f, 0f, 30, default(Color), 1.2f);
                        Main.dust[dust].velocity *= Main.rand.NextFloat(1f, 4f);
                        Main.dust[dust].noGravity = true;
                    }
                }

                for (int d = 0; d < 30; d++)
                {
                    if (Projectile.direction == 1)
                    {
                        int dust = Dust.NewDust(new Vector2(Projectile.position.X - 4, Projectile.position.Y + 2), Projectile.width - 6, Projectile.height - 6, 164, 0f, 0f, 30, default(Color), 1.2f);
                        Main.dust[dust].velocity *= Main.rand.NextFloat(0.5f, 3.5f);
                        Main.dust[dust].noGravity = false;
                    }
                    if (Projectile.direction == -1)
                    {
                        int dust = Dust.NewDust(new Vector2(Projectile.position.X + 4, Projectile.position.Y + 2), Projectile.width - 6, Projectile.height - 6, 164, 0f, 0f, 30, default(Color), 1.2f);
                        Main.dust[dust].velocity *= Main.rand.NextFloat(0.5f, 3.5f);
                        Main.dust[dust].noGravity = false;

                    }
                }
            }

            if (modPlayer.MiakodaEffectsTimer < 720)
            {
                player.GetModPlayer<tsorcRevampPlayer>().MiakodaCrescentDust2 = false;
            }
        }
    }
}
