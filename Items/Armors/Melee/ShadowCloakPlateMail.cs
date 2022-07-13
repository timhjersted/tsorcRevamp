using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Melee
{
    [AutoloadEquip(EquipType.Body)]
    public class ShadowCloakPlateMail : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Shadow Cloak Skill activates +9 Life Regen when health falls below 100, otherwise grants +2 life regen\nSet bonus: +10% Melee Damage, +27% Melee Speed");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 9;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            if (player.statLife <= 100)
            {
                player.lifeRegen += 9;
                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 21, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 245, Color.Violet, 2.0f);
                Main.dust[dust].noGravity = true;
            }
            else
            {
                player.lifeRegen += 2;
            }
            if (Main.rand.NextBool(7))
            {
                int dust2 = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 21, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 200, Color.Violet, 1.0f);
                Main.dust[dust2].noGravity = true;

                Main.dust[dust2].noGravity = true;
            }
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ShadowScalemail, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 2000);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }
    }
}
