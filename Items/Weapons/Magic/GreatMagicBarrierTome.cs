using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    [LegacyName("ShieldTome")]
    public class GreatMagicBarrierTome : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Great Magic Barrier");
            Tooltip.SetDefault("A lost legendary scroll\n" +
                                "Casts Shield on the player, raising defense by 60 for 30 seconds\n" +
                                "Does not stack with other barrier or shield spells");
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
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("WhiteTitanite").Type, 6);
            recipe.AddIngredient(Mod.Find<ModItem>("RedTitanite").Type);
            recipe.AddIngredient(Mod.Find<ModItem>("CursedSoul").Type, 30);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 80000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override bool? UseItem(Player player)
        {
            player.AddBuff(ModContent.BuffType<Buffs.GreatMagicBarrier>(), 1800, false);
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
