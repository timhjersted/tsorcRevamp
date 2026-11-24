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
}
