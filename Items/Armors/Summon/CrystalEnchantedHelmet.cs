using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Head)]
    public class CrystalEnchantedHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Dazzling armor cut from crystal\nIncreases minion damage by 10%\nIncreases your max number of minions by 1\nSet Bonus: Increases your max number of minions and turrets by 1" +
                               "\nIncreases minion damage by 10%\nWhen health falls below 166, gain 10% minion damage + 6 flat");
        }
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 20;
            Item.defense = 6;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<CrystalArmor>() && legs.type == ModContent.ItemType<CrystalGreaves>();
        }

        public override void UpdateEquip(Player player)
        {
                player.GetDamage(DamageClass.Summon) += 0.1f;
                player.maxMinions += 1;
        }

        public override void UpdateArmorSet(Player player)
        {
            player.maxMinions += 1;
            player.maxTurrets += 1;
            player.GetDamage(DamageClass.Summon) += 0.1f;

            if (player.statLife < 166)
            {
                player.GetDamage(DamageClass.Summon) += 0.1f;
                player.GetDamage(DamageClass.Summon).Flat += 6f;

                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 42, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 105, Color.Aqua, 1.0f);
                Main.dust[dust].noGravity = true;
            }
            else
            {
            }
        }
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpiderMask, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 5000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
