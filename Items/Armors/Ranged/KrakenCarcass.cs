using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Armors.Ranged
{
    [AutoloadEquip(EquipType.Body)]
    public class KrakenCarcass : ModItem
    {
        public const float AmmoReduction = 30f;
        public const float ArmorPen = 12f;
        public const float Velocity = 15f;
        public const int SoulCost = 70000;
        public const int TsunamiBaseDmg = 150;
        public const float TsunamiBaseKnockback = 5f;
        public const float TsunamiDmgBoost = 25f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(AmmoReduction, ArmorPen, TsunamiDmgBoost, Main.LocalPlayer.GetTotalDamage(DamageClass.Ranged).ApplyTo(TsunamiBaseDmg));
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 31;
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.value = PriceByRarity.Purple_11;
        }
        public override void UpdateEquip(Player player)
        {
            player.ammoCost75 = true;
            player.GetArmorPenetration(DamageClass.Ranged) += ArmorPen;
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
            string ArmorSetBonusKeybind = LangUtils.GetTextValue(Main.ReversedUpDownArmorSetBonuses ? "CommonItemTooltip.UpKeybind" : "CommonItemTooltip.DownKeybind") ;
            int ttindex1 = tooltips.FindIndex(t => t.Name == "Tooltip2");
            if (ttindex1 != -1)
            {
                tooltips.RemoveAt(ttindex1);
                tooltips.Insert(ttindex1, new TooltipLine(Mod, "Keybind", LangUtils.GetTextValue("Items.KrakenCarcass.Keybind1") + ArmorSetBonusKeybind 
                    + LangUtils.GetTextValue("Items.KrakenCarcass.Keybind2") + KrakensCastString + LangUtils.GetTextValue("Items.KrakenCarcass.Keybind3")));
            }
            int ttindex2 = tooltips.FindIndex(t => t.Name == "Tooltip3");
            if (ttindex2 != -1)
            {
                tooltips.Insert(ttindex2 + 1, new TooltipLine(Mod, "Keybind", LangUtils.GetTextValue("Items.KrakenCarcass.Tooltip1", (int)Main.LocalPlayer.GetTotalDamage(DamageClass.Ranged).ApplyTo(TsunamiBaseDmg))));
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ShroomiteBreastplate);
            recipe.AddIngredient(ModContent.ItemType<KrakenFlesh>());
            recipe.AddIngredient(ModContent.ItemType<BlueTitanite>(), 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), SoulCost);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
