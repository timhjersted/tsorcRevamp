using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class MagicShield : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Magic Shield");
            Tooltip.SetDefault("A lost beginner's tome\n" +
                                "Casts magic aura on the player, raising defense by 8 for 15 seconds" +
                                "\nDoes not stack with Barrier, Wall or Shield spells");

        }
        public override void SetDefaults()
        {
            Item.stack = 1;
            Item.width = 28;
            Item.height = 30;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Blue;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.mana = 20;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.value = PriceByRarity.Blue_1;

        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("HealingElixir").Type);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 1000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }



        public override bool? UseItem(Player player)
        {
            player.AddBuff(ModContent.BuffType<Buffs.Fog>(), 900, false);
            return true;
        }
        public override bool CanUseItem(Player player)
        {

            if (player.HasBuff(ModContent.BuffType<Buffs.ShieldCooldown>()))
            {
                return false;
            }
            if (player.HasBuff(ModContent.BuffType<Buffs.Barrier>()) || player.HasBuff(ModContent.BuffType<Buffs.Wall>()) || player.HasBuff(ModContent.BuffType<Buffs.Shield>()))
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
