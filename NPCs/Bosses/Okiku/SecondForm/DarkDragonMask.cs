using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Okiku;

namespace tsorcRevamp.NPCs.Bosses.Okiku.SecondForm {
    [AutoloadBossHead]
    public class DarkDragonMask : ModNPC {
        public bool OptionSpawned;

        public bool ShieldBroken;

        public int TimerRain;

        public int OptionId;

        public float TimerSpawn;

        public bool Initialize = false;

        public override string Texture => "tsorcRevamp/NPCs/Bosses/Okiku/FirstForm/DarkShogunMask";
        public override void SetDefaults() {
            npc.width = 28;
            npc.height = 44;
            npc.aiStyle = -1;
            npc.damage = 1;
            npc.defense = 15;
            npc.boss = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.lifeMax = 10000;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.knockBackResist = 0f;
            npc.npcSlots = 15f;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.Confused] = true;
            despawnHandler = new NPCDespawnHandler("You've been slain at the hand of Attraidies...", Color.DarkMagenta, DustID.PurpleCrystalShard);
        }

        public override void SetStaticDefaults() {
            Main.npcFrameCount[npc.type] = 3;
            DisplayName.SetDefault("Attraidies");
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale) {
            npc.lifeMax = (int)(npc.lifeMax * 0.7f * bossLifeScale);
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection) {
            if (damage > npc.life || damage * 2 > npc.life) {
                crit = false;
                damage = npc.life - 50;
            }
        }
        public override void ModifyHitByItem(Player player, Item item, ref int damage, ref float knockback, ref bool crit) {
            if (damage > npc.life || damage * 2 > npc.life) {
                crit = false;
                damage = npc.life - 50;
            }
        }

        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(npc.whoAmI);

