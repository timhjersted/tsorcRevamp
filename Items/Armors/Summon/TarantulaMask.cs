using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Head)]
    public class TarantulaMask : ModItem
    {
        public static float Dmg = 12f;
        public static float CritChance = 20f;
        public static float SummonTagStrength = 33f;
        public static int MinionSlot = 1;
        public static int TurretSlot = 1;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, MinionSlot, TurretSlot, tsorcRevampPlayer.MythrilOcrichalcumCritDmg, CritChance, SummonTagStrength);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.defense = 6;
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
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<TarantulaCarapace>() && legs.type == ModContent.ItemType<TarantulaLegs>();
        }
        public override void UpdateArmorSet(Player player)
        {
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
            recipe2.AddIngredient(ItemID.OrichalcumHeadgear);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}