using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies;

class MutantToad : ModNPC
{
    public override void SetDefaults()
    {
        Main.npcFrameCount[NPC.type] = 3;
        AnimationType = 3;
        NPC.aiStyle = 3;
        NPC.damage = 38;
        NPC.defense = 15;
        NPC.height = 40;
        NPC.width = 30;
        NPC.lifeMax = 78;
        NPC.scale = 0.9f;
        NPC.HitSound = SoundID.NPCHit1;
        NPC.DeathSound = SoundID.NPCDeath1;
        NPC.knockBackResist = 0.2f;
        NPC.value = 200;
        Banner = NPC.type;
        BannerItem = ModContent.ItemType<Banners.MutantToadBanner>();

        if (Main.hardMode)
        {
            NPC.defense = 44;
            NPC.value = 550;
            NPC.damage = 110;
            NPC.lifeMax = 290;
            NPC.knockBackResist = 0.1f;

        }

    }

    public float swimTime;
    int cursedFlamesDamage = 22;
    bool breath;
    int breathCD = 20;
    public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
    {
        NPC.lifeMax = (int)(NPC.lifeMax / 2);
        NPC.damage = (int)(NPC.damage / 2);
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        for (int num36 = 0; num36 < 200; num36++)
        {
            if (Main.npc[num36].active && Main.npc[num36].type == NPC.type)
            {
                return 0;
            }
        }
        bool nospecialbiome = !spawnInfo.Player.ZoneJungle && !(spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson) && !spawnInfo.Player.ZoneHallow && !spawnInfo.Player.ZoneMeteor && !spawnInfo.Player.ZoneDungeon; // Not necessary at all to use but needed to make all this work.

        bool sky = nospecialbiome && ((double)spawnInfo.Player.position.Y < Main.worldSurface * 0.44999998807907104);
        bool surface = nospecialbiome && !sky && (spawnInfo.Player.position.Y <= Main.worldSurface);
        bool underground = nospecialbiome && !surface && (spawnInfo.Player.position.Y <= Main.rockLayer);
        bool underworld = (spawnInfo.Player.position.Y > Main.maxTilesY - 190);
        bool cavern = nospecialbiome && (spawnInfo.Player.position.Y >= Main.rockLayer) && (spawnInfo.Player.position.Y <= Main.rockLayer * 25);
        bool undergroundJungle = (spawnInfo.Player.position.Y >= Main.rockLayer) && (spawnInfo.Player.position.Y <= Main.rockLayer * 25) && spawnInfo.Player.ZoneJungle;
        bool undergroundEvil = (spawnInfo.Player.position.Y >= Main.rockLayer) && (spawnInfo.Player.position.Y <= Main.rockLayer * 25) && (spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson);
        bool undergroundHoly = (spawnInfo.Player.position.Y >= Main.rockLayer) && (spawnInfo.Player.position.Y <= Main.rockLayer * 25) && spawnInfo.Player.ZoneHallow;
        if (!Main.dayTime && undergroundJungle)
        {
            if (Main.rand.NextBool(15))
            {
                return 1;
            }
        }

        int closeTownNPCs = 0;
        if (!Main.bloodMoon)
        {
            Vector2 playerPosition = spawnInfo.Player.position + new Vector2(spawnInfo.Player.width / 2, spawnInfo.Player.height / 2);
            for (int num36 = 0; num36 < 200; num36++)
            {
                Vector2 npcPosition = Main.npc[num36].position + new Vector2(Main.npc[num36].width / 2, Main.npc[num36].height / 2);
                if (Main.npc[num36].active && Main.npc[num36].townNPC && Vector2.Distance(playerPosition, npcPosition) < 1500)
                {
                    closeTownNPCs++;
                }
            }
        }
        if (closeTownNPCs == 1 && Main.rand.NextBool(3)) return 0;
        if (closeTownNPCs == 2 && Main.rand.NextBool(2)) return 0;
        if (closeTownNPCs == 3 && Main.rand.Next(3) <= 1) return 0;
        if (closeTownNPCs >= 4) return 0;

        return 0;
    }



    public override void OnHitPlayer(Player player, int damage, bool crit) //hook works!
    {
        
            player.AddBuff(BuffID.Poisoned, 1800, false); //poisoned!
        

        if (Main.hardMode)
        {
            player.AddBuff(BuffID.Venom, 60, false); //venom
        }

    }


