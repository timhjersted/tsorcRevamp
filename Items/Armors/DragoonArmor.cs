using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class DragoonArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("+30% melee speed" +
                "\nThe legends of Arradius spoke of the one who would wear this" +
                "\nYou are a master of all forces, the protector of Earth, the Hero of the age." +
                "\nSet Bonus: Harmonized with the four elements: fire, water, earth and air, including +2 life regen and flight" +
                "\nBoosts damage, crit chance and movement speed by 30%"); */
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 30;
            Item.value = 5000;
            Item.rare = ItemRarityID.Cyan;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetAttackSpeed(DamageClass.Melee) += 0.3f;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<DragoonHelmet>() && legs.type == ModContent.ItemType<DragoonGreaves>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.lavaImmune = true;
            player.fireWalk = true;
            player.breath = 9999999;
            player.waterWalk = true;
            player.noKnockback = true;
            player.GetDamage(DamageClass.Generic) += 0.3f;
            player.GetCritChance(DamageClass.Generic) += 30;
            player.moveSpeed += 0.3f;
            player.lifeRegen += 2;
            //player.wings = 34; // looks like Jim's Wings
            //player.wingsLogic = 34;
            player.wingTimeMax = 180;

        }
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<RedHerosShirt>());
            recipe.AddIngredient(ModContent.ItemType<SoulOfLife>(), 1);
            recipe.AddIngredient(ItemID.SoulofMight, 10);
            recipe.AddIngredient(ItemID.SoulofSight, 10);
            recipe.AddIngredient(ItemID.SoulofFright, 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 30000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
