using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Content.Items.Weapons.Ranged.Bows.SerpentsCoil
{
    public class SerpentsCoilItem : ModItem
    {
        public const int BaseDamage = 32;
        public const int SplashDropletCount = 3;
        public const byte PerkTier3Level = 3;
        public const byte PerkTier6Level = 6;
        public const int Tier3SplashDropletCount = 4;
        public const int Tier6VolleyInterval = 3;
        public const int Tier6DropletDamagePercent = 60;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(SplashDropletCount);

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 58;
            Item.DamageType = DamageClass.Ranged;
            Item.damage = BaseDamage;
            Item.knockBack = 3.5f;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.UseSound = SoundID.Item5;
            Item.shoot = AmmoID.Arrow;
            Item.useAmmo = AmmoID.Arrow;
            Item.shootSpeed = 13.5f;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.Orange_3;
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-2f, 0f);
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            byte displayedLevel = WeaponReinforcement.GetDisplayedLevel(Item);
            if (type == ProjectileID.WoodenArrowFriendly || displayedLevel >= PerkTier3Level)
            {
                type = ModContent.ProjectileType<SerpentsCoilArrowProj>();
                velocity *= 1.18f;
            }
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int venomArrowType = ModContent.ProjectileType<SerpentsCoilArrowProj>();
            if (type != venomArrowType)
            {
                return true;
            }

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

            if (perkTier >= 2)
            {
                tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
                modPlayer.serpentsCoilShots++;
                if (modPlayer.serpentsCoilShots >= Tier6VolleyInterval)
                {
                    modPlayer.serpentsCoilShots = 0;

                    // +6 Twin-Fang Volley: every 3rd shot fires two tight venom arrows for 1 ammo.
                    for (int i = -1; i <= 1; i += 2)
                    {
                        Vector2 spreadVel = velocity.RotatedBy(MathHelper.ToRadians(i * 3f));
                        Projectile.NewProjectile(source, position, spreadVel, type, damage, knockback, player.whoAmI, 0f, perkTier);
                    }

                    return false;
                }
            }

            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, perkTier);
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
