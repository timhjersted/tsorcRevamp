// ═══════════════════════════════════════════════════════════════════════════════
//  VOID LAVA  —  Liquids/VoidLava.cs
//
//  Lava inside the ice biome becomes "Void": purple, purple-lit, 4x damage, and it
//  ignores lava immunity and liquid walking. All pieces live here so the system is
//  easy to find, edit, or rip out:
//
//    1.  VoidLavaCondition  — single place to change WHERE lava counts as Void
//    2.  VoidLavaSystem     — recolors every lava texture, light and map pixel
//    3.  VoidLavaPlayer     — strips lava immunity / water walking, 4x damage, death text
//    4.  VoidLavaMapTile    — never-placed tile that only supplies the "Void" map colour
//
//  There is no ModLavaStyle in this tModLoader version, so the visual swaps the
//  pixels of vanilla's lava textures in place while the local player is in the
//  biome, and restores them on leaving / world exit / unload. Like biome water,
//  it is screen-wide: all lava on screen turns purple while you stand in the zone.
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Liquid;
using Terraria.Graphics.Light;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;

namespace tsorcRevamp.Liquids
{
    // ──────────────────────────────────────────────────────────────────────────
    //  1. VOID CONDITION
    //     Change this one method to change every rule at once (visual, light,
    //     map, immunity bypass, damage and death message all read it).
    // ──────────────────────────────────────────────────────────────────────────
    internal static class VoidLavaCondition
    {
        internal static bool IsActive(Player player)
        {
            return player.ZoneSnow;
        }
    }


    // ──────────────────────────────────────────────────────────────────────────
    //  2. VISUALS  —  purple textures, purple light, purple map
    //     Textures: lava body, block/slope fill behind tiles, lavafall, the lava
    //     bubble dust (its slot in the shared dust atlas), the lava drip gore and
    //     both lava buckets. Each keeps its vanilla pixels so it can be put back.
    //     Light: detours/IL on every vanilla orange lava light source.
    //     Map: lava tiles charted while Void is visible get the Void map colour.
    // ──────────────────────────────────────────────────────────────────────────
    public class VoidLavaSystem : ModSystem
    {
        // Read by the lighting/map hooks, which can run on worker threads.
        public static bool VoidLavaVisible;

        // Channel weights for the orange → purple remap (see BuildVoidPixels).
        private const float VoidRedFromRed = 0.65f;
        private const float VoidBlueFromRed = 0.95f;
        private const float VoidGreenFromGreen = 0.25f;

        // Light: vanilla lava glows (1, 0.6, 0.2) x pulse; Void glows violet.
        private static readonly Vector3 VoidLightTint = new Vector3(0.55f, 0.12f, 1f);

        // Dust atlas layout: dust type T sits at x = 10*T, three 8x8 frames 10px apart.
        private static readonly Rectangle LavaDustAtlasRegion = new Rectangle(10 * DustID.Lava, 0, 10, 30);

        private class RecoloredTexture
        {
            public Texture2D Texture;
            public Rectangle Region;
            public Color[] VanillaPixels;
            public Color[] VoidPixels;
        }

        private static List<RecoloredTexture> recoloredTextures;
        private static bool texturesAreVoid;

        // Map colour index vanilla uses for lava (MapHelper.liquidPosition + 1); read once
        // after content setup, when the map colour table is final.
        private static int vanillaLavaMapType = -1;

        public override void Load()
        {
            if (Main.dedServ)
            {
                return;
            }

            On_TileLightScanner.ApplyLiquidLight += ApplyVoidLavaLight;
            On_WaterfallManager.AddLight += ApplyVoidLavafallLight;
            On_MapHelper.CreateMapTile += ChartVoidLavaOnMap;
            IL_Dust.UpdateDust += PatchLavaDustLight;
            IL_Gore.Update += PatchLavaDripLight;
        }

        public override void PostSetupContent()
        {
            if (Main.dedServ)
            {
                return;
            }

            FieldInfo liquidPositionField = typeof(MapHelper).GetField("liquidPosition", BindingFlags.NonPublic | BindingFlags.Static);

            if (liquidPositionField != null)
            {
                ushort liquidPosition = (ushort)liquidPositionField.GetValue(null);
                vanillaLavaMapType = liquidPosition + LiquidID.Lava;
            }
            else
            {
                Mod.Logger.Warn("VoidLava: MapHelper.liquidPosition not found, Void lava keeps its vanilla map colour.");
            }
        }

