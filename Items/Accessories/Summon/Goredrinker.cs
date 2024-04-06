using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.Items.Weapons.Melee.Hammers;

namespace tsorcRevamp.Items.Accessories.Summon
{
    public class Goredrinker : ModItem
    {
        public const float SummonDamage = 10f;
        public const int MaxLife = 40;
        public const float HealBaseValue = 7f;
        public const float WhipDmgRange = 33f; //dmg is divided by 3 and gets applied in playermain
        public const int Cooldown = 12;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(SummonDamage, MaxLife, Cooldown);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;
            Item.value = PriceByRarity.Orange_3;
            Item.rare = ItemRarityID.Orange;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ChainKnife);
            recipe.AddIngredient(ModContent.ItemType<AncientWarhammer>());
            recipe.AddIngredient(ItemID.LifeCrystal, 2);
            recipe.AddIngredient(ModContent.ItemType<WorldRune>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3200);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().Goredrinker = true;
            player.GetDamage(DamageClass.Summon) += SummonDamage / 100f;
            player.statLifeMax2 += MaxLife;
            if (!player.HasBuff(ModContent.BuffType<GoredrinkerCooldown>()) && player.HeldItem.type != ModContent.ItemType<WitchkingsSword>())
            {
                player.GetModPlayer<tsorcRevampPlayer>().GoredrinkerReady = true;
                player.whipRangeMultiplier *= 1f + WhipDmgRange / 100f;
            }
        }
    }
}