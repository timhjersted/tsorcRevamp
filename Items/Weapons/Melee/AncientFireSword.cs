using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class AncientFireSword : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The blade is a magic flame, slicing quickly. \n" +
                                "Will set enemies ablaze and do damage over time.");

        }

        public override void SetDefaults() {

            item.stack = 1;
            //item.prefixType=121;
            item.rare = ItemRarityID.Green;
            item.damage = 22;
            item.height = 32;
            item.knockBack = (float)5.5;
            item.autoReuse = true;
            item.maxStack = 1;
            item.melee = true;
            item.scale = (float)1.05;
            item.useAnimation = 17;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 17;
            item.value = PriceByRarity.Green_2;
            item.width = 40;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.GoldBroadsword, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void OnHitNPC(Player player, NPC npc, int damage, float knockBack, bool crit) {
            if (Main.rand.Next(2) == 0) { //50% chance to occur
                npc.AddBuff(BuffID.OnFire, 360, false);
            }
        }

        public override void MeleeEffects(Player player, Rectangle rectangle) { 
            int dust = Dust.NewDust(new Vector2((float)rectangle.X, (float)rectangle.Y), rectangle.Width, rectangle.Height, 6, (player.velocity.X * 0.2f) + (player.direction * 3), player.velocity.Y * 0.2f, 100, default, 1.9f);
            Main.dust[dust].noGravity = true;
        }

    }
}
