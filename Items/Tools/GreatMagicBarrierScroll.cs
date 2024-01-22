using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Tools
{
    [LegacyName("ShieldTome", "GreatMagicBarrierTome", "greatmagicbarrier")]
    public class GreatMagicBarrierScroll : ModItem
    {
        public static int Duration = 30;
        public static int Cooldown = 150;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Buffs.GreatMagicBarrier.Defense, Duration, Cooldown);
        public override void SetStaticDefaults()
        {
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
            player.AddBuff(ModContent.BuffType<Buffs.GreatMagicBarrier>(), Duration * 60, false);
            player.AddBuff(ModContent.BuffType<Buffs.Debuffs.ShieldCooldown>(), Cooldown * 60);
            return true;
        }
        public override bool CanUseItem(Player player)
        {
            if (player.HasBuff(ModContent.BuffType<Buffs.Debuffs.ShieldCooldown>()))
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
