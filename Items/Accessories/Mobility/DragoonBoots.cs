using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Mobility
{
    public class DragoonBoots : ModItem
    {
        public static float MoveSpeedMult = 30f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MoveSpeedMult);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.defense = 5;
            Item.accessory = true;
            Item.width = 32;
            Item.height = 26;
            Item.rare = ItemRarityID.Lime;
            Item.value = PriceByRarity.Lime_7;
        }
        public override void UpdateEquip(Player player)
        {
            Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().DragoonBoots = true;
            player.moveSpeed *= 1f + MoveSpeedMult / 100f;
            player.runAcceleration *= 1.6f;
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (!hideVisual) player.GetModPlayer<tsorcRevampPlayer>().Shockwave = true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var DragoonBoots = tsorcRevamp.toggleDragoonBoots.GetAssignedKeys();
            string DragoonBootsString = DragoonBoots.Count > 0 ? DragoonBoots[0] : LangUtils.GetTextValue("Keybinds.Dragoon Boots.DisplayName") + LangUtils.GetTextValue("CommonItemTooltip.NotBound");
            int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip3");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "Keybind", Language.GetTextValue("Mods.tsorcRevamp.Items.DragoonBoots.Keybind1") + DragoonBootsString + Language.GetTextValue("Mods.tsorcRevamp.Items.DragoonBoots.Keybind2")));
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<BootsOfHaste>(), 1);
            recipe.AddIngredient(ItemID.ChlorophyteBar, 3);
            recipe.AddIngredient(ModContent.ItemType<SoulOfLife>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 30000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }

    }
}
