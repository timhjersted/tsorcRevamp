using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Head)]
    public class WitchkingHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("+25% minion damage\nIncreases your max number of minions by 2\nGrants night vision");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.defense = 16;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon) += 0.25f;
            player.maxMinions += 2;
            player.nightVision = true;
        }
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }
    }
}
