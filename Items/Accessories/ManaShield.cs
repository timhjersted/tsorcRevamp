using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    public class ManaShield : ModItem
    {

        public static int manaCost = 60;
        public static float damageResistance = 0.40f;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Focuses the user's mana into a protective shield" +
                                "\nReduces damage by 40%, but drains 60 mana per hit" +
                                "\nInhibits natural and artificial mana regen" +
                                "\nFor melee warriors only, reduces other damage by 75%");
        }

        public override void SetDefaults()
        {
            item.width = 24;
            item.height = 24;
            item.accessory = true;
            item.value = 2000;
            item.rare = ItemRarityID.Orange;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 4000);
            recipe.AddIngredient(ItemID.SoulofSight, 5);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player)
        {
            //Iterate through the five main accessory slots
            for (int i = 3; i < (8 + player.extraAccessorySlots); i++)
            {
                //If they're wearing the accesories that totally break this concept, it won't function for them.
                if (player.armor[i].type == ItemID.MagicCuffs || player.armor[i].type == ItemID.CelestialCuffs || player.armor[i].type == ItemID.ManaRegenerationBand)
                {
                    player.GetModPlayer<tsorcRevampPlayer>().manaShield = 0;
                    return;
                }
            }

            base.UpdateEquip(player);
            player.GetModPlayer<tsorcRevampPlayer>().manaShield = 1;
            player.rangedDamageMult *= 0.25f;
            player.magicDamageMult *= 0.25f;
            player.minionDamageMult *= 0.25f;
            player.manaRegen = 1;
            player.manaRegenBonus = 0;
            if (player.statMana >= manaCost)
            {
                player.endurance += damageResistance;
            }
        }
    }
}
