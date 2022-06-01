using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class PowerArmorNUHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("20% Increased Melee Damage");
            DisplayName.SetDefault("Power Armor NU Helmet");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 9;
            Item.value = 5000;
            Item.rare = ItemRarityID.Pink;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<PowerArmorNUTorso>() && legs.type == ModContent.ItemType<PowerArmorNUGreaves>();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Melee) += 0.2f;
        }

        public override void UpdateArmorSet(Player player)
        {
            player.moveSpeed += 0.15f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.20f;
            player.GetCritChance(DamageClass.Ranged) += 17;
            player.GetCritChance(DamageClass.Magic) += 17;
            player.GetCritChance(DamageClass.Melee) += 17;
            player.lifeRegen += 10;

            if (player.wet)
            {
                player.lifeRegen += 20;
                player.nightVision = true;
            }
            int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 39, (player.velocity.X) + (player.direction * 2), player.velocity.Y, 100, Color.SpringGreen, 1.0f);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].noLight = false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.ShadowHelmet, 1);
            recipe.AddIngredient(ItemID.SoulofMight, 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 10000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
