using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Head)]
    public class ShamanHood : ModItem
    {
        public static float Dmg = 14f;
        public static float CritChance = 16f;
        public static float SummonTagStrength = 24f;
        public static int MinionSlot = 2;
        public static int TurretSlot = 2;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, MinionSlot, TurretSlot, CritChance, SummonTagStrength);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.defense = 7;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon) += Dmg / 100f;
            player.GetCritChance(DamageClass.Summon) += CritChance;
            player.GetModPlayer<tsorcRevampPlayer>().SummonTagStrength += SummonTagStrength / 100f;
            player.maxMinions += MinionSlot;
            player.maxTurrets += TurretSlot;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.TikiMask);
            recipe.AddIngredient(ItemID.AdamantiteBar);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 9000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.TikiMask);
            recipe2.AddIngredient(ItemID.TitaniumHeadgear);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}