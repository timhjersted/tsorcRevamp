using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class RedHerosHat : ModItem
    {
        public static int MaxMana = 80;
        public static float ManaCost = 11f;
        public static int ManaRegen = 7;
        public static float CritChance = 20f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxMana, ManaCost, ManaRegen, CritChance);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.defense = 10;
            Item.rare = ItemRarityID.Yellow;
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
            return body.type == ModContent.ItemType<RedHerosShirt>() && legs.type == ModContent.ItemType<RedHerosPants>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.lavaRose = true;
            player.fireWalk = true;
            player.accFlipper = true;
            player.accDivingHelm = true;
            player.waterWalk = true;
            player.noKnockback = true;
            player.GetCritChance(DamageClass.Generic) += CritChance;
            player.ignoreWater = true;
            player.iceSkate = true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<BlueHerosHat>());
            recipe.AddIngredient(ItemID.SoulofFright, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 12000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
