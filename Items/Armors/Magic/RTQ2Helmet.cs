using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Magic
{
    [AutoloadEquip(EquipType.Head)]
    public class RTQ2Helmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increase magic crit by 10%\nEmergency shield kicks in +15 Defense when health is less than 141");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.defense = 8;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<RTQ2Chestplate>() && legs.type == ModContent.ItemType<RTQ2Leggings>();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Magic) += 10;
            if (player.statLife < 141)
            {
                player.statDefense += 15;
                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 60, (player.velocity.X) + (player.direction * 3), player.velocity.Y, 100, Color.Red, 3.0f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override void UpdateArmorSet(Player player)
        {
            player.GetAttackSpeed(DamageClass.Magic) += 0.15f;
            player.spaceGun = true;

            int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 60, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 100, Color.Red, 1.0f);
            Main.dust[dust].noGravity = true;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MeteorHelmet, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
