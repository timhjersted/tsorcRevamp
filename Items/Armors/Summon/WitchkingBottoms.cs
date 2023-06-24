using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Legs)]
    public class WitchkingBottoms : ModItem
    {
        public static float Dmg = 15f;
        public static int MinionSlot = 1;
        public static int SentrySlot = 1;
        public static float MoveSpeed = 44f;
        public static float TagDuration = 40f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, MinionSlot, SentrySlot, MoveSpeed, TagDuration);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 19;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon) += Dmg / 100f;
            player.maxMinions += MinionSlot;
            player.maxTurrets += SentrySlot;
            player.moveSpeed += MoveSpeed / 100f;
            player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration += TagDuration / 100f;
        }
    }
}

