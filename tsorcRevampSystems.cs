using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Tools;
using tsorcRevamp.NPCs;
using tsorcRevamp.Textures;
using tsorcRevamp.Tiles;
using tsorcRevamp.UI;
using tsorcRevamp.Utilities;

namespace tsorcRevamp
{
    class tsorcRevampSystems : ModSystem
    {
        public static RecipeGroup UpgradedMirrors;
        public static RecipeGroup CobaltHelmets;

        private static bool portableGuideCraftingActive;
        private static Item portableGuideItem = new Item();
        private static int lastPortableGuideItemType;
        private static int lastPortableGuideItemPrefix;
        private static int lastPortableGuideItemStack;
        private static int portableGuideRecipeScroll;
        private static int portableGuideSelectedRecipeIndex;
        private static int portableGuideScrollDelta;
        private static readonly List<int> portableGuideRecipeIndices = new List<int>();
        private const int PortableGuideSlotContext = ItemSlot.Context.BankItem;
        private const int PortableGuideVisibleRecipes = 5;


        static ForceLoadTexture[] mapTextures = new ForceLoadTexture[6] {
            new ForceLoadTexture("tsorcRevamp/UI/Markers/0"),
            new ForceLoadTexture("tsorcRevamp/UI/Markers/1"),
            new ForceLoadTexture("tsorcRevamp/UI/Markers/2"),
            new ForceLoadTexture("tsorcRevamp/UI/Markers/3"),
            new ForceLoadTexture("tsorcRevamp/UI/Markers/4"),
            new ForceLoadTexture("tsorcRevamp/UI/MinimapBonfire"),

        };


        public static ForceLoadTexture fissureTexture = new ForceLoadTexture("tsorcRevamp/UI/MinimapFissure");