        public override void Unload()
        {
            // SetData must run on the main thread; Unload may not.
            Main.QueueMainThreadAction(() => SetVoidTextures(false));
            VoidLavaVisible = false;
            vanillaLavaMapType = -1;
        }

        public override void OnWorldUnload()
        {
            if (Main.dedServ)
            {
                return;
            }

            SetVoidTextures(false);
            VoidLavaVisible = false;
        }

        public override void PostUpdateEverything()
        {
            if (Main.dedServ)
            {
                return;
            }

            bool shouldBeVoid = !Main.gameMenu && VoidLavaCondition.IsActive(Main.LocalPlayer);
            VoidLavaVisible = shouldBeVoid;

            if (shouldBeVoid != texturesAreVoid)
            {
                SetVoidTextures(shouldBeVoid);
            }
        }

        // Swap every lava sheet to Void (true) or back to vanilla (false). Pixel
        // arrays are built once on first use; after that a swap is a SetData per sheet.
        private static void SetVoidTextures(bool toVoid)
        {
            if (recoloredTextures == null)
            {
                if (!toVoid)
                {
                    return;
                }

                recoloredTextures = BuildVoidPixels();
            }

            foreach (RecoloredTexture recolored in recoloredTextures)
            {
                if (recolored.Texture.IsDisposed)
                {
                    continue;
                }

                Color[] pixels = recolored.VanillaPixels;

                if (toVoid)
                {
                    pixels = recolored.VoidPixels;
                }

                recolored.Texture.SetData(0, recolored.Region, pixels, 0, pixels.Length);
            }

            texturesAreVoid = toVoid;
        }

        // Grab the vanilla lava sheets and remap orange → purple. Index 1 is lava in
        // the liquid/slope/block arrays; "Images/Waterfall_1" is the lavafall and
        // matches WaterfallManager's request string, so it hits the same cached asset.
        // The dust atlas is shared by all vanilla dusts, so only the lava dust's slot is touched.
        // Remap: R → 0.65R, B → 0.95R, G → max(B, 0.25G). Every output channel stays
        // ≤ the input's max channel, so premultiplied alpha is never exceeded.
        private static List<RecoloredTexture> BuildVoidPixels()
        {
            Main.instance.LoadGore(GoreID.LavaDrip);
            Main.instance.LoadItem(ItemID.LavaBucket);
            Main.instance.LoadItem(ItemID.BottomlessLavaBucket);

            List<(Asset<Texture2D> asset, Rectangle? region)> lavaSheets = new List<(Asset<Texture2D>, Rectangle?)>
            {
                (LiquidRenderer.Instance._liquidTextures[LiquidID.Lava], null),
                (TextureAssets.Liquid[LiquidID.Lava], null),
                (TextureAssets.LiquidSlope[LiquidID.Lava], null),
                (Main.Assets.Request<Texture2D>("Images/Waterfall_1", AssetRequestMode.ImmediateLoad), null),
                (TextureAssets.Dust, LavaDustAtlasRegion),
                (TextureAssets.Gore[GoreID.LavaDrip], null),
                (TextureAssets.Item[ItemID.LavaBucket], null),
                (TextureAssets.Item[ItemID.BottomlessLavaBucket], null),
            };

            List<RecoloredTexture> built = new List<RecoloredTexture>();

            foreach ((Asset<Texture2D> asset, Rectangle? region) in lavaSheets)
            {
                asset.Wait?.Invoke();
                Texture2D texture = asset.Value;
                Rectangle recolorRegion = region ?? texture.Bounds;
                int pixelCount = recolorRegion.Width * recolorRegion.Height;

                Color[] vanillaPixels = new Color[pixelCount];
                texture.GetData(0, recolorRegion, vanillaPixels, 0, pixelCount);

                Color[] voidPixels = new Color[pixelCount];

                for (int i = 0; i < pixelCount; i++)
                {
                    Color vanilla = vanillaPixels[i];

                    float red = vanilla.R * VoidRedFromRed;
                    float green = Math.Max(vanilla.B, vanilla.G * VoidGreenFromGreen);
                    float blue = vanilla.R * VoidBlueFromRed;

                    voidPixels[i] = new Color((byte)red, (byte)green, (byte)blue, vanilla.A);
                }

                built.Add(new RecoloredTexture
                {
                    Texture = texture,
                    Region = recolorRegion,
                    VanillaPixels = vanillaPixels,
                    VoidPixels = voidPixels,
                });
            }

            return built;
        }

