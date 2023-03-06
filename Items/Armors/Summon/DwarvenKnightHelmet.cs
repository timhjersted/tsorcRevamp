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
            /* Tooltip.SetDefault("Increases minion damage by 5% and 3 flat" +
                "\nSet Bonus: Grants Holy Dodge, stats provided by this armor set are doubled while Holy Dodge is active" +
                "\nDefense, minion and turret slots are not affected by this" +
                "\nIncreases your max number of minions and turrets by 1"); */
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
            player.GetDamage(DamageClass.Summon) += 0.05f;
            player.GetDamage(DamageClass.Summon).Flat += 3f;

            if (player.HasBuff(BuffID.ShadowDodge))
            {
                player.GetDamage(DamageClass.Summon) += 0.05f;
                player.GetDamage(DamageClass.Summon).Flat += 3f;
            }
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<DwarvenArmor>() && legs.type == ModContent.ItemType<DwarvenGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.onHitDodge = true;

            player.maxMinions += 1;
            player.maxTurrets += 1;

            if (player.HasBuff(BuffID.ShadowDodge))
            {
                int dust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 42, (player.velocity.X) + (player.direction * 1), player.velocity.Y, 105, Color.Gold, 1.0f);
                Main.dust[dust].noGravity = true;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HallowedHood, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
