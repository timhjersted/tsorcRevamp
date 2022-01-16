using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class AncientFireAxe : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The blade hits with a powerful magic flame.\n" +
                                "Knocks back foes with a force that also sets them ablaze, doing damage over time.");

        }

        public override void SetDefaults() {
            item.rare = ItemRarityID.Green;
            item.damage = 25;
            item.height = 32;
            item.knockBack = (float)10;
            item.maxStack = 1;
            item.melee = true;
            item.scale = (float)1.05;
            item.autoReuse = true;
            item.useAnimation = 25;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 25;
            item.value = PriceByRarity.Green_2;
            item.width = 40;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.GoldAxe, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void OnHitNPC(Terraria.Player player, NPC npc, int damage, float knockBack, bool crit) {
            if (Main.rand.Next(2) == 0) { //50% chance to occur
                npc.AddBuff(BuffID.OnFire, 360, false);
            }
        }

        public override void MeleeEffects(Terraria.Player player, Rectangle rectangle) {
            int dust = Dust.NewDust(new Vector2((float)rectangle.X, (float)rectangle.Y), rectangle.Width, rectangle.Height, 6, (player.velocity.X * 0.2f) + (player.direction * 3), player.velocity.Y * 0.2f, 100, default, 1.9f);
            Main.dust[dust].noGravity = true;
        }

    }
}
