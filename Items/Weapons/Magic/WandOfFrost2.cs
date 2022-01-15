using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class WandOfFrost2 : ModItem {

        bool LegacyMode = ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Wand of Frost II");
            Tooltip.SetDefault("Reforged to reveal the full power of this ancient ice spell" +
                                "\nCan pass through walls");
            Item.staff[item.type] = true;
        }

        public override void SetDefaults() {
            item.damage = 45;
            item.height = 30;
            item.knockBack = 6;
            item.rare = ItemRarityID.Pink; //yes, despite not taking any mech boss items
            item.shootSpeed = 13f;
            item.magic = true;
            item.mana = 25;
            item.useAnimation = LegacyMode ? 20 : 26;
            item.autoReuse = LegacyMode ? false : true;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = LegacyMode ? 20 : 26;
            item.value = PriceByRarity.Pink_5; 
            item.width = 30;
            item.shoot = ModContent.ProjectileType<Projectiles.Icicle>();
        }
        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit) {
            target.AddBuff(BuffID.Frostburn, 360);
        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod); ;
            recipe.AddIngredient(mod.GetItem("WandOfFrost"), 1);
            recipe.AddIngredient(ItemID.CrystalShard, 60);
            recipe.AddIngredient(ItemID.SoulofLight, 5);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 25000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
