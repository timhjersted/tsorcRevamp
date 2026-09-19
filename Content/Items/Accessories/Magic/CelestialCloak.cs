using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Accessories.Magic
{
    [AutoloadEquip(new EquipType[]
    {
        EquipType.Back,
        EquipType.Front
    })]
    public class CelestialCloak : ModItem
    {
        public static int MaxMana = 40;
        public static float ManaCost = 10f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxMana, ManaCost);

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 10;
            Item.height = 12;
            Item.accessory = true;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.Yellow_8;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CelestialCuffs, 1);
            recipe.AddIngredient(ItemID.ManaCloak, 1);
            recipe.AddIngredient(ModContent.ItemType<SoulOfLife>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.statManaMax2 += MaxMana;
            player.manaCost -= ManaCost / 100f;
            player.starCloakItem = player.starCloakItem_manaCloakOverrideItem = new Item(ItemID.ManaCloak);
            player.manaFlower = true;
            player.manaMagnet = true;
            player.magicCuffs = true;
            player.starCloakItem_manaCloakOverrideItem = Item;
            player.GetModPlayer<tsorcRevampPlayer>().CelestialCloak = true;
        }

    }
}
