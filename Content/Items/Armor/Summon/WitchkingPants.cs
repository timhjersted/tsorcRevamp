using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Items.Materials.Titanite;

namespace tsorcRevamp.Content.Items.Armor.Summon
{
    [LegacyName("WitchkingBottoms")]
    [AutoloadEquip(EquipType.Legs)]
    public class WitchkingPants : ModItem
    {
        public static float Dmg = 19f;
        public static int MinionSlot = 1;
        public static int SentrySlot = 1;
        public static float MoveSpeed = 44f;
        public static float TagDuration = 20f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, MinionSlot, SentrySlot, MoveSpeed, TagDuration);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 19;
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.value = PriceByRarity.Purple_11;
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon) += Dmg / 100f;
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
            recipe.AddIngredient(ModContent.ItemType<CursedSoul>(), 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

