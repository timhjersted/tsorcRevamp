using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Armors;
using tsorcRevamp.Items.Accessories;
using static tsorcRevamp.SpawnHelper;
using System.IO;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    [AutoloadBossHead]
    class Witchking : ModNPC {
        float customAi1;
        float customspawn1;
        int chargeDamage = 0;
        bool chargeDamageFlag = false;

        public override void SetDefaults() {
            NPC.npcSlots = 5;
            NPC.aiStyle = 3;
            NPC.height = 45;
            NPC.width = 30;
            NPC.damage = 150;
            NPC.defense = 10;
            NPC.lifeMax = 60000;
            NPC.scale = 1.05f;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.value = 350000;
            NPC.knockBackResist = 0.001f;
            NPC.boss = true;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
            animationType = NPCID.PossessedArmor;
            Main.npcFrameCount[NPC.type] = Main.npcFrameCount[NPCID.PossessedArmor];
            bossBag = ModContent.ItemType<Items.BossBags.WitchkingBag>();
            despawnHandler = new NPCDespawnHandler("The Witchking claims another victim...", Color.Purple, DustID.PurpleTorch);
        }

        int blackBreathDamage = 43; 
        bool defenseBroken = false;


        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.damage = (int)(NPC.damage / 2);
            blackBreathDamage = (int)(blackBreathDamage / 2);
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) {
            if (Main.rand.Next(2) == 0) {
                target.AddBuff(BuffID.Weak, 7200);
                target.AddBuff(BuffID.Bleeding, 1200);
                target.AddBuff(ModContent.BuffType<Buffs.BrokenSpirit>(), 8200);
            }
        }


        public override float SpawnChance(NPCSpawnInfo spawnInfo) {
            Player p = spawnInfo.Player;
            if (tsorcRevampWorld.SuperHardMode) {
                if (!Main.dayTime && !Main.bloodMoon) { //normal night
                    if (spawnInfo.SpawnTileY < Main.worldSurface) { return 0.00125f; }
                    else if (p.ZoneDirtLayerHeight) { return 0.001667f; }
                    else if (p.ZoneRockLayerHeight) { return 0.00125f; }
                    else if (p.ZoneDungeon) { return 0.005f; }
                }

                else if (!Main.dayTime && Main.bloodMoon) {

                    if (!tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Witchking>())) { //bloodmoon, witchking not slain
                        if (p.ZoneDungeon) { return 0.1f; }
                        else { return 0.033f; }
                    }

                    else if (!tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Artorias>()) && p.ZoneDungeon) {//blood moon, witchking slain, artorias not slain, dungeon
                        return 0.066f;
                    }

                    else if (spawnInfo.SpawnTileY < Main.worldSurface) { return 0.01f; } //bloodmoon, surface

                }

                else if (!Main.dayTime && p.townNPCs > 2f && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Witchking>())) { return 0.033f; }

            }

            return 0f;
        }

        #region AI
        #region Movement
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            if (NPC.HasBuff(ModContent.BuffType<Buffs.DispelShadow>()))
            {
                defenseBroken = true;
            }

            bool flag2 = false;
            int num5 = 60;
            bool flag3 = true;
            if (NPC.velocity.Y == 0f && (NPC.velocity.X == 0f && NPC.direction < 0)) {
                NPC.velocity.Y -= 8f;
                NPC.velocity.X -= 2f;
            }
            else if (NPC.velocity.Y == 0f && (NPC.velocity.X == 0f && NPC.direction > 0)) {
                NPC.velocity.Y -= 8f;
                NPC.velocity.X += 2f;
            }
            if (NPC.velocity.Y == 0f && ((NPC.velocity.X > 0f && NPC.direction < 0) || (NPC.velocity.X < 0f && NPC.direction > 0))) {
                flag2 = true;
            }
            if (NPC.position.X == NPC.oldPosition.X || NPC.ai[3] >= (float)num5 || flag2) {
                NPC.ai[3] += 1f;
            }
            else {
                if ((double)Math.Abs(NPC.velocity.X) > 0.9 && NPC.ai[3] > 0f) {
                    NPC.ai[3] -= 1f;
                }
            }
            if (NPC.ai[3] > (float)(num5 * 10)) {
                NPC.ai[3] = 0f;
            }
            if (NPC.justHit) {
                NPC.ai[3] = 0f;
            }
            if (NPC.ai[3] == (float)num5) {
                NPC.netUpdate = true;
            }
            else {
                if (NPC.velocity.X == 0f) {
                    if (NPC.velocity.Y == 0f) {
                        NPC.ai[0] += 1f;
                        if (NPC.ai[0] >= 2f) {
                            NPC.direction *= -1;
                            NPC.spriteDirection = NPC.direction;
                            NPC.ai[0] = 0f;
                        }
                    }
                }
                else {
                    NPC.ai[0] = 0f;
                }
                if (NPC.direction == 0) {
                    NPC.direction = 1;
                }
            }
            if (NPC.velocity.X < -1.5f || NPC.velocity.X > 1.5f) {
                if (NPC.velocity.Y == 0f) {
                    NPC.velocity *= 0.8f;
                }
            }
            else {
                if (NPC.velocity.X < 1.5f && NPC.direction == 1) {
                    NPC.velocity.X = NPC.velocity.X + 0.07f;
                    if (NPC.velocity.X > 1.5f) {
                        NPC.velocity.X = 1.5f;
                    }
                }
                else {
                    if (NPC.velocity.X > -1.5f && NPC.direction == -1) {
                        NPC.velocity.X = NPC.velocity.X - 0.07f;
                        if (NPC.velocity.X < -1.5f) {
                            NPC.velocity.X = -1.5f;
                        }
                    }
                }
            }
            bool flag4 = false;
            if (NPC.velocity.Y == 0f) {
                int num29 = (int)(NPC.position.Y + (float)NPC.height + 8f) / 16;
                int num30 = (int)NPC.position.X / 16;
                int num31 = (int)(NPC.position.X + (float)NPC.width) / 16;
                for (int l = num30; l <= num31; l++) {
                    if (Main.tile[l, num29] == null) {
                        return;
                    }
                    if (Main.tile[l, num29].HasTile && Main.tileSolid[(int)Main.tile[l, num29].TileType]) {
                        flag4 = true;
                        break;
                    }
                }
            }
            if (flag4) {
                int num32 = (int)((NPC.position.X + (float)(NPC.width / 2) + (float)(15 * NPC.direction)) / 16f);
                int num33 = (int)((NPC.position.Y + (float)NPC.height - 15f) / 16f);
                if (Main.tile[num32, num33] == null) {
                    Main.tile[num32, num33].ClearTile();
                }
                if (Main.tile[num32, num33 - 1] == null) {
                    Main.tile[num32, num33 - 1].ClearTile();
                }
                if (Main.tile[num32, num33 - 2] == null) {
                    Main.tile[num32, num33 - 2].ClearTile();
                }
                if (Main.tile[num32, num33 - 3] == null) {
                    Main.tile[num32, num33 - 3].ClearTile();
                }
                if (Main.tile[num32, num33 + 1] == null) {
                    Main.tile[num32, num33 + 1].ClearTile();
                }
                if (Main.tile[num32 + NPC.direction, num33 - 1] == null) {
                    Main.tile[num32 + NPC.direction, num33 - 1].ClearTile();
                }
                if (Main.tile[num32 + NPC.direction, num33 + 1] == null) {
                    Main.tile[num32 + NPC.direction, num33 + 1].ClearTile();
                }
                if (Main.tile[num32, num33 - 1].HasTile && Main.tile[num32, num33 - 1].TileType == 10 && flag3) {
                    NPC.ai[2] += 1f;
                    NPC.ai[3] = 0f;
                    if (NPC.ai[2] >= 60f) {
                        NPC.velocity.X = 0.5f * (float)(-(float)NPC.direction);
                        NPC.ai[1] += 1f;
                        NPC.ai[2] = 0f;
                        bool flag5 = false;
                        if (NPC.ai[1] >= 10f) {
                            flag5 = true;
                            NPC.ai[1] = 10f;
                        }
                        WorldGen.KillTile(num32, num33 - 1, true, false, false);
                        if ((Main.netMode != NetmodeID.MultiplayerClient || !flag5) && flag5 && Main.netMode != NetmodeID.MultiplayerClient) {
                            if (NPC.type == NPCID.GoblinPeon) {
                                WorldGen.KillTile(num32, num33 - 1, false, false, false);
                                if (Main.netMode == NetmodeID.Server) {
                                    NetMessage.SendData(MessageID.TileChange, -1, -1, null, 0, (float)num32, (float)(num33 - 1), 0f, 0);
                                }
                            }
                            else {
                                bool flag6 = WorldGen.OpenDoor(num32, num33, NPC.direction);
                                if (!flag6) {
                                    NPC.ai[3] = (float)num5;
                                    NPC.netUpdate = true;
                                }
                                if (Main.netMode == NetmodeID.Server && flag6) {
                                    NetMessage.SendData(MessageID.ChangeDoor, -1, -1, null, 0, (float)num32, (float)num33, (float)NPC.direction, 0);
                                }
                            }
                        }
                    }
                }
                else {
                    if ((NPC.velocity.X < 0f && NPC.spriteDirection == -1) || (NPC.velocity.X > 0f && NPC.spriteDirection == 1)) {
                        if (Main.tile[num32, num33 - 2].HasTile && Main.tileSolid[(int)Main.tile[num32, num33 - 2].TileType]) {
                            if (Main.tile[num32, num33 - 3].HasTile && Main.tileSolid[(int)Main.tile[num32, num33 - 3].TileType]) {
                                NPC.velocity.Y = -8f;
                                NPC.netUpdate = true;
                            }
                            else {
                                NPC.velocity.Y = -7f;
                                NPC.netUpdate = true;
                            }
                        }
                        else {
                            if (Main.tile[num32, num33 - 1].HasTile && Main.tileSolid[(int)Main.tile[num32, num33 - 1].TileType]) {
                                NPC.velocity.Y = -6f;
                                NPC.netUpdate = true;
                            }
                            else {
                                if (Main.tile[num32, num33].HasTile && Main.tileSolid[(int)Main.tile[num32, num33].TileType]) {
                                    NPC.velocity.Y = -5f;
                                    NPC.netUpdate = true;
                                }
                                else {
                                    if (NPC.directionY < 0 && (!Main.tile[num32, num33 + 1].HasTile || !Main.tileSolid[(int)Main.tile[num32, num33 + 1].TileType]) && (!Main.tile[num32 + NPC.direction, num33 + 1].HasTile || !Main.tileSolid[(int)Main.tile[num32 + NPC.direction, num33 + 1].TileType])) {
                                        NPC.velocity.Y = -8f;
                                        NPC.velocity.X = NPC.velocity.X * 1.5f;
                                        NPC.netUpdate = true;
                                    }
                                    else {
                                        if (flag3) {
                                            NPC.ai[1] = 0f;
                                            NPC.ai[2] = 0f;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else {
                if (flag3) {
                    NPC.ai[1] = 0f;
                    NPC.ai[2] = 0f;
                }
            }
            #endregion
            #region Charge
            //if(Main.netMode != 1)
            //{
            if (Main.rand.Next(300) == 1) {
                chargeDamageFlag = true;
                Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                NPC.velocity.X = (float)(Math.Cos(rotation) * 10) * -1;
                NPC.velocity.Y = (float)(Math.Sin(rotation) * 10) * -1;
                NPC.ai[1] = 1f;
                NPC.netUpdate = true;
            }
            if (chargeDamageFlag == true) {
                NPC.damage = 180;
                chargeDamage++;
            }
            if (chargeDamage >= 101) {
                chargeDamageFlag = false;
                NPC.damage = 100;
                chargeDamage = 0;
            }
            #endregion
            #region Projectiles
            customAi1 += (Main.rand.Next(2, 5) * 0.1f) * NPC.scale;
            if (customAi1 >= 10f) {
                if ((customspawn1 < 36) && Main.rand.Next(800) == 1) { //was 2 and 900
                    int Spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<Enemies.GhostOfTheDarkmoonKnight>(), 0);
                    Main.npc[Spawned].velocity.Y = -8;
                    Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
                    NPC.ai[0] = 20 - Main.rand.Next(180); //was 80
                    customspawn1 += 1f;
                    if (Main.netMode != NetmodeID.MultiplayerClient) {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
                    }
                }



                if (Main.rand.Next(65) == 1) {
                    float num48 = 8f; //was 8f
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                    float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                    if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f))) {
                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int type = ModContent.ProjectileType<Projectiles.Enemy.BlackBreath>();//44;//0x37; //14;
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, type, blackBreathDamage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 40;
                        Main.projectile[num54].aiStyle = 1;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)NPC.position.X, (int)NPC.position.Y, 0x11);
                        customAi1 = 1f;
                    }
                    NPC.netUpdate = true;
                }
            }
            //}

            #region Phase Through Walls
            if ((Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))) {
                NPC.noTileCollide = false;
                NPC.noGravity = false;
            }
            if ((!Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))) {
                NPC.noTileCollide = true;
                NPC.noGravity = true;
                NPC.velocity.Y = 0f;
                if (NPC.position.Y > Main.player[NPC.target].position.Y) {
                    NPC.velocity.Y -= 3f;
                }
                if (NPC.position.Y < Main.player[NPC.target].position.Y) {
                    NPC.velocity.Y += 8f;
                }
            }
            #endregion           
        }
        #endregion
        #endregion
        public override void SendExtraAI(BinaryWriter writer)
        {
            if (NPC.HasBuff(ModContent.BuffType<Buffs.DispelShadow>()))
            {
                defenseBroken = true;
            }
            if (defenseBroken)
            {
                writer.Write(true);
            }
            else
            {
                writer.Write(false);
            }
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            bool recievedBrokenDef = reader.ReadBoolean();
            if (recievedBrokenDef == true)
            {
                defenseBroken = true;
                NPC.defense = 0;
            }
        }

        public override void ModifyHitByItem(Player player, Item item, ref int damage, ref float knockback, ref bool crit)
        {
            if (item.type == ModContent.ItemType<Items.Weapons.Melee.BarrowBlade>() || item.type == ModContent.ItemType<Items.Weapons.Melee.ForgottenGaiaSword>())
            {
                defenseBroken = true;
            }
            if (!defenseBroken)
            {
                damage = 1;
            }
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (!defenseBroken)
            {
                damage = 1;
            }
        }

        public override void HitEffect(int hitDirection, double damage) {
            if (NPC.life <= 0) {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, 54, 0.3f, 0.3f, 200, default(Color), 1f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 54, 0.2f, 0.2f, 200, default(Color), 2f);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, 54, 0.2f, 0.2f, 200, default(Color), 2f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 54, 0.2f, 0.2f, 200, default(Color), 3f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 54, 0.2f, 0.2f, 200, default(Color), 2f);
                Dust.NewDust(NPC.position, NPC.width, NPC.height, 54, 0.2f, 0.2f, 200, default(Color), 2f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 54, 0.2f, 0.2f, 200, default(Color), 3f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 54, 0.2f, 0.2f, 200, default(Color), 2f);
                Dust.NewDust(NPC.position, NPC.height, NPC.width, 54, 0.2f, 0.2f, 200, default(Color), 2f);
            }
        }
        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }
        public override void OnKill()
        {
            if (Main.expertMode)
            {
                NPC.DropBossBags();
            }
            else
            {
                Item.NewItem(NPC.getRect(), ModContent.ItemType<BrokenStrangeMagicRing>());
                if (Main.rand.NextFloat() <= .12f) Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Weapons.Melee.WitchkingsSword>(), 1, false, -1);
                if (Main.rand.Next(10) == 0) Item.NewItem(NPC.getRect(), ModContent.ItemType<WitchkingHelmet>());
                if (Main.rand.Next(10) == 0) Item.NewItem(NPC.getRect(), ModContent.ItemType<WitchkingTop>());
                if (Main.rand.Next(10) == 0) Item.NewItem(NPC.getRect(), ModContent.ItemType<WitchkingBottoms>());
                if (Main.rand.Next(20) == 0) Item.NewItem(NPC.getRect(), ModContent.ItemType<RingOfPower>(), 1, false, -1);
                if (Main.rand.NextFloat() <= .08f) Item.NewItem(NPC.getRect(), ModContent.ItemType<GoldenHairpin>(), 1, false, -1);
                if (Main.rand.NextFloat() <= .15f) Item.NewItem(NPC.getRect(), ModContent.ItemType<GuardianSoul>());
                if (Main.rand.Next(2) == 0) Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.BossItems.DarkMirror>());
                Item.NewItem(NPC.getRect(), ModContent.ItemType<CovenantOfArtorias>(), 1, false, -1);
                Item.NewItem(NPC.getRect(), ModContent.ItemType<DarkSoul>(), 2500);
            }
        }
    }
}
