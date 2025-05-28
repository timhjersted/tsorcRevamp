using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Friendly;

namespace tsorcRevamp.Projectiles.Pets
{
    class MiakodaNew : ModProjectile
    {
        public bool spawned = false;
        public int count = 0;
        public static Vector2 offset = new Vector2(25f, -40f);
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("New Moon Miakoda");
            Main.projFrames[Projectile.type] = 8;
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.CharacterPreviewAnimations[Projectile.type] = ProjectileID.Sets.SimpleLoop(0, Main.projFrames[Projectile.type], 8)
                .WithOffset(11, -36f)
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
            //DrawOffsetX = -8;
        }

        public override bool PreAI()
        {
            Player player = Main.player[Projectile.owner];

            Vector2 targetLocation = new Vector2(player.Center.X + offset.X, player.Center.Y + offset.Y);
            Vector2 toTarget = targetLocation - Projectile.position;
            float distance1 = (player.Center - Projectile.Center).Length();

            player.hornet = false;

            AIType = ProjectileID.BabyHornet;

            // If you're close enough to the player and they're standing still
            if (Main.myPlayer == player.whoAmI && distance1 < 150f && player.velocity.X == 0 && player.velocity.Y == 0)
            {
                float distance2 = toTarget.Length();

                // Fly towards the preset position
                Vector2 movement = toTarget * 0.02f;
                Projectile.velocity.X += movement.X;
                Projectile.velocity.Y += movement.Y;

                // If close enough, spawn npc and slow down movement a little more.
                if (distance2 < 20f)
                {
                    // And the npc hasn't been spawned
                    if (!spawned)
                    {
                        count++;
                        if (count > 30)
                        {
                            int npcPos = NPC.NewNPC(player.GetSource_FromThis(), (int)player.Center.X, (int)player.Center.Y, ModContent.NPCType<MiakodaNPC>());
                            MiakodaNPC npc = (MiakodaNPC)Main.npc[npcPos].ModNPC;
                            npc.SetOwner(Projectile.owner);

                            count = 0;
                            spawned = true;
                        }
                    }
                    else
                    {
                        Projectile.velocity.X *= 0.5f;
                        Projectile.velocity.Y *= 0.5f;
                    }

                    return true;
                }

                Projectile.velocity *= 0.85f;
            }

            spawned = false;

            return true;
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            float MiakodaVol = ModContent.GetInstance<tsorcRevampConfig>().MiakodaVolume / 100f;
            Lighting.AddLight(Projectile.position, .6f, .6f, .4f);


            Vector2 idlePosition = player.Center;
            Vector2 vectorToIdlePosition = idlePosition - Projectile.Center;
            float distanceToIdlePosition = vectorToIdlePosition.Length();


            if (Main.myPlayer == player.whoAmI && distanceToIdlePosition > 1500f)
            {
                Projectile.position = idlePosition;
                Projectile.velocity *= 0.1f;
                Projectile.netUpdate = true;
            }

            // The npc is spawned, teleport over the npc to hide it.
            if (spawned)
            {
                Vector2 targetLocation = new Vector2(player.Center.X + offset.X, player.Center.Y + offset.Y);
                Projectile.position = targetLocation;
                Projectile.direction = 1;
            }

            if (player.dead)
            {
                modPlayer.MiakodaNew = false;
            }
            if (modPlayer.MiakodaNew)
            {
                Projectile.timeLeft = 2;
            }

            if ((modPlayer.MiakodaEffectsTimer > 720) && (Main.rand.NextBool(3)))
            {
                if (Projectile.direction == 1)
                {
                    int dust = Dust.NewDust(new Vector2(Projectile.position.X + 4, Projectile.position.Y), Projectile.width - 6, Projectile.height - 6, 57, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                }
                if (Projectile.direction == -1)
                {
                    int dust = Dust.NewDust(new Vector2(Projectile.position.X - 4, Projectile.position.Y), Projectile.width - 6, Projectile.height - 6, 57, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                }
            }

            if (modPlayer.MiakodaEffectsTimer == 720 && MiakodaVol != 0) //sound effect the moment the timer reaches 420, to signal pet ability ready.
            {
                string[] ReadySoundChoices = new string[] { "tsorcRevamp/Sounds/Custom/MiakodaChaaa", "tsorcRevamp/Sounds/Custom/MiakodaChao", "tsorcRevamp/Sounds/Custom/MiakodaDootdoot", "tsorcRevamp/Sounds/Custom/MiakodaHi", "tsorcRevamp/Sounds/Custom/MiakodaOuuee" };
                string ReadySound = Main.rand.Next(ReadySoundChoices);
                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle(ReadySound) with { Volume = 0.4f * MiakodaVol, PitchVariance = 0.2f }, Projectile.Center);
            }

            if (modPlayer.MiakodaNewDust2) //splash effect and sound once player gets crit+heal.
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
                        int dust = Dust.NewDust(new Vector2(Projectile.position.X - 4, Projectile.position.Y), Projectile.width - 6, Projectile.height - 6, 57, 0f, 0f, 30, default(Color), 1.5f);
                        Main.dust[dust].velocity *= Main.rand.NextFloat(0.5f, 3.5f);
                        Main.dust[dust].noGravity = true;
                    }
                    if (Projectile.direction == -1)
                    {
                        int dust = Dust.NewDust(new Vector2(Projectile.position.X + 4, Projectile.position.Y), Projectile.width - 6, Projectile.height - 6, 57, 0f, 0f, 30, default(Color), 1.5f);
                        Main.dust[dust].velocity *= Main.rand.NextFloat(0.5f, 3.5f);
                        Main.dust[dust].noGravity = true;
                    }
                }
            }

            if (modPlayer.MiakodaEffectsTimer < 720)
            {
                player.GetModPlayer<tsorcRevampPlayer>().MiakodaNewDust2 = false;
            }
        }
    }
}
