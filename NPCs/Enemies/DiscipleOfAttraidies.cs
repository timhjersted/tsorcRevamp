using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Enemies {
    class DiscipleOfAttraidies : ModNPC {

        public override void SetDefaults() {
            NPC.npcSlots = 1;
            NPC.damage = 20;
            NPC.defense = 15;
            NPC.height = 44;
            NPC.timeLeft = 22500;
            NPC.lifeMax = 3500;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.lavaImmune = true;
            NPC.value = 10000;
            NPC.width = 28;
            NPC.knockBackResist = 0.2f;
            Main.npcFrameCount[NPC.type] = 3;
            animationType = NPCID.GoblinSorcerer;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            banner = NPC.type;
            bannerItem = ModContent.ItemType<Banners.DiscipleOfAttraidiesBanner>();
        }

        public override void AI() {

            NPC.netUpdate = false;
            NPC.ai[0]++; // Timer Scythe
            NPC.ai[1]++; // Timer Teleport
                         // npc.ai[2]++; // Shots

            if (NPC.life > 1000) {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X, NPC.velocity.Y, 150, Color.Black, 1f);
                Main.dust[dust].noGravity = true;
            }
            else if (NPC.life <= 500) {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X, NPC.velocity.Y, 100, Color.Black, 2f);
                Main.dust[dust].noGravity = true;
            }

            if (Main.netMode != NetmodeID.Server) {
                if (NPC.ai[0] >= 5 && NPC.ai[2] < 5) {
                    float num48 = 2f;
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    int damage = 43;
                    int type = ModContent.ProjectileType<FrozenSaw>();
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -1), type, damage, 0f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)NPC.position.X, (int)NPC.position.Y, 20);
                    NPC.ai[0] = 0;
                    NPC.ai[2]++;
                }
            }

            if (NPC.ai[1] >= 20) {
                NPC.velocity.X *= 0.57f;
                NPC.velocity.Y *= 0.17f;
            }

            if ((NPC.ai[1] >= 200 && NPC.life > 1000) || (NPC.ai[1] >= 120 && NPC.life <= 1000)) {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)NPC.position.X, (int)NPC.position.Y, 8);
                for (int num36 = 0; num36 < 10; num36++) {
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X + Main.rand.Next(-10, 10), NPC.velocity.Y + Main.rand.Next(-10, 10), 200, Color.Red, 4f);
                    Main.dust[dust].noGravity = false;
                }
                NPC.ai[3] = (float)(Main.rand.Next(360) * (Math.PI / 180));
                NPC.ai[2] = 0;
                NPC.ai[1] = 0;
                if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active) {
                    NPC.TargetClosest(true);
                }
                if (Main.player[NPC.target].dead) {
                    NPC.position.X = 0;
                    NPC.position.Y = 0;
                    if (NPC.timeLeft > 10) {
                        NPC.timeLeft = 5;
                        return;
                    }
                }
                else {

                    Player Pt = Main.player[NPC.target];
                    Vector2 NC;
                    Vector2 PtC = Pt.position + new Vector2(Pt.width / 2, Pt.height / 2);
                    NPC.position.X = Pt.position.X + (float)((600 * Math.Cos(NPC.ai[3])) * -1);
                    NPC.position.Y = Pt.position.Y - 35 + (float)((30 * Math.Sin(NPC.ai[3])) * -1);

                    float MinDIST = 300f;
                    float MaxDIST = 600f;
                    Vector2 Diff = NPC.position - Pt.position;
                    if (Diff.Length() > MaxDIST) {
                        Diff *= MaxDIST / Diff.Length();
                    }
                    if (Diff.Length() < MinDIST) {
                        Diff *= MinDIST / Diff.Length();
                    }
                    NPC.position = Pt.position + Diff;

                    NC = NPC.position + new Vector2(NPC.width / 2, NPC.height / 2);

                    float rotation = (float)Math.Atan2(NC.Y - PtC.Y, NC.X - PtC.X);
                    NPC.velocity.X = (float)(Math.Cos(rotation) * 12) * -1;
                    NPC.velocity.Y = (float)(Math.Sin(rotation) * 12) * -1;


                }
            }

            //end of W1k's Death code

            //beginning of Omnir's Ultima Weapon projectile code

            NPC.ai[3]++;

            if (Main.player[NPC.target].dead) {
                NPC.velocity.Y -= 0.04f;
                if (NPC.timeLeft > 10) {
                    NPC.timeLeft = 10;
                    return;
                }
            }

        }

        public override void FindFrame(int currentFrame) {

            if ((NPC.velocity.X > -9 && NPC.velocity.X < 9) && (NPC.velocity.Y > -9 && NPC.velocity.Y < 9)) {
                NPC.frameCounter = 0;
                NPC.frame.Y = 0;
                if (NPC.position.X > Main.player[NPC.target].position.X) {
                    NPC.spriteDirection = -1;
                }
                else {
                    NPC.spriteDirection = 1;
                }
            }

            int num = 1;
            if (!Main.dedServ) {
                num = Main.npcTexture[NPC.type].Height / Main.npcFrameCount[NPC.type];
            }
            if ((NPC.velocity.X > -2 && NPC.velocity.X < 2) && (NPC.velocity.Y > -2 && NPC.velocity.Y < 2)) {
                NPC.frameCounter = 0;
                NPC.frame.Y = 0;
            }
            else {
                NPC.frameCounter += 1.0;
            }
            if (NPC.frameCounter >= 1.0) {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type]) {
                NPC.frame.Y = 0;
            }
        }
        public override void OnKill()
        {
            Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Potions.HealingElixir>(), 3);
        }

        public override void HitEffect(int hitDirection, double damage) {
            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
            if (NPC.life <= 0) {
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Undead Caster Gore 1"), 1f);
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Undead Caster Gore 2"), 1f);
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Undead Caster Gore 2"), 1f);
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Undead Caster Gore 3"), 1f);
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Undead Caster Gore 3"), 1f);
            }
        }

    }
}
