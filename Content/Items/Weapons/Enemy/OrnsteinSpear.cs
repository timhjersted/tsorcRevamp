using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Items.Weapons.Melee;
using tsorcRevamp.Content.Projectiles.Melee.Spears;

namespace tsorcRevamp.Content.Items.Weapons.Enemy
{
    public class OrnsteinSpear : ModdedSpearItem
    {
        public override int ProjectileID => ModContent.ProjectileType<OrnsteinSpearProj>();
        public override int Width => 96;
        public override int Height => 96;
        public override int BaseDmg => 64;
        public override int BaseCritChance => 0;
        public override float BaseKnockback => 6f;
        public override int UseAnimationTime => 24;
        public override int UseTime => 24;
        public override int Rarity => ItemRarityID.Pink;
        public override int Value => PriceByRarity.fromItem(Item);
        public override SoundStyle UseSoundID => SoundID.Item71;

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (!base.CanUseItem(player) || player.ownedProjectileCounts[ModContent.ProjectileType<OrnsteinSpearLunge>()] > 0)
            {
                return false;
            }

            return player.altFunctionUse != 2
                || (!player.mount.Active && !player.GetModPlayer<tsorcRevampPlayer>().isDodging);
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                type = ModContent.ProjectileType<OrnsteinSpearLunge>();
                position = player.MountedCenter;
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient<SoulOfOrnstein>();
            recipe.AddIngredient<DarkSoulItem>(25000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
