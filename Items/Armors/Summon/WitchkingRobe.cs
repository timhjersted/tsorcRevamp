using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Summon
{
    [LegacyName("WitchkingTop")]
    [AutoloadEquip(EquipType.Body)]
    public class WitchkingRobe : ModItem
    {
        public static float AtkSpeed = 25f;
        public static float WhipRange = 30f;
        public static float DmgAmp = 6f;
        public static int MinionBoost = 2;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(AtkSpeed, WhipRange, DmgAmp, MinionBoost);
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
        public override void UpdateEquip(Player player)
        {
            player.GetAttackSpeed(DamageClass.Summon) += AtkSpeed / 100f;
            player.GetDamage(DamageClass.Summon) *= 1f + DmgAmp / 100f;
            player.maxMinions += MinionBoost;
            player.whipRangeMultiplier += WhipRange / 100f;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<WitchkingHelmet>() && legs.type == ModContent.ItemType<WitchkingPants>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().WitchPower = true;

            int i2 = (int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16;
            int j2 = (int)(player.position.Y + 2f) / 16;
            Lighting.AddLight(i2, j2, 0.92f, 0.8f, 0.65f);
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpookyBreastplate);
            recipe.AddIngredient(ModContent.ItemType<BewitchedTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
