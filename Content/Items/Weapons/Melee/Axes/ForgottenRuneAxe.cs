using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Axes
{
    class ForgottenRuneAxe : ModItem
    {
        public const int BaseDmg = 17;
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.White;
            Item.damage = BaseDmg;
            Item.knockBack = 5f;
            Item.useAnimation = 28;
            Item.useTime = 28;
            Item.width = 46;
            Item.height = 38;
            Item.axe = 55 / 5; // Same as gold axe
            Item.DamageType = DamageClass.Melee;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.White_0;
            Item.shoot = ModContent.ProjectileType<Projectiles.InvisibleNothingProj>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.Gray;
        }
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (Item.prefix == PrefixID.Dull)
            {
                damage.Flat -= 5;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Wood, 10);
            recipe.AddIngredient(ItemID.StoneBlock, 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
