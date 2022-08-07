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
            Tooltip.SetDefault("All spells have doubled speed and mana cost" +
                               "\nReduces magic damage by 30% multiplicatively" +
                               "\nSome spells cannot benefit from this.");
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
            player.manaCost *= 2f;
            Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().GemBox = true;
        }
    }

    class GemBox_Global : GlobalItem
    {
        public override float UseTimeMultiplier(Item item, Player player)
        {
            Item selected = player.inventory[player.selectedItem];
            if ((selected.DamageType == DamageClass.Magic) && (player.GetModPlayer<tsorcRevampPlayer>().GemBox))
            {
                float roundup = (float)Math.Ceiling(selected.useTime * 0.5f);
                return (roundup / (float)selected.useTime);
            }
            else return 1f;
        }
    }
}
