using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using Terraria.Localization;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class Muramassa : ModItem
    {
        public const int MaxManaSubtract = 200;
        public const int MaxManaDivisor = 15;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxManaSubtract, MaxManaDivisor);

        public override void SetDefaults()
        {
            Item.width = 48;
            Item.height = 48;
            Item.useAnimation = 18;
            Item.useTime = 18;
            Item.damage = 15;
            Item.knockBack = 3;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Orange;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Orange_3;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 10;
            Item.shoot = ModContent.ProjectileType<Projectiles.HealingWater>();
            Item.shootSpeed = 11f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Muramasa, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 6000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (player.statManaMax2 >= MaxManaSubtract)
            {
                damage.Flat += (player.statManaMax2 - MaxManaSubtract) / MaxManaDivisor;
            }
        }
    }
}
