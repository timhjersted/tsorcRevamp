using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors {
    [AutoloadEquip(EquipType.Body)]
    public class DragoonArmor2 : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Dragoon Armor II");
            Tooltip.SetDefault("A reforged upgrade to the legendary Dragoon Armor.\n" +
                "You are a master of all forces, the protector of Earth, the Hero of the age.\n" +
                "The powers of the Dragoon Cloak are embedded within its blue-plated chest piece.\n" +
                "Set bonus adds +38% to all stats and +6 HP Regen, while Dragoon Cloak effects kick in at 160 HP.");
        }

        public override void SetDefaults() {
            item.width = 18;
            item.height = 18;
            item.defense = 50;
            item.value = 5000;
            item.rare = ItemRarityID.Orange;
        }

        public override void UpdateEquip(Player player) {
            player.starCloak = true;
            player.magicCrit += 3;
            player.magicDamage += .05f;

            if (player.statLife <= 160) {
                player.lifeRegen += 8;
                player.statDefense += 12;
                player.manaRegenBuff = true;
                player.magicCrit += 3;
                player.magicDamage += .05f;
                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 21, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 245, Color.White, 2.0f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("DragoonArmor"), 1);
            recipe.AddIngredient(mod.GetItem("DragoonCloak"), 1);
            recipe.AddIngredient(mod.GetItem("DragonScale"), 10);
            recipe.AddIngredient(mod.GetItem("Humanity"), 10);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 60000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
