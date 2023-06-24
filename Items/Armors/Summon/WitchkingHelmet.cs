using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Head)]
    public class WitchkingHelmet : ModItem
    {
        public static float Dmg = 15f;
        public static int MinionSlot = 1;
        public static int SentrySlot = 1;
        public static float CritChance = 30f;
        public static float TagStrength = 40f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, MinionSlot, SentrySlot, CritChance, TagStrength);
        public override void SetStaticDefaults()
        {
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
            player.GetDamage(DamageClass.Summon) += Dmg / 100f;
            player.maxMinions += MinionSlot;
            player.maxTurrets += SentrySlot;
            player.GetCritChance(DamageClass.Summon) += CritChance;
            player.GetModPlayer<tsorcRevampPlayer>().SummonTagStrength += TagStrength / 100f;
        }
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }
    }
}
