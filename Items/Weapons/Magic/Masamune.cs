using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class Masamune : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("The blade almost cannot be seen it cuts so fast, ripping enemies to shreds in seconds." +
                                "\nHas the power to shoot a magical water flame from its blade yet it uses no mana."); */
        }

        public override void SetDefaults()
        {
            Item.width = 48;
            Item.height = 72;
            Item.useAnimation = 12;
            Item.useTime = 12;
            Item.damage = 160;
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
            //recipe.AddIngredient(ItemID.SoulofLight, 3);
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
            if (player.statManaMax2 >= 200)
            {
                damage.Flat += (player.statManaMax2 - 200) / 8;
            }
        }
    }
}
