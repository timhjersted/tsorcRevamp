using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Armor;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Armors.Ranged
{
    [AutoloadEquip(EquipType.Body)]
    public class ArcherOfLumeliaShirt : ModItem
    {
        public static float Dmg = 23f;
        public static int AmmoChance = 25;   //changing this number has no effect since an ammo consumption chance stat doesn't exist
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, ShunpoBlink.Cooldown, AmmoChance);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 20;
            Item.rare = ItemRarityID.Lime;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Ranged) += Dmg / 100f;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<ArcherOfLumeliaHairStyle>() && legs.type == ModContent.ItemType<ArcherOfLumeliaPants>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().Shunpo = true;
            player.ammoCost75 = true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            var ShunpoKeybind = tsorcRevamp.Shunpo.GetAssignedKeys();
            string ShunpoString = ShunpoKeybind.Count > 0 ? ShunpoKeybind[0] : LangUtils.GetTextValue("Keybinds.Shunpo.DisplayName") + LangUtils.GetTextValue("CommonItemTooltip.NotBound");
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
