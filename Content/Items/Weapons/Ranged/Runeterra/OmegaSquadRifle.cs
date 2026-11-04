using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Bases;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Buffs;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Projectiles;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Sounds.OmegaSquadRifle;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Sounds.ToxicShot;

namespace tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra
{
    public class OmegaSquadRifle : RuneterraDarts
    {
        public override int Width => 62;
        public override int Height => 22;
        public override int Rarity => ModContent.RarityType<DarkBlue>();
        public override int Value => Item.buyPrice(1, 0, 0, 0);
        public override float Knockback => 6f;
        public override string SoundPath => UsefulFunctions.RefactorableFilepath(typeof(OmegaSquadRifleSound));
        public override int ProjectileType => ModContent.ProjectileType<RadioactiveDart>();
        public override int Tier => 3;
        public override string LocalizationPath => "Items.OmegaSquadRifle.";
        public override float ShootSoundVolume => 0.3f;
        public override int BlindingProjectileType => ModContent.ProjectileType<RadioactiveBlindingLaser>();
        public override int BlindingProjectileCooldownType => ModContent.BuffType<RadioactiveBlindingLaserCooldown>();
        public const int BaseDamage = 350;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ToxicShot.PoisonDartDmgMult, PoisonDartPierceBonus);
        public override void CustomSetDefaults()
        {
            Item.damage = BaseDamage;
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
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 100000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}