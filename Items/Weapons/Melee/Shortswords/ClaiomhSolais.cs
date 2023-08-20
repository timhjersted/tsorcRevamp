using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Melee;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Shortswords;

namespace tsorcRevamp.Items.Weapons.Melee.Shortswords
{
    class ClaiomhSolais : ModItem
    {
        public static int InvincibilityDuration = 3;
        public static int InvincibilityCooldown = 9;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(InvincibilityCooldown, InvincibilityDuration);
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 68;
            Item.height = 68;
            Item.useStyle = ItemUseStyleID.Rapier;
            Item.shoot = ModContent.ProjectileType<ClaiomhSolaisProjectile>();
            Item.shootSpeed = 2.1f;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.useAnimation = 7;
            Item.useTime = 7;
            Item.damage = 250;
            Item.knockBack = 6f;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.Purple_11;
            Item.DamageType = DamageClass.Melee;
        }
        public override bool MeleePrefix()
        {
            return true;
        }
        public override bool AltFunctionUse(Player player)
        {
            if (!player.HasBuff(ModContent.BuffType<LaevateinnInvincibleCooldown>()))
            {
                player.AddBuff(ModContent.BuffType<LaevateinnInvincible>(), InvincibilityDuration * 60);
                player.AddBuff(ModContent.BuffType<LaevateinnInvincibleCooldown>(), InvincibilityCooldown * 60);
                return true;
            }
            else
            {
                player.altFunctionUse = 1;
                return false;
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<Laevateinn>());
            recipe.AddIngredient(ModContent.ItemType<BlueTitanite>(), 4);
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>(), 2);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
