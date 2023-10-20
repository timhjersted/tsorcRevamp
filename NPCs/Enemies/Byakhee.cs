using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    public class Byakhee : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Demon];

            // DisplayName.SetDefault("Byakhee");

            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0)
            { // Influences how the NPC looks in the Bestiary
                Velocity = 1f // Draws the NPC in the bestiary as if its walking +1 tiles in the x direction
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
        }
        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.Demon);
            NPC.damage = 40;
            NPC.defense = 8;
            NPC.lifeMax = 300;
            NPC.value = 1500f;
            NPC.aiStyle = -1;
            NPC.knockBackResist = 0.2f;
            //AIType = NPCID.Demon;
            AnimationType = NPCID.Demon;
            //Banner = Item.NPCtoBanner(NPCID.Zombie);
            //BannerItem = Item.BannerToItem(Banner);
        }

        #region Spawn

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            Player p = spawnInfo.Player;
            if (spawnInfo.Invasion)
            {
                chance = 0;
                return chance;
            }

            if (spawnInfo.Player.townNPCs > 0f) return 0f;
            if (spawnInfo.Player.ZoneOverworldHeight && Main.hardMode && spawnInfo.Player.ZoneDesert && !Main.dayTime) return 0.0627f;
            if (spawnInfo.Player.ZoneOverworldHeight && Main.hardMode && spawnInfo.Player.ZoneDesert && Main.dayTime) return 0.0368f;
            if (spawnInfo.Player.ZoneUndergroundDesert && Main.hardMode && Main.dayTime) return 0.038f;

            if (Main.hardMode && spawnInfo.Player.ZoneRain)
            {
                if (spawnInfo.Player.ZoneOverworldHeight && Main.dayTime) return 0.0433f;
                if (spawnInfo.Player.ZoneOverworldHeight && !Main.dayTime) return 0.0855f;
            }
            return chance;
        }
        #endregion

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Sky,
				//BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
				new FlavorTextBestiaryInfoElement("An interstellar creature, servant of Hastur. Closely related to the creatures of Moon Lord.\n[c/32FF82:Origin]:\nLovecraftian Mythos")
            });
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            //npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Consumables.ElixirHealingLesser>(), 40, 1, 3));
            //npcLoot.Add(ItemDropRule.Common(ItemID.GoldenKey, 65));
            //npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Scouter>(), 100));
            //npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SeerStone>(), 100));
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (Main.netMode == NetmodeID.Server) return;

            for (int i = 0; i < 15; i++)
            {
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, hit.HitDirection, -1f);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.01f;
                dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.01f;
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
            }
            if (NPC.life <= 0)
            {
                //do something when dying?

            }
        }

        #region Gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("ByakheeHead").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("ByakheeWing").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("ByakheeWing").Type, 1f);
            }
        }
        #endregion
        public override void AI()
        {
            NPC.noGravity = true;
            if (NPC.collideX)
            {
                NPC.velocity.X = NPC.oldVelocity.X * -0.5f;
                if (NPC.direction == -1 && NPC.velocity.X > 0f && NPC.velocity.X < 2f)
                {
                    NPC.velocity.X = 2f;
                }
                if (NPC.direction == 1 && NPC.velocity.X < 0f && NPC.velocity.X > -2f)
                {
                    NPC.velocity.X = -2f;
                }
            }
            if (NPC.collideY)
            {
                NPC.velocity.Y = NPC.oldVelocity.Y * -0.5f;
                if (NPC.velocity.Y > 0f && NPC.velocity.Y < 1f)
                {
                    NPC.velocity.Y = 1f;
                }
                if (NPC.velocity.Y < 0f && NPC.velocity.Y > -1f)
                {
                    NPC.velocity.Y = -1f;
                }
            }
            NPC.TargetClosest();
            if (NPC.direction == -1 && NPC.velocity.X > -4f)
            {
                NPC.velocity.X -= 0.1f;
                if (NPC.velocity.X > 4f)
                {
                    NPC.velocity.X -= 0.1f;
                }
                else if (NPC.velocity.X > 0f)
                {
                    NPC.velocity.X += 0.05f;
                }
                if (NPC.velocity.X < -4f)
                {
                    NPC.velocity.X = -4f;
                }
            }
            else if (NPC.direction == 1 && NPC.velocity.X < 4f)
            {
                NPC.velocity.X += 0.1f;
                if (NPC.velocity.X < -4f)
                {
                    NPC.velocity.X += 0.1f;
                }
                else if (NPC.velocity.X < 0f)
                {
                    NPC.velocity.X -= 0.05f;
                }
                if (NPC.velocity.X > 4f)
                {
                    NPC.velocity.X = 4f;
                }
            }
            if (NPC.directionY == -1 && (double)NPC.velocity.Y > -1.5)
            {
                NPC.velocity.Y -= 0.04f;
                if ((double)NPC.velocity.Y > 1.5)
                {
                    NPC.velocity.Y -= 0.05f;
                }
                else if (NPC.velocity.Y > 0f)
                {
                    NPC.velocity.Y += 0.03f;
                }
                if ((double)NPC.velocity.Y < -1.5)
                {
                    NPC.velocity.Y = -1.5f;
                }
            }
            else if (NPC.directionY == 1 && (double)NPC.velocity.Y < 1.5)
            {
                NPC.velocity.Y += 0.04f;
                if ((double)NPC.velocity.Y < -1.5)
                {
                    NPC.velocity.Y += 0.05f;
                }
                else if (NPC.velocity.Y < 0f)
                {
                    NPC.velocity.Y -= 0.03f;
                }
                if ((double)NPC.velocity.Y > 1.5)
                {
                    NPC.velocity.Y = 1.5f;
                }
            }
            if (NPC.wet)
            {
                if (NPC.velocity.Y > 0f)
                {
                    NPC.velocity.Y *= 0.95f;
                }
                NPC.velocity.Y -= 0.5f;
                if (NPC.velocity.Y < -4f)
                {
                    NPC.velocity.Y = -4f;
                }
                NPC.TargetClosest();
            }

            if (NPC.direction == -1 && NPC.velocity.X > -4f)
            {
                NPC.velocity.X -= 0.1f;
                if (NPC.velocity.X > 4f)
                {
                    NPC.velocity.X -= 0.1f;
                }
                else if (NPC.velocity.X > 0f)
                {
                    NPC.velocity.X += 0.05f;
                }
                if (NPC.velocity.X < -4f)
                {
                    NPC.velocity.X = -4f;
                }
            }
            else if (NPC.direction == 1 && NPC.velocity.X < 4f)
            {
                NPC.velocity.X += 0.1f;
                if (NPC.velocity.X < -4f)
                {
                    NPC.velocity.X += 0.1f;
                }
                else if (NPC.velocity.X < 0f)
                {
                    NPC.velocity.X -= 0.05f;
                }
                if (NPC.velocity.X > 4f)
                {
                    NPC.velocity.X = 4f;
                }
            }
            if (NPC.directionY == -1 && (double)NPC.velocity.Y > -1.5)
            {
                NPC.velocity.Y -= 0.04f;
                if ((double)NPC.velocity.Y > 1.5)
                {
                    NPC.velocity.Y -= 0.05f;
                }
                else if (NPC.velocity.Y > 0f)
                {
                    NPC.velocity.Y += 0.03f;
                }
                if ((double)NPC.velocity.Y < -1.5)
                {
                    NPC.velocity.Y = -1.5f;
                }
            }
            else if (NPC.directionY == 1 && (double)NPC.velocity.Y < 1.5)
            {
                NPC.velocity.Y += 0.04f;
                if ((double)NPC.velocity.Y < -1.5)
                {
                    NPC.velocity.Y += 0.05f;
                }
                else if (NPC.velocity.Y < 0f)
                {
                    NPC.velocity.Y -= 0.03f;
                }
                if ((double)NPC.velocity.Y > 1.5)
                {
                    NPC.velocity.Y = 1.5f;
                }
            }
            NPC.ai[1] += 1f;
            if (NPC.ai[1] > 200f)
            {
                if (!Main.player[NPC.target].wet && Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))
                {
                    NPC.ai[1] = 0f;
                }
                float num718 = 0.12f;
                float num719 = 0.07f;
                float num720 = 3f;
                float num721 = 1.25f;
                if (NPC.ai[1] > 1000f)
                {
                    NPC.ai[1] = 0f;
                }
                NPC.ai[2] += 1f;
                if (NPC.ai[2] > 0f)
                {
                    if (NPC.velocity.Y < num721)
                    {
                        NPC.velocity.Y += num719;
                    }
                }
                else if (NPC.velocity.Y > 0f - num721)
                {
                    NPC.velocity.Y -= num719;
                }
                if (NPC.ai[2] < -150f || NPC.ai[2] > 150f)
                {
                    if (NPC.velocity.X < num720)
                    {
                        NPC.velocity.X += num718;
                    }
                }
                else if (NPC.velocity.X > 0f - num720)
                {
                    NPC.velocity.X -= num718;
                }
                if (NPC.ai[2] > 300f)
                {
                    NPC.ai[2] = -300f;
                }
            }
            if (Main.netMode == 1)
            {
                return;
            }

            NPC.ai[0] += 1f;
            if (NPC.ai[0] == 20f || NPC.ai[0] == 40f || NPC.ai[0] == 60f || NPC.ai[0] == 80f)
            {
                if (Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))
                {
                    //float num730 = 0.2f;
                    //Vector2 vector160 = new Vector2(NPC.position.X + (float)NPC.width * 0.5f, NPC.position.Y + (float)NPC.height * 0.5f);
                    //float num731 = Main.player[NPC.target].position.X + (float)Main.player[NPC.target].width * 0.5f - vector160.X + (float)Main.rand.Next(-100, 101);
                    //float num732 = Main.player[NPC.target].position.Y + (float)Main.player[NPC.target].height * 0.5f - vector160.Y + (float)Main.rand.Next(-100, 101);
                    //float num733 = (float)Math.Sqrt(num731 * num731 + num732 * num732);
                    //num733 = num730 / num733;
                    //num731 *= num733;
                    //num732 *= num733; //(int)NPC.Center.X, (int)NPC.Center.Y
                    //SoundStyle sound = new SoundStyle($"{nameof(Liber)}/Assets/Sounds/fireball")
                    //{
                    //	Volume = 0.75f,
                    //	PitchVariance = 0.2f,
                    //	MaxInstances = 3,
                    //}; ;
                    //SoundEngine.PlaySound(sound, NPC.position);

                    //Vector2 overshoot = new Vector2(0, -240);

                    Player player = Main.player[NPC.target];
                    if (NPC.Distance(player.Center) <= 200)
                    {
                        Vector2 projectileVector = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 5, 0.035f);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projectileVector.X, projectileVector.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreathCollides>(), 20, 0f, Main.myPlayer, 1, NPC.target);
                    }
                    if (NPC.Distance(player.Center) > 200)
                    {
                        Vector2 projectileVector = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 9, 0.035f);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projectileVector.X, projectileVector.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreathCollides>(), 20, 0f, Main.myPlayer, 1, NPC.target);
                    }





                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                }
            }
            else if (NPC.ai[0] >= (float)(300 + Main.rand.Next(300)))
            {
                NPC.ai[0] = 0f;
            }
            NPC.ai[0] += 1f;
            return;
        }

    }
}
