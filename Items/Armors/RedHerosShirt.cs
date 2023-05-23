using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class RedHerosShirt : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Red Hero's Shirt");
            /* Tooltip.SetDefault("The legendary clothes of the master." +
                "\nSet bonus: Lava resistance, fire block & knockback immunity" +
                "\nPlus extended breath, water and lava walk, & swim" +
                "\nBoosts damage, crit, melee and movement speed by 14%" +
                "\nReduces mana costs by 11%" +
                "\n+3 life regen while in lava & +2 in water"); */
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 20;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<RedHerosHat>() && legs.type == ModContent.ItemType<RedHerosPants>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.lavaRose = true;
            player.fireWalk = true;
            player.accFlipper = true;
            player.accDivingHelm = true;
            player.waterWalk = true;
            player.noKnockback = true;
            player.GetDamage(DamageClass.Generic) += 0.14f;
            player.GetCritChance(DamageClass.Generic) += 14;
            player.GetAttackSpeed(DamageClass.Melee) += 0.14f;
            player.moveSpeed += 0.14f;
            player.manaCost -= 0.11f;

            if (player.lavaWet)
            {
                player.lifeRegen += 3;
                player.detectCreature = true;
            }

            if (player.wet)
            {
                player.lifeRegen += 2;
                player.detectCreature = true;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<BlueHerosShirt>());
            recipe.AddIngredient(ItemID.SoulofFright, 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 15000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
