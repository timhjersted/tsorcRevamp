using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    public class BarrierRing : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Casts Barrier when the wearer is critically wounded" +
                                "\nBarrier increases defense by 20" +
                                "\nDoes not stack with Fog, Wall or Shield spells");
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 22;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AdamantiteBar, 3);
            recipe.AddIngredient(ItemID.SoulofLight, 20);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            if ((player.statLife <= (player.statLifeMax * 0.25f)) && !(player.HasBuff(ModContent.BuffType<Buffs.Fog>()) || player.HasBuff(ModContent.BuffType<Buffs.Wall>()) || player.HasBuff(ModContent.BuffType<Buffs.Shield>())))
            {
                player.AddBuff(ModContent.BuffType<Buffs.Barrier>(), 1, false);
            }
        }

    }
}
