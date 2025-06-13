using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Tools
{
    [LegacyName("FogTome")]
    public class MagicShieldScroll : ModItem
    {
        public const int DefenseIncrease = 10;
        public const int Duration = 15;
        public const int Cooldown = 30;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DefenseIncrease, Duration, Cooldown);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.stack = 1;
            Item.width = 28;
            Item.height = 30;
            Item.rare = ItemRarityID.Blue;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.mana = 30;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.value = PriceByRarity.Blue_1;

        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.IronskinPotion);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override bool? UseItem(Player player)
        {
            player.AddBuff(ModContent.BuffType<Buffs.MagicShield>(), Duration * 60, false);
            player.AddBuff(ModContent.BuffType<ShieldCooldown>(), Cooldown * 60, false);
            return true;
        }
        public override bool CanUseItem(Player player)
        {

            if (player.HasBuff(ModContent.BuffType<Buffs.Debuffs.ShieldCooldown>()))
            {
                return false;
            }

            return true;

            // Why search through the list of all buffs 3 times if all inflict a cooldown larger than their duration?
            if (player.HasBuff(ModContent.BuffType<Buffs.MagicBarrier>()) || player.HasBuff(ModContent.BuffType<Buffs.GreatMagicShield>()) || player.HasBuff(ModContent.BuffType<Buffs.GreatMagicBarrier>()))
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
