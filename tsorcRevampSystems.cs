using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using Terraria.UI;
using Terraria.GameContent.UI;
using tsorcRevamp.UI;
using System;
using Microsoft.Xna.Framework.Graphics;
using static tsorcRevamp.MethodSwaps;
using static tsorcRevamp.ILEdits;
using System.IO;
using Terraria.ModLoader.IO;
using Terraria.Graphics.Shaders;
using Terraria.Graphics.Effects;
using ReLogic.Graphics;
using System.Net;
using System.Reflection;
using System.ComponentModel;
using Terraria.DataStructures;

namespace tsorcRevamp {
    class tsorcRevampSystems : ModSystem {
        Texture2D BonfireMinimapTexture;
        public override void PostDrawFullscreenMap(ref string mouseText) {
            if (Main.LocalPlayer.HasBuff(ModContent.BuffType<Buffs.Bonfire>())) {
                DrawMinimapBonfires();
            }
        }

        public void DrawMinimapBonfires() {
            if (BonfireMinimapTexture == null || BonfireMinimapTexture.IsDisposed) {
                BonfireMinimapTexture = ModContent.Request<Texture2D>("tsorcRevamp/UI/MinimapBonfire", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

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
            foreach (Vector2 bonfirePoint in tsorcRevampWorld.LitBonfireList) {
                Vector2 bonfireDrawCoords = bonfirePoint;
                bonfireDrawCoords.X += 1.5f;
                bonfireDrawCoords.Y += 1f;
                bonfireDrawCoords *= mapScale;
                bonfireDrawCoords += scaledMapCoords;

                //Step 3: While drawing check if it's in-range of the cursor, and if so give it a rainbow backdrop
                float hoverRange = 32 / Main.mapFullscreenScale;
                if ((mouseTile - bonfirePoint).Length() <= hoverRange) {
                    for (int i = 0; i < 4; i++) {
                        Vector2 offsetPositon = Vector2.UnitY.RotatedBy(MathHelper.PiOver2 * i) * 3;
                        Main.spriteBatch.Draw(BonfireMinimapTexture, bonfireDrawCoords + offsetPositon, null, Main.DiscoColor, 0, BonfireMinimapTexture.Size() / 2, 1.04f, SpriteEffects.None, 1);
                    }
                    Main.spriteBatch.Draw(BonfireMinimapTexture, bonfireDrawCoords, null, Color.White, 0, BonfireMinimapTexture.Size() / 2, 1f, SpriteEffects.None, 1);

                    //Step 4: Check if they're left-clicking, and close the minimap + teleport them if so
                    if (Main.mouseLeft) {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, Main.LocalPlayer.position);
                        UsefulFunctions.SafeTeleport(Main.LocalPlayer, new Vector2(bonfirePoint.X, bonfirePoint.Y - 1) * 16);
                        Main.mapFullscreen = false;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, bonfirePoint * 16);
                    }
                }
                else {
                    Main.spriteBatch.Draw(BonfireMinimapTexture, bonfireDrawCoords, null, Color.White, 0, BonfireMinimapTexture.Size() / 2, 0.85f, SpriteEffects.None, 1);
                }
            }
        }

    }
}
