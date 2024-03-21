using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Tools
{
    [LegacyName("WallTome")]
    public class GreatMagicShieldScroll : ModItem
    {
        public static int Duration = 25;
        public static int Cooldown = 600;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Duration, Cooldown, Buffs.GreatMagicShield.DefenseIncrease, Buffs.GreatMagicShield.DamagePenalty, Buffs.GreatMagicShield.Slowness);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.rare = ItemRarityID.Green;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.mana = 50;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 35;
            Item.useAnimation = 35;
            Item.value = 8000;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.IronskinPotion);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override bool? UseItem(Player player)
        {
            player.AddBuff(ModContent.BuffType<Buffs.GreatMagicShield>(), Duration * 60, false);
            player.AddBuff(ModContent.BuffType<Buffs.Debuffs.ShieldCooldown>(), Cooldown * 60); //10 minutes and 25 seconds (10 min downtime)
            return true;
        }
        public override bool CanUseItem(Player player)
        {
            if (player.HasBuff(ModContent.BuffType<Buffs.Debuffs.ShieldCooldown>()))
            {
                return false;
            }
            if (player.HasBuff(ModContent.BuffType<Buffs.MagicShield>()) || player.HasBuff(ModContent.BuffType<Buffs.MagicBarrier>()) || player.HasBuff(ModContent.BuffType<Buffs.GreatMagicBarrier>()))
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