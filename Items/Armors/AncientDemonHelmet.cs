using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class AncientDemonHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("You hear an evil whispering from inside.\n+3% magic crit");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 12;
            item.defense = 2;
            item.value = 40000;
            item.rare = ItemRarityID.Orange;
        }

        public override void UpdateEquip(Player player)
        {
            player.magicCrit += 3;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<AncientDemonArmor>() && legs.type == ModContent.ItemType<AncientDemonGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.magicCrit += 15;
            player.manaCost -= 0.15f;
            player.magicDamage += 0.15f;
            if (player.statLife <= 140) {
                player.manaRegenBuff = true;

                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 6, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 100, Color.Green, 1.0f);
                Main.dust[dust].noGravity = true;

            }
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.MoltenHelmet, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1800);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
