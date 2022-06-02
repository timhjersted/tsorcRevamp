using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    public class AttraidiesManifestation : ModNPC
    {
        public override void SetDefaults()
        {

            NPC.npcSlots = 5;
            Main.npcFrameCount[NPC.type] = 3;
            AnimationType = 29;
            NPC.aiStyle = 0;
            NPC.damage = 40;
            NPC.defense = 8;
            NPC.height = 44;
            NPC.timeLeft = 22500;
            NPC.lifeMax = 800;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.lavaImmune = true;
            NPC.value = 6000;
            NPC.width = 28;
            NPC.knockBackResist = 0.3f;
            banner = NPC.type;
            bannerItem = ModContent.ItemType<Banners.AttraidiesManifestationBanner>();

            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Poisoned] = true;
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.lifeMax = (int)(NPC.lifeMax / 2);
            NPC.damage = (int)(NPC.damage / 2);
            NPC.defense = (int)(NPC.defense * (2 / 3));
            demonSpiritDamage = (int)(demonSpiritDamage / 2);
            poisonFieldDamage = (int)(poisonFieldDamage / 2);
        }

        int demonSpiritDamage = 18;
        int poisonFieldDamage = 23;


        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (!Main.hardMode && spawnInfo.Player.ZoneDungeon && Main.rand.Next(40) == 0 && NPC.CountNPCS(ModContent.NPCType<NPCs.Enemies.JungleWyvernJuvenile.JungleWyvernJuvenileHead>()) < 1
                && NPC.CountNPCS(ModContent.NPCType<NPCs.Enemies.AttraidiesIllusion>()) < 1 && NPC.CountNPCS(ModContent.NPCType<NPCs.Enemies.DungeonMage>()) < 1)
            {
                //MaxSpawns = 1;
                if (!NPC.AnyNPCs(ModContent.NPCType<AttraidiesManifestation>()))
                {
                    return 1;
                }
            }
            return 0;
        }

        #region AI
        public override void AI()
        {
            #region check if standing on a solid tile
            // warning: this section contains a return statement
            if (NPC.velocity.Y == 0f) // no jump/fall
            {
                int y_below_feet = (int)(NPC.position.Y + (float)NPC.height + 8f) / 16;
                int x_left_edge = (int)NPC.position.X / 16;
                int x_right_edge = (int)(NPC.position.X + (float)NPC.width) / 16;
                for (int l = x_left_edge; l <= x_right_edge; l++) // check every block under feet
                {
                    if (Main.tile[l, y_below_feet] == null) // null tile means ??
                        return;

                    if (Main.tile[l, y_below_feet].HasTile && Main.tileSolid[(int)Main.tile[l, y_below_feet].TileType]) // tile exists and is solid
                    {
                        break; // one is enough so stop checking
                    }
                } // END traverse blocks under feet
            } // END no jump/fall
            #endregion

            NPC.netUpdate = false;
            NPC.ai[0]++; // Timer Scythe
            NPC.ai[1]++; // Timer Teleport
                         // npc.ai[2]++; // Shots

            if (NPC.life > 300)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X, NPC.velocity.Y, 210, Color.Red, 1f);
                Main.dust[dust].noGravity = true;
            }
            else if (NPC.life <= 300)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X, NPC.velocity.Y, 140, Color.Red, 1f);
                Main.dust[dust].noGravity = true;
            }

            //if (Main.netMode != 2)
            //{
            if (NPC.ai[2] < 4) //8 was 12 (npc.ai[0] >= 8 && npc.ai[2] < 10) 
            {
                float num48 = 6f;
                Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-10, 35 / 2); //was divided by 5 after parethesis
                float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-10, 43 / 4); //4 was 2
                if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                {
                    float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                    num51 = num48 / num51;
                    speedX *= num51;
                    speedY *= num51;
                    //int damage = 35;//(int) (14f * npc.scale);
                    int type = ModContent.ProjectileType<Projectiles.Enemy.DemonSpirit>(); ;//44;//0x37; //14;
                    int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, demonSpiritDamage, 0f, Main.myPlayer);
                    Main.projectile[num54].timeLeft = 120;
                    //Main.projectile[num54].aiStyle = 11; //11 was 4
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    NPC.ai[0] = 0;
                    NPC.ai[2]++;
                }
                //}
            }

            if (NPC.ai[1] >= 35)
            {


                NPC.velocity.X *= 0.42f;
                NPC.velocity.Y *= 0.17f;
            }

            if ((NPC.ai[1] >= 290 && NPC.life > 300) || (NPC.ai[1] >= 170 && NPC.life <= 300))
            {
                Terraria.Audio.SoundEngine.PlaySound(2, (int)NPC.position.X, (int)NPC.position.Y, 8);
                for (int num36 = 0; num36 < 10; num36++)
                {
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 55, NPC.velocity.X + Main.rand.Next(-10, 10), NPC.velocity.Y + Main.rand.Next(-10, 10), 200, Color.Red, 2f);
                    Main.dust[dust].noGravity = true;
                }
                NPC.ai[3] = (float)(Main.rand.Next(360) * (Math.PI / 180));
                NPC.ai[2] = 0;
                //npc.ai[1] = 0;
                if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
                {
                    NPC.TargetClosest(true);
                }
                if (Main.player[NPC.target].dead)
                {
                    NPC.position.X = 0;
                    NPC.position.Y = 0;
                    if (NPC.timeLeft > 10)
                    {
                        NPC.timeLeft = 10;
                        return;
                    }
                }
                else
                {
                    //region teleportation - can't believe I got this to work.. yayyyyy :D lol

                    int target_x_blockpos = (int)Main.player[NPC.target].position.X / 16; // corner not center
                    int target_y_blockpos = (int)Main.player[NPC.target].position.Y / 16; // corner not center
                    int x_blockpos = (int)NPC.position.X / 16; // corner not center
                    int y_blockpos = (int)NPC.position.Y / 16; // corner not center
                    int tp_radius = 35; // radius around target(upper left corner) in blocks to teleport into
                    int tp_counter = 0;
                    bool flag7 = false;
                    if (Math.Abs(NPC.position.X - Main.player[NPC.target].position.X) + Math.Abs(NPC.position.Y - Main.player[NPC.target].position.Y) > 600000f)
                    { // far away from target; 4000 pixels = 250 blocks
                        tp_counter = 100;
                        flag7 = false; // always telleport was true for no teleport
                    }
                    while (!flag7) // loop always ran full 100 time before I added "flag7 = true" below
                    {
                        if (tp_counter >= 100) // run 100 times
                            break; //return;
                        tp_counter++;

                        int tp_x_target = Main.rand.Next(target_x_blockpos - tp_radius, target_x_blockpos + tp_radius);  //  pick random tp point (centered on corner)
                        int tp_y_target = Main.rand.Next((target_y_blockpos - tp_radius) - 80, (target_y_blockpos + tp_radius) - 40);  //  pick random tp point (centered on corner)
                        for (int m = tp_y_target; m < target_y_blockpos + tp_radius; m++) // traverse y downward to edge of radius
                        { // (tp_x_target,m) is block under its feet I think
                            if ((m < target_y_blockpos - 22 || m > target_y_blockpos + 22 || tp_x_target < target_x_blockpos - 22 || tp_x_target > target_x_blockpos + 22) && (m < y_blockpos - 5 || m > y_blockpos + 5 || tp_x_target < x_blockpos - 5 || tp_x_target > x_blockpos + 5) && Main.tile[tp_x_target, m].HasTile)
                            { // over 14 blocks distant from player & over 5 block distant from old position & tile active(to avoid surface? want to tp onto a block?)
                                bool safe_to_stand = true;
                                bool dark_caster = false; // not a fighter type AI...
                                if (dark_caster && Main.tile[tp_x_target, m - 1].WallType == 0) // Dark Caster & ?outdoors
                                    safe_to_stand = false;
                                else if (Main.tile[tp_x_target, m - 1].LiquidType) // feet submerged in lava
                                    safe_to_stand = false;

                                if (safe_to_stand && Main.tileSolid[(int)Main.tile[tp_x_target, m].TileType] && !Collision.SolidTiles(tp_x_target - 1, tp_x_target + 1, m - 4, m - 1))
                                { // safe enviornment & solid below feet & 3x4 tile region is clear; (tp_x_target,m) is below bottom middle tile

                                    NPC.TargetClosest(true);
                                    NPC.position.X = (float)(tp_x_target * 16 - NPC.width / 2); // center x at target
                                    NPC.position.Y = (float)(m * 16 - NPC.height); // y so block is under feet
                                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                                    NPC.velocity.X = (float)(Math.Cos(rotation) * 12) * -1;
                                    NPC.velocity.Y = (float)(Math.Sin(rotation) * 6) * -1;


                                    // npc.position.X = (float)(tp_x_target * 16 - npc.width / 2); // center x at target
                                    // npc.position.Y = (float)(m * 16 - npc.height); // y so block is under feet
                                    NPC.netUpdate = true;

                                    //npc.ai[3] = -120f; // -120 boredom is signal to display effects & reset boredom next tick in section "teleportation particle effects"
                                    flag7 = true; // end the loop (after testing every lower point :/)
                                    NPC.ai[1] = 0;
                                }
                            } // END over 4 blocks distant from player...
                        } // END traverse y down to edge of radius
                    } // END try 100 times


                }
            }

            //end of W1k's Death code

            //beginning of Omnir's Ultima Weapon projectile code

            NPC.ai[3]++;

            if (NPC.ai[3] >= 10) //how often the crystal attack can happen in frames per second
            {
                if (Main.rand.Next(2) == 0) //1 in 2 chance boss will use attack when it flies down on top of you
                {
                    float num48 = 6f;
                    Vector2 vector9 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y - 300 + (NPC.height / 2));
                    float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector9.X) + Main.rand.Next(-20, 0x15);
                    float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector9.Y) + Main.rand.Next(-20, 0x15); //10 was 0x15
                    if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                    {
                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        //int damage = 45;//(int) (14f * npc.scale);
                        int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellPoisonFieldBall>();//44;//0x37; //14;
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector9.X, vector9.Y, speedX, speedY, type, poisonFieldDamage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 350;
                        Terraria.Audio.SoundEngine.PlaySound(2, (int)NPC.position.X, (int)NPC.position.Y, 25);
                        NPC.ai[3] = 0; ;
                    }
                }

                if (Main.rand.Next(350) == 0) //1 in 20 chance boss will summon an NPC
                {
                    int Random = Main.rand.Next(80);
                    int Paraspawn = 0;
                    int Paraspawn2 = 0;

                    if (Random == 0) Paraspawn = NPC.NewNPC(NPC.GetSource_FromAI(), (int)Main.player[this.NPC.target].position.X - 636 - this.NPC.width / 2, (int)Main.player[this.NPC.target].position.Y - 16 - this.NPC.width / 2, NPCID.CursedSkull, 0);
                    if (Random == 5) Paraspawn2 = NPC.NewNPC(NPC.GetSource_FromAI(), (int)Main.player[this.NPC.target].position.X + 636 - this.NPC.width / 2, (int)Main.player[this.NPC.target].position.Y - 16 - this.NPC.width / 2, NPCID.ChaosElemental, 0);
                    Main.npc[Paraspawn].velocity.X = NPC.velocity.X;
                    Main.npc[Paraspawn2].velocity.X = NPC.velocity.X;
                    Main.npc[Paraspawn2].lifeMax = 200;
                    Main.npc[Paraspawn2].life = 200; //because not setting this still gives them 800/100 hp for some reason


                    NPC.active = true;

                }
            }

            NPC.ai[3] += 1; // my attempt at adding the timer that switches back to the shadow orb
            if (NPC.ai[3] >= 600)
            {
                if (NPC.ai[1] == 0) NPC.ai[1] = 1;
                else NPC.ai[1] = 0;
            }
        }

        #endregion


        public override void FindFrame(int currentFrame)
        {

            if ((NPC.velocity.X > -9 && NPC.velocity.X < 9) && (NPC.velocity.Y > -9 && NPC.velocity.Y < 9))
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = 0;
                if (NPC.position.X > Main.player[NPC.target].position.X)
                {
                    NPC.spriteDirection = -1;
                }
                else
                {
                    NPC.spriteDirection = 1;
                }
            }

            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if ((NPC.velocity.X > -2 && NPC.velocity.X < 2) && (NPC.velocity.Y > -2 && NPC.velocity.Y < 2))
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = 0;
            }
            else
            {
                NPC.frameCounter += 1.0;
            }
            if (NPC.frameCounter >= 1.0)
            {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
            }
        }


        public override void OnKill()
        {
            Player player = Main.player[NPC.target];

            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.HealingElixir>(), 1);
            if (Main.rand.Next(100) < 20) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.IronskinPotion, 2);

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                if (Main.rand.Next(100) < 35) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.RadiantLifegem>());
                if (Main.rand.Next(100) < 35) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.RadiantLifegem>());
            }
            else
            {
                if (Main.rand.Next(100) < 50) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.GreaterHealingPotion, 3);
            }

            if (Main.rand.Next(100) < 50) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.ManaRegenerationPotion, 1);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.IronskinPotion, 1);
            if (Main.rand.Next(100) < 5) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Magic.WandOfFrost>());
            if (Main.rand.Next(100) < 1) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.GillsPotion, 1);
            if (Main.rand.Next(100) < 1) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.HunterPotion, 1);
            if (Main.rand.Next(100) < 60) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.MagicPowerPotion, 2);
            if (Main.rand.Next(100) < 10) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.RegenerationPotion, 1);
            if (Main.rand.Next(100) < 20) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.ShinePotion, 1);

            UsefulFunctions.BroadcastText("The Attraidies Illusion has been defeated...", 150, 150, 150);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Mindflayer Gore 1").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Mindflayer Gore 2").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Mindflayer Gore 3").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Mindflayer Gore 2").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Mindflayer Gore 3").Type, 1f);

        }
    }
}