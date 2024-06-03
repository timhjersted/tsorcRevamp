
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Utilities;

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
                    if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(id)) || (id == NPCID.EaterofWorldsHead && NPC.downedBoss2) || Main.player[Projectile.owner].HasItem(ModContent.ItemType<Items.Debug.DebugTome>()))
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

                    if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(id)) || (id == ModContent.NPCType<NPCs.Bosses.Okiku.FirstForm.DarkShogunMask>() && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>()))) || Main.player[Projectile.owner].HasItem(ModContent.ItemType<Items.Debug.DebugTome>()))
                    {
                        //Draw golems head instead of its body
                        int newID = id;
                        HardmodeDownedBosses[HardmodeDownedBosses.Count - 1].SetDefaults(newID);
                        if (newID == NPCID.Golem)
                        {
                            newID = NPCID.GolemHeadFree;
                            HardmodeDownedBosses[HardmodeDownedBosses.Count - 1].SetDefaults(newID);
                        }
                    }
                    else
                    {
                        HardmodeDownedBosses[HardmodeDownedBosses.Count - 1].SetDefaults(NPCID.Bunny);
                    }
                }
                foreach (int id in tsorcRevampWorld.SHMBossIDs.Keys)
                {
                    SHMDownedBosses.Add(new NPC());
                    if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(id)) || Main.player[Projectile.owner].HasItem(ModContent.ItemType<Items.Debug.DebugTome>()))
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

            if (Main.player[Projectile.owner].HeldItem.type != ModContent.ItemType<Items.BossItems.BossRematchTome>())
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < currentDownedList.Count; i++)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), currentDownedList[i].Center, UsefulFunctions.Aim(currentDownedList[i].Center, Main.player[Projectile.owner].Center, 3), ModContent.ProjectileType<ExplosionFlash>(), 0, 0, Main.myPlayer, 300, 20);
                    }
                }

                if (spawnCountdown > 0)
                {
                    SpawnBoss(spawnID);
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


            string mouseOverGuideText = "";
            string mouseOver = "";
            string DeathCountText = "";
            Vector2 mouseOverPos = Vector2.Zero;
            float mouseOverHeight = 0;
            for (int i = 0; i < currentDownedList.Count; i++)
            {
                Vector2 drawPos = new Vector2(350 * UsefulFunctions.EasingCurve(Math.Min(radius, 1)), 0).RotatedBy(-i * MathHelper.TwoPi / currentDownedList.Count);
                drawPos.X *= 1.2f;
                DeathCountText = LangUtils.GetTextValue("Items.BossRematchTome.DeathCountText", Main.player[Projectile.owner].numberOfDeathsPVE);

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

                if (currentDownedList[i].type == ModContent.NPCType<NPCs.Bosses.TheRage>())
                {
                    currentDownedList[i].ai[0] = 0;
                    currentDownedList[i].scale = 0.7f;
                }
                if (currentDownedList[i].type == ModContent.NPCType<NPCs.Bosses.TheSorrow>())
                {
                    currentDownedList[i].ai[0] = 0;
                }
                if (currentDownedList[i].type == ModContent.NPCType<NPCs.Bosses.TheHunter>())
                {
                    currentDownedList[i].ai[0] = 0;
                }

                if (currentDownedList[i].type == ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>())
                {
                    currentDownedList[i].ai[0] = 0;
                }

                if (Projectile.owner == Main.myPlayer)
                {
                    bool inHitbox = currentDownedList[i].Hitbox.Contains(Main.MouseWorld.ToPoint());
                    if (currentDownedList[i].type == NPCID.GolemHeadFree)
                    {
                        if (new Rectangle(currentDownedList[i].Hitbox.X - 20, currentDownedList[i].Hitbox.Y - 50, currentDownedList[i].Hitbox.Width + 40, currentDownedList[i].Hitbox.Height + 100).Contains(Main.MouseWorld.ToPoint()))
                        {
                            inHitbox = true;
                        }
                    }

                    if (inHitbox && radius >= 1)
                    {
                        Main.hoverItemName = currentDownedList[i].GivenOrTypeName;
                        currentDownedList[i].scale = 1.1f;
                        Main.LocalPlayer.mouseInterface = true;
                        mouseOver = currentDownedList[i].TypeName;
                        //need put mouseOverGuideText with questionmarkTexture, currently text only shows after Boss Downed
                        int nextBoss = currentDownedList[i].rarity + 1;
                        mouseOverGuideText = LangUtils.GetTextValue("Items.BossRematchTome.Next") + "\n" + LangUtils.GetTextValue("Items.BossRematchTome." + nextBoss);
                        mouseOverPos = currentDownedList[i].Center;
                        mouseOverHeight = currentDownedList[i].height;
                        if (currentDownedList[i].type == NPCID.BrainofCthulhu)
                        {
                            mouseOverHeight *= 1.3f;
                        }
                        if (currentDownedList[i].type == NPCID.Deerclops)
                        {
                            mouseOverHeight *= 0.8f;
                        }
                        if (currentDownedList[i].type == ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>())
                        {
                            mouseOverHeight *= 0.6f;
                            currentDownedList[i].ai[0] = 1;
                        }
                        if (currentDownedList[i].type == ModContent.NPCType<NPCs.Bosses.Death>())
                        {
                            mouseOverHeight *= 0.6f;
                        }
                        if (currentDownedList[i].type == ModContent.NPCType<NPCs.Bosses.TheRage>())
                        {
                            currentDownedList[i].ai[0] = currentDownedList[i].lifeMax / 20f;
                            currentDownedList[i].scale = 0.8f;
                        }
                        if (currentDownedList[i].type == ModContent.NPCType<NPCs.Bosses.TheSorrow>())
                        {
                            currentDownedList[i].ai[0] = currentDownedList[i].lifeMax / 20f;
                        }
                        if (currentDownedList[i].type == ModContent.NPCType<NPCs.Bosses.TheHunter>())
                        {
                            currentDownedList[i].ai[0] = currentDownedList[i].lifeMax / 20f;
                        }
                        if (currentDownedList[i].type == NPCID.GolemHeadFree)
                        {
                            mouseOver = Language.GetTextValue("NPCName.Golem");
                        }
                        if (currentDownedList[i].type == NPCID.MoonLordFreeEye)
                        {
                            mouseOver = Language.GetTextValue("NPCName.MoonLordHead");
                            mouseOverHeight *= 1.5f;
                        }

                        if (currentDownedList[i].type == ModContent.NPCType<NPCs.Bosses.Okiku.FirstForm.DarkShogunMask>())
                        {
                            Rectangle drawRect = ((Texture2D)Terraria.GameContent.TextureAssets.Npc[ModContent.NPCType<NPCs.Bosses.Okiku.FirstForm.DamnedSoul>()]).Bounds;
                            drawRect.Height = drawRect.Height / 4;
                            Main.spriteBatch.Draw((Texture2D)Terraria.GameContent.TextureAssets.Npc[ModContent.NPCType<NPCs.Bosses.Okiku.FirstForm.DamnedSoul>()], Projectile.Center + drawPos - Main.screenPosition, drawRect, Color.White * 0.6f, 0, drawRect.Size() / 2, currentDownedList[i].scale * 1.3f, SpriteEffects.None, 0);

                        }

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
                if (currentDownedList[i].type == NPCID.GolemHeadFree)
                {
                    currentDownedList[i].scale = 1f;
                }

                //EoL is just way too big
                if (currentDownedList[i].type == NPCID.HallowBoss)
                {
                    currentDownedList[i].scale *= 0.8f;
                }

                Main.instance.DrawNPCDirect(Main.spriteBatch, currentDownedList[i], false, Main.screenPosition);
            }


            if (mouseOver != "")
            {
                DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, FontAssets.ItemStack.Value, mouseOver, mouseOverPos - Main.screenPosition + new Vector2(1.1f * -FontAssets.ItemStack.Value.MeasureString(mouseOver).X / 2f, mouseOverHeight * 0.75f), Color.White, 0, Vector2.Zero, 1.2f, SpriteEffects.None, 0);
            }
            if (mouseOverGuideText != "")
            {
                DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, FontAssets.ItemStack.Value, mouseOverGuideText, new Vector2(Main.screenWidth/2, Main.screenHeight * 2 / 5) + new Vector2(1.1f * -FontAssets.ItemStack.Value.MeasureString(mouseOverGuideText).X / 2f, 0), Color.White, 0, Vector2.Zero, 1.2f, SpriteEffects.None, 0);
            }

            if (DeathCountText != "")
            {
                DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, FontAssets.MouseText.Value, DeathCountText, new Vector2(Main.screenWidth/2, Main.screenHeight * 3 / 5) + new Vector2(1.1f * -FontAssets.ItemStack.Value.MeasureString(DeathCountText).X / 2f, 0), Color.White, 0, Vector2.Zero, 1.2f, SpriteEffects.None, 0);
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
                UsefulFunctions.SafeTeleport(Main.player[Projectile.owner], tsorcRevampWorld.BossIDsAndCoordinates[id] * 16);
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket teleportPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                teleportPacket.Write(tsorcPacketID.TeleportAllPlayers);
                teleportPacket.WriteVector2(tsorcRevampWorld.BossIDsAndCoordinates[id] * 16);
                teleportPacket.Send();
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
            if (id == ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>())
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
                ModPacket spawnBossPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                spawnBossPacket.Write(tsorcPacketID.SpawnNPC);
                spawnBossPacket.Write(id);
                spawnBossPacket.WriteVector2(Main.player[Projectile.owner].Center + spawnOffset);
                spawnBossPacket.Send();
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