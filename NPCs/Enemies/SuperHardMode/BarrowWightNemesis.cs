using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class BarrowWightNemesis : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Barrow Wight Nemesis");
        }
        public override void SetDefaults()
        {

            NPC.npcSlots = 5;
            Main.npcFrameCount[NPC.type] = 4;
            NPC.width = 58;
            NPC.height = 48;
            NPC.aiStyle = 22;
            NPC.damage = 115;
            NPC.defense = 40;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = 5000;
            NPC.knockBackResist = 0;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.value = 6230;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.BarrowWightNemesisBanner>();
        }

        int breathDamage = 35;
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
            breathDamage = (int)(breathDamage * tsorcRevampWorld.SubtleSHMScale);
        }

        int breathCD = 45;
        //int previous = 0;
        bool breath = false;
        int chargeDamage = 0;
        bool chargeDamageFlag = false;

        //Spawns from the Surface into the Cavern, from 2/10th to 3.5/10th and again from 6.5/10th to 8/10th (Width) on Normal Mode. Also spawns in the Dungeon and in the sky in Hardmode.

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player player = spawnInfo.Player;

            float chance = 0;
            if (tsorcRevampWorld.SuperHardMode)
            {
                if (player.ZoneSkyHeight || spawnInfo.Player.ZoneDungeon)
                {
                    chance = 0.17f;
                }
                else if (player.ZoneOverworldHeight && !Main.dayTime)
                {
                    chance = 0.02f;
                }
            }

            if (!Main.dayTime)
            {
                chance *= 2;
            }
            if (Main.bloodMoon)
            {
                chance *= 2;
            }

            return chance;
        }
        #endregion


        //void DealtPlayer(Player player, double damage, NPC npc)



        //public void DamagePlayer(ref int damage, Player player);





        #region AI
        public override void AI()
        {
            NPC.ai[1] += (Main.rand.Next(2, 5) * 0.1f) * NPC.scale;
            if (NPC.ai[1] >= 10f)
            {
                NPC.TargetClosest(true);



                // charge forward code 
                if (Main.rand.Next(1650) == 1)
                {
                    NPC.netUpdate = true; //new
                    chargeDamageFlag = true;
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    NPC.velocity.X = (float)(Math.Cos(rotation) * 11) * -1;
                    NPC.velocity.Y = (float)(Math.Sin(rotation) * 11) * -1;
                    NPC.ai[1] = 1f;
                    NPC.netUpdate = true;
                }
                if (chargeDamageFlag == true)
                {
                    NPC.damage = 115;
                    chargeDamage++;
                }
                if (chargeDamage >= 115)
                {
                    chargeDamageFlag = false;
                    NPC.damage = 115;
                    chargeDamage = 0;
                }

                // fire breath attack
                if (Main.rand.Next(525) == 0)
                {
                    breath = true;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                }
                if (breath)
                {
                    float rotation = (float)Math.Atan2(NPC.Center.Y - Main.player[NPC.target].Center.Y, NPC.Center.X - Main.player[NPC.target].Center.X);
                    int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, (float)((Math.Cos(rotation) * 15) * -1), (float)((Math.Sin(rotation) * 15) * -1), ModContent.ProjectileType<Projectiles.Enemy.FrozenDragonsBreath>(), breathDamage, 0f, Main.myPlayer);
                    Main.projectile[num54].timeLeft = 30;
                    NPC.netUpdate = true;


                    breathCD--;
                    //}
                }
                if (breathCD <= 0)
                {
                    breath = false;
                    breathCD = 60;
                    //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                }

                //end fire breath attack


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
                NPC.TargetClosest(true);
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
            if (NPC.position.Y > Main.player[NPC.target].position.Y)
            {
                NPC.velocity.Y -= .05f;
            }
            if (NPC.position.Y < Main.player[NPC.target].position.Y)
            {
                NPC.velocity.Y += .05f;
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
            float num270 = .5f;
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
            if (NPC.directionY == -1 && (double)NPC.velocity.Y > -2.5)
            {
                NPC.velocity.Y = NPC.velocity.Y - 0.04f;
                if ((double)NPC.velocity.Y > 2.5)
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
                if ((double)NPC.velocity.Y < -2.5)
                {
                    NPC.velocity.Y = -2.5f;
                }
            }
            else
            {
                if (NPC.directionY == 1 && (double)NPC.velocity.Y < 2.5)
                {
                    NPC.velocity.Y = NPC.velocity.Y + 0.04f;
                    if ((double)NPC.velocity.Y < -2.5)
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
                    if ((double)NPC.velocity.Y > 2.5)
                    {
                        NPC.velocity.Y = 2.5f;
                    }
                }
            }
            Lighting.AddLight((int)NPC.position.X / 16, (int)NPC.position.Y / 16, 0.4f, 0f, 0.25f);
            return;
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

        #region Gore
        public override void OnKill()
        {
            if (NPC.life <= 0)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Barrow Wight Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Barrow Wight Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Barrow Wight Gore 2").Type, 1f);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, 45, 0.3f, 0.3f, 200, default(Color), 1f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 45, 0.2f, 0.2f, 200, default(Color), 2f);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, 45, 0.2f, 0.2f, 200, default(Color), 2f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 45, 0.2f, 0.2f, 200, default(Color), 3f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 45, 0.2f, 0.2f, 200, default(Color), 2f);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, 45, 0.2f, 0.2f, 200, default(Color), 4f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 45, 0.2f, 0.2f, 200, default(Color), 4f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 45, 0.2f, 0.2f, 200, default(Color), 2f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 45, 0.2f, 0.2f, 200, default(Color), 4f);
                if (Main.rand.Next(99) < 20) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Melee.BarrowBlade>());
                if (Main.rand.Next(99) < 2) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.PurgingStone>());
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.CursedSoul>(), 3);
            }
        }
        #endregion

        #region Magic Defense
        public int MagicDefenseValue()
        {
            return 5;
        }
        #endregion

        #region Glowing Eye Effect
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {

            int spriteWidth = NPC.frame.Width; //use same number as ini Main.npcFrameCount[npc.type]
            int spriteHeight = TextureAssets.Npc[ModContent.NPCType<BarrowWightNemesis>()].Value.Height / Main.npcFrameCount[NPC.type];

            int spritePosDifX = (int)(NPC.frame.Width / 2);
            int spritePosDifY = NPC.frame.Height - 3; // was -2

            int frame = NPC.frame.Y / spriteHeight;

            int offsetX = (int)(NPC.position.X + (NPC.width / 2) - Main.screenPosition.X - spritePosDifX + 0.5f);
            int offsetY = (int)(NPC.position.Y + NPC.height - Main.screenPosition.Y - spritePosDifY);

            SpriteEffects flop = SpriteEffects.None;
            if (NPC.spriteDirection == 1)
            {
                flop = SpriteEffects.FlipHorizontally;
            }


            //Glowing Eye Effect
            for (int i = 15; i > -1; i--)
            {
                //draw 3 levels of trail
                int alphaVal = 255 - (15 * i);
                Color modifiedColour = new Color((int)(alphaVal), (int)(alphaVal), (int)(alphaVal), alphaVal);
                spriteBatch.Draw((Texture2D)TextureAssets.Gore[Mod.Find<ModGore>("Barrow Wight Nemesis Glow").Type],
                    new Rectangle((int)(offsetX - NPC.velocity.X * (i * 0.5f)), (int)(offsetY - NPC.velocity.Y * (i * 0.5f)), spriteWidth, spriteHeight),
                    new Rectangle(0, NPC.frame.Height * frame, spriteWidth, spriteHeight),
                    modifiedColour,
                    0,
                    new Vector2(0, 0),
                    flop,
                    0);
            }
        }
        #endregion


        public override void OnHitPlayer(Player player, int target, bool crit)
        {

            if (Main.rand.Next(2) == 0)
            {

                player.AddBuff(BuffID.BrokenArmor, 600, false); //broken armor
                player.AddBuff(BuffID.Frostburn, 600, false); //Frostburn
                player.AddBuff(BuffID.Chilled, 600, false); //Chilled
                player.AddBuff(ModContent.BuffType<Buffs.CurseBuildup>(), 36000, false); //-20 life after several hits
                player.AddBuff(ModContent.BuffType<Buffs.PowerfulCurseBuildup>(), 36000, false); //-100 life after several hits
            }

            //	if (Main.rand.Next(8) == 0 && player.statLifeMax > 20) 

            //{

            //			Main.NewText("You have been cursed!");
            //	player.statLifeMax -= 20;
            //}
        }
    }
}