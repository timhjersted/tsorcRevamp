using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class DarkKnightHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("+15 defense when health falls below 80");
        }
        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 12;
            item.defense = 15;
            item.value = 10000;
            item.rare = ItemRarityID.Orange;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<DarkKnightArmor>() && legs.type == ModContent.ItemType<DarkKnightGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.waterWalk = true;
            player.noKnockback = true;
            player.meleeDamage += 0.3f;
            player.meleeSpeed += 0.3f;
            player.moveSpeed += 0.3f;

            int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 27, (player.velocity.X) + (player.direction * 3), player.velocity.Y, 100, Color.BlueViolet, 1.0f);
            Main.dust[dust].noGravity = true;
        }

        public override void UpdateEquip(Player player)
        {
            if (player.statLife <= 80)
            {
                player.statDefense += 15;
            }
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.HallowedMask, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 10000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
