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
            Tooltip.SetDefault("25% chance to not consume ammo\nInfinite breath, waterwalk, no knockback\nSet Bonus: Ranged Stats & Movement +30% + Archery Skill + No Fall DMG");
            ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 15;
            Item.value = 300000;
            Item.rare = ItemRarityID.LightPurple;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<TheUnforseeable>() && legs.type == ModContent.ItemType<TheUntouchable>();
        }

        public override void UpdateEquip(Player player)
        {
            player.ammoCost75 = true;
            player.breath = 999999;
            player.waterWalk = true;
            player.noKnockback = true;
        }

        public override void UpdateArmorSet(Player player)
        {
            player.GetDamage(DamageClass.Ranged) += 0.30f;
            player.GetCritChance(DamageClass.Ranged) += 30;
            player.moveSpeed += 0.30f;
            player.archery = true;
            player.noFallDmg = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.HallowedHelmet, 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 10000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
