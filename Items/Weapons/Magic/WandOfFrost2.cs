using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class WandOfFrost2 : ModItem
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wand of Frost II");
            Tooltip.SetDefault("Reforged to reveal the full power of this ancient ice spell" +
                                "\nCan pass through walls");
            Item.staff[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.damage = 45;
            Item.height = 30;
            Item.knockBack = 6;
            Item.rare = ItemRarityID.Pink; //yes, despite not taking any mech boss items
            Item.shootSpeed = 13f;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 25;
            Item.useAnimation = 26;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 26;
            Item.value = PriceByRarity.Pink_5;
            Item.width = 30;
            Item.shoot = ModContent.ProjectileType<Projectiles.Icicle>();
        }
        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit)
        {
            target.AddBuff(BuffID.Frostburn, 360);
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(); ;
            recipe.AddIngredient(Mod.Find<ModItem>("WandOfFrost").Type, 1);
            //recipe.AddIngredient(ItemID.CrystalShard, 30);
            recipe.AddIngredient(ItemID.SoulofFlight, 9);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 25000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
