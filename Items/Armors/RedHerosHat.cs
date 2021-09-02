using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class RedHerosHat : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Red Hero's hat");
            Tooltip.SetDefault("Skill: Longer invincibility after being hit, +80 mana\nCan be upgraded to it's master form with 80,000 Dark Souls");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 12;
            item.defense = 10;
            item.value = 2000;
            item.rare = ItemRarityID.LightRed;
        }

        public override void UpdateEquip(Player player)
        {
            player.longInvince = true;
            player.statManaMax2 += 80;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<RedHerosShirt>() && legs.type == ModContent.ItemType<RedHerosPants>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = "Harmonizes you with fire and water, plus a 14% boost to all stats";
            player.lavaImmune = true;
            player.fireWalk = true;
            player.accFlipper = true;
            player.accDivingHelm = true;
            player.waterWalk = true;
            player.noKnockback = true;
            player.allDamage += 0.14f;
            player.rangedCrit += 14;
            player.meleeCrit += 14;
            player.magicCrit += 14;
            player.thrownCrit += 14;
            player.meleeSpeed += 0.14f;
            player.moveSpeed += 0.14f;
            player.manaCost -= 0.14f;

            if (player.lavaWet)
            {
                player.lifeRegen += 4;
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
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("BlueHerosHat"), 1);
            recipe.AddIngredient(ItemID.SoulofSight, 2);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 10000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
