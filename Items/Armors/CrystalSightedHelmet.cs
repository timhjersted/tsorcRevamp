using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class CrystalSightedHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Dazzling armor cut from crystal\nWhen health falls below 80, ranged damage increases 50% (20% above normal)\nSighted helmet also offers +5% melee critical chance.");
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 20;
            Item.defense = 5;
            Item.value = 7000000;
            Item.rare = ItemRarityID.Pink;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<CrystalArmor>() && legs.type == ModContent.ItemType<CrystalGreaves>();
        }

        public override void UpdateEquip(Player player)
        {
            if (player.statLife <= 80)
            {
                player.GetDamage(DamageClass.Ranged) -= 0.3f;
                player.GetDamage(DamageClass.Magic) += 0.2f;
                player.GetCritChance(DamageClass.Melee) += 5;

                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 42, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 105, Color.Aqua, 1.0f);
                Main.dust[dust].noGravity = true;
            }
            else
            {
                player.GetDamage(DamageClass.Ranged) -= 0.3f;
                player.GetDamage(DamageClass.Magic) -= 0.3f;
                player.GetCritChance(DamageClass.Melee) += 5;
            }
        }

        public override void UpdateArmorSet(Player player)
        {
            player.GetAttackSpeed(DamageClass.Melee) += 0.4f;
            player.GetDamage(DamageClass.Melee) += 0.15f;
            player.enemySpawns = true;
        }

        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CobaltHelmet, 1);
            recipe.AddIngredient(ItemID.CrystalShard, 30);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
