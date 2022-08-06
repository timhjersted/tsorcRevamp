using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Magic
{
    [LegacyName("AncientDragonScaleHelmet")]
    [AutoloadEquip(EquipType.Head)]
    public class DragonScaleHelmet : ModItem
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
            Item.rare = ItemRarityID.LightPurple;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.thorns = 1f;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<DragonScaleMail>() && legs.type == ModContent.ItemType<DragonScaleGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.manaCost -= 0.09f;
            player.statManaMax2 += 60;
            if (player.statLife <= 100)
            {

                player.manaRegenBuff = true;
                player.starCloakItem = new Item(ItemID.StarCloak); ;
                player.GetCritChance(DamageClass.Magic) += 40;
                player.GetDamage(DamageClass.Magic) += 0.60f;

                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 65, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 100, Color.Blue, 2.0f);
                Main.dust[dust].noGravity = true;
            }
            else
            {
                player.GetCritChance(DamageClass.Magic) += 20;
                player.GetDamage(DamageClass.Magic) += .30f;
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MythrilHood, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
