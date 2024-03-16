using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Spears;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    public class SupremeDragoonLance : ModdedSpearItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/Spears/DragoonLance";
        public override int ProjectileID => ModContent.ProjectileType<SupremeDragoonLanceProjectile>();
        public override int Width => 50;
        public override int Height => 50;
        public override int BaseDmg => 300;
        public override int BaseCritChance => 26;
        public override float BaseKnockback => 15f;
        public override int UseAnimationTime => 15;
        public override int UseTime => 15;
        public override int Rarity => ItemRarityID.Purple;
        public override int Value => PriceByRarity.fromItem(Item);
        public override SoundStyle UseSoundID => SoundID.Item71;
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DragoonLance>());
            recipe.AddIngredient(ModContent.ItemType<FlameOfTheAbyss>(), 9);
            recipe.AddIngredient(ModContent.ItemType<DragonEssence>(), 9);
            recipe.AddIngredient(ModContent.ItemType<SoulOfArtorias>());
            recipe.AddIngredient(ModContent.ItemType<GuardianSoul>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 170000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
