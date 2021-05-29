using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Potions;

namespace tsorcRevamp.NPCs.Enemies {
    public class Dunlending : ModNPC {
        public override void SetStaticDefaults() {
            Main.npcFrameCount[npc.type] = Main.npcFrameCount[NPCID.PossessedArmor];
        }
        public override void SetDefaults() {
            npc.npcSlots = 1;
            npc.knockBackResist = 0.4f;
            npc.aiStyle = -1;
            npc.damage = 20;
            npc.defDamage = 2;
            npc.height = 40;
            npc.width = 20;
            npc.lifeMax = 40;
            npc.value = 250;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath2;
            animationType = NPCID.PossessedArmor;
        }

        public override void HitEffect(int hitDirection, double damage) {
            for (int i = 0; i < 25; i++) {
                int dustType = 5;
                int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, dustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.06f;
                dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.06f;
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (npc.life <= 0) {
                for (int i = 0; i < 4; i++) {
                    Dust.NewDust(npc.position, npc.width, npc.height, 5, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default(Color), 1f);
                }

                Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dunlending Gore 1"), 1f);
                Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dunlending Gore 2"), 1f);
                Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dunlending Gore 3"), 1f);
                Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dunlending Gore 2"), 1f);
                Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dunlending Gore 3"), 1f);
            }
        }

        public override void AI() {
            int sound_type = 0;
            int sound_frequency = 1000;

            float acceleration = 0.05f;
            float top_speed = 1.5f;
            float braking_power = 0.2f;

            #region play creature sounds, target/face player, respond to boredom
            if (sound_type > 0 && Main.rand.Next(sound_frequency) <= 0)
                Main.PlaySound(sound_type, (int)npc.position.X, (int)npc.position.Y, 1); // random creature sounds
            npc.TargetClosest(true); //  Target the closest player & face him (If passed as a parameter, a bool will determine whether it should face the target or not)

            if (npc.velocity.X == 0f) {
                if (npc.velocity.Y == 0f) { // not moving
                    if (npc.ai[0] == 0f)
                        npc.ai[0] = 1f; // facing change delay
                    else { // change movement and facing direction, reset delay
                        npc.direction *= -1;
                        npc.spriteDirection = npc.direction;
                        npc.ai[0] = 0f;
                    }
                }
            }
            else // moving in x direction,
                npc.ai[0] = 0f; // reset facing change delay

            if (npc.direction == 0) // what does it mean if direction is 0?
                npc.direction = 1; // flee right if direction not set? or is initial direction?

            #endregion

            #region melee movement
            if (Math.Abs(npc.velocity.X) > top_speed && npc.velocity.Y == 0f) {
                npc.velocity *= (1f - braking_power);
            }
            else {
                npc.velocity.X += npc.direction * acceleration;
            }

            #endregion

            #region check if standing on a solid tile
            bool standing_on_solid_tile = false;
            if (npc.velocity.Y == 0f) // no jump/fall
            {
                int y_below_feet = (int)(npc.position.Y + (float)npc.height + 8f) / 16;
                int x_left_edge = (int)npc.position.X / 16;
                int x_right_edge = (int)(npc.position.X + (float)npc.width) / 16;
                for (int l = x_left_edge; l <= x_right_edge; l++) // check every block under feet
                {
                    if (Main.tile[l, y_below_feet] == null) // null tile means ??
                        return;

                    if (Main.tile[l, y_below_feet].active() && Main.tileSolid[(int)Main.tile[l, y_below_feet].type]) // tile exists and is solid
                    {
                        standing_on_solid_tile = true;
                        break; // one is enough so stop checking
                    }
                } // END traverse blocks under feet
            } // END no jump/fall
            #endregion
            
            #region new Tile()s, jumping
            if (standing_on_solid_tile)  //  if standing on solid tile
            {
                int x_in_front = (int)((npc.position.X + (float)(npc.width / 2) + (float)(15 * npc.direction)) / 16f); // 15 pix in front of center of mass
                int y_above_feet = (int)((npc.position.Y + (float)npc.height - 15f) / 16f); // 15 pix above feet
                if (Main.tile[x_in_front, y_above_feet] == null) {
                    Main.tile[x_in_front, y_above_feet] = new Tile();
                }

                if (Main.tile[x_in_front, y_above_feet - 1] == null) {
                    Main.tile[x_in_front, y_above_feet - 1] = new Tile();
                }

                if (Main.tile[x_in_front, y_above_feet - 2] == null) {
                    Main.tile[x_in_front, y_above_feet - 2] = new Tile();
                }

                if (Main.tile[x_in_front, y_above_feet - 3] == null) {
                    Main.tile[x_in_front, y_above_feet - 3] = new Tile();
                }

                if (Main.tile[x_in_front, y_above_feet + 1] == null) {
                    Main.tile[x_in_front, y_above_feet + 1] = new Tile();
                }
                //  create? 2 other tiles farther in front
                if (Main.tile[x_in_front + npc.direction, y_above_feet - 1] == null) {
                    Main.tile[x_in_front + npc.direction, y_above_feet - 1] = new Tile();
                }

                if (Main.tile[x_in_front + npc.direction, y_above_feet + 1] == null) {
                    Main.tile[x_in_front + npc.direction, y_above_feet + 1] = new Tile();
                }

                else // standing on solid tile but not in front of a passable door
   {
                    if ((npc.velocity.X < 0f && npc.spriteDirection == -1) || (npc.velocity.X > 0f && npc.spriteDirection == 1)) {  //  moving forward
                        if (Main.tile[x_in_front, y_above_feet - 2].active() && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet - 2].type]) { // 3 blocks above ground level(head height) blocked
                            if (Main.tile[x_in_front, y_above_feet - 3].active() && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet - 3].type]) { // 4 blocks above ground level(over head) blocked
                                npc.velocity.Y = -8f; // jump with power 8 (for 4 block steps)
                                npc.netUpdate = true;
                            }
                            else {
                                npc.velocity.Y = -7f; // jump with power 7 (for 3 block steps)
                                npc.netUpdate = true;
                            }
                        } // for everything else, head height clear:
                        else if (Main.tile[x_in_front, y_above_feet - 1].active() && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet - 1].type]) { // 2 blocks above ground level(mid body height) blocked
                            npc.velocity.Y = -6f; // jump with power 6 (for 2 block steps)
                            npc.netUpdate = true;
                        }
                        else if (Main.tile[x_in_front, y_above_feet].active() && Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet].type]) { // 1 block above ground level(foot height) blocked
                            npc.velocity.Y = -5f; // jump with power 5 (for 1 block steps)
                            npc.netUpdate = true;
                        }
                        else if (npc.directionY < 0 && (!Main.tile[x_in_front, y_above_feet + 1].active() || !Main.tileSolid[(int)Main.tile[x_in_front, y_above_feet + 1].type]) && (!Main.tile[x_in_front + npc.direction, y_above_feet + 1].active() || !Main.tileSolid[(int)Main.tile[x_in_front + npc.direction, y_above_feet + 1].type])) { // rising? & jumps gaps & no solid tile ahead to step on for 2 spaces in front
                            npc.velocity.Y = -8f; // jump with power 8
                            npc.velocity.X = npc.velocity.X * 1.5f; // jump forward hard as well; we're trying to jump a gap
                            npc.netUpdate = true;
                        }
                    } // END moving forward, still: standing on solid tile but not in front of a passable door
                }
            }

            #endregion
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) {
            var playerY = spawnInfo.playerFloorY;
            var player = spawnInfo.player;
            bool oUnderSurface = (playerY >= (Main.maxTilesY * 0.2f) && playerY < (Main.maxTilesY * 0.3f));
            bool oUnderground = (playerY >= (Main.maxTilesY * 0.3f) && playerY < (Main.maxTilesY * 0.4f));
            bool oCavern = (playerY >= (Main.maxTilesY * 0.4f) && playerY < (Main.maxTilesY * 0.6f));

            float chance = 0;
            if (player.ZoneOverworldHeight) {
                if (Main.dayTime) chance = 0.067f;
                else chance = 0.125f;
            }
            if (oUnderSurface || oUnderground || oCavern) {
                if (Main.dayTime) chance = 0.067f;
                else chance = 0.1f;
            }
            return chance;
        }
        public override void NPCLoot() {
            Item.NewItem(npc.getRect(), ItemID.Torch, 1);
            Item.NewItem(npc.getRect(), ItemID.HealingPotion, Main.rand.Next(20) == 0 ? 6 : 1); // 1/5 chance of 6, else 1
            if (Main.rand.NextFloat() < 0.6f) { //60%
                Item.NewItem(npc.getRect(), ItemID.ShinePotion, Main.rand.Next(1, 3));
            }

            if (Main.rand.NextFloat() < 0.1f) { //10%
                Item.NewItem(npc.getRect(), ModContent.ItemType<BoostPotion>());
            }
            if (Main.rand.Next(10) == 0) { //8%
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Melee.DunlendingAxe>());
            }

            if (Main.rand.Next(20) == 0) { //5%
                Item.NewItem(npc.getRect(), ItemID.IronskinPotion, Main.rand.Next(1, 2));
            }
            if (Main.rand.Next(20) == 0) {
                Item.NewItem(npc.getRect(), ItemID.ManaRegenerationPotion);
            }
            if (Main.rand.Next(20) == 0) {
                Item.NewItem(npc.getRect(), ItemID.SpelunkerPotion);
            }
            if (Main.rand.Next(20) == 0) {
                Item.NewItem(npc.getRect(), ItemID.SwiftnessPotion);
            }
            if (Main.rand.Next(20) == 0) {
                Item.NewItem(npc.getRect(), ItemID.BattlePotion);
            }
            if (Main.rand.Next(50) == 0) { //2%
                Item.NewItem(npc.getRect(), ItemID.RegenerationPotion, Main.rand.Next(1, 5));
            }
            if (Main.rand.Next(100) == 0) { //1%
                Item.NewItem(npc.getRect(), ModContent.ItemType<CrimsonPotion>());
            }
        }
    }
}
