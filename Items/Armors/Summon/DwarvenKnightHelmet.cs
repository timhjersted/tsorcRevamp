using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Head)]
    class DwarvenKnightHelmet : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases minion damage by 12%" +
                "\nIncreases your max number of minions by 1" +
                "\nSet Bonus: Increases your max number of minions and turrets by 1" +
                "\nWhen health is above 333, gain 7% minion damage + 6 flat and 6 life regen");
        }

        public override void SetDefaults()
        {
            Item.height = Item.width = 18;
            Item.defense = 8;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon) += 0.12f;
            player.maxMinions += 1;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<DwarvenArmor>() && legs.type == ModContent.ItemType<DwarvenGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.maxMinions += 1;
            player.maxTurrets += 1;

            if (player.statLife > 333)
            {
                player.GetDamage(DamageClass.Summon) += 0.07f;
                player.GetDamage(DamageClass.Summon).Flat += 6f;
                player.lifeRegen += 6;

                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 42, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 105, Color.Gold, 1.0f);
                Main.dust[dust].noGravity = true;
            }
            else
            {
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HallowedHood, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 6600);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