        // Lava tiles emit violet instead of orange while Void is visible. Same pulse
        // as vanilla (0.55 base + mouseTextColor shimmer); only raises channels, never lowers.
        private static void ApplyVoidLavaLight(On_TileLightScanner.orig_ApplyLiquidLight orig, TileLightScanner self, Tile tile, ref Vector3 lightColor)
        {
            bool isVoidLava = VoidLavaVisible && tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Lava;

            if (!isVoidLava)
            {
                orig(self, tile, ref lightColor);
                return;
            }

            float glow = 0.55f + (270 - Main.mouseTextColor) / 900f;
            Vector3 voidGlow = VoidLightTint * glow;

            lightColor = Vector3.Max(lightColor, voidGlow);
        }

        // Lavafalls are waterfall style 1. Vanilla: (0.55 + pulse) * 0.4 in red, 0.3/0.1 of that in G/B.
        private static void ApplyVoidLavafallLight(On_WaterfallManager.orig_AddLight orig, int waterfallType, int x, int y)
        {
            if (!VoidLavaVisible || waterfallType != 1)
            {
                orig(waterfallType, x, y);
                return;
            }

            float glow = (0.55f + (270 - Main.mouseTextColor) / 900f) * 0.4f;
            Vector3 voidGlow = VoidLightTint * glow;

            Lighting.AddLight(x, y, voidGlow.X, voidGlow.Y, voidGlow.Z);
        }

        // Lava you chart while Void is visible is stored with the Void map colour, so the
        // ice biome's lava stays purple on the map after you leave. Map saves keep it too.
        private static MapTile ChartVoidLavaOnMap(On_MapHelper.orig_CreateMapTile orig, int i, int j, byte light)
        {
            MapTile mapTile = orig(i, j, light);

            if (!VoidLavaVisible || mapTile.Type != vanillaLavaMapType)
            {
                return mapTile;
            }

            ushort voidMapType = (ushort)MapHelper.TileToLookup(ModContent.TileType<VoidLavaMapTile>(), 0);

            return MapTile.Create(voidMapType, mapTile.Light, mapTile.Color);
        }

        // Lava bubble dust (type 35) adds orange light in Dust.UpdateDust:
        //   brightness = scale * 0.3 + 0.4;  Lighting.AddLight(x, y, b, b*0.5, b*0.3)
        // Swap that one AddLight call for AddLavaDustLight. Cosmetic, so a miss only logs.
        private void PatchLavaDustLight(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            bool foundBrightness = cursor.TryGotoNext(MoveType.After,
                instruction => instruction.MatchLdcR4(0.3f),
                instruction => instruction.MatchMul(),
                instruction => instruction.MatchLdcR4(0.4f),
                instruction => instruction.MatchAdd());

            bool foundLightCall = foundBrightness && cursor.TryGotoNext(MoveType.Before,
                instruction => instruction.MatchCall(typeof(Lighting), nameof(Lighting.AddLight)));

            if (!foundLightCall)
            {
                Mod.Logger.Warn("VoidLava: lava dust light IL not found, lava bubbles keep their orange glow.");
                return;
            }

            cursor.Remove();
            cursor.Emit(OpCodes.Call, typeof(VoidLavaSystem).GetMethod(nameof(AddLavaDustLight), BindingFlags.NonPublic | BindingFlags.Static));
        }

        // Lava drip gore (716) adds orange light in Gore.Update:
        //   r,g,b = 1,1,1;  pulse = 0.6 * frame curve;  Lighting.AddLight(pos, pulse, pulse*0.5, pulse*0.1)
        // The 1,1,1,0.6 local setup is unique to that block. Cosmetic, so a miss only logs.
        private void PatchLavaDripLight(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            bool foundSetup = cursor.TryGotoNext(MoveType.After,
                instruction => instruction.MatchLdcR4(1f),
                instruction => instruction.MatchStloc(out _),
                instruction => instruction.MatchLdcR4(1f),
                instruction => instruction.MatchStloc(out _),
                instruction => instruction.MatchLdcR4(1f),
                instruction => instruction.MatchStloc(out _),
                instruction => instruction.MatchLdcR4(0.6f));

            bool foundLightCall = foundSetup && cursor.TryGotoNext(MoveType.Before,
                instruction => instruction.MatchCall(typeof(Lighting), nameof(Lighting.AddLight)));

            if (!foundLightCall)
            {
                Mod.Logger.Warn("VoidLava: lava drip light IL not found, lava drips keep their orange glow.");
                return;
            }

            cursor.Remove();
            cursor.Emit(OpCodes.Call, typeof(VoidLavaSystem).GetMethod(nameof(AddLavaDripLight), BindingFlags.NonPublic | BindingFlags.Static));
        }

