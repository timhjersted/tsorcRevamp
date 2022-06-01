using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class DragoonHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Harmonized with Sky and Fire\n+120 Mana");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.defense = 15;
            Item.value = 10000;
            Item.rare = ItemRarityID.Orange;
        }

        public override void UpdateEquip(Player player)
        {
            player.statManaMax2 += 120;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<DragoonArmor>() && legs.type == ModContent.ItemType<DragoonGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = "Harmonized with the four elements: fire, water, earth and air, including +2 life regen, flight, and a 30% boost to all stats";
            player.lavaImmune = true;
            player.fireWalk = true;
            player.breath = 9999999;
            player.waterWalk = true;
            player.noKnockback = true;
            player.GetDamage(DamageClass.Generic) += 0.30f;
            player.GetCritChance(DamageClass.Ranged) += 30;
            player.GetCritChance(DamageClass.Melee) += 30;
            player.GetCritChance(DamageClass.Magic) += 30;
            player.GetCritChance(DamageClass.Throwing) += 30;
            player.GetAttackSpeed(DamageClass.Melee) += 0.30f;
            player.moveSpeed += 0.30f;
            player.manaCost -= 0.30f;
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
            recipe.AddIngredient(Mod.Find<ModItem>("RedHerosHat").Type, 1);
            recipe.AddIngredient(ModContent.ItemType<SoulOfLife>(), 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
