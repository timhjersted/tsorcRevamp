using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Summon
{
    [LegacyName("WitchkingBottoms")]
    [AutoloadEquip(EquipType.Legs)]
    public class WitchkingPants : ModItem
    {
        public static float DmgAmp = 7f;
        public static int MinionSlot = 1;
        public static int SentrySlot = 1;
        public static float MoveSpeed = 44f;
        public static float TagDuration = 40f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DmgAmp, MinionSlot, SentrySlot, MoveSpeed, TagDuration);
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
            player.GetDamage(DamageClass.Summon) *= 1f + DmgAmp / 100f;
            player.maxMinions += MinionSlot;
            player.maxTurrets += SentrySlot;
            player.moveSpeed += MoveSpeed / 100f;
            player.GetModPlayer<tsorcRevampPlayer>().SummonTagDuration += TagDuration / 100f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpookyLeggings);
            recipe.AddIngredient(ModContent.ItemType<BewitchedTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

