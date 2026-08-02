using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    [AutoloadEquip(EquipType.Face)]
    public class PhoenixSkull : ModItem
    {
        public static int Cooldown = 300;
        public static float HealthPercent = 1f;
        public static float LifeSteal = 2f;
        public static int Duration = 5;
        public static float LifeThreshold = 50f;
        public const int BossChargeDuration = 40;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Cooldown, HealthPercent, LifeSteal, Duration, LifeThreshold, BossChargeDuration);
        public override void SetStaticDefaults()
        {
            int EquipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Face);
            ArmorIDs.Face.Sets.PreventHairDraw[EquipSlot] = true;
            ArmorIDs.Face.Sets.OverrideHelmet[EquipSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.accessory = true;
            Item.width = 18;
            Item.height = 18;
            Item.expert = true;
            Item.value = PriceByRarity.LightRed_4;
        }
        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().PhoenixSkull = true;
        }
    }
}