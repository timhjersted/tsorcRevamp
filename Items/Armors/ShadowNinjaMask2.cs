using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    class ShadowNinjaMask2 : ModItem
    {

        public override string Texture => "tsorcRevamp/Items/Armors/Melee/ShadowNinjaMask";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shadow Ninja Mask II");
            Tooltip.SetDefault("+30% Melee Crit" +
                "\nSet Bonus: +30% to all damage, melee speed, crit chance and +30 rapid life regen" +
                "\nDefense is capped at 60" +
                "\nDamage reduction is converted into movement speed");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.fromItem(Item);
            Item.defense = 6;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<Melee.ShadowNinjaTop>() && legs.type == ModContent.ItemType<Melee.ShadowNinjaBottoms>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.GetDamage(DamageClass.Generic) += 0.3f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.3f;
            player.GetCritChance(DamageClass.Generic) += 30;
            if(player.statDefense >=60)
            {
                player.statDefense = 60;
            }
            player.lifeRegen += 30;
            player.moveSpeed += player.endurance;
            player.endurance = 0f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<Melee.ShadowNinjaMask>());
            recipe.AddIngredient(ModContent.ItemType<SoulOfAttraidies>());
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 150000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
