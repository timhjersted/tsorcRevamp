using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class BarrierRing : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Casts Magic Barrier when the wearer is critically wounded" +
                                "\nMagic Barrier increases defense by 20" +
                                "\nDoes not stack with other barrier or shield spells");
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
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            if ((player.statLife <= (player.statLifeMax * 0.25f)) && !(player.HasBuff(ModContent.BuffType<Buffs.MagicShield>()) || player.HasBuff(ModContent.BuffType<Buffs.GreatMagicShield>()) || player.HasBuff(ModContent.BuffType<Buffs.GreatMagicBarrier>())))
            {
                player.AddBuff(ModContent.BuffType<Buffs.MagicBarrier>(), 1, false);
            }
        }

    }
}
