using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Armor;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Body)]
    public class ShamanCloak : ModItem
    {
        public static float Dmg = 24f;
        public static int MinionSlots = 1;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, MinionSlots, ShunpoDash.Cooldown);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 30;
            Item.defense = 13;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon) += Dmg / 100f;
            player.maxMinions += MinionSlots;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return (head.type == ModContent.ItemType<ShamanHood>() || head.type == ModContent.ItemType<ShamanMask>()) && legs.type == ModContent.ItemType<ShamanPants>();
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
            recipe.AddIngredient(ItemID.TikiShirt);
            recipe.AddIngredient(ItemID.AdamantiteBar);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 9800);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.TikiShirt);
            recipe2.AddIngredient(ItemID.TitaniumBreastplate);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}