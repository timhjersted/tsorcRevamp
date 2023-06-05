using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class BlueHerosShirt : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Blue Hero's Shirt");
            /* Tooltip.SetDefault("Set Bonus: Grants extended breath & swimming skills" +
                "\n+9% damage, crit, melee and movement speed, -8% mana costs, 20% less chance to consume ammo" +
                "\n+3 life regen speed, faster movement & hunter vision while in water"); */
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 16;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<BlueHerosHat>() && legs.type == ModContent.ItemType<BlueHerosPants>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.accFlipper = true;
            player.accDivingHelm = true;
            player.GetDamage(DamageClass.Generic) += 0.09f;
            player.GetCritChance(DamageClass.Generic) += 9;
            player.GetAttackSpeed(DamageClass.Melee) += 0.09f;
            player.moveSpeed += 0.09f;
            player.manaCost -= 0.08f;
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
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HerosShirt, 1);
            recipe.AddIngredient(ItemID.Flipper, 1);
            recipe.AddIngredient(ItemID.DivingHelmet, 1);
            recipe.AddIngredient(ItemID.MythrilBar, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.HerosShirt, 1);
            recipe2.AddIngredient(ItemID.DivingGear, 1);
            recipe2.AddIngredient(ItemID.MythrilBar, 1);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe2.AddTile(TileID.DemonAltar);
            recipe2.Register();

            Recipe recipe3 = CreateRecipe();
            recipe3.AddIngredient(ItemID.HerosShirt, 1);
            recipe3.AddIngredient(ItemID.JellyfishDivingGear, 1);
            recipe3.AddIngredient(ItemID.MythrilBar, 1);
            recipe3.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe3.AddTile(TileID.DemonAltar);
            recipe3.Register();

            Recipe recipe4 = CreateRecipe();
            recipe4.AddIngredient(ItemID.HerosShirt, 1);
            recipe4.AddIngredient(ItemID.ArcticDivingGear, 1);
            recipe4.AddIngredient(ItemID.MythrilBar, 1);
            recipe4.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe4.AddTile(TileID.DemonAltar);
            recipe4.Register();
        }
    }
}
