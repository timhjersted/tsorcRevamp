using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions {
    public class StrengthPotion : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Increases damage by 15%, critical strike chance " +
                             "\nby 2%, defense by 15, and swing speed by 15%.");

        }

        public override void SetDefaults() {
            item.width = 20;
            item.height = 26;
            item.useStyle = ItemUseStyleID.EatingUsing;
            item.useAnimation = 15;
            item.useTime = 15;
            item.useTurn = true;
            item.UseSound = SoundID.Item3;
            item.maxStack = 30;
            item.consumable = true;
            item.rare = ItemRarityID.Blue;
            item.value = 1000;
            item.buffType = ModContent.BuffType<Buffs.Strength>();
            item.buffTime = 36000;
        }
    }
}