        // Stand-in for the lava dust's Lighting.AddLight(int, int, r, g, b). Red carries the brightness.
        private static void AddLavaDustLight(int x, int y, float red, float green, float blue)
        {
            if (!VoidLavaVisible)
            {
                Lighting.AddLight(x, y, red, green, blue);
                return;
            }

            Vector3 voidGlow = VoidLightTint * red;
            Lighting.AddLight(x, y, voidGlow.X, voidGlow.Y, voidGlow.Z);
        }

        // Stand-in for the lava drip's Lighting.AddLight(Vector2, r, g, b). Red carries the brightness.
        private static void AddLavaDripLight(Vector2 position, float red, float green, float blue)
        {
            if (!VoidLavaVisible)
            {
                Lighting.AddLight(position, red, green, blue);
                return;
            }

            Vector3 voidGlow = VoidLightTint * red;
            Lighting.AddLight(position, voidGlow.X, voidGlow.Y, voidGlow.Z);
        }
    }


    // ──────────────────────────────────────────────────────────────────────────
    //  3. PLAYER EFFECT  —  Void ignores lava immunity
    //     PostUpdateMiscEffects runs after buffs, equips and armor sets have all
    //     granted their lava protection, and before movement does lava collision,
    //     so clearing the flags here makes vanilla's own lava damage apply in full.
    //     Runs on the owning client, which is also where vanilla deals lava damage.
    // ──────────────────────────────────────────────────────────────────────────
    public class VoidLavaPlayer : ModPlayer
    {
        // Void hits this many times harder than vanilla lava (80 base, 200 in Remix).
        public const float VoidDamageMultiplier = 4f;

        public override void PostUpdateMiscEffects()
        {
            if (!VoidLavaCondition.IsActive(Player))
            {
                return;
            }

            // lavaImmune: Obsidian Skin, Lava Waders, etc. lavaMax: Lava Charm's grace
            // timer (vanilla clamps lavaTime to it). lavaRose/ashWoodBonus: damage cuts.
            Player.lavaImmune = false;
            Player.lavaMax = 0;
            Player.lavaRose = false;
            Player.ashWoodBonus = false;

            // No standing on liquid here. waterWalk = water + lava (Lava Waders, Supersonic
            // speed perk, etc.), waterWalk2 = water only (Water Walking Boots/potion).
            // Every source sets these in buffs/accessories, all before this hook.
            Player.waterWalk = false;
            Player.waterWalk2 = false;

            // Obsidian Skin also grants On Fire! immunity, which would cancel the burn
            // vanilla lava applies. Only lift it while actually touching Void lava.
            bool touchingLava = Collision.LavaCollision(Player.position, Player.width, Player.height);

            if (touchingLava)
            {
                Player.buffImmune[BuffID.OnFire] = false;
            }
        }

        // Vanilla lava contact damage is Hurt(ByOther(2), ...) on the owning client.
        // SourceDamage scales it before defense, same as a stronger hit would.
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            bool isLavaHit = modifiers.DamageSource != null && modifiers.DamageSource.SourceOtherIndex == 2;

            if (isLavaHit && VoidLavaCondition.IsActive(Player))
            {
                modifiers.SourceDamage *= VoidDamageMultiplier;
            }
        }

        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
        {
            // Vanilla lava damage uses "other" reason 2 ("tried to swim in lava").
            bool diedToLava = damageSource.SourceOtherIndex == 2;

            if (diedToLava && VoidLavaCondition.IsActive(Player))
            {
                damageSource = PlayerDeathReason.ByCustomReason(NetworkText.FromKey(
                    "Mods.tsorcRevamp.DeathText.VoidDeathReason", Player.name));
            }

            return true;
        }
    }


    // ──────────────────────────────────────────────────────────────────────────
    //  4. MAP ENTRY  —  supplies the Void colour + "Void" hover name on the map
    //     Never placed in the world. Mods can only add map colours through a tile's
    //     map entry, and a modded entry also survives map save/load by name.
    //     Texture is a permanent borrow: this tile is never drawn.
    // ──────────────────────────────────────────────────────────────────────────
    public class VoidLavaMapTile : ModTile
    {
        public override string Texture => "Terraria/Images/Tiles_" + TileID.AmethystGemspark;

        public override void SetStaticDefaults()
        {
            AddMapEntry(new Color(125, 40, 210), CreateMapEntryName());
        }
    }
}
