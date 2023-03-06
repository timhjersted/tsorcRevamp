using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Magic
{
    public class GemBox : ModItem
    {
        public override void SetStaticDefaults()
        { //TODO "Double cast all spells"? maybe some day
            /* Tooltip.SetDefault("All spells can be casted twice as fast" +
                               "\nReduces magic damage by 30% multiplicatively" +
                               "\nSome spells cannot benefit from this."); */
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetDamage(DamageClass.Magic) *= .7f;
            player.GetAttackSpeed(DamageClass.Magic) *= 2;
        }
    }
}
