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

        internal static float visualLife = -1f;
        internal static float visualMana = -1f;
        internal static float visualStamina = -1f;

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

            int enemyDebugIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
            if (enemyDebugIndex != -1)
            {
                layers.Insert(enemyDebugIndex, new LegacyGameInterfaceLayer(
                    "tsorcRevamp: EnemyDebugUI",
                    delegate
                    {
                        if (mod.EnemySelectionUI.Visible)
                        {
                            mod._enemySelectionUI.Draw(Main.spriteBatch, new GameTime());
                        }
                        if (mod.SpawnPointConfigUI.Visible)
                        {
                            mod._spawnPointConfigUI.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }

            int resourceBarIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Resource Bars"));
            if (resourceBarIndex != -1)
            {
                if (ModContent.GetInstance<tsorcRevampConfig>().UseCustomResourceBars)
                {
                    // NOTE: We intentionally do NOT disable the whole "Vanilla: Resource Bars" layer here.
                    // That layer (Main.GUIBarsDrawInner) draws the breath meter and buff/debuff icons in
                    // addition to the life & mana bars, so disabling it hid all of them. Instead, only the
                    // vanilla life & mana drawing is suppressed via the GUIBarsDrawInner detour in
                    // MethodSwaps.cs (it swaps ActivePlayerResourcesSet to a no-op set), leaving breath and
                    // buffs intact. Our custom bars draw on top.
                    layers.Insert(resourceBarIndex + 1, new LegacyGameInterfaceLayer(
                        "tsorcRevamp: Custom Resource Bars",
                        delegate
                        {
                            DrawCustomResourceBars(Main.spriteBatch);
                            return true;
                        },
                        InterfaceScaleType.UI)
                    );
                }

                layers.Insert(resourceBarIndex, new LegacyGameInterfaceLayer(
                    "tsorcRevamp: Dark Soul Counter UI",
                    delegate
                    {
                        if (!Main.playerInventory)
                        {
                            mod._darkSoulCounterUIState.Draw(Main.spriteBatch, new GameTime());
                        }
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
                        if (!Main.playerInventory)
                        {
                            mod._estusFlaskUIState.Draw(Main.spriteBatch, new GameTime());
                        }
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
                        if (!Main.playerInventory)
                        {
                            mod._ceruleanFlaskUIState.Draw(Main.spriteBatch, new GameTime());
                        }
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
            if (Items.Debug.EnemyDebugTome.JustClosedUI && !Main.mouseLeft && !Main.mouseRight)
            {
                Items.Debug.EnemyDebugTome.JustClosedUI = false;
            }

            tsorcRevamp mod = ModContent.GetInstance<tsorcRevamp>();

            // If the player is no longer holding the Enemy Debug Tome, close its menus and drop any selection.
            if (Main.LocalPlayer.HeldItem.type != ModContent.ItemType<Items.Debug.EnemyDebugTome>())
            {
                if (mod.SpawnPointConfigUI.Visible)
                {
                    mod.SpawnPointConfigUI.Hide();
                }
                if (mod.EnemySelectionUI.Visible)
                {
                    mod.EnemySelectionUI.Hide();
                }
                mod.EnemySelectionUI.SelectedNpcType = 0;
                mod.EnemySelectionUI.QuickAddMode = false;
            }

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
                mod.PotionBagUserInterface?.Update(gameTime);
            }
            if (mod.EnemySelectionUI.Visible)
            {
                mod._enemySelectionUI?.Update(gameTime);
            }
            if (mod.SpawnPointConfigUI.Visible)
            {
                mod._spawnPointConfigUI?.Update(gameTime);
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

            // Smooth visual decay for damage indicators (both overhead and custom top-right bars)
            if (!Main.gameMenu)
            {
                Player player = Main.LocalPlayer;
                if (player != null && player.active && !player.dead)
                {
                    int healthCurrent = player.statLife;
                    int manaCurrent = player.statMana;
                    var staminaPlayer = player.GetModPlayer<tsorcRevampStaminaPlayer>();
                    float staminaCurrent = staminaPlayer.staminaResourceCurrent;

                    if (visualLife < 0f || visualLife < healthCurrent) visualLife = healthCurrent;
                    else if (visualLife > healthCurrent)
                    {
                        visualLife -= Math.Max(0.2f, (visualLife - healthCurrent) * 0.08f);
                        if (visualLife < healthCurrent) visualLife = healthCurrent;
                    }

                    if (visualMana < 0f || visualMana < manaCurrent) visualMana = manaCurrent;
                    else if (visualMana > manaCurrent)
                    {
                        visualMana -= Math.Max(0.1f, (visualMana - manaCurrent) * 0.08f);
                        if (visualMana < manaCurrent) visualMana = manaCurrent;
                    }

                    if (visualStamina < 0f || visualStamina < staminaCurrent) visualStamina = staminaCurrent;
                    else if (visualStamina > staminaCurrent)
                    {
                        visualStamina -= Math.Max(0.1f, (visualStamina - staminaCurrent) * 0.08f);
                        if (visualStamina < staminaCurrent) visualStamina = staminaCurrent;
                    }
                }
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

        public override void OnWorldLoad()
        {
            visualLife = -1f;
            visualMana = -1f;
            visualStamina = -1f;
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
            visualLife = -1f;
            visualMana = -1f;
            visualStamina = -1f;
        }

        private static void DrawCustomResourceBars(SpriteBatch spriteBatch)
        {
            Player player = Main.LocalPlayer;
            if (player == null || !player.active || player.dead) return;

            int healthCurrent = player.statLife;
            int healthMax = player.statLifeMax2;
            int manaCurrent = player.statMana;
            int manaMax = player.statManaMax2;

            var staminaPlayer = player.GetModPlayer<tsorcRevampStaminaPlayer>();
            float staminaCurrent = staminaPlayer.staminaResourceCurrent;
            float staminaMax = staminaPlayer.staminaResourceMax2;

            // Dimensions
            int barHeight = 12;
            int gap = 8;

            // Aligns the right edge of the bars to the far right of the screen (aligned with the map right edge)
            // Shifts leftwards when the inventory is open to make room for numbers on the right side.
            int rightX = Main.playerInventory ? (Main.screenWidth - 90) : (Main.screenWidth - 10);
            int startY = 15;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Texture2D barEmpty = ModContent.Request<Texture2D>("tsorcRevamp/Textures/StaminaBar_empty").Value;            // Helper to get compressed bar width based on max capacity:
            // - Under or equal to 400: 1.0x scale
            // - 401 to 500: 20% compressed (0.8x scale for all chunks)
            // - 501 or more: 30% compressed (0.7x scale for all chunks)
            int GetBarWidth(float maxVal)
            {
                float scale = 1f;
                if (maxVal > 400f && maxVal <= 500f)
                {
                    scale = 0.8f;
                }
                else if (maxVal > 500f)
                {
                    scale = 0.7f;
                }
                return (int)(maxVal * scale);
            }

            int GetFillWidth(float val, float maxVal)
            {
                float currentVal = Math.Min(val, maxVal);
                if (currentVal <= 0f) return 0;

                float scale = 1f;
                if (maxVal > 400f && maxVal <= 500f)
                {
                    scale = 0.8f;
                }
                else if (maxVal > 500f)
                {
                    scale = 0.7f;
                }
                return (int)(currentVal * scale);
            }

            // Reusable bar drawing helper using 3-slice rendering of the overhead stamina bar sprite
            void DrawBar(int y, float current, float visualCurrent, float max, Color fillColor, Color highlightColor, Color shadowColor, Color bgColor)
            {
                int maxBarWidth = GetBarWidth(max);
                int startX = rightX - maxBarWidth;

                float ratio = max > 0 ? (current / max) : 0f;
                ratio = MathHelper.Clamp(ratio, 0f, 1f);

                float visualRatio = max > 0 ? (visualCurrent / max) : 0f;
                visualRatio = MathHelper.Clamp(visualRatio, 0f, 1f);

                // Draw the 3-sliced empty bar sprite as the border casing
                // Left slice (5px wide)
                spriteBatch.Draw(barEmpty, new Rectangle(startX - 5, y, 5, barHeight), new Rectangle(0, 0, 5, 12), Color.White);
                // Middle slice (maxBarWidth wide)
                spriteBatch.Draw(barEmpty, new Rectangle(startX, y, maxBarWidth, barHeight), new Rectangle(5, 0, 30, 12), Color.White);
                // Right slice (4px wide)
                spriteBatch.Draw(barEmpty, new Rectangle(startX + maxBarWidth, y, 4, barHeight), new Rectangle(35, 0, 4, 12), Color.White);

                // Draw solid black inside the fill area to hide the textured empty center (so colors look clean)
                spriteBatch.Draw(pixel, new Rectangle(startX, y + 2, maxBarWidth, barHeight - 3), Color.Black * 0.8f);

                // Draw dark resource background container
                spriteBatch.Draw(pixel, new Rectangle(startX, y + 2, maxBarWidth, barHeight - 3), bgColor);

                // Draw damage yellow indicator (if visualCurrent > current)
                if (visualRatio > ratio)
                {
                    int fillStart = GetFillWidth(current, max);
                    int fillWidth = GetFillWidth(visualCurrent, max) - fillStart;
                    if (fillWidth > 0)
                    {
                        // Draw yellow highlight (top row, 2px high)
                        spriteBatch.Draw(pixel, new Rectangle(startX + fillStart, y + 2, fillWidth, 2), new Color(255, 225, 120));
                        // Draw yellow main (middle row, 5px high)
                        spriteBatch.Draw(pixel, new Rectangle(startX + fillStart, y + 4, fillWidth, 5), new Color(240, 190, 50));
                        // Draw yellow shadow (bottom row, 2px high)
                        spriteBatch.Draw(pixel, new Rectangle(startX + fillStart, y + 9, fillWidth, 2), new Color(160, 110, 10));
                    }
                }

                // Draw current resource fill
                int currentFillWidth = GetFillWidth(current, max);
                if (currentFillWidth > 0)
                {
                    // Draw highlight (top row, 2px high)
                    spriteBatch.Draw(pixel, new Rectangle(startX, y + 2, currentFillWidth, 2), highlightColor);
                    // Draw main fill (middle row, 5px high)
                    spriteBatch.Draw(pixel, new Rectangle(startX, y + 4, currentFillWidth, 5), fillColor);
                    // Draw shadow (bottom row, 2px high)
                    spriteBatch.Draw(pixel, new Rectangle(startX, y + 9, currentFillWidth, 2), shadowColor);
                }

                // Draw faint vertical segment lines every 20 points (compressed uniformly)
                int maxSegments = (int)(max / 20f) + 1;
                for (int i = 1; i < maxSegments; i++)
                {
                    float segmentVal = i * 20f;
                    if (segmentVal < max)
                    {
                        int lineX = startX + GetFillWidth(segmentVal, max);
                        spriteBatch.Draw(pixel, new Rectangle(lineX, y + 2, 1, barHeight - 3), Color.White * 0.15f);
                    }
                }

                // Draw custom pixelated black and gold outline borders over the casing
                int L = startX - 5;
                int T = y;
                int W = maxBarWidth + 9;
                int H = barHeight;

                // 1. Draw outer black border (1px thick outline)
                // Top line
                spriteBatch.Draw(pixel, new Rectangle(L, T, W, 1), Color.Black);
                // Bottom line
                spriteBatch.Draw(pixel, new Rectangle(L, T + H - 1, W, 1), Color.Black);
                // Left line
                spriteBatch.Draw(pixel, new Rectangle(L, T + 1, 1, H - 2), Color.Black);
                // Right line
                spriteBatch.Draw(pixel, new Rectangle(L + W - 1, T + 1, 1, H - 2), Color.Black);

                // 2. Draw inner gold border (1px thick outline inside top, left, right)
                Color borderGoldColor = new Color(200, 160, 60);
                // Top gold line
                spriteBatch.Draw(pixel, new Rectangle(L + 1, T + 1, W - 2, 1), borderGoldColor);
                // Left gold line
                spriteBatch.Draw(pixel, new Rectangle(L + 1, T + 2, 1, H - 3), borderGoldColor);
                // Right gold line
                spriteBatch.Draw(pixel, new Rectangle(L + W - 2, T + 2, 1, H - 3), borderGoldColor);
            }

            // Health (Red)
            DrawBar(startY, healthCurrent, visualLife, healthMax, new Color(230, 45, 45), new Color(255, 130, 120), new Color(140, 20, 45), new Color(45, 10, 10, 180));

            // Mana (Blue)
            DrawBar(startY + barHeight + gap, manaCurrent, visualMana, manaMax, new Color(30, 110, 230), new Color(100, 175, 255), new Color(20, 45, 140), new Color(10, 20, 50, 180));

            // Stamina (Green)
            int stamY = startY + (barHeight + gap) * 2;
            DrawBar(stamY, staminaCurrent, visualStamina, staminaMax, new Color(40, 190, 80), new Color(120, 230, 150), new Color(15, 90, 40), new Color(10, 40, 15, 180));

            // Retain divider line on Stamina bar (dodge threshold at 30 stamina)
            if (staminaMax > 30)
            {
                int stamStartX = rightX - GetBarWidth(staminaMax);
                int dividerX = stamStartX + GetFillWidth(30, staminaMax);
                spriteBatch.Draw(pixel, new Rectangle(dividerX, stamY + 2, 1, barHeight - 3), Color.White * 0.75f);
            }

            // Render numeric text on the right when inventory is open
            if (Main.playerInventory)
            {
                void DrawText(int y, float current, float max, int endX)
                {
                    string text = $"{(int)current}/{(int)max}";
                    Vector2 textSize = FontAssets.MouseText.Value.MeasureString(text);
                    // Draw close to the right of the bar (e.g. 8px margin)
                    Vector2 textPos = new Vector2(endX + 8, y - (textSize.Y - barHeight) / 2f + 1f);
                    Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow(
                        spriteBatch, FontAssets.MouseText.Value, text, textPos, Color.White, 0f, Vector2.Zero, new Vector2(0.85f)
                    );
                }

                DrawText(startY, healthCurrent, healthMax, rightX);
                DrawText(startY + barHeight + gap, manaCurrent, manaMax, rightX);
                DrawText(stamY, staminaCurrent, staminaMax, rightX);
            }
        }

        private static Color GetDustColor(int dustId)
        {
            return dustId switch
            {
                Terraria.ID.DustID.Shadowflame => Color.MediumPurple,
                Terraria.ID.DustID.Torch => Color.OrangeRed,
                Terraria.ID.DustID.HallowedTorch => Color.HotPink,
                Terraria.ID.DustID.IceTorch => Color.SkyBlue,
                Terraria.ID.DustID.GreenTorch => Color.LightGreen,
                Terraria.ID.DustID.GoldFlame => Color.Gold,
                Terraria.ID.DustID.BoneTorch => Color.White,
                Terraria.ID.DustID.PurpleTorch => Color.Purple,
                _ => Color.White
            };
        }

        private static void DrawLineScreen(SpriteBatch spriteBatch, Vector2 startScreen, Vector2 endScreen, Color color, float thickness)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 segment = endScreen - startScreen;
            Vector2 scale = new Vector2(segment.Length() / pixel.Width, thickness / pixel.Height);
            Vector2 origin = new Vector2(0f, pixel.Height / 2f);
            spriteBatch.Draw(pixel, startScreen, null, color, segment.ToRotation(), origin, scale, SpriteEffects.None, 0f);
        }

        private void DrawHardcodedEvent(ScriptedEvent ev, bool isDormant)
        {
            if (ev.DynamicEventID != null) return;
            Vector2 centerPos = ev.centerpoint;
            
            // Only draw if within 3000 pixels of the screen center to save performance
            Vector2 screenCenter = Main.screenPosition + new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);
            if (Vector2.DistanceSquared(centerPos, screenCenter) > 3000 * 3000)
            {
                return;
            }

            // Draw the ring (always drawn in editor mode: white/faint for invisible, correct color for visible)
            Color ringColor = ev.visible ? GetDustColor(ev.dustID) : Color.White * 0.4f;
            if (isDormant)
            {
                ringColor = ringColor * 0.4f;
            }
            float thickness = 2f;

            float radiusInPixels = (float)System.Math.Sqrt(ev.radius);

            if (ev.square)
            {
                Vector2 topLeft = centerPos + new Vector2(-radiusInPixels, -radiusInPixels) - Main.screenPosition;
                Vector2 topRight = centerPos + new Vector2(radiusInPixels, -radiusInPixels) - Main.screenPosition;
                Vector2 bottomLeft = centerPos + new Vector2(-radiusInPixels, radiusInPixels) - Main.screenPosition;
                Vector2 bottomRight = centerPos + new Vector2(radiusInPixels, radiusInPixels) - Main.screenPosition;
                DrawLineScreen(Main.spriteBatch, topLeft, topRight, ringColor, thickness);
                DrawLineScreen(Main.spriteBatch, topRight, bottomRight, ringColor, thickness);
                DrawLineScreen(Main.spriteBatch, bottomRight, bottomLeft, ringColor, thickness);
                DrawLineScreen(Main.spriteBatch, bottomLeft, topLeft, ringColor, thickness);
            }
            else
            {
                int points = 60;
                float step = MathHelper.TwoPi / points;
                for (int i = 0; i < points; i++)
                {
                    Vector2 p1 = centerPos + new Vector2(radiusInPixels, 0).RotatedBy(i * step) - Main.screenPosition;
                    Vector2 p2 = centerPos + new Vector2(radiusInPixels, 0).RotatedBy((i + 1) * step) - Main.screenPosition;
                    DrawLineScreen(Main.spriteBatch, p1, p2, ringColor, thickness);
                }
            }

            // Draw the center icon
            Texture2D icon = ModContent.Request<Texture2D>("tsorcRevamp/Items/Debug/EnemyDebugTome", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            if (icon != null)
            {
                Color iconColor = isDormant ? Color.White * 0.4f : Color.White;
                Main.spriteBatch.Draw(icon, centerPos - Main.screenPosition, null, iconColor, 0f, icon.Size() / 2f, 1.0f, SpriteEffects.None, 0f);
            }

            // Draw the NPCs
            if (ev.eventNPCs != null)
            {
                foreach (var npc in ev.eventNPCs)
                {
                    NPC dummyNPC = new NPC();
                    dummyNPC.SetDefaults(npc.type);
                    dummyNPC.active = true;
                    dummyNPC.Bottom = new Vector2(npc.spawnCoords.X * 16 + 8, npc.spawnCoords.Y * 16 + 16);
                    if (isDormant)
                    {
                        dummyNPC.color = Color.White * 0.4f;
                    }
                    Main.instance.LoadNPC(npc.type);
                    Main.instance.DrawNPCDirect(Main.spriteBatch, dummyNPC, false, Main.screenPosition);
                }
            }
        }

        public override void PostDrawTiles()
        {
            if (Main.LocalPlayer.HeldItem.type == ModContent.ItemType<Items.Debug.EnemyDebugTome>())
            {
                // Use TransformationMatrix (not ZoomMatrix) so world-space draws line up with Main.MouseWorld.
                // ZoomMatrix anchors/translates differently than the matrix vanilla uses to draw NPCs, which makes
                // world draws drift from the cursor proportionally to its distance from screen center when zoomed.
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                
                // Draw hardcoded EnabledEvents safely
                if (tsorcScriptedEvents.EnabledEvents != null)
                {
                    var enabledCopy = tsorcScriptedEvents.EnabledEvents.ToArray();
                    foreach (var ev in enabledCopy)
                    {
                        if (ev != null)
                        {
                            DrawHardcodedEvent(ev, false);
                        }
                    }
                }

                // Draw hardcoded RunningEvents safely
                if (tsorcScriptedEvents.RunningEvents != null)
                {
                    var runningCopy = tsorcScriptedEvents.RunningEvents.ToArray();
                    foreach (var ev in runningCopy)
                    {
                        if (ev != null)
                        {
                            DrawHardcodedEvent(ev, false);
                        }
                    }
                }

                // Draw hardcoded DisabledEvents safely
                if (tsorcScriptedEvents.DisabledEvents != null)
                {
                    var disabledCopy = tsorcScriptedEvents.DisabledEvents.ToArray();
                    foreach (var ev in disabledCopy)
                    {
                        if (ev != null)
                        {
                            DrawHardcodedEvent(ev, true);
                        }
                    }
                }

                var dynamicCopy = tsorcScriptedEvents.DynamicEvents != null ? tsorcScriptedEvents.DynamicEvents.ToArray() : System.Array.Empty<DynamicSpawnEvent>();
                foreach (var ev in dynamicCopy)
                {
                    if (ev == null) continue;

                    // Hide events that don't belong to the current world (e.g. Adventure-only events in a Remix world).
                    if (!tsorcScriptedEvents.IsEventVisibleInCurrentWorld(ev)) continue;

                    Vector2 centerPos = new Vector2(ev.CenterX * 16 + 8, ev.CenterY * 16 + 8);

                    // Only draw if within 3000 pixels of the screen center to save performance
                    Vector2 screenCenter = Main.screenPosition + new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);
                    if (Vector2.DistanceSquared(centerPos, screenCenter) > 3000 * 3000)
                    {
                        continue;
                    }

                    var configUI = ModContent.GetInstance<tsorcRevamp>().SpawnPointConfigUI;
                    bool isSelected = configUI.Visible && configUI.CurrentEvent == ev;

                    // Draw the ring (always drawn in editor mode: white/faint for invisible, correct color for visible)
                    Color ringColor = ev.VisibleRing ? GetDustColor(ev.TriggerDust) : Color.White * 0.4f;
                    float thickness = 2f;

                    if (isSelected)
                    {
                        ringColor = Color.Gold;
                        thickness = 3f;
                    }

                    int points = 60;
                    float step = MathHelper.TwoPi / points;
                    float radiusInPixels = (float)System.Math.Sqrt(ev.Radius);
                    for (int i = 0; i < points; i++)
                    {
                        Vector2 p1 = centerPos + new Vector2(radiusInPixels, 0).RotatedBy(i * step) - Main.screenPosition;
                        Vector2 p2 = centerPos + new Vector2(radiusInPixels, 0).RotatedBy((i + 1) * step) - Main.screenPosition;
                        DrawLineScreen(Main.spriteBatch, p1, p2, ringColor, thickness);
                    }

                    // Draw the center icon. Quick-add events use their single NPC as the marker, so no book icon.
                    if (!ev.SingleNpcMarker)
                    {
                        Texture2D icon = ModContent.Request<Texture2D>("tsorcRevamp/Items/Debug/EnemyDebugTome", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                        if (icon != null)
                        {
                            Main.spriteBatch.Draw(icon, centerPos - Main.screenPosition, null, Color.White, 0f, icon.Size() / 2f, 1f, SpriteEffects.None, 0f);
                        }
                    }

                    // Draw the NPCs
                    foreach (var npc in ev.Npcs)
                    {
                        NPC dummyNPC = new NPC();
                        dummyNPC.SetDefaults(npc.NpcID);
                        dummyNPC.active = true;
                        dummyNPC.Bottom = new Vector2(npc.SpawnX * 16 + 8, npc.SpawnY * 16 + 16);
                        
                        if (isSelected)
                        {
                            float pulse = (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 4f) * 0.2f + 0.8f;
                            dummyNPC.color = new Color(255, 230, 100) * pulse;
                        }

                        // Load the texture safely
                        Main.instance.LoadNPC(npc.NpcID);
                        
                        Main.instance.DrawNPCDirect(Main.spriteBatch, dummyNPC, false, Main.screenPosition);
                    }
                }

                Main.spriteBatch.End();

                // Draw cursor preview in a separate identity-matrix batch so it is immune to zoom drift.
                // TransformationMatrix scales from the top-left corner, not the screen center where Terraria
                // anchors zoom — so any world-space or screen-relative draw drifts proportionally to distance
                // from screen center. Drawing at Main.MouseScreen with no matrix has zero drift at any zoom level.
                var enemyUI = ModContent.GetInstance<tsorcRevamp>().EnemySelectionUI;
                if (enemyUI.SelectedNpcType != 0)
                {
                    int selectedType = enemyUI.SelectedNpcType;
                    Main.instance.LoadNPC(selectedType);

                    var textureAsset = Terraria.GameContent.TextureAssets.Npc[selectedType];
                    Texture2D npcTexture = textureAsset?.Value;
                    if (npcTexture != null)
                    {
                        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);

                        int frameCount = Main.npcFrameCount[selectedType];
                        if (frameCount < 1) frameCount = 1;
                        Rectangle sourceRect = npcTexture.Frame(1, frameCount, 0, 0);

                        // Bottom-center of sprite sits at mouse cursor — matches placement which snaps to the hovered tile
                        Vector2 origin = new Vector2(sourceRect.Width / 2f, sourceRect.Height);
                        // Scale by zoom so the preview matches the in-game NPC size at the current zoom level.
                        float zoom = Main.GameViewMatrix.Zoom.X;
                        Main.spriteBatch.Draw(npcTexture, Main.MouseScreen, sourceRect, Color.White * 0.6f, 0f, origin, zoom, SpriteEffects.None, 0f);

                        Main.spriteBatch.End();
                    }
                }
            }
        }
    }
}
