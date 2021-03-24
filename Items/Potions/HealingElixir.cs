using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions {
    class HealingElixir : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Heals the player of all bleeding, poisoned, confused and broken armor debuffs." + 
                                "\nGrants the life regeneration buff.");
        }
        public override void SetDefaults() {
            item.width = 16;
            item.height = 16;
            item.consumable = true;
            item.useAnimation = 30;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 45;
            item.maxStack = 200;
            item.scale = 1;
            item.rare = ItemRarityID.Orange;
            item.value = 500000;
            item.buffType = BuffID.Regeneration;
            item.buffTime = 7200;
        }

        public override bool UseItem(Player player) {
            int buffIndex = 0;

            foreach (int buffType in player.buffType) {

                if ((buffType == BuffID.Bleeding) || (buffType == BuffID.Poisoned) || (buffType == BuffID.Confused) || (buffType == BuffID.BrokenArmor)) {
                    player.DelBuff(buffIndex);
                }
                buffIndex++;
            }
            return true;
        }

    }
}
