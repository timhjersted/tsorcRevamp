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
    [AutoloadEquip(EquipType.Body)]
    public class NecromancersShirt : ModItem
    {
        public const float ManaCost = 20f;
        public const int MaxMana = 220;
        public const int SoulCost = 70000;
        public const int SkullBaseDmg = 250;
        public const float SkullBaseKnockback = 5f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxMana, ManaCost);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 14;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.statManaMax2 += MaxMana;
            player.manaCost -= ManaCost / 100f;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<NecromancersSkullmask>() && legs.type == ModContent.ItemType<NecromancersPants>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().Lich = true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var NecromancersSpellKey = tsorcRevamp.NecromancersSpell.GetAssignedKeys();
            string NecromancersSpellString = NecromancersSpellKey.Count > 0 ? NecromancersSpellKey[0] : LangUtils.GetTextValue("Keybinds.Necromancers Spell.DisplayName") + LangUtils.GetTextValue("CommonItemTooltip.NotBound");
            int ttindex1 = tooltips.FindIndex(t => t.Name == "Tooltip3");
            if (ttindex1 != -1)
            {
                tooltips.RemoveAt(ttindex1);
                tooltips.Insert(ttindex1, new TooltipLine(Mod, "Keybind", LangUtils.GetTextValue("Items.NecromancersShirt.Keybind1") + NecromancersSpellString + LangUtils.GetTextValue("Items.NecromancersShirt.Keybind2")));
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpectreRobe);
            recipe.AddIngredient(ModContent.ItemType<LichBone>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), SoulCost);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
