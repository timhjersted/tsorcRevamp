using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Items.Accessories.Expert
{
    public class RingofArtorias : ModItem
    {
        public override void SetStaticDefaults()
        {
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
            player.buffImmune[ModContent.BuffType<PowerfulCurseBuildup>()] = true;
            player.buffImmune[BuffID.Frozen] = true;
            player.buffImmune[BuffID.Blackout] = true;
        }

    }
}

