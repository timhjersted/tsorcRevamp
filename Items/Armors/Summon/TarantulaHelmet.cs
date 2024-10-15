using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Head)]
    public class TarantulaHelmet : ModItem
    {
        public static float WhipDmg = 25f;
        public static float WhipRange = 30f;
        public static float SummonTagDuration = 15f;
        public static float CritChance = 10f;
        public static float AtkSpeed = 12f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(WhipDmg, WhipRange, AtkSpeed, tsorcRevampPlayer.MythrilOcrichalcumCritDmg, SummonTagDuration, CritChance);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.defense = 9;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.SummonMeleeSpeed) += WhipDmg / 100f;
            player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration += SummonTagDuration / 100f;
            player.GetCritChance(DamageClass.Summon) += CritChance;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<TarantulaCarapace>() && legs.type == ModContent.ItemType<TarantulaLegs>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.whipRangeMultiplier += WhipRange / 100f;
            player.GetAttackSpeed(DamageClass.Summon) += AtkSpeed / 100f;
            player.GetModPlayer<tsorcRevampPlayer>().MythrilOrichalcumCritDamage = true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpiderMask);
            recipe.AddIngredient(ItemID.MythrilBar);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 8000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.SpiderMask);
            recipe2.AddIngredient(ItemID.OrichalcumMask);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}