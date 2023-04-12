using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    [LegacyName("ShieldTome", "GreatMagicBarrierTome", "greatmagicbarrier")]
    public class GreatMagicBarrierScroll : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Great Magic Barrier");
            /* Tooltip.SetDefault("A lost legendary scroll\n" +
                                "[c/ffbf00:Casts Great Magic Barrier on the player, raising defense by 60 for 30 seconds]\n" +
                                "2 minute cooldown. Does not stack with other barrier or shield spells"); */
        }
        public override void SetDefaults()
        {
            Item.stack = 1;
            Item.width = 28;
            Item.height = 30;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Cyan;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.mana = 150;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.value = PriceByRarity.Cyan_9;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>(), 3);
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 3);
            recipe.AddIngredient(ModContent.ItemType<CursedSoul>(), 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override bool? UseItem(Player player)
        {
            player.AddBuff(ModContent.BuffType<Buffs.GreatMagicBarrier>(), 1800, false);
            player.AddBuff(ModContent.BuffType<Buffs.ShieldCooldown>(), 9000); //2 minutes and 30 seconds (2 min downtime)
            return true;
        }
        public override bool CanUseItem(Player player)
        {
            if (player.HasBuff(ModContent.BuffType<Buffs.ShieldCooldown>()))
            {
                return false;
            }
            if (player.HasBuff(ModContent.BuffType<Buffs.MagicShield>()) || player.HasBuff(ModContent.BuffType<Buffs.MagicBarrier>()) || player.HasBuff(ModContent.BuffType<Buffs.GreatMagicShield>()))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
