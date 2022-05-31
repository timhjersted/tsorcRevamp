using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class RTQ2Helmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("10% Less Mana Usage.\nEmergency shield kicks in +15 Defense when health is less than 61");
            DisplayName.SetDefault("RTQ2 Helmet");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.defense = 6;
            Item.value = 100000;
            Item.rare = ItemRarityID.Pink;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<RTQ2Chestplate>() && legs.type == ModContent.ItemType<RTQ2Leggings>();
        }

        public override void UpdateEquip(Player player)
        {
            player.manaCost -= 0.10f;
            if (player.statLife <= 60)
            {
                player.statDefense += 15;
                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 60, (player.velocity.X) + (player.direction * 3), player.velocity.Y, 100, Color.Red, 3.0f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override void UpdateArmorSet(Player player)
        {
            player.magicDamage += 0.15f;
            player.statManaMax2 += 60;
            player.spaceGun = true;

            int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 60, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 100, Color.Red, 1.0f);
            Main.dust[dust].noGravity = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.MeteorHelmet, 1);
            recipe.AddIngredient(ItemID.SoulofLight, 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
