using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class BlueHerosHat : ModItem
    {
        public static int MaxMana = 60;
        public static float ManaCost = 8f;
        public static int ManaRegen = 5;
        public static float CritChance = 11f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxMana, ManaCost, ManaRegen, CritChance, RedHerosHat.SoulCost + RedHerosShirt.SoulCost + RedHerosPants.SoulCost, RedHerosHat.SoulCost2 + RedHerosShirt.SoulCost2 + RedHerosPants.SoulCost2);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.defense = 10;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.statManaMax2 += MaxMana;
            player.manaCost -= ManaCost / 100f;
            player.manaRegenBonus += ManaRegen;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<BlueHerosShirt>() && legs.type == ModContent.ItemType<BlueHerosPants>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.accFlipper = true;
            player.accDivingHelm = true;
            player.GetCritChance(DamageClass.Generic) += CritChance;
            player.ignoreWater = true;
            player.iceSkate = true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HerosHat, 1);
            recipe.AddIngredient(ItemID.MythrilBar, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
