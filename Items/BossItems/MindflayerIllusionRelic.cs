using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems {
    class MindflayerIllusionRelic : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The final battle with Attraidies. \n" +
                "No more illusions.");
        }

        public override void SetDefaults() {
            item.rare = ItemRarityID.LightRed;
            item.width = 38;
            item.height = 34;
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.useAnimation = 45;
            item.useTime = 45;
            item.maxStack = 1;
            item.consumable = false;
            item.channel = true;
        }
        public override bool UseItem(Player player)
        {
            Main.NewText("I am impressed you've made it this far, Red. But I'm done playing games. It's time to end this...", 175, 75, 255);

            int offset = 50 * 16;
            int effectOffset = 65;
            Vector2 spawnPoint = new Vector2(player.position.X, player.position.Y);
            int dustType = DustID.PurpleCrystalShard;
            Vector2 vfx = new Vector2(spawnPoint.X, spawnPoint.Y);
            if (player.direction == 1)
            {
                spawnPoint.X += offset;
                vfx.X += offset - effectOffset;
            }
            else
            {
                spawnPoint.X -= offset;
                vfx.X -= offset;
            }


            NPC Attraidies = Main.npc[NPC.NewNPC((int)spawnPoint.X, (int)spawnPoint.Y, ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>())];
            
            for (int i = 0; i < 50; i++)
            {
                vfx = Attraidies.Center;
                Vector2 vel = Main.rand.NextVector2Circular(10,10);
                int dust;
                dust = Dust.NewDust(vfx, 30, 30, dustType, vel.X, vel.Y, 100, default, 5f);
                Main.dust[dust].noGravity = true;
                Dust.NewDust(vfx, 30, 30, dustType, vel.X, vel.Y, 240, default, 5f);
                Main.dust[dust].noGravity = true;
                Dust.NewDust(vfx, 30, 30, DustID.Fire, vel.X, vel.Y, 200, default, 3f);

                Dust.NewDustPerfect(player.position, dustType, vel, 100, default, 5f).noGravity = true;
            }
            
            //Flip it turnways if the player is facing the other way
            spawnPoint.X -= 14 * player.direction;

            Main.dayTime = false;
            Main.time = 0;
            return true;
        }

        //Was gonna make it have to charge up for a second to activate, but... eh
        //int cast = 0;
        public override bool CanUseItem(Player player) {
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>())) {
                return false;
            }
            for (int i = 0; i < 50; i++)
            {
                Dust.NewDustPerfect(player.Center, DustID.PurpleCrystalShard, Main.rand.NextVector2Circular(10, 10), 100, default, 5f).noGravity = true;
            }
            return true;
        }
    }
}