    public override void AI()
    {



        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.3f, Pitch = 0.1f }, NPC.Center); //flame thrower



        if (NPC.life <= 50 && breath == false && Main.hardMode)
        { 
            breath = true;
        }


        if (breath)
        {

            NPC.velocity.X *= 0.7f;
            NPC.velocity.Y *= 0.7f;
            Lighting.AddLight(NPC.Center, Color.YellowGreen.ToVector3() * 3f);


            //play breath sound
            if (Main.rand.NextBool(3))
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.3f, Pitch = 0.1f }, NPC.Center); //flame thrower
            }

            float rotation = (float)Math.Atan2(NPC.Center.Y - Main.player[NPC.target].Center.Y, NPC.Center.X - Main.player[NPC.target].Center.X);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X + (5 * NPC.direction), NPC.Center.Y /*+ (5f * npc.direction)*/, NPC.velocity.X * 3f + (float)Main.rand.Next(-2, 2), NPC.velocity.Y * 3f + (float)Main.rand.Next(-2, 2), ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreath>(), cursedFlamesDamage, 0f, Main.myPlayer); //JungleWyvernFire      cursed dragons breath
            }
            NPC.netUpdate = true;


            breathCD--;


        }

        if (breathCD <= 0)
        {
            breath = false;
            //breathCD = 30;
            NPC.life= 0;
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Mutant Toad Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Mutant Toad Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Mutant Toad Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Mutant Toad Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Mutant Toad Gore 3").Type, 1f);
            }
        }


        if (Main.rand.NextBool(1000))
        { 
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Zombie13 with { Volume = 0.5f, PitchVariance = 1f }, NPC.Center); 
        }
        //I made decent swim code yay
        Player Player = Main.player[NPC.target];

        if (NPC.wet && Player.position.Y > NPC.position.Y)
        {
            //NPC.frameCounter = 1;
            //couldn't figure out how to do the resting frame when falling 
        }

        if ( NPC.wet && Player.position.Y < NPC.position.Y)
        { 
            swimTime++;
            

            //Swim at intervals
            if (swimTime >= 55 && swimTime <= 65 || swimTime >= 95 && swimTime <= 105)
            {
                if (Player.position.X < NPC.position.X)
                {
                    NPC.direction = 1;
                }
                if (Player.position.X > NPC.position.X)
                {
                    NPC.direction = -1;
                }

                NPC.velocity.Y -= 1.2f;
                NPC.netUpdate = true;
                //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item32 with { Volume = 1f, Pitch = 0.0f }, NPC.position); //wing flap sound

            }
            if (swimTime >= 125 && swimTime <= 140)
            {
                if (Player.position.X < NPC.position.X)
                {
                    NPC.direction = 1;
                    
                }
                if (Player.position.X > NPC.position.X)
                {
                    NPC.direction = -1;
                   
                }
                NPC.velocity.Y -= 1.4f;
                NPC.netUpdate = true;
                //Terraria.Audio.SoundEngine.PlaySound(SoundID.Item32 with { Volume = 1f, Pitch = 0.1f }, NPC.position);
                swimTime = 20;
            }


        }

        //int num3 = 60;
        bool flag2 = false;
        int num5 = 60;
        bool flag3 = true;
        if (NPC.velocity.Y == 0f && ((NPC.velocity.X > 0f && NPC.direction < 0) || (NPC.velocity.X < 0f && NPC.direction > 0)))
        {
            flag2 = true;
        }
        if (NPC.position.X == NPC.oldPosition.X || NPC.ai[3] >= (float)num5 || flag2)
        {
            NPC.ai[3] += 1f;
        }
        else
        {
            if ((double)Math.Abs(NPC.velocity.X) > 0.9 && NPC.ai[3] > 0f)
            {
                NPC.ai[3] -= 1f;
            }
        }
        if (NPC.ai[3] > (float)(num5 * 10))
        {
            NPC.ai[3] = 0f;
        }
        if (NPC.justHit)
        {
            NPC.ai[3] = 0f;
        }
        if (NPC.ai[3] == (float)num5)
        {
            NPC.netUpdate = true;
        }
        NPC.TargetClosest(true);
        if (NPC.velocity.X < -1.5f || NPC.velocity.X > 1.5f)
        {
            if (NPC.velocity.Y == 0f)
            {
                NPC.velocity *= 0.8f;
            }
        }
        else
        {
            if (NPC.velocity.X < 0.9f && NPC.direction == 1)
            {
                NPC.velocity.X = NPC.velocity.X + 0.07f;
                if (NPC.velocity.X > 0.9f)
                {
                    NPC.velocity.X = 0.9f;
                }
            }
            else
            {
                if (NPC.velocity.X > -0.9f && NPC.direction == -1)
                {
                    NPC.velocity.X = NPC.velocity.X - 0.07f;
                    if (NPC.velocity.X < -0.9f)
                    {
                        NPC.velocity.X = -0.9f;
                    }
                }
            }
        }
        bool flag4 = false;
        if (NPC.velocity.Y == 0f)
        {
            int num29 = (int)(NPC.position.Y + (float)NPC.height + 8f) / 16;
            int num30 = (int)NPC.position.X / 16;
            int num31 = (int)(NPC.position.X + (float)NPC.width) / 16;
            for (int l = num30; l <= num31; l++)
            {
                if (Main.tile[l, num29] == null)
                {
                    return;
                }
                if (Main.tile[l, num29].HasTile && Main.tileSolid[(int)Main.tile[l, num29].TileType])
                {
                    flag4 = true;
                    break;
                }
            }
        }
        if (flag4)
        {
            int num32 = (int)((NPC.position.X + (float)(NPC.width / 2) + (float)(15 * NPC.direction)) / 16f);
            int num33 = (int)((NPC.position.Y + (float)NPC.height - 15f) / 16f);
            if (NPC.type == 109)
            {
                num32 = (int)((NPC.position.X + (float)(NPC.width / 2) + (float)((NPC.width / 2 + 16) * NPC.direction)) / 16f);
            }
            if (Main.tile[num32, num33] == null)
            {
                Main.tile[num32, num33].ClearTile();
            }
            if (Main.tile[num32, num33 - 1] == null)
            {
                Main.tile[num32, num33 - 1].ClearTile();
            }
            if (Main.tile[num32, num33 - 2] == null)
            {
                Main.tile[num32, num33 - 2].ClearTile();
            }
            if (Main.tile[num32, num33 - 3] == null)
            {
                Main.tile[num32, num33 - 3].ClearTile();
            }
            if (Main.tile[num32, num33 + 1] == null)
            {
                Main.tile[num32, num33 + 1].ClearTile();
            }
            if (Main.tile[num32 + NPC.direction, num33 - 1] == null)
            {
                Main.tile[num32 + NPC.direction, num33 - 1].ClearTile();
            }
            if (Main.tile[num32 + NPC.direction, num33 + 1] == null)
            {
                Main.tile[num32 + NPC.direction, num33 + 1].ClearTile();
            }
            if (Main.tile[num32, num33 - 1].HasTile && Main.tile[num32, num33 - 1].TileType == 10 && flag3)
            {
                NPC.ai[2] += 1f;
                NPC.ai[3] = 0f;
                if (NPC.ai[2] >= 60f)
                {
                    NPC.velocity.X = 0.5f * (float)(-(float)NPC.direction);
                    NPC.ai[1] += 1f;
                    NPC.ai[2] = 0f;
                    bool flag5 = false;
                    if (NPC.ai[1] >= 10f)
                    {
                        flag5 = true;
                        NPC.ai[1] = 10f;
                    }
                    WorldGen.KillTile(num32, num33 - 1, true, false, false);
                    if ((Main.netMode != 1 || !flag5) && flag5 && Main.netMode != 1)
                    {
                        if (NPC.type == 26)
                        {
                            WorldGen.KillTile(num32, num33 - 1, false, false, false);
                            if (Main.netMode == 2)
                            {
                                NetMessage.SendData(17, -1, -1, null, 0, (float)num32, (float)(num33 - 1), 0f, 0);
                            }
                        }
                        else
                        {
                            bool flag6 = WorldGen.OpenDoor(num32, num33, NPC.direction);
                            if (!flag6)
                            {
                                NPC.ai[3] = (float)num5;
                                NPC.netUpdate = true;
                            }
                            if (Main.netMode == 2 && flag6)
                            {
                                NetMessage.SendData(19, -1, -1, null, 0, (float)num32, (float)num33, (float)NPC.direction, 0);
                            }
                        }
                    }
                }
            }
            else
            {
                if ((NPC.velocity.X < 0f && NPC.spriteDirection == -1) || (NPC.velocity.X > 0f && NPC.spriteDirection == 1))
                {
                    if (Main.tile[num32, num33 - 2].HasTile && Main.tileSolid[(int)Main.tile[num32, num33 - 2].TileType])
                    {
                        if (Main.tile[num32, num33 - 3].HasTile && Main.tileSolid[(int)Main.tile[num32, num33 - 3].TileType])
                        {
                            NPC.velocity.Y = -8f;
                            NPC.netUpdate = true;
                        }
                        else
                        {
                            NPC.velocity.Y = -7f;
                            NPC.netUpdate = true;
                        }
                    }
                    else
                    {
                        if (Main.tile[num32, num33 - 1].HasTile && Main.tileSolid[(int)Main.tile[num32, num33 - 1].TileType])
                        {
                            NPC.velocity.Y = -6f;
                            NPC.netUpdate = true;
                        }
                        else
                        {
                            if (Main.tile[num32, num33].HasTile && Main.tileSolid[(int)Main.tile[num32, num33].TileType])
                            {
                                NPC.velocity.Y = -5f;
                                NPC.netUpdate = true;
                            }
                            else
                            {
                                if (NPC.directionY < 0 && NPC.type != 67 && (!Main.tile[num32, num33 + 1].HasTile || !Main.tileSolid[(int)Main.tile[num32, num33 + 1].TileType]) && (!Main.tile[num32 + NPC.direction, num33 + 1].HasTile || !Main.tileSolid[(int)Main.tile[num32 + NPC.direction, num33 + 1].TileType]))
                                {
                                    NPC.velocity.Y = -8f;
                                    NPC.velocity.X = NPC.velocity.X * 1.5f;
                                    NPC.netUpdate = true;
                                }
                                else
                                {
                                    if (flag3)
                                    {
                                        NPC.ai[1] = 0f;
                                        NPC.ai[2] = 0f;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        else
        {
            if (flag3)
            {
                NPC.ai[1] = 0f;
                NPC.ai[2] = 0f;
            }
        }
    }

    public override void OnKill()
    {
        if (NPC.life <= 0)
        {

            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Mutant Toad Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Mutant Toad Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Mutant Toad Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Mutant Toad Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Mutant Toad Gore 3").Type, 1f);
            }
        }
    }
}