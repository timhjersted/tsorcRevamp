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
                        if (Main.LocalPlayer.HasBuff(ModContent.BuffType<Buffs.InCombat>())) {
                            Main.mapFullscreen = false;
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
                    else if (Main.LocalPlayer.HasBuff(ModContent.BuffType<Buffs.InCombat>())) {
                        mouseText = "Can not teleport while in combat!";
                    }
                }
                else
                {
                    Main.spriteBatch.Draw(BonfireMinimapTexture, bonfireDrawCoords, null, Color.White, 0, BonfireMinimapTexture.Size() / 2, 0.85f, SpriteEffects.None, 1);
                }
            }
            MapMarkersUIState.Visible = true;

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

            if(tsorcRevamp.MarkerSelected > -1) {
                Main.spriteBatch.Draw(mapTextures[tsorcRevamp.MarkerSelected].texture, new Vector2(Main.MouseScreen.X - 32, Main.MouseScreen.Y - 32), Color.White);
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
