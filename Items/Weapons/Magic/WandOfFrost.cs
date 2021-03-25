using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class WandOfFrost : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Wand of Frost");
            Tooltip.SetDefault("A powerful wand made for fighting magic users that can shoot through walls." +
                                "\nCan be upgraded with 25,000 Dark Souls, 60 Crystal Shards, and 5 Souls of Light");
            Item.staff[item.type] = true;
        }

        public override void SetDefaults() {
            item.damage = 35;
            item.height = 30;
            item.knockBack = 4;
            item.rare = ItemRarityID.Green;
            item.shootSpeed = 6;
            item.magic = true;
            item.mana = 15;
            item.useAnimation = 20;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 20;
            item.value = 200000;
            item.width = 30;
            item.shoot = ModContent.ProjectileType<Projectiles.Icicle>();
        }
        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit) {
            target.AddBuff(BuffID.Frostburn, 360);
        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod); ;
            recipe.AddIngredient(mod.GetItem("WoodenWand"), 1);
            recipe.AddIngredient(ItemID.CrystalShard, 100);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 6000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
