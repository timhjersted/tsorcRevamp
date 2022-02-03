using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    class BasaliskHunter : ModNPC
    {
        public override void SetDefaults()
        {

            npc.npcSlots = 2;
            Main.npcFrameCount[npc.type] = 12;
            animationType = 28;
            npc.knockBackResist = 0.03f;
            npc.damage = 95;
            npc.defense = 90; //was 105
            npc.height = 54;
            npc.width = 54;
            npc.lifeMax = 3500; //was 17000
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath5;
            npc.value = 4620;
            npc.lavaImmune = true;
            banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.BasaliskHunterBanner>();

            npc.buffImmune[BuffID.Confused] = true;
            npc.buffImmune[BuffID.OnFire] = true;
        }

        int meteorDamage = 17;
        int cursedBreathDamage = 27;
        int cursedFlamesDamage = 27;
        int darkExplosionDamage = 35;
        int disruptDamage = 65;
        int bioSpitDamage = 50;
        int bioSpitfinalDamage = 40;

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = (int)(npc.lifeMax / 2);
            npc.damage = (int)(npc.damage / 2);
            meteorDamage = (int)(meteorDamage / 2);
            cursedBreathDamage = (int)(cursedBreathDamage / 2);
            darkExplosionDamage = (int)(darkExplosionDamage / 2);
            disruptDamage = (int)(disruptDamage / 2);
            bioSpitDamage = (int)(bioSpitDamage / 2);
            bioSpitfinalDamage = (int)(bioSpitfinalDamage / 2);
        }


        public Player player
        {
            get => Main.player[npc.target];
        }


        //PROJECTILE HIT LOGIC
        public override void OnHitByItem(Player player, Item item, int damage, float knockback, bool crit)
        {
            tsorcRevampAIs.RedKnightOnHit(npc, true);
        }

        public override void OnHitByProjectile(Projectile projectile, int damage, float knockback, bool crit)
        {
            tsorcRevampAIs.RedKnightOnHit(npc, projectile.melee);
        }

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player player = spawnInfo.player;

            bool FrozenOcean = spawnInfo.spawnTileX > (Main.maxTilesX - 800);
            bool Ocean = spawnInfo.spawnTileX < 800 || FrozenOcean;

            float chance = 0;

            if (spawnInfo.water) return 0f;

            if (tsorcRevampWorld.SuperHardMode)
            {
                if (((player.ZoneMeteor || player.ZoneCorrupt || player.ZoneCrimson || player.ZoneUndergroundDesert) && (player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight)) && !player.ZoneDungeon)
                {
                    chance = 0.33f;
                }
                else
                {
                    if (player.ZoneOverworldHeight && (player.ZoneMeteor || player.ZoneCorrupt || player.ZoneCrimson || player.ZoneHoly) && !Ocean && !Main.dayTime)
                    {
                        chance = 0.11f;
                    }
                }
            }
            if (Main.bloodMoon)
            {
                chance *= 2;
            }

            return chance;
        }
        #endregion

        float breathTimer = 0;
        float projTimer = 0;
        float disruptTimer = 0;
        public override void AI()
        {
            tsorcRevampAIs.FighterAI(npc, 3, .1f, 0.001f, true, 0, false, 26, 1000, 0.3f, 1.1f, true);

            bool clearLineofSight = Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height);
            tsorcRevampAIs.SimpleProjectile(npc, ref projTimer, 60, ProjectileID.DD2DrakinShot, bioSpitDamage, 5, clearLineofSight, true, 2, 17);
            tsorcRevampAIs.SimpleProjectile(npc, ref disruptTimer, 360, ModContent.ProjectileType<Projectiles.Enemy.HypnoticDisrupter>(), bioSpitDamage, 5, clearLineofSight && npc.Distance(player.Center) > 230, true, 2, 17);
            tsorcRevampAIs.LeapAtPlayer(npc, 8, 5, 2, 128);

            breathTimer++;
            if(breathTimer > 480)
            {
                breathTimer = -90;
            }

            if(breathTimer < 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 breathVel = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 12);
                    breathVel += Main.rand.NextVector2Circular(-1.5f, 1.5f);
                    Projectile.NewProjectile(npc.Center.X + (5 * npc.direction), npc.Center.Y, breathVel.X, breathVel.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyCursedBreath>(), cursedFlamesDamage, 0f, Main.myPlayer);
                    npc.ai[3] = 0; //Reset bored counter. No teleporting mid-breath attack
                }                
            }

            if (breathTimer > 360)
            {
                UsefulFunctions.DustRing(npc.Center, (int)(48 * ((480 - breathTimer) / 120)), DustID.CursedTorch, 48, 4);
                Lighting.AddLight(npc.Center, Color.GreenYellow.ToVector3() * 5);
            }            

            if (breathTimer == 0)
            {
                Projectile.NewProjectile(npc.Center.X, npc.Center.Y, 0, 0, ModContent.ProjectileType<Projectiles.Enemy.DarkExplosion>(), darkExplosionDamage, 0f, Main.myPlayer);
            }

            if (npc.justHit)
            {
                projTimer = 0;
                disruptTimer = 0;
            }

            //Temporary. They had a bug that stopped them from jumping up steps, this will fix it for now.
            npc.aiStyle = 3;
        }


        #region FindFrame
        public override void FindFrame(int currentFrame)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type];
            }
            if (npc.velocity.Y == 0f)
            {
                if (npc.direction == 1)
                {
                    npc.spriteDirection = 1;
                }
                if (npc.direction == -1)
                {
                    npc.spriteDirection = -1;
                }
                if (npc.velocity.X == 0f)
                {
                    npc.frame.Y = 0;
                    npc.frameCounter = 0.0;
                }
                else
                {
                    npc.frameCounter += (double)(Math.Abs(npc.velocity.X) * 2f);
                    npc.frameCounter += 1.0;
                    if (npc.frameCounter > 6.0)
                    {
                        npc.frame.Y = npc.frame.Y + num;
                        npc.frameCounter = 0.0;
                    }
                    if (npc.frame.Y / num >= Main.npcFrameCount[npc.type])
                    {
                        npc.frame.Y = num * 2;
                    }
                }
            }
            else
            {
                npc.frameCounter = 0.0;
                npc.frame.Y = num;
                npc.frame.Y = 0;
            }
        }
        #endregion

        #region Debuffs
        public override void OnHitPlayer(Player player, int target, bool crit) 
        {

            if (Main.rand.Next(2) == 0)
            {
                player.AddBuff(37, 10800, false); //horrified
                player.AddBuff(20, 1200, false); //poisoned

            }
            if (Main.rand.Next(6) == 0)
            {
                player.AddBuff(36, 600, false); //broken armor
                player.AddBuff(ModContent.BuffType<Buffs.BrokenSpirit>(), 1800, false);
                player.AddBuff(ModContent.BuffType<Buffs.CurseBuildup>(), 18000, false); //-20 life if counter hits 100
            }


        }
        #endregion


        public override void NPCLoot()
        {
            if (npc.life <= 0)
            {
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 1"), 1.1f);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 2"), 1.1f);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 3"), 1.1f);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 2"), 1.1f);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 1"), 1.1f);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 3"), 1.1f);
                Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 2"), 1.1f);

                for (int i = 0; i < 10; i++)
                {
                    Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Blood Splat"), 1.1f);
                }
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.CursedSoul>(), 3 + Main.rand.Next(3));
                if (Main.rand.Next(100) < 8) Item.NewItem(npc.getRect(), ItemID.GreaterHealingPotion);
            }
        }
    }
}