            if (Initialize) {
                OptionSpawned = false;
                ShieldBroken = false;
                TimerRain = 0;
                OptionId = 0;
                TimerSpawn = 0;
            }
            int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 62, 0, 0, 100, Color.White, 1.0f);
            Main.dust[dust].noGravity = true;

            for (int num36 = 0; num36 < 10; num36++) {
                if (Main.player[npc.target].buffType[num36] == 18) {
                    Main.player[npc.target].buffType[num36] = 0;
                    Main.player[npc.target].buffTime[num36] = 0;
                    if (Main.netMode != NetmodeID.Server && Main.myPlayer == npc.target) {
                        Main.NewText("What a horrible night to have your Gravitation buff dispelled...", 150, 150, 150);
                    }
                    break;
                }
            }

            if (OptionSpawned == false) {
                OptionId = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<ShadowDragonHead>(), npc.whoAmI);
                if (Main.netMode == NetmodeID.Server && OptionId < 200) {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, OptionId, 0f, 0f, 0f, 0);
                }
                Main.npc[OptionId].velocity.Y = -10;
                OptionSpawned = true;
            }

            //npc.ai[2]++;
            npc.ai[1]++;

            if (Main.npc[OptionId].active && Main.npc[OptionId].type == ModContent.NPCType<ShadowDragonHead>()) {
                ShieldBroken = false;
            }
            else {
                ShieldBroken = true;
            }

            if (ShieldBroken) {

                if (npc.velocity.X > 7 || npc.velocity.X < -7 || npc.velocity.Y > 7 || npc.velocity.Y < -7) {
                    npc.velocity.X = 0;
                    npc.velocity.Y = 0;
                }
                if (Main.player[npc.target].position.X + (Main.player[npc.target].width / 2) + 100 < npc.position.X + (npc.width / 2)) {
                    if (npc.velocity.X > -6) { float accel = (Main.rand.Next(-40, 40) / 10) + 0.4f; npc.velocity.X -= accel; }
                }
                if (Main.player[npc.target].position.X + (Main.player[npc.target].width / 2) - 100 > npc.position.X + (npc.width / 2)) {
                    if (npc.velocity.X < 6) { float accel = (Main.rand.Next(-40, 40) / 10) + 0.4f; npc.velocity.X += accel; }
                }

                if (Main.player[npc.target].position.Y + (Main.player[npc.target].height / 2) - 200 < npc.position.Y + (npc.height / 2)) {
                    if (npc.velocity.Y > 0f) npc.velocity.Y -= 0.4f;
                    else npc.velocity.Y -= 0.05f;
                }
                if (Main.player[npc.target].position.Y + (Main.player[npc.target].height / 2) - 200 > npc.position.Y + (npc.height / 2)) {
                    if (npc.velocity.Y < 0f) npc.velocity.Y += 0.4f;
                    else npc.velocity.Y += 0.1f;
                }

                if (npc.position.Y < Main.player[npc.target].position.Y - 50) {
                    TimerRain++;
                    if (TimerRain >= 2) {
                        Vector2 vector8 = new Vector2(npc.position.X + (npc.width / 2), npc.position.Y + (npc.height / 2));
                        Projectile.NewProjectile(vector8.X, vector8.Y, Main.rand.Next(-100, 100) / 10, -7, ModContent.ProjectileType<ObscureDrop>(), 64, 0f, 0);
                        TimerRain = 0;
                    }
                }
                TimerSpawn++;
                if (TimerSpawn >= 600) {
                    OptionId = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<ShadowDragonHead>(), npc.whoAmI);
                    Main.npc[OptionId].velocity.Y = -10;
                    TimerSpawn = 0;
                }

            }
            else {

                if (Main.npc[OptionId].type == ModContent.NPCType<ShadowDragonHead>() && (Main.npc[OptionId].position.X + (Main.npc[OptionId].width / 2) < npc.position.X + (npc.width / 2))) {
                    if (npc.velocity.X > -16) { npc.velocity.X -= 1.6f; }
                }
                if (Main.npc[OptionId].type == ModContent.NPCType<ShadowDragonHead>() && Main.npc[OptionId].position.X + (Main.npc[OptionId].width / 2) > npc.position.X + (npc.width / 2)) {
                    if (npc.velocity.X < 16) { npc.velocity.X += 1.6f; }
                }

                if (Main.npc[OptionId].type == ModContent.NPCType<ShadowDragonHead>() && Main.npc[OptionId].position.Y + (Main.npc[OptionId].height / 2) < npc.position.Y + (npc.height / 2)) {
                    if (npc.velocity.Y > 0f) npc.velocity.Y -= 1.6f;
                    else npc.velocity.Y -= 0.2f;
                }
                if (Main.npc[OptionId].type == ModContent.NPCType<ShadowDragonHead>() && Main.npc[OptionId].position.Y + (Main.npc[OptionId].height / 2) > npc.position.Y + (npc.height / 2)) {
                    if (npc.velocity.Y < 0f) npc.velocity.Y += 1.6f;
                    else npc.velocity.Y += 0.2f;
                }

            }

            if (npc.life <= 1000) {
                Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                Vector2 goreVel = new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, -10) * 0.2f);
                Gore.NewGore(vector8, goreVel, mod.GetGoreSlot("Gores/Mindflayer Gore 1"), 1f);
                Gore.NewGore(vector8, goreVel, mod.GetGoreSlot("Gores/Mindflayer Gore 2"), 1f);
                Gore.NewGore(vector8, goreVel, mod.GetGoreSlot("Gores/Mindflayer Gore 3"), 1f);
                Gore.NewGore(vector8, goreVel, mod.GetGoreSlot("Gores/Mindflayer Gore 3"), 1f);
                Gore.NewGore(vector8, goreVel, mod.GetGoreSlot("Gores/Mindflayer Gore 2"), 1f);

                for (int num36 = 0; num36 < 50; num36++) {
                    int dustDeath = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, 54, npc.velocity.X + Main.rand.Next(-10, 10), npc.velocity.Y + Main.rand.Next(-10, 10), 200, Color.White, 4f);
                    Main.dust[dustDeath].noGravity = true;
                }
                Main.npc[OptionId].life = 0;
                NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<ThirdForm.Okiku>(), 0);
                npc.active = false;

                Main.NewText("??????????????????? A booming laughter echoes all around you!", 175, 75, 255);

            }
        }




        public override void FindFrame(int frameHeight) {

            if ((npc.velocity.X > -2 && npc.velocity.X < 2) && (npc.velocity.Y > -2 && npc.velocity.Y < 2)) {
                npc.frameCounter = 0;
                npc.frame.Y = 0;
                if (npc.position.X > Main.player[npc.target].position.X) {
                    npc.spriteDirection = -1;
                }
                else {
                    npc.spriteDirection = 1;
                }
            }
            int num = 1;
            if (!Main.dedServ) {
                num = Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type];
            }

            if (npc.velocity.X > 1.5f) npc.frame.Y = num;
            if (npc.velocity.X < -1.5f) npc.frame.Y = num * 2;
            if (npc.velocity.X > -1.5f && npc.velocity.X < 1.5f) npc.frame.Y = 0;
            if (ShieldBroken) {
                if (npc.alpha > 40) npc.alpha -= 1;
                if (npc.alpha < 40) npc.alpha += 1;
            }
            else {
                if (npc.alpha < 200) npc.alpha += 1;
                if (npc.alpha > 200) npc.alpha -= 1;
            }
        }



    }
}
