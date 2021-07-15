using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using tsorcRevamp;

namespace tsorcRevamp.Projectiles.Pets
{
    class MiakodaCrescent : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crescent Moon Miakoda");
            Main.projFrames[projectile.type] = 8;
            Main.projPet[projectile.type] = true;
        }

        public override void SetDefaults()
        {
            projectile.CloneDefaults(ProjectileID.BabyHornet);
            projectile.width = 18;
            projectile.height = 16;
            projectile.friendly = true;
            projectile.tileCollide = false;
            aiType = ProjectileID.BabyHornet;
            projectile.scale = 0.85f;
            drawOffsetX = -8;
        }

        public override bool PreAI()
        {
            Player player = Main.player[projectile.owner];
            player.hornet = false;

            return true;
        }
        public override void AI()
        {
            Player player = Main.player[projectile.owner];
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            float MiakodaVol = ModContent.GetInstance<tsorcRevampConfig>().MiakodaVolume / 100f;
            Lighting.AddLight(projectile.position, .6f, .45f, .6f);


            Vector2 idlePosition = player.Center;
            Vector2 vectorToIdlePosition = idlePosition - projectile.Center;
            float distanceToIdlePosition = vectorToIdlePosition.Length();


            if (Main.myPlayer == player.whoAmI && distanceToIdlePosition > 1500f)
            {
                // Whenever you deal with non-regular events that change the behavior or position drastically, make sure to only run the code on the owner of the projectile,
                // and then set netUpdate to true
                projectile.position = idlePosition;
                projectile.velocity *= 0.1f;
                projectile.netUpdate = true;
            }

            if (player.dead)
            {
                modPlayer.MiakodaCrescent = false;
            }
            if (modPlayer.MiakodaCrescent)
            {
                projectile.timeLeft = 2;
            }

            if (modPlayer.MiakodaEffectsTimer > 720)
            {
                Lighting.AddLight(projectile.position, .8f, .65f, .8f);

                if (Main.rand.Next(3) == 0)
                {
                    if (projectile.direction == 1)
                    {
                        int dust = Dust.NewDust(new Vector2(projectile.position.X + 6, projectile.position.Y), projectile.width - 6, projectile.height - 6, 234, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 30, default(Color), 1f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].noLight = true;
                    }
                    if (projectile.direction == -1)
                    {
                        int dust = Dust.NewDust(new Vector2(projectile.position.X - 6, projectile.position.Y), projectile.width - 6, projectile.height - 6, 234, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 30, default(Color), 1f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].noLight = true;
                    }
                }
            }

            if (modPlayer.MiakodaEffectsTimer == 720 && MiakodaVol != 0) //sound effect the moment the timer reaches 420, to signal pet ability ready.
            {
                string[] ReadySoundChoices = new string[] { "Sounds/Custom/MiakodaChaaa", "Sounds/Custom/MiakodaChao", "Sounds/Custom/MiakodaDootdoot", "Sounds/Custom/MiakodaHi", "Sounds/Custom/MiakodaOuuee" };
                string ReadySound = Main.rand.Next(ReadySoundChoices);
                Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, ReadySound).WithVolume(.4f * MiakodaVol).WithPitchVariance(.2f), projectile.Center);
            }

            if (modPlayer.MiakodaCrescentDust2) //splash effect and sound once player gets crit+heal.
            {
                if (MiakodaVol != 0)
                {
                    string[] AmgerySoundChoices = new string[] { "Sounds/Custom/MiakodaScream", "Sounds/Custom/MiakodaChaoExcl", "Sounds/Custom/MiakodaUwuu" };
                    string AmgerySound = Main.rand.Next(AmgerySoundChoices);
                    Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, AmgerySound).WithVolume(.6f * MiakodaVol).WithPitchVariance(.2f), projectile.Center);
                }

                for (int d = 0; d < 90; d++)
                {
                    if (projectile.direction == 1)
                    {
                        int dust = Dust.NewDust(new Vector2(projectile.position.X - 4, projectile.position.Y + 2), projectile.width - 6, projectile.height - 6, 164, 0f, 0f, 30, default(Color), 1.2f);
                        Main.dust[dust].velocity *= Main.rand.NextFloat(1f, 4f);
                        Main.dust[dust].noGravity = true;
                    }
                    if (projectile.direction == -1)
                    {
                        int dust = Dust.NewDust(new Vector2(projectile.position.X + 4, projectile.position.Y + 2), projectile.width - 6, projectile.height - 6, 164, 0f, 0f, 30, default(Color), 1.2f);
                        Main.dust[dust].velocity *= Main.rand.NextFloat(1f, 4f);
                        Main.dust[dust].noGravity = true;
                    }
                }

                for (int d = 0; d < 30; d++)
                {
                    if (projectile.direction == 1)
                    {
                        int dust = Dust.NewDust(new Vector2(projectile.position.X - 4, projectile.position.Y + 2), projectile.width - 6, projectile.height - 6, 164, 0f, 0f, 30, default(Color), 1.2f);
                        Main.dust[dust].velocity *= Main.rand.NextFloat(0.5f, 3.5f);
                        Main.dust[dust].noGravity = false;
                    }
                    if (projectile.direction == -1)
                    {
                        int dust = Dust.NewDust(new Vector2(projectile.position.X + 4, projectile.position.Y + 2), projectile.width - 6, projectile.height - 6, 164, 0f, 0f, 30, default(Color), 1.2f);
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
