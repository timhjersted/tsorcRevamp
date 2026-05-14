using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Spears;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    public class CelestialLance : ModdedSpearItem
    {
        public override int ProjectileID => ModContent.ProjectileType<CelestialLanceProj>();
        public override int Width => 44;
        public override int Height => 44;
        public override int BaseDmg => 350;
        public override int BaseCritChance => 6;
        public override float BaseKnockback => 10;
        public override int UseAnimationTime => 28;
        public override int UseTime => 28;
        public override int Rarity => ModContent.RarityType<OrangeRed>();
        public override int Value => PriceByRarity.fromItem(Item);
        public override SoundStyle UseSoundID => SoundID.Item71;

        public override bool? UseItem(Player player)
        {
            int type = global::Terraria.ID.ProjectileID.StarVeilStar;
            int damage = (int)player.GetTotalDamage(DamageClass.Melee).ApplyTo(Item.damage);

            Projectile Star = Projectile.NewProjectileDirect(
                Item.GetSource_FromThis(),
                player.Center,
                player.DirectionTo(Main.MouseWorld) * 20f,
                type,
                damage,
                Item.knockBack / 3,
                player.whoAmI
            );
            Star.DamageType = DamageClass.Melee;
            Star.CritChance = (int)player.GetTotalCritChance(DamageClass.Melee) + Item.crit;
            Star.netUpdate = true;
            return null;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>(), 12);
            recipe.AddIngredient(ModContent.ItemType<BequeathedSoul>());
            recipe.AddIngredient(ItemID.FragmentStardust, 8);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 140000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
