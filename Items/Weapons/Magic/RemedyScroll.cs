using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    [LegacyName("EsunaTome")]
    class RemedyScroll : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Remedy");
            // Tooltip.SetDefault("A lost tome known to cure all but the rarest of ailments.");
        }
        public override void SetDefaults()
        {
            Item.height = 10;
            Item.knockBack = 4;
            Item.rare = ItemRarityID.Cyan;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.mana = 40;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.value = PriceByRarity.Cyan_9;
            Item.width = 34;
        }

        public override bool? UseItem(Player player)
        {
            int buffIndex = 0;

            foreach (int buffType in player.buffType)
            {

                if ((buffType == BuffID.Bleeding)
                    || (buffType == BuffID.Poisoned)
                    || (buffType == BuffID.Confused)
                    || (buffType == BuffID.BrokenArmor)
                    || (buffType == BuffID.Darkness)
                    || (buffType == BuffID.OnFire)
                    || (buffType == BuffID.Slow)
                    || (buffType == BuffID.Weak)
                    || (buffType == BuffID.CursedInferno)
                    || (buffType == BuffID.Cursed)
                    || (buffType == BuffID.Silenced)
                    || (buffType == BuffID.Silenced)
                    || (buffType == BuffID.Silenced)
                    )
                {
                    player.buffTime[buffIndex] = 0;
                }
                buffIndex++;
            }
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome);
            recipe.AddIngredient(ModContent.ItemType<GuardianSoul>(), 1);
            recipe.AddIngredient(ModContent.ItemType<Potions.HealingElixir>(), 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
