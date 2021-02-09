using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    public abstract class Titanite : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A rare and valuable ore.");
        }

        public override void SetDefaults() {
            item.width = 16;
            item.height = 16;
            item.rare = ItemRarityID.LightRed;
            item.maxStack = 99;
        }
    }

    public class BlueTitanite : Titanite {
        public override void SetStaticDefaults() {
            base.SetStaticDefaults();
        }

        public override void SetDefaults() {
            base.SetDefaults();
        }
    }

    public class RedTitanite : Titanite {
        public override void SetStaticDefaults() {
            base.SetStaticDefaults();
        }

        public override void SetDefaults() {
            base.SetDefaults();
        }
    }

    public class WhiteTitanite : Titanite {
        public override void SetStaticDefaults() {
            base.SetStaticDefaults();
        }

        public override void SetDefaults() {
            base.SetDefaults();
        }
    }
}
