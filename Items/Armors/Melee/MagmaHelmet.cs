using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Melee
{
    [AutoloadEquip(EquipType.Head)]
    public class MagmaHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("+17% melee damage");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 5;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<MagmaBreastplate>() && legs.type == ModContent.ItemType<MagmaGreaves>();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Melee) += 0.17f;
        }

        public override void UpdateArmorSet(Player player)
        {
            player.GetCritChance(DamageClass.Melee) += 17;
            player.GetAttackSpeed(DamageClass.Melee) += 0.14f;
            player.fireWalk = true;
            if (Main.rand.NextBool(3))
            {
                Color color = new Color();
                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 6, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 200, color, 1.0f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].noLight = false;
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MoltenHelmet, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
