using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Magic
{
    [AutoloadEquip(EquipType.Body)]
    public class MimeticSuit : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Burning Mana Skill activates +5 Mana Regen when life falls below 120\nSet bonus: 13% Reduced Mana Cost, +6% Magic Crit, +15% Magic Damage");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 7;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            if (player.statLife <= 120)
            {
                player.manaRegen += 5;
                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 6, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 100, Color.Green, 1.0f);
                Main.dust[dust].noGravity = true;
            }
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<MimeticHat>() && legs.type == ModContent.ItemType<MimeticPants>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.manaCost -= 0.13f;
            player.GetDamage(DamageClass.Magic) += 0.15f;
            player.GetCritChance(DamageClass.Magic) += 6;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.JungleShirt, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
