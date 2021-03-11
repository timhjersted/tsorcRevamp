using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class TheUnknowable : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("25% chance to not consume ammo\nLonger breath, waterwalk, no knockback\nSet Bonus: Ranged Stats & Movement +30% + Archery Skill + No Fall DMG");
        }
        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 15;
            item.value = 300000;
            item.rare = ItemRarityID.LightPurple;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<TheUnforseeable>() && legs.type == ModContent.ItemType<TheUntouchable>();
        }

        public override void UpdateEquip(Player player)
        {
            player.ammoCost75 = true;
            player.breath = 10800;
            player.waterWalk = true;
            player.noKnockback = true;
        }

        public override void UpdateArmorSet(Player player)
        {
            player.rangedDamage += 0.30f;
            player.rangedCrit += 30;
            player.moveSpeed += 0.30f;
            player.archery = true;
            player.noFallDmg = true;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.HallowedHelmet, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 10000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
