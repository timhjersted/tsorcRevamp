// ═══════════════════════════════════════════════════════════════════════════════
//  POISON SWAMP WATER  —  Liquids/PoisonSwampWater.cs
//
//  Turns standing water green and applies Poison to submerged players whenever
//  the "swamp condition" below is met.  The pieces live here so the system
//  is easy to find, edit, or rip out:
//
//    1.  IsInPoisonSwamp()  — single place to change WHEN it activates
//    2.  PoisonSwampWaterStyle    — green liquid texture (lava spritesheet, tinted)
//    3.  PoisonSwampSceneEffect   — wires condition → water style each frame
//    4.  PoisonSwampPlayerEffect  — applies Poison buff while player swims in it
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Xna.Framework;
using System;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Liquids
{
    // ──────────────────────────────────────────────────────────────────────────
    //  1. SWAMP CONDITION
    //     Change this one method to change every rule at once.
    //     Active in the Jungle/Dungeon overlap or the underground Jungle.
    //     The Dungeon zone uses our reduced-tile-count detection from
    //     MethodSwaps.cs (Player_InZonePurity), so the Forgotten City qualifies.
    //
    //     Future option: scan for a specific wall type instead:
    //       return player.ZoneJungle &&
    //              UsefulFunctions.NearWallType(player, WallID.MudWall, radius);
    // ──────────────────────────────────────────────────────────────────────────
    internal static class PoisonSwampCondition
    {
        internal static bool IsUndergroundJungle(Player player) =>
            player.ZoneJungle && (player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight);

        internal static bool IsActive(Player player)
        {
            return (player.ZoneJungle && player.ZoneDungeon) || IsUndergroundJungle(player);
        }
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
    //  4. PLAYER EFFECT  —  poison buildup while swimming in swamp water
    //     Runs every tick for the local player only.  Adds BuildupPerTick to the
    //     Poison Buildup meter (Buffs/Debuffs/PoisonBuildup.cs); a full meter
    //     poisons for 60s.  Poison immunity (Bezoar etc.) blocks the buildup.
    // ──────────────────────────────────────────────────────────────────────────
    public class PoisonSwampPlayerEffect : ModPlayer
    {
        // 1 per tick = a full meter after ~1.7s of continuous contact.
        public const int BuildupPerTick = 1;
        private int slowJungleWaterTicks;

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer) return;

            bool inSwampWater = PoisonSwampCondition.IsActive(Player)
                && Player.wet
                && !Player.lavaWet
                && !Player.honeyWet
                && !Player.shimmerWet;

            if (!inSwampWater)
            {
                slowJungleWaterTicks = 0;
                return;
            }

            // Underground Jungle water outside the Dungeon builds poison at one third the usual rate.
            if (PoisonSwampCondition.IsUndergroundJungle(Player) && !Player.ZoneDungeon)
            {
                if (++slowJungleWaterTicks < 3)
                    return;

                slowJungleWaterTicks = 0;
            }
            else
            {
                slowJungleWaterTicks = 0;
            }

            Buffs.Debuffs.PoisonBuildup.Apply(Player, BuildupPerTick);
        }
    }


    // ──────────────────────────────────────────────────────────────────────────
    //  5. SURFACE BUBBLES + ACID WATER LOOP
    //     Scans a small region around the local player every few ticks for water
    //     tiles whose top edge is open air, then fires green rising-dust particles
    //     to replicate the "lava surface bubbling" look.  The same scan finds the
    //     nearest swamp water, which sets the volume of the acid water loop.
    //
    //     Tuning knobs:
    //       ScanHalfWidth / ScanHalfHeight — tile search box around the player
    //       BubbleChance   — 1-in-N chance per open-surface water tile per tick
    //       Interval       — only runs every N game ticks (60 = once per second)
    //       AcidHearingTiles / AcidMaxVolume / AcidFadePerTick — the loop
    // ──────────────────────────────────────────────────────────────────────────
    public class PoisonSwampBubbleSystem : ModSystem
    {
        private const int ScanHalfWidth  = 30;   // tiles left/right of player
        private const int ScanHalfHeight = 20;   // tiles above/below player
        private const int BubbleChance   = 10;   // 1-in-N per surface tile per interval
        private const int Interval        = 5;   // run every N ticks

        // Acid loop: full volume touching swamp water, silent AcidHearingTiles (20) tiles away - inside the scan box, so the
        // scan always sees the water that sets it. The volume glides toward its target by AcidFadePerTick, so walking in
        // or out fades over ~1s instead of jumping on each 5-tick scan. Ambient type: the ambient slider scales it.
        private static readonly SoundStyle AcidLoopStyle = new SoundStyle("tsorcRevamp/Sounds/HollowKnight/acid_water_loop", SoundType.Ambient) with { IsLooped = true, MaxInstances = 0 };
        private const float AcidHearingTiles = 20f;
        private const float AcidMaxVolume = 0.6f;
        private const float AcidFadePerTick = 1f / 60f;

        private static int _timer;
        private static float acidTargetVolume;
        private static float acidVolume;
        private static SlotId acidSlot = SlotId.Invalid;

        public override void OnWorldUnload()
        {
            if (SoundEngine.TryGetActiveSound(acidSlot, out ActiveSound acidSound))
            {
                acidSound.Stop();
            }

            acidSlot = SlotId.Invalid;
            acidVolume = 0f;
            acidTargetVolume = 0f;
        }

        public override void PostUpdateEverything()
        {
            if (Main.dedServ)
            {
                return;
            }

            _timer++;

            if (_timer >= Interval)
            {
                _timer = 0;

                Player player = Main.LocalPlayer;
                acidTargetVolume = 0f;

                if (PoisonSwampCondition.IsActive(player))
                {
                    int playerTileX = (int)(player.Center.X / 16f);
                    int playerTileY = (int)(player.Center.Y / 16f);

                    int left = Math.Max(0, playerTileX - ScanHalfWidth);
                    int right = Math.Min(Main.maxTilesX - 1, playerTileX + ScanHalfWidth);
                    int top = Math.Max(1, playerTileY - ScanHalfHeight);
                    int bottom = Math.Min(Main.maxTilesY - 1, playerTileY + ScanHalfHeight);

                    // Squared tile distance to the nearest water tile; MaxValue = none found.
                    float nearestWaterDistanceSquared = float.MaxValue;

                    for (int x = left; x <= right; x++)
                    {
                        for (int y = top; y <= bottom; y++)
                        {
                            Tile tile = Main.tile[x, y];

                            // Must be a water tile (liquidType 0) with liquid present
                            if (tile.LiquidAmount <= 0 || tile.LiquidType != LiquidID.Water)
                            {
                                continue;
                            }

                            float offsetX = x - playerTileX;
                            float offsetY = y - playerTileY;
                            float distanceSquared = offsetX * offsetX + offsetY * offsetY;

                            if (distanceSquared < nearestWaterDistanceSquared)
                            {
                                nearestWaterDistanceSquared = distanceSquared;
                            }

                            // Top neighbour must be air (open water surface)
                            Tile above = Main.tile[x, y - 1];

                            if (above.HasTile || above.LiquidAmount > 0)
                            {
                                continue;
                            }

                            if (!Main.rand.NextBool(BubbleChance))
                            {
                                continue;
                            }

                            // Spawn a rising green bubble-dust just above the water surface
                            Vector2 bubblePosition = new Vector2(x * 16 + Main.rand.Next(16), (y - 1) * 16 + Main.rand.Next(8));
                            float riseSpeed = Main.rand.NextFloat(-1.8f, -0.6f);
                            int dustIndex = Dust.NewDust(bubblePosition, 4, 4, DustID.Poisoned, 0f, riseSpeed, 80, default, Main.rand.NextFloat(0.7f, 1.3f));
                            Main.dust[dustIndex].noGravity = true;
                            Main.dust[dustIndex].fadeIn = Main.rand.NextFloat(0.4f, 0.8f);
                        }
                    }

                    if (nearestWaterDistanceSquared < float.MaxValue)
                    {
                        float nearestWaterDistance = (float)Math.Sqrt(nearestWaterDistanceSquared);
                        float closeness = MathHelper.Clamp(1f - nearestWaterDistance / AcidHearingTiles, 0f, 1f);
                        acidTargetVolume = closeness * AcidMaxVolume;
                    }
                }
            }

            // Every tick: glide toward the target, start the loop when it becomes audible, stop it once fully faded.
            float fadeStep = AcidFadePerTick * AcidMaxVolume;

            if (acidVolume < acidTargetVolume)
            {
                acidVolume = MathHelper.Min(acidVolume + fadeStep, acidTargetVolume);
            }
            else if (acidVolume > acidTargetVolume)
            {
                acidVolume = MathHelper.Max(acidVolume - fadeStep, acidTargetVolume);
            }

            bool acidPlaying = SoundEngine.TryGetActiveSound(acidSlot, out ActiveSound acidSound);

            if (acidVolume > 0f)
            {
                if (!acidPlaying)
                {
                    acidSlot = SoundEngine.PlaySound(AcidLoopStyle);
                    acidPlaying = SoundEngine.TryGetActiveSound(acidSlot, out acidSound);
                }

                if (acidPlaying)
                {
                    acidSound.Volume = acidVolume;
                }
            }
            else if (acidPlaying)
            {
                acidSound.Stop();
                acidSlot = SlotId.Invalid;
            }
        }
    }
}
