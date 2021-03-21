using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    public class MythrilGlove : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Casts and sustains Wall when wearer is critically wounded" +
                               "\nWall gives +50 defense" +
                               "\nDoes not stack with Fog, Barrier or Shield spells");
        }

        public override void SetDefaults()
        {
            item.width = 30;
            item.height = 34;
            item.accessory = true;
            item.rare = ItemRarityID.Cyan;
            item.value = 500000;
        }

        public override void UpdateEquip(Player player)
        {
            if ((player.statLife <= (player.statLifeMax * 0.30f)) && !(player.HasBuff(ModContent.BuffType<Buffs.Fog>()) || player.HasBuff(ModContent.BuffType<Buffs.Barrier>()) || player.HasBuff(ModContent.BuffType<Buffs.Shield>())))
            {
                player.AddBuff(ModContent.BuffType<Buffs.Wall>(), 1, false);
            }
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.TitanGlove);
            recipe.AddIngredient(ItemID.MythrilBar, 10);
            recipe.AddIngredient(mod.GetItem("GuardianSoul"));
            recipe.AddIngredient(mod.GetItem("SoulOfAttraidies"));
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 50000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
