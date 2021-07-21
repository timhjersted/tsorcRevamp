using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class RTQ2Chestplate : ModItem
    {
        bool LegacyMode = ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("RTQ2 Chestplate");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 10;
            item.value = 100000;
            item.rare = ItemRarityID.Pink;
        }

        public override void UpdateEquip(Player player)
        {
            player.magicCrit += 15;
            if (!LegacyMode) {
                player.magicDamage += 0.10f;
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            int ttindex = tooltips.FindLastIndex(t => t.mod == "Terraria" && t.Name != "ItemName" && t.Name != "Social" && t.Name !=  "SocialDesc");
            if (ttindex != -1) {// if we find one
                if (LegacyMode) {
                    tooltips.Add(new TooltipLine(mod, "LegacyRTQ2Chest", "+15% Magic Critical chance.\nSet bonus: +15% magic damage, +60 mana, Space Gun Skill"));
                }
                else {
                    tooltips.Add(new TooltipLine(mod, "RevampRTQ2Chest", "+15% Magic Critical chance, +10% magic damage\nSet bonus: +15% magic damage, +60 mana, Space Gun Skill"));
                }
            }
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.MeteorSuit, 1);
            recipe.AddIngredient(ItemID.SoulofLight, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
