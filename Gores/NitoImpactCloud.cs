using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Gores
{
    ///<summary>
    ///Dark-gray smoke gores kicked up by Gravelord Nito's Overhead Cleave slam cloud (see NitoSlamCloud).
    ///Reuses the mod's Cloud Gore 1/2/3 sheets via Texture override and tints them through GetAlpha,
    ///mirroring ChaosImpactCloud without adding new texture assets.
    ///</summary>
    public abstract class NitoImpactCloud : ModGore
    {
        public override void SetStaticDefaults()
        {
            // Drift and thin out like smoke rather than tumbling like solid debris.
            UpdateType = Terraria.ID.GoreID.Smoke1;
        }

        public override Color? GetAlpha(Gore gore, Color lightColor)
        {
            // Dark ash-gray floor dust torn up by Nito's heavy greatsword slam; alpha fade follows vanilla's smoke schedule.
            float fade = 1f - gore.alpha / 255f;
            return new Color(58, 56, 62) * fade;
        }
    }

    public class NitoImpactCloud1 : NitoImpactCloud
    {
        public override string Texture => "tsorcRevamp/Gores/Cloud Gore 1";
    }

    public class NitoImpactCloud2 : NitoImpactCloud
    {
        public override string Texture => "tsorcRevamp/Gores/Cloud Gore 2";
    }

    public class NitoImpactCloud3 : NitoImpactCloud
    {
        public override string Texture => "tsorcRevamp/Gores/Cloud Gore 3";
    }

    ///<summary>
    ///Black smoke flung out of Barrowlord Ossric's body when he dies (GravelordNito.RunDying). Flies straight
    ///out with drag, spins, ignores tiles and fades to nothing over LifeTicks. Spawn speed decides reach:
    ///with 0.96 drag the total travel is about 24.4 x the starting speed, so 13.1 px/tick covers 20 tiles.
    ///</summary>
    public class NitoDeathSmoke : ModGore
    {
        public const int LifeTicks = 90;
        public const float Drag = 0.96f;
        public const float SpeedFor20Tiles = 13.1f;

        public override string Texture => "tsorcRevamp/Gores/DarkSmoke";

        public override void OnSpawn(Gore gore, Terraria.DataStructures.IEntitySource source)
        {
            gore.timeLeft = LifeTicks;
            gore.alpha = 0;
        }

        // Returning false skips vanilla's update, so there is no gravity and no tile collision.
        public override bool Update(Gore gore)
        {
            gore.timeLeft--;
            if (gore.timeLeft <= 0)
            {
                gore.active = false;
                return false;
            }

            gore.position += gore.velocity;
            gore.velocity *= Drag;

            float spinDirection = 1f;
            if (gore.velocity.X < 0f)
            {
                spinDirection = -1f;
            }
            gore.rotation += spinDirection * 0.11f;
            gore.scale += 0.004f;

            // Fade accelerates toward the end: mostly solid early, gone by LifeTicks.
            float age = (LifeTicks - gore.timeLeft) / (float)LifeTicks;
            gore.alpha = (int)(255f * age * age);
            return false;
        }

        // Full brightness regardless of lighting (he dies in a dark dungeon); the sprite's own colours are black.
        public override Color? GetAlpha(Gore gore, Color lightColor)
        {
            float fade = 1f - gore.alpha / 255f;
            return Color.White * fade;
        }
    }
}
