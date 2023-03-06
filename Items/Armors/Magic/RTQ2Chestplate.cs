using Terraria;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Magic
{
    [AutoloadEquip(EquipType.Body)]
    public class RTQ2Chestplate : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("RTQ2 Chestplate");
            /* Tooltip.SetDefault("Increases magic critical strike chance by 7%" +
                "\nSet Bonus: +10% magic attack speed, space gun effect" +
                "\nEmergency shield kicks in when health is less than 141, increasing defense by 15"); */
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 8;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Magic) += 7;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<RTQ2Helmet>() && legs.type == ModContent.ItemType<RTQ2Leggings>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.GetAttackSpeed(DamageClass.Magic) += 0.1f;
            player.spaceGun = true;

            int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 60, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 100, Color.Red, 1.0f);
            Main.dust[dust].noGravity = true;

            if (player.statLife < 141)
            {
                player.statDefense += 15;
                int dust2 = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 60, (player.velocity.X) + (player.direction * 3), player.velocity.Y, 100, Color.Red, 3.0f);
                Main.dust[dust2].noGravity = true;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MeteorSuit, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
