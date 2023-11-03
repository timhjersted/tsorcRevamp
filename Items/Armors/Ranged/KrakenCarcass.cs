using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Armors.Magic
{
    [Autoload(false)]
    [AutoloadEquip(EquipType.Body)]
    public class KrakenCarcass : ModItem
    {
        public const float AmmoReduction = 25f;
        public const int SoulCost = 70000;
        public const int TsunamiBaseDmg = 100;
        public const float TsunamiBaseKnockback = 15f;
        public const float TsunamiDmgBoost = 50f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(AmmoReduction);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 30;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.ammoCost75 = true;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<KrakenHead>() && legs.type == ModContent.ItemType<KrakenLegs>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().Kraken = true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var KrakensCastKey = tsorcRevamp.KrakensCast.GetAssignedKeys();
            string KrakensCastString = KrakensCastKey.Count > 0 ? KrakensCastKey[0] : LangUtils.GetTextValue("Keybinds.Krakens Cast.DisplayName") + LangUtils.GetTextValue("CommonItemTooltip.NotBound");
            int ttindex1 = tooltips.FindIndex(t => t.Name == "Tooltip3");
            if (ttindex1 != -1)
            {
                tooltips.RemoveAt(ttindex1);
                tooltips.Insert(ttindex1, new TooltipLine(Mod, "Keybind", LangUtils.GetTextValue("Items.KrakenCarcass.Keybind1") + KrakensCastString + LangUtils.GetTextValue("Items.KrakenCarcass.Keybind2")));
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ShroomiteBreastplate);
            recipe.AddIngredient(ModContent.ItemType<LichBone>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), SoulCost);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
