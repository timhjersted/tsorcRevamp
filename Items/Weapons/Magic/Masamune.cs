using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class Masamune : ModItem
    {
        public const int MaxManaSubtract = 200;
        public const int MaxManaDivisor = 6;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxManaSubtract, MaxManaDivisor);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 48;
            Item.height = 72;
            Item.useAnimation = 12;
            Item.useTime = 12;
            Item.damage = 200;
            Item.mana = 30;
            Item.knockBack = 9;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Red;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Red_10;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<Projectiles.HealingWater>();
            Item.shootSpeed = 13f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<Murassame>(), 1);
            recipe.AddIngredient(ModContent.ItemType<GuardianSoul>(), 1);
            recipe.AddIngredient(ModContent.ItemType<BlueTitanite>(), 10);
            recipe.AddIngredient(ModContent.ItemType<GhostWyvernSoul>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 250000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (player.statManaMax2 >= MaxManaSubtract)
            {
                damage.Flat += (player.statManaMax2 - MaxManaSubtract) / 6;
            }
        }
    }
}
