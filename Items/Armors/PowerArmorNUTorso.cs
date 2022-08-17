using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class PowerArmorNUTorso : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("17% Increased Ranged & Magic Damage" +
                "\n+80 mana, -10% mana cost" +
                "\nA powerful armor forged by the god of chaos." +
                "\nSet Bonus: +20% attack speed, +10 life regen/20 in water");
            DisplayName.SetDefault("Power Armor NU Torso");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 7;
            Item.rare = ItemRarityID.Lime;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Ranged) += 0.17f;
            player.GetDamage(DamageClass.Magic) += 0.17f;
            player.manaCost -= 0.1f;
            player.statManaMax2 += 80;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<PowerArmorNUHelmet>() && legs.type == ModContent.ItemType<PowerArmorNUGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.GetAttackSpeed(DamageClass.Generic) += 0.2f;

            player.lifeRegen += 10;

            if (player.wet)
            {
                player.lifeRegen += 10;
                player.nightVision = true;
            }
            int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 39, (player.velocity.X) + (player.direction * 2), player.velocity.Y, 100, Color.SpringGreen, 1.0f);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].noLight = false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SoulofMight, 11);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
