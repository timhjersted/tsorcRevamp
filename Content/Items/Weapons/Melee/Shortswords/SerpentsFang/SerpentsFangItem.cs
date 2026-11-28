using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Shortswords.SerpentsFang
{
    public class SerpentsFangItem : ModItem
    {
        public const int BaseDamage = 32;
        public const int LungeStaminaCost = 25;
        public const int PoisonDurationSeconds = 4;
        public const int VenomDurationSeconds = 4;
        public const byte PerkTier3Level = 3;
        public const byte PerkTier6Level = 6;
        public const int Tier3LungeStaminaCost = 18;
        public const int Tier6LungeDropletCount = 3;
        public const int Tier6LungeDropletPercent = 55;
        public const int Tier6LungeCritBonusPercent = 25;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(
            LungeStaminaCost,
            PoisonDurationSeconds,
            VenomDurationSeconds);

        public override void SetDefaults()
        {
            Item.width = 54;
            Item.height = 54;
            Item.DamageType = DamageClass.Melee;
            Item.damage = BaseDamage;
            Item.knockBack = 3.5f;
            Item.crit = 6;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.Orange_3;
            Item.useStyle = ItemUseStyleID.Rapier;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<SerpentsFangProj>();
            Item.shootSpeed = 2.4f;
        }

        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                byte displayedLevel = WeaponReinforcement.GetDisplayedLevel(Item);
                int staminaCost = displayedLevel >= PerkTier3Level ? Tier3LungeStaminaCost : LungeStaminaCost;
                tsorcRevampStaminaPlayer staminaPlayer = player.GetModPlayer<tsorcRevampStaminaPlayer>();
                if (staminaPlayer.staminaResourceCurrent < staminaCost)
                {
                    return false;
                }

                Item.useTime = 24;
                Item.useAnimation = 24;
                Item.UseSound = SoundID.DD2_MonkStaffSwing;
            }
            else
            {
                Item.useTime = 15;
                Item.useAnimation = 15;
                Item.UseSound = SoundID.Item1;
            }

            return base.CanUseItem(player);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (Main.myPlayer != player.whoAmI)
            {
                return false;
            }

            bool isLunge = player.altFunctionUse == 2;
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

            Vector2 aimDir = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            if (isLunge)
            {
                int staminaCost = perkTier >= 1 ? Tier3LungeStaminaCost : LungeStaminaCost;
                tsorcRevampStaminaPlayer staminaPlayer = player.GetModPlayer<tsorcRevampStaminaPlayer>();
                staminaPlayer.staminaResourceCurrent -= staminaCost;

                // Longer coiled lunge dash with a slight lift when grounded so floor friction does not stall it.
                Vector2 lungeVelocity = aimDir * 13.5f;
                if (player.velocity.Y == 0f && lungeVelocity.Y > -1.4f)
                {
                    lungeVelocity.Y = -1.4f;
                }

                player.velocity = lungeVelocity;
                modPlayer.serpentsFangDashTicks = 12;
                modPlayer.serpentsFangIsLunge = true;
                modPlayer.serpentsFangDashDir = aimDir;

                if (!Main.dedServ)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 wakeVel = -aimDir * Main.rand.NextFloat(1.5f, 3.8f) + Main.rand.NextVector2Circular(1.4f, 1.4f);
                        Dust wake = Dust.NewDustPerfect(player.Center, DustID.AncientLight, wakeVel, 50, new Color(185, 85, 255), Main.rand.NextFloat(1.0f, 1.3f));
                        wake.noGravity = true;
                    }
                }
            }
            else
            {
                // Small forward step-push on left-click thrusts; stops cleanly on enemy impact.
                float pushX = aimDir.X * 5.2f;
                if (System.Math.Abs(player.velocity.X) < System.Math.Abs(pushX) || System.Math.Sign(player.velocity.X) != System.Math.Sign(pushX))
                {
                    player.velocity.X = pushX;
                }

                if (aimDir.Y < -0.25f && player.velocity.Y > aimDir.Y * 4f)
                {
                    player.velocity.Y = aimDir.Y * 4f;
                }

                modPlayer.serpentsFangDashTicks = 6;
                modPlayer.serpentsFangIsLunge = false;
                modPlayer.serpentsFangDashDir = aimDir;
            }

            // ai[1] carries the lunge flag and ai[2] carries the reinforcement perk tier (0 / 1 / 2).
            float lungeFlag = isLunge ? 1f : 0f;
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, lungeFlag, perkTier);
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
