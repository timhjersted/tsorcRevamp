using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Ranged
{
    [AutoloadEquip(EquipType.Head)]
    class ShellHelmet : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Armor made from the shell of a legendary creature." +
                "Increases ranged critical strike chance by 10%" +
                "\nSet Bonus: Ranged Critical Strike Chance is doubled under 50% max health" +
                "\nReduces chance to consume ammo by 25%");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.fromItem(Item);
            Item.defense = 7;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<ShellArmor>() && legs.type == ModContent.ItemType<ShellGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.GetCritChance(DamageClass.Ranged) += 10;

            if (player.statLife <= (player.statLifeMax / 2))
            {
                player.GetCritChance(DamageClass.Ranged) *= 2;

                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 6, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 100, Color.Black, 1.0f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.NecroHelmet);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
