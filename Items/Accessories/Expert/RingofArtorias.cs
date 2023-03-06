using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Expert
{
    public class RingofArtorias : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Ring of Artorias");
            /* Tooltip.SetDefault("The great Ring of Artorias." +
                                "\nOnce held strength too great for this world to bear." +
                                "\n[c/ffbf00:It now grants immunity to Powerful Curse Buildup, Slow and Freezing.]"); */
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 38;
            Item.accessory = true;
            Item.value = PriceByRarity.Red_10;
            Item.expert = true;
        }

        public override void UpdateEquip(Player player)
        {
            player.buffImmune[ModContent.BuffType<Buffs.PowerfulCurseBuildup>()] = true;
            //player.statDefense += 10;
            player.buffImmune[BuffID.Frozen] = true;
            player.buffImmune[BuffID.Slow] = true;
            //player.GetDamage(DamageClass.Generic) *= 2;
        }

    }
}

