using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class WandOfFrost : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Wand of Frost");
            Tooltip.SetDefault("A powerful wand made for fighting magic users that can shoot through walls." +
                                "\nCan be upgraded with 25,000 Dark Souls, 60 Crystal Shards, and 5 Souls of Light");
            Item.staff[Item.type] = true;
        }

        public override void SetDefaults() {
            Item.damage = 35;
            Item.height = 30;
            Item.knockBack = 4;
            Item.rare = ItemRarityID.Orange;
            Item.shootSpeed = 11;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.mana = 15;
            Item.autoReuse = true;
            Item.useAnimation = 26;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 26;
            Item.value = PriceByRarity.Orange_3;
            Item.width = 30;
            Item.shoot = ModContent.ProjectileType<Projectiles.Icicle>();
        }
        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit) {
            target.AddBuff(BuffID.Frostburn, 360);
        }
        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod); ;
            recipe.AddIngredient(Mod.GetItem("WoodenWand"), 1);
            recipe.AddIngredient(ItemID.CrystalShard, 100);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 6000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
