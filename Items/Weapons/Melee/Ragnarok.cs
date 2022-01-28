using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class Ragnarok : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Ragnarok");

        }

        public override void SetDefaults() {
            item.stack = 1;
            item.rare = ItemRarityID.Cyan;
            item.useTurn = true;
            item.autoReuse = true;
            item.damage = 170;
            item.knockBack = 10;
            item.scale = 0.8f;
            item.maxStack = 1;
            item.melee = true;
            item.scale = (float)1.3;
            item.useAnimation = 15;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = PriceByRarity.Cyan_9;
        }
    }
}
