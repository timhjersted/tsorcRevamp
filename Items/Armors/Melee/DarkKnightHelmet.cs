using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Melee
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
            Item.width = 18;
            Item.height = 12;
            Item.defense = 15;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<DarkKnightArmor>() && legs.type == ModContent.ItemType<DarkKnightGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.waterWalk = true;
            player.noKnockback = true;
            player.GetDamage(DamageClass.Melee) += 0.3f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.3f;
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
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HallowedMask, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
