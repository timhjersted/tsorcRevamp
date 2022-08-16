using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Magic
{
    [LegacyName("AncientDragonScaleMail")]
    [AutoloadEquip(EquipType.Body)]
    public class DragonScaleMail : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A powerful magic/low defense set chosen by skilled Paladins with a taste for high risk/reward combat" +
                "\nSet bonus: 20% magic crit, +30% magic damage, +60 mana, -9% mana cost + Darkmoon Cloak skill" +
                "\nDarkmoon Cloak activates rapid mana regen, Star Cloak & Doubles magic crit and damage when life falls below 100");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 8;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<DragonScaleHelmet>() && legs.type == ModContent.ItemType<DragonScaleGreaves>();
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
            recipe.AddIngredient(ItemID.MythrilChainmail, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
