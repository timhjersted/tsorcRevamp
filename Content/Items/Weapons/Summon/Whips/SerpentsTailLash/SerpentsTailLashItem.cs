using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Content.Items.Weapons.Summon.Whips.SerpentsTailLash
{
    public class SerpentsTailLashItem : ModItem
    {
        public const int BaseDamage = 32;
        public const int SummonTagDamage = 6;
        public const int NeedleStrikeInterval = 3;
        public const int NeedleBonusPercent = 40;
        public const int NeedleDropletCount = 2;
        public const int VenomDurationSeconds = 4;
        public const byte PerkTier3Level = 3;
        public const byte PerkTier6Level = 6;
        public const int Tier3SummonTagDamage = 14;
        public const int Tier3SummonTagCrit = 5;
        public const int Tier6SummonTagDamage = 24;
        public const int Tier6SummonTagCrit = 10;
        public const int Tier6NeedleStrikeInterval = 2;
        public const int Tier6NeedleDropletCount = 3;
        public const int Tier6NeedleDropletPercent = 75;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(
            SummonTagDamage,
            NeedleStrikeInterval,
            NeedleBonusPercent,
            VenomDurationSeconds,
            NeedleDropletCount);

        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 60;
            Item.height = 52;
            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.damage = BaseDamage;
            Item.knockBack = 2.5f;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.Orange_3;
            Item.shoot = ModContent.ProjectileType<SerpentsTailLashProj>();
            Item.shootSpeed = 4f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 28;
            Item.useAnimation = 28;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
        }

        public override bool MeleePrefix()
        {
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (Main.myPlayer != player.whoAmI)
            {
                return false;
            }

            byte displayedLevel = WeaponReinforcement.GetDisplayedLevel(Item);
            int strikeInterval = displayedLevel >= PerkTier6Level ? Tier6NeedleStrikeInterval : NeedleStrikeInterval;

            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.serpentsTailLashCracks++;

            bool isNeedleStrike = false;
            if (modPlayer.serpentsTailLashCracks >= strikeInterval)
            {
                modPlayer.serpentsTailLashCracks = 0;
                isNeedleStrike = true;
            }

            if (isNeedleStrike)
            {
                // Trim the swing animation slightly so needle thrusts feel snappier than the baseline lash.
                int fastAnimation = (int)(player.itemAnimation * 0.82f);
                if (fastAnimation < 12)
                {
                    fastAnimation = 12;
                }

                player.itemAnimation = fastAnimation;
                player.itemAnimationMax = fastAnimation;
                player.itemTime = fastAnimation;
            }

            // Pack reinforcement level (bits 1+) and needle flag (bit 0) into ai[2] so both sync cleanly in MP.
            float packedState = displayedLevel * 2 + (isNeedleStrike ? 1 : 0);
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, 0f, packedState);
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
