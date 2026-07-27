using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]

    public class SymbolOfAvarice : ModItem
    {
        public static float SoulAmplifier = 40f;
        /// <summary>Life drained per second while worn, as a percent of MAX life. Was a flat 8 lifeRegen
        /// (-4 HP/sec) regardless of pool size, so the curse cost 80% of an early 100 HP bar but only 20% of a
        /// late 400 HP one — it stopped being a real trade exactly when soul farming mattered most.
        /// Separate constant from PowerWithin's despite the matching value, so the two can diverge.</summary>
        public static float LifeDrainPercent = 1.5f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(SoulAmplifier);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 28;
            Item.defense = 8;
            Item.rare = ItemRarityID.Lime;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().SOADrain = true;
        }
    }
}
