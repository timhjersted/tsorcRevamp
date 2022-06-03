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
            NPC.damage = 60;
            NPC.defense = 38;
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

        int lightningDamage = 90;
        int antiMatterBlastDamage = 65;
        int crazedPurpleCrushDamage = 80;
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

            if (NPC.ai[1] >= 10f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.ai[1] += (Main.rand.Next(2, 5) * 0.1f) * NPC.scale;
                if (Main.rand.Next(70) == 1)
                {
                    Vector2 projVector = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 15);
                    projVector += Main.rand.NextVector2Circular(5, 5);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellLightning4Ball>(), lightningDamage, 0f, Main.myPlayer, Main.rand.Next(30, 180));
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    NPC.ai[1] = 1f;
                }
                if (Main.rand.Next(220) == 1)
                {
                    Vector2 startVector = new Vector2((NPC.position.X + ((((NPC.width + 50) * 5f) * (NPC.direction * 2)) / 20f) + 130), NPC.position.Y + (NPC.height - 75));
                    Vector2 projVector = UsefulFunctions.GenerateTargetingVector(startVector, Main.player[NPC.target].Center, 8);
                    projVector += Main.rand.NextVector2Circular(7, 7);
                    projVector += Main.player[NPC.target].velocity / 2;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.Okiku.PhasedMatterBlast>(), antiMatterBlastDamage, 0f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    NPC.ai[1] = 1f;
                }
                if (Main.rand.Next(20) == 1)
                {
                    Vector2 projVector = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 11);
                    projVector += Main.rand.NextVector2Circular(3, 3);
                    projVector += Main.player[NPC.target].velocity / 2;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.CrazedPurpleCrush>(), crazedPurpleCrushDamage, 0f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
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
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 1").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 2").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 3").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 4").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 5").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 6").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 7").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 8").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Fire Fiend Marilith Gore 9").Type, 1f);

            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.HolyWarElixir>(), 1);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.GuardianSoul>(), 1);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Magic.Ice3Tome>(), 1);
            if (!tsorcRevampWorld.Slain.ContainsKey(NPC.type))
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DarkSoul>(), 30000);
            }
        }
    }
}