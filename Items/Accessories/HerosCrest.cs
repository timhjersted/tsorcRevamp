using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories; 
public class HerosCrest : ModItem {
    public override void SetStaticDefaults() {
        // DisplayName.SetDefault("Hero's Crest");
        /* Tooltip.SetDefault("Damage, crit chance, life, and defense\n" +
                            "increased by 6%. Luck increased by 0.06\n" +
                            "\"Proof of the hero's accomplishments\""); */
    }

    public override void SetDefaults() {
        Item.width = 24;
        Item.height = 24;
        Item.accessory = true;
        Item.maxStack = 1;
        Item.rare = ItemRarityID.Yellow;
        Item.value = PriceByRarity.Yellow_8;
    }

    public override void UpdateAccessory(Player player, bool hideVisual) {
        player.GetModPlayer<HerosCrestPlayer>().HerosCrest = true;
    }
    private class HerosCrestPlayer : ModPlayer {
        public bool HerosCrest;

        public override void PostUpdateEquips() {
            if (!HerosCrest) return;
            Player.GetDamage(DamageClass.Generic) *= 1.06f;
            Player.GetCritChance(DamageClass.Generic) += 6;
            Player.statLifeMax2 = (int)(Player.statLifeMax2 * 1.06f);
            Player.statDefense *= 1.06f;
            Player.luck += 0.06f;
        }
        public override void ResetEffects() {
            HerosCrest = false;
        }
    }
}
