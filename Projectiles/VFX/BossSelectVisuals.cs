
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Graphics.Effects;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria.Graphics.Shaders;
using tsorcRevamp.Items.BossItems;
using System.Collections.Generic;
using Terraria.ModLoader.Config;

namespace tsorcRevamp.Projectiles.VFX
{
    class BossSelectVisuals : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.width = 48;
            Projectile.height = 62;
            Projectile.penetrate = -1;
            Projectile.scale = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 999;
        }

        bool inititalized = false;
        float radius = 0;
        public override void AI()
        {
            if (!inititalized)
            {
                PreHardmodeDownedBosses = new List<NPC>();
                HardmodeDownedBosses = new List<NPC>();
                SHMDownedBosses = new List<NPC>();
                foreach (int id in tsorcRevampWorld.PreHardmodeBossIDs.Keys)
                {
                    PreHardmodeDownedBosses.Add(new NPC());
                    if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(id)) || (id == NPCID.EaterofWorldsHead && NPC.downedBoss2))
                    {
                        PreHardmodeDownedBosses[PreHardmodeDownedBosses.Count - 1].SetDefaults(id);
                    }
                    else
                    {
                        PreHardmodeDownedBosses[PreHardmodeDownedBosses.Count - 1].SetDefaults(NPCID.Bunny);
                    }
                }
                foreach (int id in tsorcRevampWorld.HardmodeBossIDs.Keys)
                {
                    HardmodeDownedBosses.Add(new NPC());
                    
                    if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(id)) || (id == ModContent.NPCType<NPCs.Bosses.Okiku.FirstForm.DarkShogunMask>() && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>()))))
                    {
                        //Draw golems head instead of its body
                        int newID = id;
                        if (newID == NPCID.Golem)
                        {
                            newID = NPCID.GolemHeadFree;
                        }
                        HardmodeDownedBosses[HardmodeDownedBosses.Count - 1].SetDefaults(newID);
                    }
                    else
                    {
                        HardmodeDownedBosses[HardmodeDownedBosses.Count - 1].SetDefaults(NPCID.Bunny);
                    }
                }
                foreach (int id in tsorcRevampWorld.SHMBossIDs.Keys)
                {
                    SHMDownedBosses.Add(new NPC());
                    if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(id)))
                    {
                        int newID = id;
                        if (newID == NPCID.MoonLordCore)
                        {
                            newID = NPCID.MoonLordFreeEye;
                        }
                        SHMDownedBosses[SHMDownedBosses.Count - 1].SetDefaults(newID);
                    }
                    else
                    {
                        SHMDownedBosses[SHMDownedBosses.Count - 1].SetDefaults(NPCID.Bunny);
                    }
                }

                inititalized = true;
            }

            //Main.player[Projectile.owner].mouseInterface = true;
            radius += 1f / 60f;

            Projectile.Center = Main.player[Projectile.owner].Center;
            if (currentDownedList != null && Projectile.timeLeft == 1 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < currentDownedList.Count; i++)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), currentDownedList[i].Center, UsefulFunctions.Aim(currentDownedList[i].Center, Main.player[Projectile.owner].Center, 3), ModContent.ProjectileType<ExplosionFlash>(), 0, 0, Main.myPlayer, 300, 20);
                }
            }

            if (spawnCountdown > 0)
            {
                spawnCountdown--;
                if (spawnCountdown == 0)
                {
                    SpawnBoss(spawnID);
                    Projectile.Kill();
                }
            }
            else
            {
                //Blight
                if (currentDownedList == SHMDownedBosses && SHMDownedBosses[6].type != NPCID.Bunny)
                {
                    int dust = Dust.NewDust(Projectile.Center + new Vector2(450 * UsefulFunctions.EasingCurve(Math.Min(radius, 1)), 0).RotatedBy(-6 * MathHelper.TwoPi / currentDownedList.Count), 60, 110, 15, 0, 0, 250, default, 5f);
                    Main.dust[dust].noGravity = true;
                }
            }

            if (Main.player[Projectile.owner].HeldItem.type != ModContent.ItemType<Items.BossItems.BossRematchTome>() && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < currentDownedList.Count; i++)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), currentDownedList[i].Center, UsefulFunctions.Aim(currentDownedList[i].Center, Main.player[Projectile.owner].Center, 3), ModContent.ProjectileType<ExplosionFlash>(), 0, 0, Main.myPlayer, 300, 20);
                }
                Projectile.Kill();
            }
        }

        List<NPC> PreHardmodeDownedBosses;
        List<NPC> HardmodeDownedBosses;
        List<NPC> SHMDownedBosses;
        int spawnCountdown = 0;
        int spawnID = 0;
        List<NPC> currentDownedList;
        public static Texture2D buttonTexture;
        public static Texture2D questionmarkTexture;
        public override bool PreDraw(ref Color lightColor)
        {
            if (currentDownedList == null)
            {
                currentDownedList = PreHardmodeDownedBosses;
            }

            if (spawnCountdown != 0)
            {
                if (spawnCountdown == 115 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < currentDownedList.Count; i++)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), currentDownedList[i].Center, UsefulFunctions.Aim(currentDownedList[i].Center, Main.player[Projectile.owner].Center, 3), ModContent.ProjectileType<ExplosionFlash>(), 0, 0, Main.myPlayer, 300, 20);
                    }
                }
                return false;
            }

            UsefulFunctions.EnsureLoaded(ref buttonTexture, "tsorcRevamp/UI/Button_Forward");
            UsefulFunctions.EnsureLoaded(ref questionmarkTexture, "tsorcRevamp/UI/QuestionMark");

            //Draw left and right buttons
            if (Projectile.owner == Main.myPlayer)
            {
                //Right button
                if (currentDownedList != SHMDownedBosses)
                {
                    float buttonScale = 1.2f;
                    if (Vector2.Distance(Main.MouseWorld, Main.player[Projectile.owner].Center + new Vector2(600, 0)) < 120)
                    {
                        buttonScale = 1.4f;
                        Main.LocalPlayer.mouseInterface = true;
                        if (Main.mouseLeft && Main.mouseLeftRelease)
                        {
                            Projectile.timeLeft = 999;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                            if (currentDownedList == HardmodeDownedBosses)
                            {
                                currentDownedList = SHMDownedBosses;
                            }
                            else
                            {
                                currentDownedList = HardmodeDownedBosses;
                            }
                        }
                    }

                    Main.spriteBatch.Draw(buttonTexture, Main.player[Projectile.owner].Center + new Vector2(600, 0) - Main.screenPosition, buttonTexture.Bounds, Color.White, 0, buttonTexture.Bounds.Size() / 2, buttonScale, SpriteEffects.None, 0);
                }

                //Left button
                if (currentDownedList != PreHardmodeDownedBosses)
                {
                    float buttonScale = 1.2f;
                    if (Vector2.Distance(Main.MouseWorld, Main.player[Projectile.owner].Center + new Vector2(-600, 0)) < 120)
                    {
                        buttonScale = 1.4f;
                        Main.LocalPlayer.mouseInterface = true;
                        if (Main.mouseLeft && Main.mouseLeftRelease)
                        {
                            Projectile.timeLeft = 999;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                            if (currentDownedList == HardmodeDownedBosses)
                            {
                                currentDownedList = PreHardmodeDownedBosses;
                            }
                            else
                            {
                                currentDownedList = HardmodeDownedBosses;
                            }
                        }
                    }
                    Main.spriteBatch.Draw(buttonTexture, Main.player[Projectile.owner].Center + new Vector2(-600, 0) - Main.screenPosition, buttonTexture.Bounds, Color.White, 0, buttonTexture.Bounds.Size() / 2, buttonScale, SpriteEffects.FlipHorizontally, 0);
                }
            }


            for (int i = 0; i < currentDownedList.Count; i++)
            {
                Vector2 drawPos = new Vector2(350 * UsefulFunctions.EasingCurve(Math.Min(radius, 1)), 0).RotatedBy(-i * MathHelper.TwoPi / currentDownedList.Count);
                drawPos.X *= 1.2f;

                //Bunnies are used in place of non-defeated bosses, and are not rendered
                if (currentDownedList[i].type == NPCID.Bunny)
                {
                    Main.spriteBatch.Draw(questionmarkTexture, Projectile.Center + drawPos - Main.screenPosition, questionmarkTexture.Bounds, Color.White, 0, questionmarkTexture.Bounds.Size() / 2, currentDownedList[i].scale * 1.3f, SpriteEffects.None, 0);
                    continue;
                }

                currentDownedList[i].Center = Projectile.Center + drawPos;
                Lighting.AddLight(currentDownedList[i].Center, TorchID.White);
                currentDownedList[i].scale = 0.9f;
                currentDownedList[i].alpha = 0;

                //King and queen slime don't scale down right
                if (currentDownedList[i].type == NPCID.KingSlime || currentDownedList[i].type == NPCID.QueenSlimeBoss)
                {
                    currentDownedList[i].scale = 1f;
                }


                if (Projectile.owner == Main.myPlayer)
                {
                    if (currentDownedList[i].Hitbox.Contains(Main.MouseWorld.ToPoint()) && radius >= 1)
                    {
                        Main.hoverItemName = currentDownedList[i].GivenOrTypeName;
                        currentDownedList[i].scale = 1.1f;
                        Main.LocalPlayer.mouseInterface = true;
                        if (Main.mouseLeft && Main.mouseLeftRelease)
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                            spawnCountdown = 120;
                            spawnID = currentDownedList[i].type;
                            TeleportAllPlayersToArena(spawnID);
                        }
                    }
                }

                //Golem's head doesn't scale up *or* down right
                if(currentDownedList[i].type == NPCID.GolemHeadFree)
                {
                    currentDownedList[i].scale = 1f;
                }
                
                //EoL is just way too big
                if (currentDownedList[i].type == NPCID.HallowBoss)
                {
                    currentDownedList[i].scale *= 0.8f;
                }

                if (currentDownedList[i].type == ModContent.NPCType<NPCs.Bosses.Okiku.FirstForm.DarkShogunMask>())
                {
                    Rectangle drawRect = ((Texture2D)Terraria.GameContent.TextureAssets.Npc[ModContent.NPCType<NPCs.Bosses.Okiku.FirstForm.DamnedSoul>()]).Bounds;
                    drawRect.Height = drawRect.Height / 4;
                    Main.spriteBatch.Draw((Texture2D)Terraria.GameContent.TextureAssets.Npc[ModContent.NPCType<NPCs.Bosses.Okiku.FirstForm.DamnedSoul>()], Projectile.Center + drawPos - Main.screenPosition, drawRect, Color.White* 0.6f, 0, drawRect.Size() / 2, currentDownedList[i].scale * 1.3f, SpriteEffects.None, 0);
                }

                Main.instance.DrawNPCDirect(Main.spriteBatch, currentDownedList[i], false, Main.screenPosition);
            }

            return false;
        }

        public void TeleportAllPlayersToArena(int id)
        {
            //No teleporting outside of adventure mode
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                return;
            }

            //Golem head gets drawn, but its body must be spawned
            if (id == NPCID.GolemHeadFree)
            {
                id = NPCID.Golem;
            }
            
            //Moon lords eye gets drawn, but its body must be spawned
            if (id == NPCID.MoonLordFreeEye)
            {
                id = NPCID.MoonLordCore;
            }

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.player[Projectile.owner].Center = tsorcRevampWorld.BossIDsAndCoordinates[id] * 16;
            }
            //TODO: Make teleport all players packet
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket timePacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                timePacket.Write(tsorcPacketID.TeleportAllPlayers);
                timePacket.WriteVector2(tsorcRevampWorld.BossIDsAndCoordinates[id] * 16);
                timePacket.Send();
            }
        }

        public void SpawnBoss(int id)
        {
            //WoF must spawn to the left of the player, or else the vibes get bad
            Vector2 spawnOffset = new Vector2(0, -400);
            if (id == NPCID.WallofFlesh)
            {
                spawnOffset = new Vector2(-1000, 200);
            }

            //The "Prime Intro" NPC must be spawned instead of prime itself for its intro to work properly on the adventure map
            if (id == ModContent.NPCType<NPCs.Bosses.PrimeV2.TheMachine>())
            {
                id = ModContent.NPCType<NPCs.Bosses.PrimeV2.PrimeIntro>();
            }

            //For Serris, just spawn three of the wormy bois and call it a day.
            if (id == ModContent.NPCType<NPCs.Bosses.Serris.SerrisX>())
            {
                id = ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>();
                SpawnBoss(id);
                SpawnBoss(id);
                SpawnBoss(id);
                return;
            }

            //Spawn serris worms with a random offset around the player
            if(id == ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>())
            {
                spawnOffset = Main.rand.NextVector2CircularEdge(600, 600);
            }
           
            //Spawn in the partners of various bosses
            if (id == ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>())
            {
                SpawnBoss(ModContent.NPCType<NPCs.Bosses.WyvernMage.MechaDragonHead>());
            }

            if (id == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>())
            {
                SpawnBoss(ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonHead>());
            }

            if (id == ModContent.NPCType<NPCs.Bosses.Slogra>())
            {
                SpawnBoss(ModContent.NPCType<NPCs.Bosses.Gaibon>());
            }
            if (id == ModContent.NPCType<NPCs.Bosses.Cataluminance>())
            {
                SpawnBoss(ModContent.NPCType<NPCs.Bosses.RetinazerV2>());
                SpawnBoss(ModContent.NPCType<NPCs.Bosses.SpazmatismV2>());
            }

            //Golem head gets drawn, but its body must be spawned
            if (id == NPCID.GolemHeadFree)
            {
                id = NPCID.Golem;
            }

            //Moon lords eye gets drawn, but its body must be spawned
            if (id == NPCID.MoonLordFreeEye)
            {
                id = NPCID.MoonLordCore;
            }

            //Tell the server to spawn it
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket timePacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                timePacket.Write(tsorcPacketID.SpawnNPC);
                timePacket.Write(id);
                timePacket.WriteVector2(Main.player[Projectile.owner].Center + spawnOffset);
                timePacket.Send();
            }
            else if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Main.player[Projectile.owner].Center + spawnOffset, Vector2.Zero, ModContent.ProjectileType<ExplosionFlash>(), 0, 0, Main.myPlayer, 600, 40);

                NPC.NewNPCDirect(Projectile.GetSource_FromThis(), Main.player[Projectile.owner].Center + spawnOffset, id);
            }
        }

        public override bool? CanDamage()
        {
            return false;
        }
    }
}