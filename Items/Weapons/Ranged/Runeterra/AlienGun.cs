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
    public class AlienGun : RuneterraDarts
    {
        public override int Width => 56;
        public override int Height => 24;
        public override int Rarity => ItemRarityID.LightPurple;
        public override int Value => Item.buyPrice(0, 30, 0, 0);
        public override float Knockback => 5f;
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Ranged/AlienGun/";
        public override int ProjectileType => ModContent.ProjectileType<AlienLaser>();
        public override int Tier => 2;
        public override string LocalizationPath => "Items.AlienGun.";
        public override float ShootSoundVolume => 1f;
        public override int BlindingProjectileType => ModContent.ProjectileType<AlienBlindingLaser>();
        public override int BlindingProjectileCooldownType => ModContent.BuffType<AlienBlindingLaserCooldown>();
        public const int BaseDamage = 80;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ToxicShot.PoisonDartDmgMult);
        public override void CustomSetDefaults()
        {
            Item.damage = BaseDamage;
            BaseLaserManaCost = 20;
        }
        public override Vector2? HoldoutOffset()
        {
            return new Vector2(0f, -9f);
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ModContent.ItemType<ToxicShot>());
            recipe.AddIngredient(ItemID.ChlorophyteBar, 11);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 35000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}