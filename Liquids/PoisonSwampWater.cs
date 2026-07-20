// ═══════════════════════════════════════════════════════════════════════════════
//  POISON SWAMP WATER  —  Liquids/PoisonSwampWater.cs
//
//  Turns standing water green and applies Poison to submerged players whenever
//  the "swamp condition" below is met.  All four pieces live here so the system
//  is easy to find, edit, or rip out:
//
//    1.  IsInPoisonSwamp()  — single place to change WHEN it activates
//    2.  PoisonSwampWaterStyle    — green liquid texture (lava spritesheet, tinted)
//    3.  PoisonSwampSceneEffect   — wires condition → water style each frame
//    4.  PoisonSwampPlayerEffect  — applies Poison buff while player swims in it
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Liquids
{
    // ──────────────────────────────────────────────────────────────────────────
    //  1. SWAMP CONDITION
    //     Change this one method to change every rule at once.
    //     Current rule: player is inside both a Jungle and Dungeon biome.
    //     The Dungeon zone uses our reduced-tile-count detection from
    //     MethodSwaps.cs (Player_InZonePurity), so the Forgotten City qualifies.
    //
    //     Future option: scan for a specific wall type instead:
    //       return player.ZoneJungle &&
    //              UsefulFunctions.NearWallType(player, WallID.MudWall, radius);
    // ──────────────────────────────────────────────────────────────────────────
    internal static class PoisonSwampCondition
    {
        internal static bool IsActive(Player player) =>
            player.ZoneJungle && player.ZoneDungeon;
    }


    // ──────────────────────────────────────────────────────────────────────────
    //  2. WATER STYLE  —  the green visual
    //     Texture: Liquids/PoisonSwampWater.png, generated once via
    //     /extractpoisontex (see Commands/ExtractPoisonWaterTexture.cs).
    //     It is vanilla's lava spritesheet pixel-shifted to green, so water
    //     tiles show the same bubble/surface animation as lava.
    //
    //     Tuning knobs:
    //       ChooseWaterfallStyle() — waterfall drawn above this water
    //       GetSplashDust()        — dust when something enters the liquid
    //       GetDropletGore()       — drip particle falling off block edges
    // ──────────────────────────────────────────────────────────────────────────
    public class PoisonSwampWaterStyle : ModWaterStyle
    {
        // Points to Liquids/PoisonSwampWater.png in the mod source tree.
        public override string Texture => "tsorcRevamp/Liquids/PoisonSwampWater";

        // Use the default water waterfall (index 0 = clear).
        // Change to e.g. 2 (corrupt) for a darker atmospheric fall.
        public override int ChooseWaterfallStyle() => 0;

        public override int GetSplashDust() => DustID.Poisoned;
        public override int GetDropletGore() => GoreID.LavaDrip;

        // Tint the tile lighting behind the water green so the water reads clearly
        // as toxic even when the surface texture animation is ambiguous.
        // Values > 1 boost; values < 1 suppress.  Net effect: rich green cast.
        public override void LightColorMultiplier(ref float r, ref float g, ref float b)
        {
            r = 0.40f;
            g = 1.10f;
            b = 0.40f;
        }
    }


    // ──────────────────────────────────────────────────────────────────────────
    //  3. SCENE EFFECT  —  activates the water style based on the condition
    //     BiomeHigh beats all vanilla biome water styles (jungle, corrupt, etc).
    //     Lower to BiomeMedium if you want hallow/crimson fountains to override.
    // ──────────────────────────────────────────────────────────────────────────
    public class PoisonSwampSceneEffect : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BiomeHigh;

        public override ModWaterStyle WaterStyle => ModContent.GetInstance<PoisonSwampWaterStyle>();

        public override bool IsSceneEffectActive(Player player) =>
            PoisonSwampCondition.IsActive(player);
    }


    // ──────────────────────────────────────────────────────────────────────────
    //  4. PLAYER EFFECT  —  debuff while swimming in swamp water
    //     Runs every tick for the local player only.  AddBuff handles immunity
    //     (Obsidian Skin, Ankh Shield, etc.) automatically.
    //
    //     Tuning knobs:
    //       debuffDuration  — ticks added per frame (refreshes timer each tick,
    //                         so the debuff lingers this many frames after exiting)
    //       SuperHardMode   — escalates Poison → Venom once SuperHardMode is active
    // ──────────────────────────────────────────────────────────────────────────
    public class PoisonSwampPlayerEffect : ModPlayer
    {
        // 120 ticks = 2 seconds of lingering debuff after exiting the water.
        private const int debuffDuration = 120;

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer) return;

            bool inSwampWater = PoisonSwampCondition.IsActive(Player)
                && Player.wet
                && !Player.lavaWet
                && !Player.honeyWet
                && !Player.shimmerWet;

            if (!inSwampWater) return;

            // SuperHardMode escalates to Venom; otherwise plain Poison.
            int debuff = tsorcRevampWorld.SuperHardMode ? BuffID.Venom : BuffID.Poisoned;
            Player.AddBuff(debuff, debuffDuration);
        }
    }


    // ──────────────────────────────────────────────────────────────────────────
    //  5. SURFACE BUBBLE SYSTEM
    //     Scans a small region around the local player every few ticks for water
    //     tiles whose top edge is open air, then fires green rising-dust particles
    //     to replicate the "lava surface bubbling" look.
    //
    //     Tuning knobs:
    //       ScanHalfWidth / ScanHalfHeight — tile search box around the player
    //       BubbleChance   — 1-in-N chance per open-surface water tile per tick
    //       Interval       — only runs every N game ticks (60 = once per second)
    // ──────────────────────────────────────────────────────────────────────────
    public class PoisonSwampBubbleSystem : ModSystem
    {
        private const int ScanHalfWidth  = 30;   // tiles left/right of player
        private const int ScanHalfHeight = 20;   // tiles above/below player
        private const int BubbleChance   = 10;   // 1-in-N per surface tile per interval
        private const int Interval        = 5;   // run every N ticks

        private static int _timer;

        public override void PostUpdateEverything()
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server) return;

            _timer++;
            if (_timer < Interval) return;
            _timer = 0;

            Player player = Main.LocalPlayer;
            if (!PoisonSwampCondition.IsActive(player)) return;

            int cx = (int)(player.Center.X / 16f);
            int cy = (int)(player.Center.Y / 16f);

            int x0 = Math.Max(0, cx - ScanHalfWidth);
            int x1 = Math.Min(Main.maxTilesX - 1, cx + ScanHalfWidth);
            int y0 = Math.Max(1, cy - ScanHalfHeight);
            int y1 = Math.Min(Main.maxTilesY - 1, cy + ScanHalfHeight);

            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    Tile tile = Main.tile[x, y];
                    // Must be a water tile (liquidType 0) with liquid present
                    if (tile.LiquidAmount <= 0 || tile.LiquidType != LiquidID.Water) continue;
                    // Top neighbour must be air (open water surface)
                    Tile above = Main.tile[x, y - 1];
                    if (above.HasTile || above.LiquidAmount > 0) continue;

                    if (!Main.rand.NextBool(BubbleChance)) continue;

                    // Spawn a rising green bubble-dust just above the water surface
                    Vector2 pos = new Vector2(x * 16 + Main.rand.Next(16), (y - 1) * 16 + Main.rand.Next(8));
                    int d = Dust.NewDust(pos, 4, 4, DustID.Poisoned, 0f, Main.rand.NextFloat(-1.8f, -0.6f), 80, default, Main.rand.NextFloat(0.7f, 1.3f));
                    Main.dust[d].noGravity = true;
                    Main.dust[d].fadeIn = Main.rand.NextFloat(0.4f, 0.8f);
                }
            }
        }
    }
}
