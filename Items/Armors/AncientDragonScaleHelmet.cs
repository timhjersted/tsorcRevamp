using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class AncientDragonScaleHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("It is made of razor sharp dragon scales.\nThorns Effect");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 20;
            Item.defense = 3;
            Item.value = 1000000;
            Item.rare = ItemRarityID.Orange;
        }

        public override void UpdateEquip(Player player)
        {
            player.thorns = 1f;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<AncientDragonScaleMail>() && legs.type == ModContent.ItemType<AncientDragonScaleGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            if (player.statLife <= 100)
            {
                player.manaCost -= 0.17f;
                player.manaRegenBuff = true;
                player.starCloak = true;
                player.GetCritChance(DamageClass.Magic) += 40;
                player.GetDamage(DamageClass.Magic) += 0.60f;
                player.statManaMax2 += 60;
                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 65, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 100, Color.Blue, 2.0f);
                Main.dust[dust].noGravity = true;
            }
            else
            {
                player.manaCost -= 0.17f;
                player.GetCritChance(DamageClass.Magic) += 20;
                player.GetDamage(DamageClass.Magic) += .30f;
                player.statManaMax2 += 60;
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.MythrilHood, 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 3500);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
