using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Content.Items.Weapons.Magic.Tomes.CausticMaw
{
    public class CausticMawItem : ModItem
    {
        public const int BaseDamage = 34;
        public const int ManaCost = 12;
        public const int PoolDurationSeconds = 3;
        public const byte PerkTier3Level = 3;
        public const byte PerkTier6Level = 6;
        public const int Tier3PoolDurationTicks = 270;
        public const int Tier3PoolHitCooldown = 11;
        public const int Tier6PoolWidth = 70;
        public const int Tier6PoolDamagePercent = 55;
        public const int Tier6EruptionDropletCount = 3;
        public const int Tier6EruptionDropletPercent = 45;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(PoolDurationSeconds);

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.DamageType = DamageClass.Magic;
            Item.damage = BaseDamage;
            Item.knockBack = 3f;
            Item.mana = ManaCost;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 24;
            Item.useAnimation = 24;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.UseSound = SoundID.Item17;
            Item.shoot = ModContent.ProjectileType<CausticMawProj>();
            Item.shootSpeed = 12.5f;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.Orange_3;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (Main.myPlayer != player.whoAmI)
            {
                return false;
            }

            byte displayedLevel = WeaponReinforcement.GetDisplayedLevel(Item);
            int perkTier = 0;
            if (displayedLevel >= PerkTier6Level)
            {
                perkTier = 2;
            }
            else if (displayedLevel >= PerkTier3Level)
            {
                perkTier = 1;
            }

            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, perkTier);
            return false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<GreatSerpentSoul>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 8000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
