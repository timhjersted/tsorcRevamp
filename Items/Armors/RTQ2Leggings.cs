using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class RTQ2Leggings : ModItem
    {
        bool LegacyMode = ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("RTQ2 Leggings");
        }
        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 6;
            item.value = 100000;
            item.rare = ItemRarityID.Orange;
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.1f;
            if (LegacyMode) {
                player.magicDamage += 0.25f;
                player.statManaMax2 += 40;
            }
            else {
                player.statManaMax2 += 20;
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            int ttindex = tooltips.FindLastIndex(t => t.mod == "Terraria" && t.Name != "ItemName" && t.Name != "Social" && t.Name !=  "SocialDesc");
            if (ttindex != -1) {// if we find one
                if (LegacyMode) {
                    tooltips.Add(new TooltipLine(mod, "LegacyRTQ2Leggings", "+10% Movespeed"));
                }
                else {
                    tooltips.Add(new TooltipLine(mod, "RevampRTQ2Leggings", "+10% Movespeed, +20 mana"));
                }
            }
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.MeteorLeggings, 1);
            recipe.AddIngredient(ItemID.SoulofLight, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

