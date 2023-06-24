using rail;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Body)]
    public class WitchkingTop : ModItem
    {
        public static int SetBonusMinionBoost = 1;
        public static int SetBonusSentryBoost = 1;
        public static float AtkSpeed = 25f;
        public static float WhipRange = 30f;
        public static float Dmg = 15f;
        public static int MinionBoost = 1;
        public static int SentryBoost = 1;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(SetBonusMinionBoost, SetBonusSentryBoost, AtkSpeed, WhipRange, Dmg, MinionBoost, SentryBoost);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 22;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<WitchkingHelmet>() && legs.type == ModContent.ItemType<WitchkingBottoms>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.maxMinions += SetBonusMinionBoost;
            player.maxTurrets += SetBonusSentryBoost;
            player.GetAttackSpeed(DamageClass.Summon) += AtkSpeed / 100f;
            player.whipRangeMultiplier += WhipRange / 100f;
            player.GetModPlayer<tsorcRevampPlayer>().WitchPower = true;

            int i2 = (int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16;
            int j2 = (int)(player.position.Y + 2f) / 16;
            Lighting.AddLight(i2, j2, 0.92f, 0.8f, 0.65f);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon) += Dmg / 100f;
            player.maxMinions += MinionBoost;
            player.maxTurrets += SentryBoost;
        }
    }
}
