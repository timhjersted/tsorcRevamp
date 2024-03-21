using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Tools
{
    [LegacyName("BarrierTome", "MagicBarrier")]
    public class MagicBarrierScroll : ModItem
    {
        public static int Duration = 20;
        public static int Cooldown = 60;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Buffs.MagicBarrier.DefenseIncrease, Duration, Cooldown);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.stack = 1;
            Item.width = 34;
            Item.height = 10;
            Item.rare = ItemRarityID.Pink;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.mana = 130;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.value = PriceByRarity.Pink_5;

        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.SoulofSight, 1);
            recipe.AddIngredient(ItemID.SoulofLight, 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override bool? UseItem(Player player)
        {
            player.AddBuff(ModContent.BuffType<Buffs.MagicBarrier>(), Duration * 60, false);
            player.AddBuff(ModContent.BuffType<Buffs.Debuffs.ShieldCooldown>(), Cooldown * 60);
            return true;
        }
        public override bool CanUseItem(Player player)
        {

            if (player.HasBuff(ModContent.BuffType<Buffs.Debuffs.ShieldCooldown>()))
            {
                return false;
            }

            if (player.HasBuff(ModContent.BuffType<Buffs.MagicShield>()) || player.HasBuff(ModContent.BuffType<Buffs.GreatMagicShield>()) || player.HasBuff(ModContent.BuffType<Buffs.GreatMagicBarrier>()))
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
