using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Projectiles.Melee.Spears;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Spears
{
    public class SupremeDragoonLance : ModdedSpearItem
    {
        public override int ProjectileID => ModContent.ProjectileType<SupremeDragoonLanceProjectile>();
        public override int Width => 74;
        public override int Height => 74;
        public override int BaseDmg => 450;
        public override int BaseCritChance => 26;
        public override float BaseKnockback => 15f;
        public override int UseAnimationTime => 21;
        public override int UseTime => 21;
        public override int Rarity => ModContent.RarityType<DarkBlue>();
        public override int Value => PriceByRarity.fromItem(Item);
        public override SoundStyle UseSoundID => SoundID.Item71;
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DragoonLance>());
            recipe.AddIngredient(ModContent.ItemType<FlameOfTheAbyss>(), 8);
            recipe.AddIngredient(ModContent.ItemType<DragonEssence>(), 8);
            recipe.AddIngredient(ModContent.ItemType<SoulOfOccultist>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 120000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
