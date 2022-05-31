using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions {
    class HolyWarElixir : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Will make you invincible.\nCannot be used again for 60 seconds after wearing off.")
                ;
        }

        public override void SetDefaults() {
            Item.height = 62;
            Item.consumable = true;
            Item.height = 34;
            Item.maxStack = 30;
            Item.rare = ItemRarityID.Pink;
            Item.useAnimation = 17;
            Item.UseSound = SoundID.Item3;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.value = 0;
            Item.width = 14;
            Item.buffType = ModContent.BuffType<Buffs.Invincible>();
            Item.buffTime = 600;
        }

        public override bool CanUseItem(Player player) {
            if (player.HasBuff(ModContent.BuffType<Buffs.ElixirCooldown>())) {
                return false;
            }
            return base.CanUseItem(player);
        }

        public override bool? UseItem(Player player) {
            player.AddBuff(ModContent.BuffType<Buffs.ElixirCooldown>(), 4200);
            return base.UseItem(player);
        }
    }
}
