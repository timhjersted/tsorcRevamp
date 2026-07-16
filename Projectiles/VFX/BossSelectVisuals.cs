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
using tsorcRevamp.NPCs.Bosses.SuperHardMode;
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
            Projectile.timeLeft = 99999;
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
                // Parallel lists tracking the *intended* rarity for each slot, even when the displayed
                // sprite is a bunny placeholder for an undefeated boss. This lets the hover clue lookup
                // work for both defeated bosses (real sprite) and undefeated ones (question mark).
                PreHardmodeRarities = new List<int>();
                HardmodeRarities = new List<int>();
                SHMRarities = new List<int>();
                // Parallel lists of the encounter's primary NPC IDs. These remain the real IDs even
                // where the defeated drawing substitutes Golem's head or Moon Lord's eye.
                PreHardmodeIds = new List<int>();
                HardmodeIds = new List<int>();
                SHMIds = new List<int>();
                NPC tempNPC = new NPC();
                foreach (int id in tsorcRevampWorld.PreHardmodeBossIDs.Keys)
                {
                    tempNPC.SetDefaults(id);
                    PreHardmodeRarities.Add(tempNPC.rarity);
                    PreHardmodeIds.Add(id);
                    PreHardmodeDownedBosses.Add(new NPC());
                    if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(id)) || (id == NPCID.EaterofWorldsHead && NPC.downedBoss2) || Main.player[Projectile.owner].HasItem(ModContent.ItemType<Items.Debug.DebugTome>()) || ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
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
                    tempNPC.SetDefaults(id);
                    HardmodeRarities.Add(tempNPC.rarity);
                    HardmodeIds.Add(id);
                    HardmodeDownedBosses.Add(new NPC());

                    if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(id)) || Main.player[Projectile.owner].HasItem(ModContent.ItemType<Items.Debug.DebugTome>()) || ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
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
                    tempNPC.SetDefaults(id);
                    SHMRarities.Add(tempNPC.rarity);
                    SHMIds.Add(id);
                    SHMDownedBosses.Add(new NPC());
                    if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(id)) || Main.player[Projectile.owner].HasItem(ModContent.ItemType<Items.Debug.DebugTome>()) || ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
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

                // Compute the next undefeated critical path boss across all three eras. The smallest
                // required-boss rarity whose slot is still a bunny.
                nextUndefeatedCriticalPath = int.MaxValue;
                FindNextCriticalPath(PreHardmodeDownedBosses, PreHardmodeRarities, PreHardmodeIds);
                FindNextCriticalPath(HardmodeDownedBosses, HardmodeRarities, HardmodeIds);
                FindNextCriticalPath(SHMDownedBosses, SHMRarities, SHMIds);

                if (currentDownedList == null)
                {
                    currentDownedList = PreHardmodeDownedBosses;
                    currentRarityList = PreHardmodeRarities;
                    currentIdList = PreHardmodeIds;
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
                // Blight's own texture is an empty outline, so its dust supplies the visible body.
                // Locate its actual Tome slot instead of relying on the old hard-coded index, which
                // placed the effect over Fire/Earth Fiend as the roster changed.
                int blightIndex = SHMIds?.IndexOf(ModContent.NPCType<Blight>()) ?? -1;
                if (currentDownedList == SHMDownedBosses && blightIndex >= 0 && SHMDownedBosses[blightIndex].type != NPCID.Bunny)
                {
                    Vector2 blightDrawPos = new Vector2(350 * UsefulFunctions.EasingCurve(Math.Min(radius, 1)), 0)
                        .RotatedBy(-blightIndex * MathHelper.TwoPi / currentDownedList.Count);
                    blightDrawPos.X *= 1.2f;
                    int dust = Dust.NewDust(Projectile.Center + blightDrawPos - new Vector2(30f, 55f), 60, 110, 185, 0, 0, 250, default, 5f);
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
        // Rarity values per slot, captured from each boss's real SetDefaults rarity. Used for clue
        // lookup so that undefeated bosses (drawn as question marks) can still surface their hint
        // when hovered. Parallel to the three Boss lists above.
        List<int> PreHardmodeRarities;
        List<int> HardmodeRarities;
        List<int> SHMRarities;
        // Primary encounter NPC IDs per slot, retained even when the displayed NPC is a bunny,
        // Golem's detached head, or Moon Lord's eye.
        List<int> PreHardmodeIds;
        List<int> HardmodeIds;
        List<int> SHMIds;
        // Smallest required-boss presentation order that is still undefeated across all three eras.
        // Drives two things: which boss slot gets a gold pulse (highlight = "go here next") and the
        // upper bound for which undefeated bosses surface their "Where to find:" hint on hover.
        // Computed once during init in AI() — bosses can't be killed while the menu is open.
        int nextUndefeatedCriticalPath = int.MaxValue;
        int spawnCountdown = 0;
        int spawnID = 0;
        List<NPC> currentDownedList;
        List<int> currentRarityList;
        List<int> currentIdList;
        public static Texture2D buttonTexture;
        public static Texture2D questionmarkTexture;

        private static uint centerTextUpdate;
        private static string centerNameText = "";
        private static string centerClueText = "";
        private static string centerDeathCountText = "";
        private static Color centerNameColor = Color.White;
        private static float centerDeathCountY;

        public override bool PreDraw(ref Color lightColor)
        {
            if (currentDownedList == null)
            {
                currentDownedList = PreHardmodeDownedBosses;
                currentRarityList = PreHardmodeRarities;
                currentIdList = PreHardmodeIds;
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

            // Keep the world legible behind the Tome without dimming its sprites or text.
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black * 0.48f);

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
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                            if (currentDownedList == HardmodeDownedBosses)
                            {
                                currentDownedList = SHMDownedBosses;
                                currentRarityList = SHMRarities;
                                currentIdList = SHMIds;
                            }
                            else
                            {
                                currentDownedList = HardmodeDownedBosses;
                                currentRarityList = HardmodeRarities;
                                currentIdList = HardmodeIds;
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
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                            if (currentDownedList == HardmodeDownedBosses)
                            {
                                currentDownedList = PreHardmodeDownedBosses;
                                currentRarityList = PreHardmodeRarities;
                                currentIdList = PreHardmodeIds;
                            }
                            else
                            {
                                currentDownedList = HardmodeDownedBosses;
                                currentRarityList = HardmodeRarities;
                                currentIdList = HardmodeIds;
                            }
                        }
                    }
                    Main.spriteBatch.Draw(buttonTexture, Main.player[Projectile.owner].Center + new Vector2(-600, 0) - Main.screenPosition, buttonTexture.Bounds, Color.White, 0, buttonTexture.Bounds.Size() / 2, buttonScale, SpriteEffects.FlipHorizontally, 0);
                }
            }


            string mouseOverGuideText = "";
            string mouseOver = "";
            BossEncounterPresentation mouseOverEntry = null;
            bool mouseOverDefeated = false;
            string DeathCountText = "";
            float bottomSlotScreenY = float.MinValue;
            for (int i = 0; i < currentDownedList.Count; i++)
            {
                Vector2 drawPos = new Vector2(350 * UsefulFunctions.EasingCurve(Math.Min(radius, 1)), 0).RotatedBy(-i * MathHelper.TwoPi / currentDownedList.Count);
                drawPos.X *= 1.2f;
                bottomSlotScreenY = Math.Max(bottomSlotScreenY, Projectile.Center.Y + drawPos.Y - Main.screenPosition.Y);
                DeathCountText = LangUtils.GetTextValue("Items.BossRematchTome.DeathCountText", Main.player[Projectile.owner].numberOfDeathsPVE);

                //Bunnies are used in place of non-defeated bosses. They draw as a 
                //a question mark. Critical-path boss question mark is yellow
                //instead of black, signposting where to head next.
                //
                //Clue gating: hover surfaces the "Where to find:" hint only if the boss's rarity is
                //≤ nextUndefeatedCriticalPath. That means after each critical-path defeat, the cluster
                //of optional bosses up to the next critical path unlocks their clues - those are noted by purple question marks. Era gate also
                //applies as a safety so HM/SHM clues don't leak in pre-hardmode.
                if (currentDownedList[i].type == NPCID.Bunny)
                {
                    float qmScale = currentDownedList[i].scale * 1.3f;
                    Vector2 qmCenter = Projectile.Center + drawPos;
                    Vector2 qmHalfSize = questionmarkTexture.Bounds.Size() * qmScale / 2f;
                    Rectangle qmHitbox = new Rectangle(
                        (int)(qmCenter.X - qmHalfSize.X),
                        (int)(qmCenter.Y - qmHalfSize.Y),
                        (int)(qmHalfSize.X * 2),
                        (int)(qmHalfSize.Y * 2));

                    int bossRarity = (currentRarityList != null && i < currentRarityList.Count) ? currentRarityList[i] : 0;
                    int mysteryNpcType = currentIdList != null && i < currentIdList.Count ? currentIdList[i] : 0;
                    EncounterPresentationRegistry.TryGet(mysteryNpcType, out BossEncounterPresentation encounterEntry);
                    int progressionOrder = encounterEntry?.ProgressionOrder ?? bossRarity;
                    bool isNextCriticalPath = encounterEntry != null && encounterEntry.IsRequired && progressionOrder == nextUndefeatedCriticalPath;
                    bool optionalMysteryRevealed = encounterEntry?.AlwaysRevealClue == true;
                    bool clueRevealed = optionalMysteryRevealed || (progressionOrder > 0 && progressionOrder <= nextUndefeatedCriticalPath);

                    // Undefeated bosses just show a question mark — no boss silhouette behind it.
                    // Previous attempts (manual texture draw, DrawNPCDirect + npc.color tint) either
                    // missed sprites, scaled them wrong, or failed to apply the tint to mod bosses
                    // with custom PreDraw + glowmask layers. The question mark alone is a clean,
                    // reliable progression indicator. Once a boss is defeated, the existing defeated
                    // branch below draws the real sprite as before.
                    //
                    // Revealed entries use their classification color. The next required boss keeps
                    // its red classification color and receives a separate gold pulse behind it.
                    Color qmColor = clueRevealed && encounterEntry != null ? encounterEntry.TomeColor : Color.White;
                    if (isNextCriticalPath)
                    {
                        float pulse = 1.12f + 0.05f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f);
                        float pulseAlpha = 0.35f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f);
                        Main.spriteBatch.Draw(questionmarkTexture, qmCenter - Main.screenPosition, questionmarkTexture.Bounds,
                            new Color(255, 190, 45) * pulseAlpha, 0, questionmarkTexture.Bounds.Size() / 2,
                            qmScale * pulse, SpriteEffects.None, 0);
                    }
                    Main.spriteBatch.Draw(questionmarkTexture, qmCenter - Main.screenPosition, questionmarkTexture.Bounds, qmColor, 0, questionmarkTexture.Bounds.Size() / 2, qmScale, SpriteEffects.None, 0);

                    bool eraUnlocked =
                        currentDownedList == PreHardmodeDownedBosses ||
                        (currentDownedList == HardmodeDownedBosses && Main.hardMode) ||
                        (currentDownedList == SHMDownedBosses && tsorcRevampWorld.SuperHardMode);
                    bool criticalPathGateOpen = optionalMysteryRevealed || (progressionOrder > 0 && progressionOrder <= nextUndefeatedCriticalPath);

                    if (eraUnlocked && criticalPathGateOpen && Projectile.owner == Main.myPlayer && qmHitbox.Contains(Main.MouseWorld.ToPoint()) && radius >= 1 && currentRarityList != null && i < currentRarityList.Count)
                    {
                        Main.LocalPlayer.mouseInterface = true;
                        mouseOver = "???";
                        mouseOverEntry = encounterEntry;
                        mouseOverDefeated = false;
                        // Undefeated boss within the current critical-path window: show its "Where to
                        // find:" clue so the player can hunt it. Bosses beyond the next critical path
                        // stay fully hidden (no clue) until that critical path is defeated.
                        int clueIndex = encounterEntry?.ClueIndex ?? currentRarityList[i];
                        mouseOverGuideText = LangUtils.GetTextValue("Items.BossRematchTome.Location") + "\n" + LangUtils.GetTextValue("Items.BossRematchTome." + clueIndex);
                    }
                    continue;
                }

                currentDownedList[i].Center = Projectile.Center + drawPos;
                Lighting.AddLight(currentDownedList[i].Center, TorchID.White);
                int primaryNpcType = currentIdList != null && i < currentIdList.Count ? currentIdList[i] : currentDownedList[i].type;
                float previewScale = GetPreviewScale(currentDownedList[i].type, primaryNpcType);
                currentDownedList[i].scale = previewScale;
                currentDownedList[i].alpha = 0;

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
                        currentDownedList[i].scale = previewScale * 1.15f;
                        Main.LocalPlayer.mouseInterface = true;
                        mouseOver = currentDownedList[i].TypeName;
                        // Defeated boss hover: show the NEXT boss's clue (rarity + 1). Falls back to the
                        // parallel rarity list because some entries override their NPC type at draw time
                        // (Golem head, Moon Lord eye, etc.) which can zero out currentDownedList[i].rarity.
                        int baseRarity = (currentRarityList != null && i < currentRarityList.Count)
                            ? currentRarityList[i]
                            : currentDownedList[i].rarity;
                        EncounterPresentationRegistry.TryGet(primaryNpcType, out mouseOverEntry);
                        mouseOverDefeated = true;
                        if (mouseOverEntry?.AlwaysRevealClue == true)
                        {
                            mouseOverGuideText = LangUtils.GetTextValue("Items.BossRematchTome.Location") + "\n" + LangUtils.GetTextValue("Items.BossRematchTome." + mouseOverEntry.ClueIndex);
                        }
                        else
                        {
                            int nextBoss = mouseOverEntry?.FollowingClueIndex ?? baseRarity + 1;
                            mouseOverGuideText = LangUtils.GetTextValue("Items.BossRematchTome.Next") + "\n" + LangUtils.GetTextValue("Items.BossRematchTome." + nextBoss);
                        }
                        if (currentDownedList[i].type == ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>())
                        {
                            currentDownedList[i].ai[0] = 1;
                        }
                        if (currentDownedList[i].type == ModContent.NPCType<NPCs.Bosses.TheRage>())
                        {
                            currentDownedList[i].ai[0] = currentDownedList[i].lifeMax / 20f;
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

                // Puppet bosses are assembled by PlayerRenderer rather than an ordinary NPC texture.
                // That pipeline is unreliable inside preview UIs (and external NPC browsers), often
                // leaving only their emissive weapon/effects. Use their existing legacy full-body art
                // for a stable Tome portrait while leaving the live encounter renderer untouched.
                if (!DrawPuppetTomePreview(primaryNpcType, currentDownedList[i].Center, currentDownedList[i].scale))
                {
                    Main.instance.DrawNPCDirect(Main.spriteBatch, currentDownedList[i], false, Main.screenPosition);
                }
            }


            Color mouseOverColor = Color.White;
            if (mouseOver != "")
            {
                if (mouseOverEntry != null)
                {
                    mouseOver = mouseOverEntry.FormatTitle(mouseOver);
                    mouseOverColor = mouseOverDefeated ? new Color(90, 220, 105) : mouseOverEntry.TomeColor;
                }
            }

            centerTextUpdate = Main.GameUpdateCount;
            centerNameText = mouseOver;
            centerClueText = mouseOverGuideText;
            centerDeathCountText = DeathCountText;
            centerNameColor = mouseOverColor;
            centerDeathCountY = Math.Min(Main.screenHeight - 105f, bottomSlotScreenY - 120f);

            return false;
        }

        internal static void DrawCenterTextOverlay()
        {
            if (centerTextUpdate != Main.GameUpdateCount)
            {
                return;
            }

            if (centerNameText != "")
            {
                DrawCenteredWrappedText(FontAssets.ItemStack.Value, centerNameText, Main.screenWidth / 2f,
                    Main.screenHeight * 0.34f, centerNameColor, 1.2f, Math.Min(1200f, Main.screenWidth * 0.72f));
            }
            if (centerClueText != "")
            {
                DrawCenteredWrappedText(FontAssets.ItemStack.Value, centerClueText, Main.screenWidth / 2f,
                    Main.screenHeight * 0.43f, Color.White, 1.1f, Math.Min(1200f, Main.screenWidth * 0.72f));
            }
            if (centerDeathCountText != "")
            {
                DrawCenteredWrappedText(FontAssets.MouseText.Value, centerDeathCountText, Main.screenWidth / 2f,
                    centerDeathCountY, Color.White, 1.2f, Main.screenWidth * 0.5f);
            }
        }

        private static float GetPreviewScale(int npcType, int primaryNpcType)
        {
            if (npcType == NPCID.GolemHeadFree) return 1f;
            if (npcType == NPCID.HallowBoss) return 0.72f;
            if (npcType == NPCID.KingSlime) return 0.82f;
            if (npcType == NPCID.QueenSlimeBoss) return 1f;
            if (npcType == NPCID.Deerclops) return 0.75f;
            if (primaryNpcType == ModContent.NPCType<NPCs.Bosses.VesselOfSouls.VesselOfSouls>()) return 0.72f;
            if (primaryNpcType == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Fiends.WaterFiendKraken>()) return 0.65f;
            if (primaryNpcType == ModContent.NPCType<Chaos>()) return 0.65f;
            if (primaryNpcType == ModContent.NPCType<NPCs.Bosses.TheRage>()) return 0.7f;
            return 0.9f;
        }

        private static bool DrawPuppetTomePreview(int primaryNpcType, Vector2 worldCenter, float previewScale)
        {
            string texturePath;
            int frameHeight;
            float portraitScale;

            if (primaryNpcType == ModContent.NPCType<Artorias>())
            {
                texturePath = "tsorcRevamp/NPCs/Bosses/SuperHardMode/Artorias";
                frameHeight = 42;
                portraitScale = 1.5f;
            }
            else if (primaryNpcType == ModContent.NPCType<Gwyn>())
            {
                texturePath = "tsorcRevamp/NPCs/UnusedSprites/OldGwyn";
                frameHeight = 56;
                portraitScale = 1.2f;
            }
            else
            {
                return false;
            }

            Texture2D texture = ModContent.Request<Texture2D>(texturePath,
                ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Rectangle source = new Rectangle(0, 0, texture.Width, frameHeight);
            Main.spriteBatch.Draw(texture, worldCenter - Main.screenPosition, source, Color.White, 0f,
                source.Size() / 2f, previewScale * portraitScale, SpriteEffects.None, 0f);
            return true;
        }

        private static void DrawCenteredWrappedText(DynamicSpriteFont font, string text, float centerX,
            float topY, Color color, float scale, float maxWidth)
        {
            List<string> lines = WrapText(font, text, scale, maxWidth);
            float y = topY;
            foreach (string line in lines)
            {
                Vector2 size = font.MeasureString(line) * scale;
                Vector2 position = new Vector2(centerX - size.X / 2f, y);
                DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, font, line, position + new Vector2(2f),
                    Color.Black * 0.85f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, font, line, position,
                    color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                y += font.LineSpacing * scale;
            }
        }

        private static List<string> WrapText(DynamicSpriteFont font, string text, float scale, float maxWidth)
        {
            List<string> lines = new();
            foreach (string paragraph in text.Replace("\r", "").Split('\n'))
            {
                if (paragraph.Length == 0)
                {
                    lines.Add(string.Empty);
                    continue;
                }

                string currentLine = string.Empty;
                foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    string candidate = currentLine.Length == 0 ? word : currentLine + " " + word;
                    if (currentLine.Length > 0 && font.MeasureString(candidate).X * scale > maxWidth)
                    {
                        lines.Add(currentLine);
                        currentLine = word;
                    }
                    else
                    {
                        currentLine = candidate;
                    }
                }

                if (currentLine.Length > 0)
                {
                    lines.Add(currentLine);
                }
            }
            return lines;
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

            // The Witchking must spawn lower in order to avoid getting stuck in the lava/room above.
            if (id == ModContent.NPCType<Witchking>())
            {
                spawnOffset.X -= 200;
                spawnOffset.Y += 250;
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

        // Scans an era's slots and lowers nextUndefeatedCriticalPath if it finds a still-bunny
        // required-boss slot with a smaller presentation order. Called once per era from AI() init.
        private void FindNextCriticalPath(List<NPC> list, List<int> rarities, List<int> primaryNpcTypes)
        {
            if (list == null || rarities == null || primaryNpcTypes == null) return;
            for (int k = 0; k < list.Count && k < rarities.Count && k < primaryNpcTypes.Count; k++)
            {
                if (list[k].type == NPCID.Bunny
                    && EncounterPresentationRegistry.TryGet(primaryNpcTypes[k], out BossEncounterPresentation entry)
                    && entry.IsRequired
                    && entry.ProgressionOrder < nextUndefeatedCriticalPath)
                {
                    nextUndefeatedCriticalPath = entry.ProgressionOrder;
                }
            }
        }
    }
}
