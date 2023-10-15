using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class DragoonHelmet : ModItem
    {
        public static int MaxMana = 140;
        public static float ManaCost = 14f;
        public static int ManaRegen = 9;
        public static float CritChance = 30f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxMana, ManaCost, ManaRegen, CritChance);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.defense = 15;
            Item.rare = ItemRarityID.Cyan;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.statManaMax2 += MaxMana;
            player.manaCost -= ManaCost / 100f;
            player.manaRegenBonus += ManaRegen;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            //Lets it work with mix-and-match sets, so upgrading one piece doesn't fuck people over
            //The way it works is that the head checks if the others are valid. If the head isn't on, the chest checks. If that isn't on either, then the greaves check.
            //This way only one of them ever applies it at a time.

            if (body.type == ModContent.ItemType<DragoonArmor>() || body.type == ModContent.ItemType<DragoonArmor2>())
            {
                if (legs.type == ModContent.ItemType<DragoonGreaves>() || legs.type == ModContent.ItemType<DragoonGreaves2>())
                {
                    return true;
                }
            }

            return false;
        }

        public override void UpdateArmorSet(Player player)
        {
            ApplyDragoonSetBonus(player);
        }
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }

        //It's set up like this to enable mix and matching (and to keep all its stat changes in one place)
        public static void ApplyDragoonSetBonus(Player player)
        {
            player.lavaImmune = true;
            player.fireWalk = true;
            player.breath = 9999999;
            player.waterWalk = true;
            player.noKnockback = true;
            player.GetCritChance(DamageClass.Generic) += CritChance;
            //player.wings = 34; // looks like Jim's Wings
            //player.wingsLogic = 34;
            //player.wingTimeMax = 180;
            player.ignoreWater = true;
            player.iceSkate = true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<RedHerosHat>());
            recipe.AddIngredient(ModContent.ItemType<SoulOfLife>(), 1);
            recipe.AddIngredient(ItemID.SoulofMight, 10);
            recipe.AddIngredient(ItemID.SoulofSight, 10);
            recipe.AddIngredient(ItemID.SoulofFright, 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 24000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
