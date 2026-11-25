using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Projectiles.Melee.Spears;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Spears.HolyLance
{
    public class HolyLanceItem : ModdedSpearItem
    {
        public override int ProjectileID => ModContent.ProjectileType<HolyLanceProj>();
        public override int Width => 76;
        public override int Height => 76;
        public override int BaseDmg => 53;
        public override int BaseCritChance => 0;
        public override float BaseKnockback => 6.5f;
        public override int UseAnimationTime => 24;
        public override int UseTime => 24;
        public override int Rarity => ItemRarityID.LightRed;
        public override int Value => PriceByRarity.fromItem(Item);
        public override SoundStyle UseSoundID => SoundID.Item71;
        private int _swingCounter = 0; 
        public override bool? UseItem(Player player)
        {
            base.UseItem(player);
            _swingCounter++; 

            if (_swingCounter >= 2)
            {
                _swingCounter = 0; 

                Projectile.NewProjectile(Item.GetSource_FromThis(), player.Center, player.DirectionTo(Main.MouseWorld) * 15,
                    Terraria.ID.ProjectileID.FairyQueenRangedItemShot, Item.damage, Item.knockBack, player.whoAmI
                );
            }

            return null;
        }

        public override void AddRecipes() 
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MythrilHalberd);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 6000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
