using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Gores
{
    ///<summary>
    ///The dust clouds Cataclysm Dive throws up: three on the impact itself, then a steady dribble that trails
    ///each ground shockwave as it crawls outward (see ChaosShockwave).
    ///
    ///No new art. These reuse the mod's existing "Cloud Gore 1/2/3" sheets through a Texture override and only
    ///change how they are coloured — GetAlpha is ModGore's tint hook, which is the one thing a bare autoloaded
    ///gore cannot do, since vanilla Gore has no per-instance colour field at all.
    ///</summary>
    public abstract class ChaosImpactCloud : ModGore
    {
        public override void SetStaticDefaults()
        {
            //Drift and thin out like smoke rather than tumbling like debris.
            UpdateType = Terraria.ID.GoreID.Smoke1;
        }

        public override Color? GetAlpha(Gore gore, Color lightColor)
        {
            //Dark blue instead of the lit white the cloud sheets normally draw as: this is floor torn up by
            //something enormous landing on it, not a puff of steam. Fade rides the gore's own alpha so it
            //still dissolves on vanilla's schedule.
            float fade = 1f - gore.alpha / 255f;
            return new Color(52, 72, 132) * fade;
        }
    }

    public class ChaosImpactCloud1 : ChaosImpactCloud
    {
        public override string Texture => "tsorcRevamp/Gores/Cloud Gore 1";
    }

    public class ChaosImpactCloud2 : ChaosImpactCloud
    {
        public override string Texture => "tsorcRevamp/Gores/Cloud Gore 2";
    }

    public class ChaosImpactCloud3 : ChaosImpactCloud
    {
        public override string Texture => "tsorcRevamp/Gores/Cloud Gore 3";
    }
}
