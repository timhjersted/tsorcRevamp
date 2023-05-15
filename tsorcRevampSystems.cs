using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using tsorcRevamp.Items;
using tsorcRevamp.UI;
using Terraria.Localization;
using System;
using tsorcRevamp.Textures;
using ReLogic.Graphics;
using tsorcRevamp.Tiles;
using System.Linq;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp
{
    class tsorcRevampSystems : ModSystem {
        public static RecipeGroup UpgradedMirrors;
        public static RecipeGroup CobaltHelmets;


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
            foreach (ForceLoadTexture texture in mapTextures) {
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
                        Main.spriteBatch.Draw(BonfireMinimapTexture, bonfireDrawCoords + offsetPositon, null, Main.DiscoColor, 0, BonfireMinimapTexture.Size() / 2, 1.04f, SpriteEffects.None, 1);
                    }
                    Main.spriteBatch.Draw(BonfireMinimapTexture, bonfireDrawCoords, null, Color.White, 0, BonfireMinimapTexture.Size() / 2, 1f, SpriteEffects.None, 1);
                    mouseText = "Teleport to Bonfire Checkpoint";

                    //Step 4: Check if they're left-clicking, and close the minimap + teleport them if so
                    if (Main.mouseLeft && !tsorcRevampWorld.BossAlive)
                    {
                        if (Main.LocalPlayer.HasBuff(ModContent.BuffType<InCombat>())) {
                            if (Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().TextCooldown >= 0) {
                                Main.NewText("Can not teleport while in combat!");
                            }
                        }
                        else {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, Main.LocalPlayer.position);
                            UsefulFunctions.SafeTeleport(Main.LocalPlayer, new Vector2(bonfirePoint.X, bonfirePoint.Y - 1) * 16);
                            Main.mapFullscreen = false;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, bonfirePoint * 16);
                            if (Main.netMode == NetmodeID.MultiplayerClient) {
                                Main.LocalPlayer.AddBuff(ModContent.BuffType<Buffs.Loading>(), 15);
                            }
                        }
                    }

                    if (tsorcRevampWorld.BossAlive)
                    {
                        mouseText = "Can not teleport while a boss is alive!";
                    }
                    else if (Main.LocalPlayer.HasBuff(ModContent.BuffType<InCombat>())) {
                        mouseText = "Can not teleport while in combat!";
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
                if((mouseTile - tsorcRevampWorld.AbyssPortalLocation / 16).Length() <= hoverRange)
                {
                    mouseText = "Abyssal Fissure";
                }
            }


            foreach (KeyValuePair<Vector2, int> marker in tsorcRevampWorld.MapMarkers) {
                Vector2 markerDrawCoords = marker.Key;
                markerDrawCoords.X += 1.5f;
                markerDrawCoords.Y += 1f;
                markerDrawCoords *= mapScale;
                markerDrawCoords += scaledMapCoords;
                Texture2D markerTexture = mapTextures[marker.Value].texture;
                Main.spriteBatch.Draw(markerTexture, markerDrawCoords, null, Color.White, 0, markerTexture.Size() / 2, 0.85f, SpriteEffects.None, 1);

                mouseTile = new Vector2((float)Math.Floor(mouseTile.X), (float)Math.Floor(mouseTile.Y));

                if (tsorcRevamp.MarkerSelected == 4 && (mouseTile - marker.Key).Length() < hoverRange && Main.mouseLeft) { //delete mode
                    tsorcRevampWorld.MapMarkers.Remove(marker.Key);
                }
            }

            if (!MapMarkersUIState.Switching && tsorcRevamp.MarkerSelected > -1 && tsorcRevamp.MarkerSelected != 4 && Main.mouseLeft && !tsorcRevampWorld.MapMarkers.ContainsKey(mouseTile)) {
                tsorcRevampWorld.MapMarkers.Add(mouseTile, tsorcRevamp.MarkerSelected);
                tsorcRevamp.MarkerSelected = -1;
            }

            else if (MapMarkersUIState.HoveringOver > -1) {
                string hoverText = "Click to select map marker. Click on the map to place a marker.";


                if (MapMarkersUIState.HoveringOver == MapMarkersUIState.REMOVE_ID) {
                    hoverText = "Click to begin erasing markers.";
                }

                if (tsorcRevamp.MarkerSelected == MapMarkersUIState.HoveringOver) {
                    hoverText = "Click to stop editing markers.";
                }

                mouseText = hoverText;

            }

            if (tsorcRevamp.MarkerSelected > -1) {
                Main.spriteBatch.Draw(mapTextures[tsorcRevamp.MarkerSelected].texture, new Vector2(Main.MouseScreen.X - 24, Main.MouseScreen.Y + 24), Color.White);
            }
            ModContent.GetInstance<tsorcRevamp>().MarkerInterface.Draw(Main.spriteBatch, new GameTime());
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
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
        }

        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            tsorcRevampPlayer modPlayer = Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.Draw(spriteBatch);
            if (tsorcRevamp.NearbySoapstone != null) {
                SoapstoneTileEntity soapstone = tsorcRevamp.NearbySoapstone;
                float scaleMod = (float)((ModContent.GetInstance<tsorcRevampConfig>().SoapstoneScale) / 100f) + 1;

                if (soapstone.timer > 0 && !soapstone.hidden) {
                    float textWidth = soapstone.textWidth > 0 ? soapstone.textWidth : SoapstoneMessage.DEFAULT_WIDTH;
                    textWidth *= scaleMod;

                    string text = UsefulFunctions.WrapString(soapstone.text, FontAssets.ItemStack.Value, textWidth, scaleMod);
                    textWidth += FontAssets.ItemStack.Value.MeasureString(" ").X * scaleMod;
                    float alpha = (soapstone.timer / 20f);
                    if(soapstone.timer >= 20)
                    {
                        alpha = 1;
                    }
                    //Main.NewText("Alpha: " + alpha + " timer: " + soapstone.timer);
                    Vector2 textPosition = (new Vector2(soapstone.Position.X, soapstone.Position.Y) * 16f - Main.screenPosition) - new Vector2((textWidth / 2) - 4, 128);
                    
                    //right padding
                    textWidth += FontAssets.ItemStack.Value.MeasureString(" ").X * scaleMod;

                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied); //allows it to have alpha

                    Texture2D boxTexture = ModContent.Request<Texture2D>("tsorcRevamp/UI/blackpixel", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

                    int lineCount = text.Count(a => a == '\n') + 1;
                    float height = scaleMod * (FontAssets.ItemStack.Value.LineSpacing * lineCount) + 8;
                    Rectangle drect = new((int)textPosition.X - 4, (int)textPosition.Y - 4, (int)textWidth + 8, (int)height);
                    Color bgColor = new(0, 0, 0, (0.5f * alpha) + 0.1f);
                    Main.spriteBatch.Draw(boxTexture, drect, bgColor);

                    Color textColor = new(1, 1, 1, alpha);
                    DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, FontAssets.ItemStack.Value, text, textPosition, textColor, 0, Vector2.Zero, scaleMod, SpriteEffects.None, 0);

                    //recycle the rect for the show button
                    textPosition.Y = drect.Y + height + 8;
                    drect.Y = drect.Y + (int)height + 4;

                    //the leading space is not a typo. WrapString prepends a space and is always called on normal text, but not buttons
                    string hideButtonText = " Click to Hide ";
                    Vector2 size = FontAssets.ItemStack.Value.MeasureString(hideButtonText) * scaleMod;

                    drect.Width = (int)size.X;
                    drect.Height = (int)size.Y;


                    Main.spriteBatch.Draw(boxTexture, drect, bgColor);

                    
                    //the leading space is not a typo. WrapString prepends a space and is always called on normal text, but not buttons
                    DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, FontAssets.ItemStack.Value, " Click to Hide", textPosition, textColor, 0, Vector2.Zero, scaleMod, SpriteEffects.None, 0);
                    Point scaledMouseScreen = (Main.MouseScreen * Main.UIScale).ToPoint();
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin();
                    if (drect.Contains(scaledMouseScreen)) {
                        Main.LocalPlayer.mouseInterface = true;
                        if (Main.mouseLeft && Main.mouseLeftRelease) {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                            soapstone.timer = 0;
                            soapstone.hidden = true;
                        }
                    }
                }
                else {
                    if (!soapstone.nearPlayer) return;

                    string showButtonText = " Click to Show  ";
                    Vector2 textSize = FontAssets.ItemStack.Value.MeasureString(showButtonText) * scaleMod;
                    Vector2 textPosition = (new Vector2(soapstone.Position.X, soapstone.Position.Y) * 16f - Main.screenPosition) - new Vector2((textSize.X / 2) - 4, 64);

                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);

                    Texture2D boxTexture = ModContent.Request<Texture2D>("tsorcRevamp/UI/blackpixel", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

                    Rectangle drect = new((int)textPosition.X - 4, (int)textPosition.Y - 4, (int)textSize.X, (int)textSize.Y);
                    Main.spriteBatch.Draw(boxTexture, drect, new(0, 0, 0, 113));

                    DynamicSpriteFontExtensionMethods.DrawString(Main.spriteBatch, FontAssets.ItemStack.Value, showButtonText, textPosition, new(255, 255, 255, 170), 0, Vector2.Zero, scaleMod, SpriteEffects.None, 0);
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin();
                    Point scaledMouseScreen = (Main.MouseScreen * Main.UIScale).ToPoint();
                    if (drect.Contains(scaledMouseScreen)) {
                        if (Main.mouseLeft && Main.mouseLeftRelease) {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuTick);
                            soapstone.timer = 25;
                            soapstone.hidden = false;
                        }
                    }
                }
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
