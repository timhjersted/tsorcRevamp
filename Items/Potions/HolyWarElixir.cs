using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions {
    class HolyWarElixir : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Will make you invincible.");
        }
        public override void SetDefaults() {
            item.height = 62;
            item.consumable = true;
            item.height = 34;
            item.maxStack = 30;
            item.rare = ItemRarityID.Pink;
            item.useAnimation = 17;
            item.UseSound = SoundID.Item3;
            item.useStyle = ItemUseStyleID.EatingUsing;
            item.useTime = 17;
            item.useTurn = true;
            item.value = 0;
            item.width = 14;
            item.buffType = ModContent.BuffType<Buffs.Invincible>();
            item.buffTime = 600;
        }
    }
}
