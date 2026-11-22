using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.VesselOfSouls;

namespace tsorcRevamp.Tiles
{
    public class tsorcGlobalTile : GlobalTile
    {
        public override bool PreDraw(int i, int j, int type, SpriteBatch spriteBatch)
        {
            // Keep platforms as readable arena geometry. This is render-only: collision, framing,
            // world saves, and multiplayer tile state are not changed.
            if (VesselOfSoulsFadeSystem.WorldHidden && !TileID.Sets.Platforms[type])
                return false;

            return base.PreDraw(i, j, type, spriteBatch);
        }

        public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && !tsorcRevamp.DroppableTiles.Contains(type) && !tsorcRevamp.PlaceAllowedModTiles.Contains(type) && !tsorcRevamp.PlaceAllowed.Contains(type))
            {
                noItem = true;
            }
            base.KillTile(i, j, type, ref fail, ref effectOnly, ref noItem);
        }

        public override void ModifyLight(int i, int j, int type, ref float r, ref float g, ref float b)
        {
            // Rainbow Brick is a plain white brick here. Vanilla gives it a 0.25-strength Main.Disco colour-cycling
            // light; zero it. The matching draw-colour fix is ILEdits.RainbowBrick_NoColorCycle.
            if (type == TileID.RainbowBrick)
            {
                r = 0f;
                g = 0f;
                b = 0f;
            }
        }
    }

    // Spike and Wooden Spike touch damage, tiered by world stage. Vanilla reads these from the global
    // TileID.Sets.TouchDamageImmediate table when a player touches the tile, so rewriting the two entries changes
    // only these tiles (cactus, thorns, hot tiles keep their own entries). PreUpdatePlayers runs on server and every
    // client before player movement; Main.hardMode and SuperHardMode are synced world flags, so all machines agree.
    public class SpikeDamageSystem : ModSystem
    {
        public const int VanillaSpikeDamage = 60;
        public const int VanillaWoodenSpikeDamage = 80;

        public const int SpikeDamageHardmode = 120;
        public const int SpikeDamageSuperHardmode = 180;

        public const int WoodenSpikeDamagePreHardmode = 100;
        public const int WoodenSpikeDamageHardmode = 200;
        public const int WoodenSpikeDamageSuperHardmode = 300;

        public override void PreUpdatePlayers()
        {
            // Pre-Hardmode Spikes stay at vanilla 60.
            int spikeDamage = VanillaSpikeDamage;
            int woodenSpikeDamage = WoodenSpikeDamagePreHardmode;

            if (tsorcRevampWorld.SuperHardMode)
            {
                spikeDamage = SpikeDamageSuperHardmode;
                woodenSpikeDamage = WoodenSpikeDamageSuperHardmode;
            }
            else if (Main.hardMode)
            {
                spikeDamage = SpikeDamageHardmode;
                woodenSpikeDamage = WoodenSpikeDamageHardmode;
            }

            TileID.Sets.TouchDamageImmediate[TileID.Spikes] = spikeDamage;
            TileID.Sets.TouchDamageImmediate[TileID.WoodenSpikes] = woodenSpikeDamage;
        }

        // The table is vanilla's own static set, so put the vanilla numbers back when the mod unloads.
        public override void Unload()
        {
            TileID.Sets.TouchDamageImmediate[TileID.Spikes] = VanillaSpikeDamage;
            TileID.Sets.TouchDamageImmediate[TileID.WoodenSpikes] = VanillaWoodenSpikeDamage;
        }
    }

    public class VesselVoidGlobalWall : GlobalWall
    {
        public override bool PreDraw(int i, int j, int type, SpriteBatch spriteBatch)
        {
            return !VesselOfSoulsFadeSystem.WorldHidden;
        }
    }

    public class tsorcGlobalWall : GlobalWall
    {
        public override void ModifyLight(int i, int j, int type, ref float r, ref float g, ref float b)
        {
            // Rainbow Brick Wall is a plain white wall here. Vanilla gives it a 0.15-strength Main.Disco colour-cycling
            // light; zero it. The matching draw-colour fix is ILEdits.RainbowBrickWall_NoColorCycle.
            if (type == WallID.RainbowBrick)
            {
                r = 0f;
                g = 0f;
                b = 0f;
            }
        }
    }
}
