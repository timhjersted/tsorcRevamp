using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.Fiends
{
    [AutoloadBossHead]
    class FireFiendMarilith : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.scale = 1;
            NPC.npcSlots = 10;
            Main.npcFrameCount[NPC.type] = 8;
            NPC.width = 120;
            NPC.height = 160;
            NPC.damage = 260;
            NPC.defense = 40;
            NPC.aiStyle = 22;
            AnimationType = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = 300000;
            NPC.timeLeft = 22500;
            NPC.alpha = 100;
            NPC.friendly = false;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.knockBackResist = 0f;
            NPC.lavaImmune = true;
            NPC.boss = true;
            NPC.value = 600000;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
            despawnHandler = new NPCDespawnHandler("Fire Fiend Marilith decends to the underworld...", Color.OrangeRed, DustID.FireworkFountain_Red);
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fire Fiend Marilith");
        }

        int lightningDamage = 86;
        int antiMatterBlastDamage = 96;
        int crazedPurpleCrushDamage = 76;

        //oolicile sorcerer
        public float FlameShotTimer;
        public float FlameShotCounter;

        //chaos
        int holdTimer = 0;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.damage = (int)(NPC.damage * 1.3 / 2);
            NPC.defense = NPC.defense += 12;
        }

        #region AI
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            bool flag25 = false;

            //Flame attack starts at 2/3 health
            if (NPC.life <= 200000)
            { 
                FlameShotTimer++;
            }

            Player player = Main.player[NPC.target];
            //chaos code: announce proximity debuffs once
            if (holdTimer > 1)
            {
                holdTimer--;
            }
            //Proximity Debuffs
            if (Vector2.Distance(NPC.Center, Main.player[NPC.target].Center) < 1200)
            {
                player.AddBuff(BuffID.Oiled, 600, false);
                player.AddBuff(BuffID.Poisoned, 60, false);

                if (holdTimer <= 0 && Main.netMode != NetmodeID.Server)
                {
                    Main.NewText("Marilith has poisoned the air with an incendiary fog!", 199, 21, 133);//medium violet red
                    holdTimer = 3000;
                }
                
            }
            //getting close to marilith triggers on fire!
            if (NPC.Distance(player.Center) < 350)
            {
                player.AddBuff(BuffID.OnFire, 120, false);
                
            }

            //FIRE FROM ABOVE ATTACK
            //Counts up each tick. Used to space out shots
            if (FlameShotTimer >= 25 && FlameShotCounter < 11)
            {

                Projectile.NewProjectile(NPC.GetSource_FromThis(), (float)player.position.X - 500 + Main.rand.Next(600), (float)player.position.Y - 600f, (float)(-40 + Main.rand.Next(80)) / 10, 4.5f, ProjectileID.CultistBossFireBall, crazedPurpleCrushDamage, 2f, Main.myPlayer); //ProjectileID.NebulaBlaze2 would be cool to use at the end of attraidies or gwyn fight with the text, "The spirit of your father summons cosmic light to aid you!"

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                NPC.netUpdate = true; //new

                FlameShotTimer = 0;
                FlameShotCounter++;

            }
            //Chance to trigger fire from above
            if (Main.rand.NextBool(900))
            {
                FlameShotCounter = 0;
            }

            if (NPC.ai[1] >= 10f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.ai[1] += (Main.rand.Next(2, 5) * 0.1f) * NPC.scale;
                if (Main.rand.NextBool(90))
                {
                    Vector2 projVector = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 15);
                    projVector += Main.rand.NextVector2Circular(5, 5);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellLightning4Ball>(), lightningDamage, 0f, Main.myPlayer, Main.rand.Next(30, 180));
                    //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.1f, Pitch = -0.1f }, NPC.Center);//magic ice
                    NPC.ai[1] = 1f;
                }
                if (Main.rand.NextBool(220))
                {
                    Vector2 startVector = new Vector2((NPC.position.X + ((((NPC.width + 50) * 5f) * (NPC.direction * 2)) / 20f) + 130), NPC.position.Y + (NPC.height - 75));
                    Vector2 projVector = UsefulFunctions.GenerateTargetingVector(startVector, Main.player[NPC.target].Center, 8);
                    projVector += Main.rand.NextVector2Circular(7, 7);
                    projVector += Main.player[NPC.target].velocity / 2;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.Okiku.PhasedMatterBlast>(), antiMatterBlastDamage, 0f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.8f, Pitch = 0.0f }, player.Center); //wobble
                    NPC.ai[1] = 1f;
                }
                if (Main.rand.NextBool(20))
                {
                    Vector2 projVector = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 11);
                    projVector += Main.rand.NextVector2Circular(3, 3);
                    projVector += Main.player[NPC.target].velocity / 2;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.CrazedPurpleCrush>(), crazedPurpleCrushDamage, 0f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item80 with { Volume = 0.3f, Pitch = 0.5f }, NPC.Center); //acid flame
                    NPC.ai[1] = 1f;
                }
            }

            if (NPC.justHit)
            {
                NPC.ai[2] = 0f;
            }
            if (NPC.ai[2] >= 0f)
            {
                int num258 = 16;
                bool flag26 = false;
                bool flag27 = false;
                if (NPC.position.X > NPC.ai[0] - (float)num258 && NPC.position.X < NPC.ai[0] + (float)num258)
                {
                    flag26 = true;
                }
                else
                {
                    if ((NPC.velocity.X < 0f && NPC.direction > 0) || (NPC.velocity.X > 0f && NPC.direction < 0))
                    {
                        flag26 = true;
                    }
                }
                num258 += 24;
                if (NPC.position.Y > NPC.ai[1] - (float)num258 && NPC.position.Y < NPC.ai[1] + (float)num258)
                {
                    flag27 = true;
                }
                if (flag26 && flag27)
                {
                    NPC.ai[2] += 1f;
                    if (NPC.ai[2] >= 30f && num258 == 16)
                    {
                        flag25 = true;
                    }
                    if (NPC.ai[2] >= 60f)
                    {
                        NPC.ai[2] = -200f;
                        NPC.direction *= -1;
                        NPC.velocity.X = NPC.velocity.X * -1f;
                        NPC.collideX = false;
                    }
                }
                else
                {
                    NPC.ai[0] = NPC.position.X;
                    NPC.ai[1] = NPC.position.Y;
                    NPC.ai[2] = 0f;
                }
            }
            else
            {
                NPC.ai[2] += 1f;
                if (Main.player[NPC.target].position.X + (float)(Main.player[NPC.target].width / 2) > NPC.position.X + (float)(NPC.width / 2))
                {
                    NPC.direction = -1;
                }
                else
                {
                    NPC.direction = 1;
                }
            }
            int num259 = (int)((NPC.position.X + (float)(NPC.width / 2)) / 16f) + NPC.direction * 2;
            int num260 = (int)((NPC.position.Y + (float)NPC.height) / 16f);
            bool flag28 = true;
            //bool flag29; //What is this? It doesn't seem to do anything, so i'm commenting it out for now.
            int num261 = 3;
            for (int num269 = num260; num269 < num260 + num261; num269++)
            {
                if (Main.tile[num259, num269] == null)
                {
                    Main.tile[num259, num269].ClearTile();
                }
                if ((Main.tile[num259, num269].HasTile && Main.tileSolid[(int)Main.tile[num259, num269].TileType]) || Main.tile[num259, num269].LiquidAmount > 0)
                {
                    //	if (num269 <= num260 + 1)
                    //{
                    //		flag29 = true;
                    //	}
                    flag28 = false;
                    break;
                }
            }
            if (flag25)
            {
                //	flag29 = false;
                flag28 = true;
            }
            if (flag28)
            {
                NPC.velocity.Y = NPC.velocity.Y + 0.1f;
                if (NPC.velocity.Y > 3f)
                {
                    NPC.velocity.Y = 3f;
                }
            }
            else
            {
                if (NPC.directionY < 0 && NPC.velocity.Y > 0f)
                {
                    NPC.velocity.Y = NPC.velocity.Y - 0.1f;
                }
                if (NPC.velocity.Y < -4f)
                {
                    NPC.velocity.Y = -4f;
                }
            }
            if (NPC.collideX)
            {
                NPC.velocity.X = NPC.oldVelocity.X * -0.4f;
                if (NPC.direction == -1 && NPC.velocity.X > 0f && NPC.velocity.X < 1f)
                {
                    NPC.velocity.X = 1f;
                }
                if (NPC.direction == 1 && NPC.velocity.X < 0f && NPC.velocity.X > -1f)
                {
                    NPC.velocity.X = -1f;
                }
            }
            if (NPC.collideY)
            {
                NPC.velocity.Y = NPC.oldVelocity.Y * -0.25f;
                if (NPC.velocity.Y > 0f && NPC.velocity.Y < 1f)
                {
                    NPC.velocity.Y = 1f;
                }
                if (NPC.velocity.Y < 0f && NPC.velocity.Y > -1f)
                {
                    NPC.velocity.Y = -1f;
                }
            }
            float num270 = 2f;
            if (NPC.direction == -1 && NPC.velocity.X > -num270)
            {
                NPC.velocity.X = NPC.velocity.X - 0.1f;
                if (NPC.velocity.X > num270)
                {
                    NPC.velocity.X = NPC.velocity.X - 0.1f;
                }
                else
                {
                    if (NPC.velocity.X > 0f)
                    {
                        NPC.velocity.X = NPC.velocity.X + 0.05f;
                    }
                }
                if (NPC.velocity.X < -num270)
                {
                    NPC.velocity.X = -num270;
                }
            }
            else
            {
                if (NPC.direction == 1 && NPC.velocity.X < num270)
                {
                    NPC.velocity.X = NPC.velocity.X + 0.1f;
                    if (NPC.velocity.X < -num270)
                    {
                        NPC.velocity.X = NPC.velocity.X + 0.1f;
                    }
                    else
                    {
                        if (NPC.velocity.X < 0f)
                        {
                            NPC.velocity.X = NPC.velocity.X - 0.05f;
                        }
                    }
                    if (NPC.velocity.X > num270)
                    {
                        NPC.velocity.X = num270;
                    }
                }
            }
            if (NPC.directionY == -1 && (double)NPC.velocity.Y > -1.5)
            {
                NPC.velocity.Y = NPC.velocity.Y - 0.04f;
                if ((double)NPC.velocity.Y > 1.5)
                {
                    NPC.velocity.Y = NPC.velocity.Y - 0.05f;
                }
                else
                {
                    if (NPC.velocity.Y > 0f)
                    {
                        NPC.velocity.Y = NPC.velocity.Y + 0.03f;
                    }
                }
                if ((double)NPC.velocity.Y < -1.5)
                {
                    NPC.velocity.Y = -1.5f;
                }
            }
            else
            {
                if (NPC.directionY == 1 && (double)NPC.velocity.Y < 1.5)
                {
                    NPC.velocity.Y = NPC.velocity.Y + 0.04f;
                    if ((double)NPC.velocity.Y < -1.5)
                    {
                        NPC.velocity.Y = NPC.velocity.Y + 0.05f;
                    }
                    else
                    {
                        if (NPC.velocity.Y < 0f)
                        {
                            NPC.velocity.Y = NPC.velocity.Y - 0.03f;
                        }
                    }
                    if ((double)NPC.velocity.Y > 1.5)
                    {
                        NPC.velocity.Y = 1.5f;
                    }
                }
            }
            Lighting.AddLight((int)NPC.position.X / 16, (int)NPC.position.Y / 16, 0.4f, 0f, 0.25f);
        }
        #endregion

        #region Frames
        public override void FindFrame(int currentFrame)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if (NPC.velocity.X < 0)
            {
                NPC.spriteDirection = -1;
            }
            else
            {
                NPC.spriteDirection = 1;
            }
            NPC.rotation = NPC.velocity.X * 0.08f;
            NPC.frameCounter += 1.0;
            if (NPC.frameCounter >= 4.0)
            {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
            }
            if (NPC.ai[3] == 0)
            {
                NPC.alpha = 0;
            }
            else
            {
                NPC.alpha = 200;
            }
        }
        #endregion
        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.MarilithBag>()));
        }
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 4").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 5").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 6").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 7").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 8").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 9").Type, 1f);
            }
            if (!Main.expertMode)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.HolyWarElixir>(), 1);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.GuardianSoul>(), 1);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Magic.Ice3Tome>(), 1);
                if (ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
                {
                    Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.LargeSapphire);
                }
                if (!tsorcRevampWorld.Slain.ContainsKey(NPC.type))
                {
                    Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DarkSoul>(), 30000);
                }
            }            
        }
    }
}