using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class DarkKnightArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("+30 % Melee Critical Chance\nThe fiercest armor for melee warriors\nSet bonus grants waterwalk, no knockback, plus 30% boost to Melee Speed, Melee Damage and Move Speed\nSmall chance to regain life from melee strikes");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 25;
            item.value = 500000;
            item.rare = ItemRarityID.Orange;
        }

        public override void UpdateEquip(Player player)
        {
            player.meleeCrit += 30;
            Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().MeleeArmorVamp10 = true;
        }

        /*
        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit)
        {
            if (Main.rand.Next(2) == 0)
            {
                player.HealEffect(damage / 10);
                player.statLife += (damage / 10);
            }
        }*/

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.HallowedPlateMail, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 20000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
