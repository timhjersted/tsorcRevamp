using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Armor;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Magic
{
    [AutoloadEquip(EquipType.Body)]
    public class GenjiArmor : ModItem
    {
        public static float ManaCost = 19f;
        public static int ManaRegen = 9;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ManaCost, ManaRegen, ShunpoDash.Cooldown);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 17;
            Item.rare = ItemRarityID.Lime;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.manaCost -= ManaCost;
            player.manaRegenBonus += ManaRegen;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<GenjiHelmet>() && legs.type == ModContent.ItemType<GenjiGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().Shunpo = true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            var ShunpoKeybind = tsorcRevamp.Shunpo.GetAssignedKeys();
            string ShunpoString = ShunpoKeybind.Count > 0 ? ShunpoKeybind[0] : "Shunpo: <NOT BOUND>";
            int ttindex1 = tooltips.FindIndex(t => t.Name == "Tooltip3");
            if (ttindex1 != -1)
            {
                tooltips.RemoveAt(ttindex1);
                tooltips.Insert(ttindex1, new TooltipLine(Mod, "Keybind", Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.ShunpoKeybind1") + ShunpoString + Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.ShunpoKeybind2")));
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AdamantiteBreastplate);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.AdamantiteBreastplate);
            recipe2.AddIngredient(ItemID.TitaniumBreastplate);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}
