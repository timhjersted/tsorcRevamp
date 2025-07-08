using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Accessories.Mobility;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Accessories.Melee
{
    public class DragoonGear : ModItem
    {
        public static float MoveSpeedMult = 30f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MoveSpeedMult, DragoonHorn.MeleeDmg);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.defense = 5;
            Item.width = 32;
            Item.height = 26;
            Item.accessory = true;
            Item.expert = true;
            Item.value = PriceByRarity.Purple_11;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ModContent.ItemType<DragoonBoots>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DragoonHorn>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 45000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.noFallDmg = true;
            player.GetModPlayer<tsorcRevampPlayer>().DragoonHorn = true;
            player.GetModPlayer<tsorcRevampPlayer>().DragoonBoots = true;
            player.moveSpeed *= 1f + MoveSpeedMult / 100f;
            player.runAcceleration *= 1.6f;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var DragoonBoots = tsorcRevamp.toggleDragoonBoots.GetAssignedKeys();
            string DragoonBootsString = DragoonBoots.Count > 0 ? DragoonBoots[0] : LangUtils.GetTextValue("Keybinds.Dragoon Boots.DisplayName") + LangUtils.GetTextValue("CommonItemTooltip.NotBound");
            int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip4");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "Keybind", Language.GetTextValue("Mods.tsorcRevamp.Items.DragoonBoots.Keybind1") + DragoonBootsString + Language.GetTextValue("Mods.tsorcRevamp.Items.DragoonBoots.Keybind2")));
            }
        }

    }
}
