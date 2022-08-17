using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Expert
{
    public class DragonWings : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("The wings of Seath the Scaleless" +
                                "\nProvides immunity to all fire and lava damage, as well as perfect sight and hunting abilities." +
                                "\nIncludes +10% ranged damage, archery effect, +10% ranged critical strike chance");
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 42;
            Item.accessory = true;
            Item.value = PriceByRarity.Red_10;
            Item.expert = true;
        }


        public override void UpdateEquip(Player player)
        {
            //todo: make these wings alternative to supersonic that can hover
            player.lavaImmune = true;
            player.fireWalk = true;
            player.noKnockback = true;
            player.buffImmune[BuffID.Darkness] = true;
            player.buffImmune[BuffID.OnFire] = true;
            player.AddBuff(BuffID.Archery, 300);
            player.AddBuff(BuffID.NightOwl, 300);
            player.AddBuff(BuffID.Hunter, 300);
        }
    }
}
