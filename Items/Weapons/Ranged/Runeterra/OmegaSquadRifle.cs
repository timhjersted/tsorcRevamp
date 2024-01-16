using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Ranged;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Ranged.Runeterra;

namespace tsorcRevamp.Items.Weapons.Ranged.Runeterra
{
    public class OmegaSquadRifle : RuneterraDarts
    {
        public override int Width => 62;
        public override int Height => 22;
        public override int Rarity => ItemRarityID.Red;
        public override int Value => Item.buyPrice(1, 0, 0, 0);
        public override float Knockback => 6f;
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Ranged/OmegaSquadRifle/";
        public override int ProjectileType => ModContent.ProjectileType<RadioactiveDart>();
        public override int Tier => 3;
        public override string LocalizationPath => "Items.OmegaSquadRifle.";
        public override float ShootSoundVolume => 0.3f;
        public override int BlindingProjectileType => ModContent.ProjectileType<RadioactiveBlindingLaser>();
        public override int BlindingProjectileCooldownType => ModContent.BuffType<RadioactiveBlindingLaserCooldown>();
        public const int BaseDamage = 350;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ToxicShot.PoisonDartDmgMult);
        public override void CustomSetDefaults()
        {
            Item.damage = BaseDamage;
            BaseLaserManaCost = 30;
        }
        public override Vector2? HoldoutOffset()
        {
            return new Vector2(0f, -8f);
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ModContent.ItemType<AlienGun>());
            recipe.AddIngredient(ItemID.LunarBar, 12);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}