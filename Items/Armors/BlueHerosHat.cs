using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class BlueHerosHat : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Blue Hero's hat");
            Tooltip.SetDefault("Worn by the hero himself!\nCan be upgraded eventually with 5 Souls of Sight and 40,000 Dark Souls\n+40 Mana");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.defense = 10;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.statManaMax2 += 40;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<BlueHerosShirt>() && legs.type == ModContent.ItemType<BlueHerosPants>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.accFlipper = true;
            player.accDivingHelm = true;
            player.GetDamage(DamageClass.Generic) += 0.09f;
            player.GetCritChance(DamageClass.Generic) += 9;
            player.GetAttackSpeed(DamageClass.Melee) += 0.09f;
            player.moveSpeed += 0.09f;
            player.manaCost -= 0.09f;
            player.ammoCost80 = true;

            if (player.wet)
            {
                player.lifeRegen += 3;
                player.detectCreature = true;
                player.moveSpeed *= 5f;
            }
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HerosHat, 1);
            recipe.AddIngredient(ItemID.MythrilBar, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
