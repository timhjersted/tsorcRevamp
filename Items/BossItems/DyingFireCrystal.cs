using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems {
    class DyingFireCrystal : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The fading Crystal of Fire. \n" + "Will summon Marilith. \n" + "Item is non-consumable");
        }

        public override void SetDefaults() {
            Item.rare = ItemRarityID.LightRed;
            Item.width = 12;
            Item.height = 12;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 5;
            Item.useTime = 5;
            Item.maxStack = 1;
            Item.consumable = false;
        }


        public override bool? UseItem(Player player) {
            Main.NewText("Fire Fiend Maralith erupts from a pillar of dark flame", Color.OrangeRed);

            int offset = 50 * 16;
            int effectOffset = 65;
            Vector2 spawnPoint = new Vector2(player.position.X, player.position.Y);
            int dustType = 174;
            Vector2 vfx = new Vector2(spawnPoint.X, spawnPoint.Y);
            if (player.direction == 1)
            {
                spawnPoint.X += offset;
                vfx.X += offset - effectOffset;
            } else
            {
                spawnPoint.X -= offset;
                vfx.X -= offset;
            }
            for (int i = 0; i < 50; i++)
            {
                Color color = Color.OrangeRed;
                int dust = Dust.NewDust(vfx, 160, 0, dustType, Main.rand.Next(-2, 2), Main.rand.Next(0, 3) * -2, 100, Color.Orange, 10f);
               //Main.dust[dust].noGravity = false;
                dust = Dust.NewDust(vfx, 130, 40, dustType, Main.rand.Next(-1, 1), Main.rand.Next(0, 20) * -2, 100, Color.OrangeRed, 9f);
                //Main.dust[dust].noGravity = false;
                dust = Dust.NewDust(vfx, 130, 50, 182, Main.rand.Next(-1, 1), Main.rand.Next(0, 30) * -2, 100, Color.Red, 8f);
                //Main.dust[dust].noGravity = false;
                for (int j = 0; j < 3; j++)
                {
                    dust = Dust.NewDust(vfx, 50, 30, 231, Main.rand.Next(-1, 1), Main.rand.Next(0, 60) * -2, 100, Color.DarkRed, 2f);
                }
               // Main.dust[dust].noGravity = true; 182, 174, 127, 90, 259, 230, 266, 233, 170
            }

            //NPC's spawn off-center, this adjusts her so the particles are centered around her
            if (player.direction == 1)
            {
                spawnPoint.X -= 40;
            }
            else
            {
                spawnPoint.X += 40;
            }
            spawnPoint.Y -= 5 * 16;
            NPC.NewNPC((int)spawnPoint.X, (int)spawnPoint.Y, ModContent.NPCType<NPCs.Bosses.Fiends.FireFiendMarilith>());

            return true;
        }
        public override bool CanUseItem(Player player) {
            return (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Fiends.FireFiendMarilith>()));
        }

       
        public override void AddRecipes() {
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                Recipe recipe = new Recipe(Mod);
                recipe.AddIngredient(Mod.GetItem("RedTitanite"), 10);
                recipe.AddIngredient(Mod.GetItem("DarkSoul"), 1000);
                recipe.AddTile(TileID.DemonAltar);
                recipe.SetResult(this, 1);
                recipe.AddRecipe();
            }
        }
      
    }
}