        public override void PostDrawFullscreenMap(ref string mouseText)
        {
            foreach (ForceLoadTexture texture in mapTextures)
            {
                texture.KeepLoaded();
            }

            Texture2D BonfireMinimapTexture = mapTextures[5].texture;

            //Step 1: Convert mouse position on the minimap screen to position in-world
            //Also convert these to vectors because it dramatically simplifies calculations. Why aren't they vectors to start with?
            Vector2 scrCenter = new Vector2((Main.screenWidth / 2), (Main.screenHeight / 2));
            Vector2 mouse = new Vector2(Main.mouseX, Main.mouseY);

            mouse -= scrCenter;
            mouse *= Main.UIScale;
            mouse += scrCenter;

            Vector2 mapPos = Main.mapFullscreenPos * Main.mapFullscreenScale;
            Vector2 scrOrigin = scrCenter - mapPos;

            scrOrigin.X += 10 * Main.mapFullscreenScale;
            scrOrigin.Y += 10 * Main.mapFullscreenScale;

            Vector2 mouseTile = (mouse - scrOrigin) / Main.mapFullscreenScale;
            mouseTile.X += 10;
            mouseTile.Y += 10;

            //Step 2: Convert world coordinates to minimap fucko-units for every bonfire as they get drawn
            float mapScale = Main.mapFullscreenScale / Main.UIScale;
            Vector2 scaledMapCoords = Main.mapFullscreenPos * mapScale * -1;
            scaledMapCoords += scrCenter;

            float hoverRange = 32 / Main.mapFullscreenScale;

            foreach (Vector2 bonfirePoint in tsorcRevampWorld.LitBonfireList)
            {
                Vector2 bonfireDrawCoords = bonfirePoint;
                bonfireDrawCoords.X += 1.5f;
                bonfireDrawCoords.Y += 1f;
                bonfireDrawCoords *= mapScale;
                bonfireDrawCoords += scaledMapCoords;

                //Step 3: While drawing check if it's in-range of the cursor, and if so give it a rainbow backdrop
                if ((mouseTile - bonfirePoint).Length() <= hoverRange)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                        Main.spriteBatch.Draw(BonfireMinimapTexture, bonfireDrawCoords + offsetPositon, null, Color.White, 0, BonfireMinimapTexture.Size() / 2, 1.04f, SpriteEffects.None, 1);
                    }
                    Main.spriteBatch.Draw(BonfireMinimapTexture, bonfireDrawCoords, null, Color.White, 0, BonfireMinimapTexture.Size() / 2, 1f, SpriteEffects.None, 1);
                    mouseText = LangUtils.GetTextValue("World.TPToBonfire");

                    //Step 4: Check if they're left-clicking, and close the minimap + teleport them if so
                    if (Main.mouseLeft && Main.mouseLeftRelease && !tsorcRevampWorld.BossAlive)
                    {
                        if (Main.LocalPlayer.HasBuff(ModContent.BuffType<InCombat>()))
                        {
                            if (Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().TextCooldown >= 0)
                            {
                                Main.NewText(LangUtils.GetTextValue("World.NoTPCombat"));
                            }
                        }
                        else
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, Main.LocalPlayer.position);
                            UsefulFunctions.SafeTeleport(Main.LocalPlayer, new Vector2(bonfirePoint.X, bonfirePoint.Y - 1) * 16);
                            Main.mapFullscreen = false;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, bonfirePoint * 16);
                            if (Main.netMode == NetmodeID.MultiplayerClient)
                            {
                                Main.LocalPlayer.AddBuff(ModContent.BuffType<Buffs.Loading>(), 15);
                            }
                        }
                    }

                    if (tsorcRevampWorld.BossAlive)
                    {
                        mouseText = LangUtils.GetTextValue("World.NoTPBoss");
                    }
                    else if (Main.LocalPlayer.HasBuff(ModContent.BuffType<InCombat>()))
                    {
                        mouseText = LangUtils.GetTextValue("World.NoTPCombat");
                    }
                }
                else
                {
                    Main.spriteBatch.Draw(BonfireMinimapTexture, bonfireDrawCoords, null, Color.White, 0, BonfireMinimapTexture.Size() / 2, 0.85f, SpriteEffects.None, 1);
                }
            }
            MapMarkersUIState.Visible = true;

            if (tsorcRevampWorld.SuperHardMode)
            {
                Vector2 abyssFissureCoords = tsorcRevampWorld.AbyssPortalLocation / 16;
                abyssFissureCoords.X += 1.5f;
                abyssFissureCoords.Y += 1f;
                abyssFissureCoords *= mapScale;
                abyssFissureCoords += scaledMapCoords;
                fissureTexture.KeepLoaded();
                Texture2D minimapFissureTexture = fissureTexture.texture;
                Main.spriteBatch.Draw(minimapFissureTexture, abyssFissureCoords, null, Color.White, 0, minimapFissureTexture.Size() / 2, 1, SpriteEffects.None, 1);
                if ((mouseTile - tsorcRevampWorld.AbyssPortalLocation / 16).Length() <= hoverRange)
                {
                    mouseText = LangUtils.GetTextValue("World.AbyssalFissure");
                }
            }


            foreach (KeyValuePair<Vector2, int> marker in tsorcRevampWorld.MapMarkers)
            {
                Vector2 markerDrawCoords = marker.Key;
                markerDrawCoords.X += 1.5f;
                markerDrawCoords.Y += 1f;
                markerDrawCoords *= mapScale;
                markerDrawCoords += scaledMapCoords;
                Texture2D markerTexture = mapTextures[marker.Value].texture;
                Main.spriteBatch.Draw(markerTexture, markerDrawCoords, null, Color.White, 0, markerTexture.Size() / 2, 0.85f, SpriteEffects.None, 1);

                mouseTile = new Vector2((float)Math.Floor(mouseTile.X), (float)Math.Floor(mouseTile.Y));

                if (tsorcRevamp.MarkerSelected == 4 && (mouseTile - marker.Key).Length() < hoverRange && Main.mouseLeft)
                { //delete mode
                    tsorcRevampWorld.MapMarkers.Remove(marker.Key);
                }
            }

            if (!MapMarkersUIState.Switching && tsorcRevamp.MarkerSelected > -1 && tsorcRevamp.MarkerSelected != 4 && Main.mouseLeft && !tsorcRevampWorld.MapMarkers.ContainsKey(mouseTile))
            {
                tsorcRevampWorld.MapMarkers.Add(mouseTile, tsorcRevamp.MarkerSelected);
                tsorcRevamp.MarkerSelected = -1;
            }

            else if (MapMarkersUIState.HoveringOver > -1)
            {
                string hoverText = LangUtils.GetTextValue("UI.SelectMarker");


                if (MapMarkersUIState.HoveringOver == MapMarkersUIState.REMOVE_ID)
                {
                    hoverText = LangUtils.GetTextValue("UI.EraseMarkers");
                }

                if (tsorcRevamp.MarkerSelected == MapMarkersUIState.HoveringOver)
                {
                    hoverText = LangUtils.GetTextValue("UI.StopEditMarkers");
                }

                mouseText = hoverText;

            }

            if (tsorcRevamp.MarkerSelected > -1)
            {
                Main.spriteBatch.Draw(mapTextures[tsorcRevamp.MarkerSelected].texture, new Vector2(Main.MouseScreen.X - 24, Main.MouseScreen.Y + 24), Color.White);
            }
            ModContent.GetInstance<tsorcRevamp>().MarkerInterface.Draw(Main.spriteBatch, new GameTime());

            // Draw discovered location names on the fullscreen map.
            // Fade out when zoomed below 0.5 so labels don't overlap at low zoom levels.
            float labelAlpha = MathHelper.Clamp((Main.mapFullscreenScale - 0.3f) / 0.2f, 0f, 1f);
            if (labelAlpha > 0f)
            {
                DynamicSpriteFont labelFont = FontAssets.ItemStack.Value;
                float labelScale = 0.75f;
                foreach (KeyValuePair<Vector2, string> loc in tsorcRevampWorld.DiscoveredLocations)
                {
                    Vector2 drawCoords = loc.Key;
                    drawCoords.X += 1.5f;
                    drawCoords.Y += 1f;
                    drawCoords *= mapScale;
                    drawCoords += scaledMapCoords;

                    Vector2 labelSize = labelFont.MeasureString(loc.Value) * labelScale;
                    Vector2 labelPos = drawCoords - new Vector2(labelSize.X / 2f, labelSize.Y + 2f);
                    DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, labelFont, loc.Value, labelPos + Vector2.One, Color.Black * 0.7f * labelAlpha, 0f, Vector2.Zero, labelScale, SpriteEffects.None, 0f);
                    DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, labelFont, loc.Value, labelPos, Color.White * 0.9f * labelAlpha, 0f, Vector2.Zero, labelScale, SpriteEffects.None, 0f);
                }
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            layers.Insert(0, new LegacyGameInterfaceLayer(
                "tsorcRevamp: Death Fade Overlay",
                delegate
                {
                    if (deathFadeAlpha > 0f)
                    {
                        Texture2D pixel = TextureAssets.MagicPixel.Value;
                        Main.spriteBatch.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black * deathFadeAlpha);
                    }
                    return true;
                },
                InterfaceScaleType.UI)
            );

            tsorcRevamp mod = ModContent.GetInstance<tsorcRevamp>();
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex != -1)
            {
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "tsorcRevamp: BonfireUI",
                    delegate
                    {
                        if (BonfireUIState.Visible)
                        {

                            mod._bonfireUIState.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }

            int resourceBarIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Resource Bars"));
            if (resourceBarIndex != -1)
            {
                layers.Insert(resourceBarIndex, new LegacyGameInterfaceLayer(
                    "tsorcRevamp: Dark Soul Counter UI",
                    delegate
                    {
                        mod._darkSoulCounterUIState.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }

            int inventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
            if (inventoryIndex != -1)
            {
                layers.Insert(inventoryIndex, new LegacyGameInterfaceLayer(
                    "tsorcRevamp: Portable Guide Scroll Swallower",
                    delegate
                    {
                        SwallowPortableGuideScroll();
                        return true;
                    },
                    InterfaceScaleType.UI)
                );

                layers.Insert(inventoryIndex + 2, new LegacyGameInterfaceLayer(
                    "tsorcRevamp: Portable Guide Crafting Slot",
                    delegate
                    {
                        DrawPortableGuideCraftingSlot(Main.spriteBatch);
                        return true;
                    },
                    InterfaceScaleType.UI)
                );

                layers.Insert(inventoryIndex, new LegacyGameInterfaceLayer(
                    "tsorcRevamp: Emerald Herald UI",
                    delegate
                    {
                        mod.EmeraldHeraldUserInterface.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }

            int resourceBarIndex2 = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Resource Bars"));
            if (resourceBarIndex2 != -1)
            {
                layers.Insert(resourceBarIndex2, new LegacyGameInterfaceLayer(
                    "tsorcRevamp: Estus Flask UI",
                    delegate
                    {
                        mod._estusFlaskUIState.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
            int resourceBarIndex3 = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Resource Bars"));
            if (resourceBarIndex3 != -1)
            {
                layers.Insert(resourceBarIndex3, new LegacyGameInterfaceLayer(
                    "tsorcRevamp: Cerulean Flask UI",
                    delegate
                    {
                        mod._ceruleanFlaskUIState.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }

            int potionBagIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (potionBagIndex != -1)
            {
                layers.Insert(potionBagIndex, new LegacyGameInterfaceLayer(
                    "tsorcRevamp: Potion Bag UI",
                    delegate
                    {
                        if (PotionBagUIState.Visible)
                        {
                            mod.PotionBagUserInterface.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }

        public override void AddRecipeGroups()
        {
            UpgradedMirrors = new RecipeGroup(() => "Upgraded Mirrors",
                ModContent.ItemType<GreatMagicMirror>(),
                ModContent.ItemType<VillageMirror>()
            );
            CobaltHelmets = new RecipeGroup(() => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.CobaltHelmet)}",
                ItemID.CobaltHelmet, ItemID.CobaltHat, ItemID.CobaltMask);

            RecipeGroup.RegisterGroup("tsorcRevamp:CobaltHelmet", CobaltHelmets);
        }

        public override void UpdateUI(GameTime gameTime)
        {
            tsorcRevamp mod = ModContent.GetInstance<tsorcRevamp>();
            if (BonfireUIState.Visible)
            {
                mod._bonfireUIState?.Update(gameTime);
            }
            if (DarkSoulCounterUIState.Visible)
            {
                mod._darkSoulCounterUIState?.Update(gameTime);
            }

            mod.EmeraldHeraldUserInterface?.Update(gameTime);

            if (EstusFlaskUIState.Visible)
            {
                mod._estusFlaskUIState?.Update(gameTime);
            }
            if (CeruleanFlaskUIState.Visible)
            {
                mod._ceruleanFlaskUIState?.Update(gameTime);
            }

            if (PotionBagUIState.Visible)
            {
                mod.PotionBagUserInterface.Update(gameTime);
            }

            if (MapMarkersUIState.Visible) mod.MarkerInterface.Update(gameTime);

            UpdatePortableGuideCrafting();

            if (Main.player[Main.myPlayer].active && Main.player[Main.myPlayer].dead)
            {
                // Fade to black over 8 seconds (480 ticks)
                deathFadeAlpha = MathHelper.Clamp(deathFadeAlpha + 1f / 480f, 0f, 1f);
            }
            else
            {
                deathFadeAlpha = 0f;
            }
        }

        public static Vector2 OurDrawBorderString(SpriteBatch sb, string text, DynamicSpriteFont font, Vector2 pos, Color color, float scale = 1f, float anchorx = 0f, float anchory = 0f, int maxCharactersDisplayed = -1)
        {
            if (maxCharactersDisplayed != -1 && text.Length > maxCharactersDisplayed)
                text.Substring(0, maxCharactersDisplayed);

            Vector2 vector = font.MeasureString(text);
            Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow(sb, font, text, pos, color, 0f, new Vector2(anchorx, anchory) * vector, new Vector2(scale), -1f, 1.5f);
            return vector * scale;
        }

        public static float deathFadeAlpha = 0f;

        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            tsorcRevampPlayer modPlayer = Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.Draw(spriteBatch);
            if (tsorcRevamp.NearbySoapstone != null)
            {
                SoapstoneTileEntity soapstone = tsorcRevamp.NearbySoapstone;
                float scaleMod = (float)((ModContent.GetInstance<tsorcRevampConfig>().SoapstoneScale / 100f) + 1) / Main.GameViewMatrix.Zoom.X;
                //different Font because MouseText don't perform well in chinese language
                DynamicSpriteFont font = FontAssets.MouseText.Value;
                if (Language.ActiveCulture.Name == "zh-Hans")
                {
                    font = FontAssets.ItemStack.Value;
                }

                if (soapstone.timer > 0 && !soapstone.hidden)
                {
                    float textWidth = soapstone.textWidth > 0 ? soapstone.textWidth : SoapstoneMessage.DEFAULT_WIDTH;
                    textWidth *= scaleMod;

                    //Wrap when find blank between words, but chinese language don't have " ", so manually edit all the textWidth in Soapstones_zh-Hans.json
                    string text = UsefulFunctions.WrapString(soapstone.text, font, textWidth, scaleMod);
                    textWidth += font.MeasureString(" ").X * scaleMod;
                    float alpha = (soapstone.timer / 20f);
                    if (soapstone.timer >= 20)
                    {
                        alpha = 1;
                    }
                    //Main.NewText("Alpha: " + alpha + " timer: " + soapstone.timer);
                    Vector2 textPosition = (new Vector2(soapstone.Position.X, soapstone.Position.Y) * 16f - Main.screenPosition) - new Vector2((textWidth / 2) - 4, 64);
                    Vector2 textPositionWorld = new Vector2(soapstone.Position.X, soapstone.Position.Y) * 16f - new Vector2((textWidth / 2) - 4, 64);

                    //right padding
                    textWidth += font.MeasureString(" ").X * scaleMod;

                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix); //allows it to have alpha

                    Texture2D boxTexture = ModContent.Request<Texture2D>("tsorcRevamp/UI/blackpixel", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

                    int lineCount = text.Count(a => a == '\n') + 1;
                    float height = scaleMod * (font.LineSpacing * lineCount) + 8;
                    Rectangle drect = new((int)textPosition.X - 4, (int)textPosition.Y - 4, (int)textWidth + 8, (int)height);
                    Rectangle drectWorld = new((int)textPositionWorld.X - 4, (int)textPositionWorld.Y - 4, (int)textWidth + 8, (int)height);

                    Color bgColor = new(0, 0, 0, (0.5f * alpha) + 0.25f);
                    Main.spriteBatch.Draw(boxTexture, drect, bgColor);

                    //DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, FontAssets.ItemStack.Value, text, textPosition, Color.White, 0, Vector2.Zero, scaleMod, SpriteEffects.None, 0);
                    OurDrawBorderString(Main.spriteBatch, text, font, textPosition, Color.White, scaleMod, 0, 0, -1);
                    // Fall through so DrawLocationBanner still runs.
                }
                else if (soapstone.nearPlayer)
                {
                    // Show the "Show Hint / Show Story" click-to-open button.
                    string showButtonText = SoapstoneTileEntity.BuildShowButtonText(soapstone);
                    Vector2 textSize = FontAssets.ItemStack.Value.MeasureString(showButtonText) * scaleMod;
                    Vector2 textPosition = (new Vector2(soapstone.Position.X, soapstone.Position.Y) * 16f - Main.screenPosition - new Vector2((textSize.X / 2) - 16, 20));
                    Vector2 textPositionWorld = new Vector2(soapstone.Position.X, soapstone.Position.Y) * 16f - new Vector2((textSize.X / 2) - 16, 20);

                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix); //allows it to have alpha

                    Texture2D boxTexture = ModContent.Request<Texture2D>("tsorcRevamp/UI/blackpixel", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

                    Rectangle drect = new((int)textPosition.X - 4, (int)textPosition.Y - 4, (int)textSize.X, (int)textSize.Y);
                    Rectangle drectWorld = new((int)textPositionWorld.X - 4, (int)textPositionWorld.Y - 4, (int)textSize.X, (int)textSize.Y);
                    //Main.ViewPosition
                    Matrix matrix = Matrix.Invert(Main.GameViewMatrix.ZoomMatrix);
                    Vector2 transformedPosition = Vector2.Transform(Main.screenPosition, matrix);
                    Vector2 transformedMouse = Vector2.Transform(Main.MouseScreen, matrix);

                    Main.spriteBatch.Draw(boxTexture, drect, new(0, 0, 0, 113));

                    DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, FontAssets.ItemStack.Value, showButtonText, textPosition, new(255, 255, 255, 170), 0, Vector2.Zero, scaleMod, SpriteEffects.None, 0);

                    UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);

                    if (drectWorld.Contains(tsorcRevampPlayer.RealMouseWorld.ToPoint()))
                    {
                        Main.LocalPlayer.mouseInterface = true;
                        if (Main.mouseLeft && Main.mouseLeftRelease)
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                            soapstone.timer = 25;
                            soapstone.hidden = false;
                            // Sticky open: bubble stays visible until the player moves the mouse
                            // far from this click point.  SoapstoneTile.PostDraw holds timer at
                            // 25 unconditionally while manuallyOpened is true.
                            soapstone.manuallyOpened = true;
                            soapstone.clickedAtMouse = Main.MouseScreen;
                            // Read state should only flip on actual viewing (i.e. now), not on
                            // mere proximity when AutoOpen is off.
                            soapstone.read = true;
                        }
                    }
                }
            }

            DrawLocationBanner(spriteBatch);

            // ── FighterAI navigation debug overlay ────────────────────────────────
            if (ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
            {
                DrawNavDebug(spriteBatch);
            }
        }

        /// <summary>
        /// Draws a small navigation-state readout in the lower-left corner for the nearest
        /// enemy NPC that uses FighterAI (NavigationTier >= 1).  Only rendered when DebugMode
        /// is enabled in the mod config.
        /// </summary>
        private static void DrawNavDebug(SpriteBatch spriteBatch)
        {
            // Find the nearest active enemy with navigation intelligence.
            NPC target = null;
            float closestDistSq = float.MaxValue;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || npc.friendly || npc.lifeMax <= 5) continue;
                tsorcRevampGlobalNPC g = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
                if (g.NavigationTier < 1) continue;
                float dSq = Vector2.DistanceSquared(npc.Center, Main.LocalPlayer.Center);
                if (dSq < closestDistSq) { closestDistSq = dSq; target = npc; }
            }
            if (target == null) return;

            tsorcRevampGlobalNPC gNpc   = target.GetGlobalNPC<tsorcRevampGlobalNPC>();
            Player               tPlayer = Main.player[target.target];
            bool   los    = tPlayer.CanHit(target);
            float  yDiff  = tPlayer.Center.Y - target.Center.Y;   // negative = player above
            float  dist   = target.Distance(tPlayer.Center);
            string wpStr  = gNpc.WaypointTimer > 0
                ? $"{gNpc.WaypointAction} ({gNpc.WaypointTarget.X / 16f:F1},{gNpc.WaypointTarget.Y / 16f:F1}) T:{gNpc.WaypointTimer}"
                : "none";

            string[] lines =
            {
                $"[NAV] {target.TypeName}  dist:{dist:F0}px",
                $"NavTier:{gNpc.NavigationTier}  LOS:{los}  YDiff:{yDiff:F0}px",
                $"Bored:{gNpc.BoredTimer}  Stuck:{gNpc.StuckTimer}  HaltAtLedge:{gNpc.HaltAtLedge}",
                $"Waypoint: {wpStr}",
                $"Intent:{gNpc.LastNavIntent}  Result:{gNpc.LastWaypointResult}",
                $"CD:{gNpc.WaypointSearchCooldown}  Fail:{gNpc.WaypointSearchFailures}  JumpCD:{gNpc.NavJumpCooldown}",
                $"RunUp:{gNpc.LedgeRunUpTimer}  Vault:{gNpc.LedgeVaultTimer}  StopFire:{gNpc.CanStopToFire}",
            };

            DynamicSpriteFont font  = FontAssets.MouseText.Value;
            float             lineH = 18f;
            float             startY = Main.screenHeight - (lines.Length * lineH) - 10f;
            for (int i = 0; i < lines.Length; i++)
            {
                Utils.DrawBorderStringFourWay(spriteBatch, font, lines[i],
                    10f, startY + i * lineH,
                    Color.White, Color.Black, Vector2.Zero, 0.85f);
            }
        }

        // White all-caps location announcement. Centered horizontally, top quarter vertically.
        // Drawn entirely client-side; timer ticks here once per frame.
        private void DrawLocationBanner(SpriteBatch spriteBatch)
        {
            if (tsorcRevamp.LocationBannerTimer <= 0 || string.IsNullOrEmpty(tsorcRevamp.LocationBannerText))
                return;

            // The soapstone draw path may leave the spriteBatch in GameViewMatrix state.
            // Restart in UIScaleMatrix so screen-space centering works at any UIScale.
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);

            int timer = tsorcRevamp.LocationBannerTimer;
            int total = tsorcRevamp.LOCATION_BANNER_TOTAL;
            int fadeIn = tsorcRevamp.LOCATION_BANNER_FADE_IN;
            int fadeOut = tsorcRevamp.LOCATION_BANNER_FADE_OUT;

            int elapsed = total - timer;
            float alpha;
            if (elapsed < fadeIn)
                alpha = elapsed / (float)fadeIn;
            else if (timer < fadeOut)
                alpha = timer / (float)fadeOut;
            else
                alpha = 1f;
            if (alpha < 0f) alpha = 0f;
            if (alpha > 1f) alpha = 1f;

            DynamicSpriteFont font = FontAssets.DeathText.Value;
            float scale = 0.85f;
            string text = tsorcRevamp.LocationBannerText;
            // Use the measured center as the draw origin so any invisible font padding cancels out.
            // Position is the screen center point where the origin is placed.
            Vector2 origin = font.MeasureString(text) / 2f;
            Vector2 center = new(Main.screenWidth / 2f, Main.screenHeight / 5f);

            DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, font, text, center + new Vector2(2, 2), Color.Black * alpha, 0f, origin, scale, SpriteEffects.None, 0f);
            DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, font, text, center, Color.White * alpha, 0f, origin, scale, SpriteEffects.None, 0f);

            tsorcRevamp.LocationBannerTimer--;
            if (tsorcRevamp.LocationBannerTimer <= 0)
                tsorcRevamp.LocationBannerText = null;
        }

        private static void UpdatePortableGuideCrafting()
        {
            if (!ShouldShowPortableGuideCraftingSlot())
            {
                if (portableGuideCraftingActive)
                {
                    ReturnPortableGuideItemToPlayer();
                    portableGuideCraftingActive = false;
                }
                return;
            }

            portableGuideCraftingActive = true;

            if (PortableGuideItemChanged())
            {
                RebuildPortableGuideRecipes();
                StorePortableGuideItemState();
                portableGuideRecipeScroll = 0;
                portableGuideSelectedRecipeIndex = 0;
            }
        }

        private static bool ShouldShowPortableGuideCraftingSlot()
        {
            return Main.playerInventory
                && string.IsNullOrEmpty(Main.npcChatText)
                && !Main.InReforgeMenu;
        }

        private static void StorePortableGuideItemState()
        {
            lastPortableGuideItemType = portableGuideItem.type;
            lastPortableGuideItemPrefix = portableGuideItem.prefix;
            lastPortableGuideItemStack = portableGuideItem.stack;
        }

        private static bool PortableGuideItemChanged()
        {
            return portableGuideItem.type != lastPortableGuideItemType
                || portableGuideItem.prefix != lastPortableGuideItemPrefix
                || portableGuideItem.stack != lastPortableGuideItemStack;
        }

        private static void ReturnPortableGuideItemToPlayer()
        {
            if (portableGuideItem == null || portableGuideItem.IsAir)
                return;

            Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_Misc("PortableGuide"), portableGuideItem.type, portableGuideItem.stack);
            portableGuideItem.TurnToAir();
            StorePortableGuideItemState();
        }

        private static void RebuildPortableGuideRecipes()
        {
            portableGuideRecipeIndices.Clear();
            portableGuideSelectedRecipeIndex = 0;
            if (portableGuideItem == null || portableGuideItem.IsAir)
            {
                return;
            }

            for (int i = 0; i < Recipe.maxRecipes; i++)
            {
                Recipe recipe = Main.recipe[i];
                if (recipe == null || recipe.Disabled || recipe.createItem.IsAir)
                {
                    continue;
                }

                if (recipe.HasIngredient(portableGuideItem.type))
                {
                    portableGuideRecipeIndices.Add(i);
                }
            }
        }

        private static void DrawPortableGuideCraftingSlot(SpriteBatch spriteBatch)
        {
            if (!ShouldShowPortableGuideCraftingSlot())
            {
                return;
            }

            float oldScale = Main.inventoryScale;
            Main.inventoryScale = 0.85f;

            try
            {
                int slotIndexX = 13;
                int slotIndexY = 0;
                int slotPosX = (int)(20f + (float)(slotIndexX * 56) * Main.inventoryScale);
                int slotPosY = (int)(20f + (float)(slotIndexY * 56) * Main.inventoryScale) + 18;
                Vector2 slotPosition = new Vector2(slotPosX, slotPosY);
                Rectangle slotRectangle = new Rectangle(slotPosX, slotPosY, (int)(52 * Main.inventoryScale), (int)(52 * Main.inventoryScale));

                // Draw the "Guide" text label directly above the slot
                DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, FontAssets.MouseText.Value, Lang.GetNPCNameValue(NPCID.Guide), new Vector2(slotPosX + 5f, slotPosY - 15), new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor), 0f, default, 0.75f, SpriteEffects.None, 0);

                if (slotRectangle.Contains(Main.MouseScreen.ToPoint()) && !PlayerInput.IgnoreMouseInterface)
                {
                    Main.LocalPlayer.mouseInterface = true;
                    ItemSlot.Handle(ref portableGuideItem, PortableGuideSlotContext);
                }

                ItemSlot.Draw(spriteBatch, ref portableGuideItem, PortableGuideSlotContext, slotPosition);

                if (PortableGuideItemChanged())
                {
                    RebuildPortableGuideRecipes();
                    StorePortableGuideItemState();
                    portableGuideRecipeScroll = 0;
                    portableGuideSelectedRecipeIndex = 0;
                }

                // Align the first recipe icon's top with the 2nd slot beside it. Both share slotPosY (38) and
                // inventoryScale (0.85). The 2nd slot top = 38 + (int)(56*0.85) = 85; the recipe top =
                // 38 + (int)(52*0.85) + offset = 82 + offset. So offset = 3 puts them on the same line.
                DrawPortableGuideRecipeResults(spriteBatch, new Vector2(slotPosX, slotPosY + (int)(52 * Main.inventoryScale) + 3), slotPosition);
            }
            finally
            {
                Main.inventoryScale = oldScale;
            }
        }

        private static void DrawPortableGuideRecipeResults(SpriteBatch spriteBatch, Vector2 position, Vector2 slotPosition)
        {
            if (portableGuideItem == null || portableGuideItem.IsAir)
            {
                return;
            }

            // The recipe count is now folded into the "Showing N recipes that use X" line beside the Guide slot
            // (drawn below), so there's no separate count text under the slot anymore — that lets the first recipe
            // icon sit higher, in line with the 2nd slot.
            if (portableGuideRecipeIndices.Count == 0)
            {
                int noneX = (int)(slotPosition.X + (52 * Main.inventoryScale) + 15);
                int noneY = (int)slotPosition.Y;
                string none = $"Showing 0 recipes that use {portableGuideItem.Name}";
                DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, FontAssets.MouseText.Value, none, new Vector2(noneX, noneY), new Color(200, 200, 200, 255), 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
                return;
            }

            const int rowHeight = 48;
            int visibleRows = Math.Min(PortableGuideVisibleRecipes, portableGuideRecipeIndices.Count);

            // Bounding box for scroll interaction (covers Guide slot and the recipe icons column below)
            int totalScrollHeight = 58 + 20 + visibleRows * rowHeight;
            Rectangle scrollArea = new Rectangle((int)position.X - 50, (int)position.Y - 58, 150, totalScrollHeight);

            // Clamp selection index within bounds
            if (portableGuideSelectedRecipeIndex < 0)
            {
                portableGuideSelectedRecipeIndex = 0;
            }
            if (portableGuideSelectedRecipeIndex >= portableGuideRecipeIndices.Count)
            {
                portableGuideSelectedRecipeIndex = Math.Max(0, portableGuideRecipeIndices.Count - 1);
            }

            // Selection controlled by scroll wheel
            if (scrollArea.Contains(Main.MouseScreen.ToPoint()) && !PlayerInput.IgnoreMouseInterface)
            {
                Main.LocalPlayer.mouseInterface = true;
                int scrollDelta = portableGuideScrollDelta != 0 ? portableGuideScrollDelta : PlayerInput.ScrollWheelDelta;
                if (scrollDelta != 0)
                {
                    int scrollDirection = -Math.Sign(scrollDelta); // -1 for scroll up, +1 for scroll down
                    if (portableGuideRecipeIndices.Count > 0)
                    {
                        portableGuideSelectedRecipeIndex += scrollDirection;
                        if (portableGuideSelectedRecipeIndex < 0)
                        {
                            portableGuideSelectedRecipeIndex = 0;
                        }
                        if (portableGuideSelectedRecipeIndex >= portableGuideRecipeIndices.Count)
                        {
                            portableGuideSelectedRecipeIndex = portableGuideRecipeIndices.Count - 1;
                        }

                        // Auto-scroll viewport to keep selection in view
                        if (portableGuideSelectedRecipeIndex < portableGuideRecipeScroll)
                        {
                            portableGuideRecipeScroll = portableGuideSelectedRecipeIndex;
                        }
                        else if (portableGuideSelectedRecipeIndex >= portableGuideRecipeScroll + PortableGuideVisibleRecipes)
                        {
                            portableGuideRecipeScroll = portableGuideSelectedRecipeIndex - PortableGuideVisibleRecipes + 1;
                        }
                    }
                    PlayerInput.ScrollWheelDelta = 0; // swallow
                    portableGuideScrollDelta = 0; // consume
                }
            }

            int startRecipe = Math.Min(portableGuideRecipeScroll, Math.Max(0, portableGuideRecipeIndices.Count - PortableGuideVisibleRecipes));
            int endRecipe = Math.Min(portableGuideRecipeIndices.Count, startRecipe + PortableGuideVisibleRecipes);

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 selectedRowPosition = Vector2.Zero;
            bool foundSelectedRow = false;

            for (int i = startRecipe; i < endRecipe; i++)
            {
                Recipe recipe = Main.recipe[portableGuideRecipeIndices[i]];
                Item resultItem = recipe.createItem;

                int drawRow = i - startRecipe;
                // No header above anymore, so the first icon starts at the top of the recipe column (in line with the 2nd slot).
                Vector2 rowPosition = new Vector2(position.X, position.Y + drawRow * rowHeight);
                Rectangle slotRect = new Rectangle((int)rowPosition.X, (int)rowPosition.Y, 44, 44);

                bool isHovered = slotRect.Contains(Main.MouseScreen.ToPoint()) && !PlayerInput.IgnoreMouseInterface;
                bool isSelected = (i == portableGuideSelectedRecipeIndex);

                if (isHovered)
                {
                    Main.LocalPlayer.mouseInterface = true;
                }

                if (isSelected)
                {
                    selectedRowPosition = rowPosition;
                    foundSelectedRow = true;

                    // Yellow highlight using the background asset swap trick
                    ReLogic.Content.Asset<Texture2D> originalBack = TextureAssets.InventoryBack4;
                    TextureAssets.InventoryBack4 = TextureAssets.InventoryBack14;

                    // Draw recipe slot with yellow background
                    ItemSlot.Draw(spriteBatch, ref resultItem, ItemSlot.Context.CraftingMaterial, rowPosition);

                    // Restore original background
                    TextureAssets.InventoryBack4 = originalBack;
                }
                else
                {
                    // Draw standard recipe slot
                    ItemSlot.Draw(spriteBatch, ref resultItem, ItemSlot.Context.CraftingMaterial, rowPosition);
                }

                // If hovered, call MouseHover AFTER ItemSlot.Draw to cleanly show the tooltip
                if (isHovered)
                {
                    ItemSlot.MouseHover(ref resultItem, ItemSlot.Context.CraftingMaterial);
                }

                // Click selection
                if (isHovered && Main.mouseLeft && Main.mouseLeftRelease)
                {
                    portableGuideSelectedRecipeIndex = i;
                    Main.mouseLeftRelease = false;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                }

                // Darken the top or bottom slot if there are more items to scroll to
                bool isBottomRow = (i == endRecipe - 1);
                bool hasMoreRecipesBelow = (startRecipe + PortableGuideVisibleRecipes < portableGuideRecipeIndices.Count);
                bool isTopRow = (i == startRecipe);
                bool hasMoreRecipesAbove = (startRecipe > 0);

                if (!isSelected && ((isBottomRow && hasMoreRecipesBelow) || (isTopRow && hasMoreRecipesAbove)))
                {
                    // Draw a dark semi-transparent overlay to shade the slot and item
                    spriteBatch.Draw(pixel, slotRect, new Color(0, 0, 0, 110));
                }
            }

            // Draw details panel for the selected recipe (to the right of the Guide slot - fixed position next to it)
            if (portableGuideSelectedRecipeIndex >= 0 && portableGuideSelectedRecipeIndex < portableGuideRecipeIndices.Count)
            {
                Recipe selectedRecipe = Main.recipe[portableGuideRecipeIndices[portableGuideSelectedRecipeIndex]];

                // Align details to the right of the Guide slot
                int detailsX = (int)(slotPosition.X + (52 * Main.inventoryScale) + 15);
                int detailsY = (int)slotPosition.Y;

                // 1. First line: "Showing N recipes that use [Guide Item Name]" (count folded in here)
                int recipeCount = portableGuideRecipeIndices.Count;
                string text1 = recipeCount == 1
                    ? $"Showing 1 recipe that uses {portableGuideItem.Name}"
                    : $"Showing {recipeCount} recipes that use {portableGuideItem.Name}";
                DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, FontAssets.MouseText.Value, text1, new Vector2(detailsX, detailsY), new Color(200, 200, 200, 255), 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);

                // 2. Second line: "Required objects: [Station Name]"
                List<string> reqs = new List<string>();
                foreach (int tileID in selectedRecipe.requiredTile)
                {
                    reqs.Add(GetRequiredTileName(tileID));
                }
                foreach (Condition condition in selectedRecipe.Conditions)
                {
                    reqs.Add(condition.Description.Value);
                }

                string stationText = reqs.Count > 0 ? string.Join(", ", reqs) : "By Hand";
                string text2 = $"Required objects: {stationText}";
                DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, FontAssets.MouseText.Value, text2, new Vector2(detailsX, detailsY + 14), new Color(180, 180, 180, 255), 0f, Vector2.Zero, 0.70f, SpriteEffects.None, 0f);

                // 3. Horizontal list of ingredients - drawn next to the selected recipe row
                if (foundSelectedRow)
                {
                    int ingY = (int)(selectedRowPosition.Y + 6);
                    float currentScale = Main.inventoryScale;

                    int ingIndex = 0;
                    for (int j = 0; j < selectedRecipe.requiredItem.Count; j++)
                    {
                        Item ingredientItem = selectedRecipe.requiredItem[j];
                        if (ingredientItem.type <= 0) continue;

                        int ingX = (int)(selectedRowPosition.X + 54 + ingIndex * (38 * currentScale));
                        Rectangle ingRect = new Rectangle(ingX, ingY, (int)(44 * 0.75f), (int)(44 * 0.75f));

                        bool isIngHovered = ingRect.Contains(Main.MouseScreen.ToPoint()) && !PlayerInput.IgnoreMouseInterface;

                        if (isIngHovered)
                        {
                            Main.LocalPlayer.mouseInterface = true;
                        }

                        // Draw the small ingredient slot
                        Main.inventoryScale = currentScale * 0.75f;
                        ItemSlot.Draw(spriteBatch, ref ingredientItem, ItemSlot.Context.CraftingMaterial, new Vector2(ingX, ingY));
                        Main.inventoryScale = currentScale;

                        if (isIngHovered)
                        {
                            ItemSlot.MouseHover(ref ingredientItem, ItemSlot.Context.CraftingMaterial);
                        }

                        ingIndex++;
                    }
                }
            }
        }

        private static string GetRequiredTileName(int tileID)
        {
            if (tileID == TileID.WorkBenches) return "Work Bench";
            if (tileID == TileID.Anvils) return "Iron Anvil";
            if (tileID == TileID.Furnaces) return "Furnace";
            if (tileID == TileID.DemonAltar) return "Demon Altar";
            if (tileID == TileID.MythrilAnvil) return "Mythril Anvil";
            if (tileID == TileID.AdamantiteForge) return "Adamantite Forge";
            if (tileID == TileID.TinkerersWorkbench) return "Tinkerer's Workshop";
            if (tileID == TileID.ImbuingStation) return "Imbuing Station";
            if (tileID == TileID.DyeVat) return "Dye Vat";
            if (tileID == TileID.Loom) return "Loom";
            if (tileID == TileID.Sawmill) return "Sawmill";
            if (tileID == TileID.CrystalBall) return "Crystal Ball";
            if (tileID == TileID.Autohammer) return "Autohammer";

            try
            {
                int lookup = Terraria.Map.MapHelper.TileToLookup(tileID, 0);
                string name = Lang.GetMapObjectName(lookup);
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }
            catch {}

            return "By Hand";
        }

        private static void SwallowPortableGuideScroll()
        {
            portableGuideScrollDelta = 0;
            if (!ShouldShowPortableGuideCraftingSlot() || portableGuideItem == null || portableGuideItem.IsAir)
            {
                return;
            }

            int slotIndexX = 13;
            int slotPosX = (int)(20f + (float)(slotIndexX * 56) * 0.85f);
            int slotPosY = (int)(20f + (float)(0 * 56) * 0.85f) + 18;

            Rectangle activeArea = new Rectangle(slotPosX - 50, slotPosY - 25, 420, 380);

            if (activeArea.Contains(Main.MouseScreen.ToPoint()) && !PlayerInput.IgnoreMouseInterface)
            {
                Main.LocalPlayer.mouseInterface = true;
                portableGuideScrollDelta = PlayerInput.ScrollWheelDelta;
                PlayerInput.ScrollWheelDelta = 0;
            }
        }

        public override void Unload()
        {
            UpgradedMirrors = null;
            CobaltHelmets = null;
        }

        public override void PreSaveAndQuit()
        {
            TextureAssets.Sun = ModContent.Request<Texture2D>("Terraria/Images/Sun");
            TextureAssets.Sun2 = ModContent.Request<Texture2D>("Terraria/Images/Sun2");
            TextureAssets.Sun3 = ModContent.Request<Texture2D>("Terraria/Images/Sun3");
            for (int i = 0; i < TextureAssets.Moon.Length; i++)
            {
                TextureAssets.Moon[i] = ModContent.Request<Texture2D>("Terraria/Images/Moon_" + i);
            }
        }
    }
}
