using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using tsorcRevamp.Items;

namespace tsorcRevamp {
    public class tsorcRevampPlayer : ModPlayer {

        //public bool GreatMagicMirror;

        public int warpX;
        public int warpY;
        public int warpWorld;
        public bool warpSet;

        public int townWarpX;
        public int townWarpY;
        public int townWarpWorld;
        public bool townWarpSet;

        public bool SilverSerpentRing = true;

        public override TagCompound Save() {
            return new TagCompound {
            {"warpX", warpX},
            {"warpY", warpY},
            {"warpWorld", warpWorld},
            {"warpSet", warpSet},
            {"townWarpX", townWarpX},
            {"townWarpY", townWarpY},
            {"townWarpWorld", townWarpWorld},
            {"townWarpSet", townWarpSet},
            };

        }

        public override void Load(TagCompound tag) {
            warpX = tag.GetInt("warpX");
            warpY = tag.GetInt("warpY");
            warpWorld = tag.GetInt("warpWorld");
            warpSet = tag.GetBool("warpSet");
            townWarpX = tag.GetInt("townWarpX");
            townWarpY = tag.GetInt("townWarpY");
            townWarpWorld = tag.GetInt("townWarpWorld");
            townWarpSet = tag.GetBool("townWarpSet");
        }

        public override void ResetEffects() {
            SilverSerpentRing = false;
        }

    }
}
