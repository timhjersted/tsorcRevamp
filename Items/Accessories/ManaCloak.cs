using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class ManaCloak : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Rapid mana regen, +15% magic crit & +15% magic dmg when health falls below 150\n" +
                                "Provides Star cloak, +5 magic crit & +5% magic damage boost normally");
        }

        public override void SetDefaults() {
            item.width = 28;
            item.height = 28;
            item.accessory = true;
            item.maxStack = 1;
            item.rare = ItemRarityID.LightRed;
            item.value = 200000;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 20000);
            recipe.AddIngredient(ItemID.SoulofNight, 2);
            recipe.AddIngredient(ItemID.SoulofLight, 2);
            recipe.AddIngredient(ItemID.StarCloak, 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {

            player.GetModPlayer<tsorcRevampPlayer>().ManaCloak = true;

            if (player.statLife <= 150) {
                player.manaRegenBuff = true;
                player.starCloak = true;
                player.magicCrit += 15;
                player.magicDamage += .15f;

                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 65, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 100, Color.Blue, 2.0f);
                Main.dust[dust].noGravity = true;

            }
            else {
                player.starCloak = true;
                player.magicCrit += 5;
                player.magicDamage += .05f;
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode)
            {
                //only insert the tooltip if the last valid line is not the name, the "Equipped in social slot" line, or the "No stats will be gained" line (aka do not insert if in a vanity slot)
                int ttindex = tooltips.FindLastIndex(t => t.mod == "Terraria" && t.Name != "ItemName" && t.Name != "Social" && t.Name != "SocialDesc");
                if (ttindex != -1)
                {// if we find one
                    //insert the extra tooltip line
                    tooltips.Insert(ttindex + 1, new TooltipLine(mod, "RevampManaCloak", "[c/00ff00:Revamped Mode:] Halves the duration of Magic Imbue Cooldown"));
                }
            }
        }
    }
}
