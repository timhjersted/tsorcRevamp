using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Armors.Summon
{
    [LegacyName("WitchkingTop")]
    [AutoloadEquip(EquipType.Body)]
    public class WitchkingRobe : ModItem
    {
        public static float Dmg = 20f;
        public static int MinionBoost = 2;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, MinionBoost);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 22;
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.value = PriceByRarity.Purple_11;
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon) += Dmg / 100f;
            player.maxMinions += MinionBoost;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<WitchkingHelmet>() && legs.type == ModContent.ItemType<WitchkingPants>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.aggro -= 800;
            int i2 = (int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16;
            int j2 = (int)(player.position.Y + 2f) / 16;
            Lighting.AddLight(i2, j2, 0.92f, 0.8f, 0.65f);
            player.GetModPlayer<tsorcRevampPlayer>().Witch = true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var WitchScreamKey = tsorcRevamp.WitchScream.GetAssignedKeys();
            string WitchScreamString = WitchScreamKey.Count > 0 ? WitchScreamKey[0] : LangUtils.GetTextValue("Keybinds.Witchking Scream.DisplayName") + LangUtils.GetTextValue("CommonItemTooltip.NotBound");
            int ttindex1 = tooltips.FindIndex(t => t.Name == "Tooltip2");
            if (ttindex1 != -1)
            {
                tooltips.RemoveAt(ttindex1);
                tooltips.Insert(ttindex1, new TooltipLine(Mod, "Keybind", LangUtils.GetTextValue("Items.WitchkingRobe.Keybind1") + WitchScreamString + LangUtils.GetTextValue("Items.WitchkingRobe.Keybind2")));
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpookyBreastplate);
            recipe.AddIngredient(ModContent.ItemType<BewitchedTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<CursedSoul>(), 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
