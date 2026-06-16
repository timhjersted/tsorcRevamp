using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Buffs;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    class CrazedDemonSpirit : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 4;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.ShadowFlame] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn2] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Ichor] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][ModContent.BuffType<DarkInferno>()] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][ModContent.BuffType<CrimsonBurn>()] = true;
        }
        // HM Enemy
        int demonDamage = 20;
        public override void SetDefaults()
        {
            NPC.aiStyle = 22;
            NPC.npcSlots = 6;
            AnimationType = 60;
            NPC.width = 50;
            NPC.height = 50;
            NPC.damage = 48;
            NPC.defense = 18;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = 500;
            NPC.friendly = false;
            NPC.noTileCollide = true;
            NPC.lavaImmune = true;
            NPC.noGravity = true;
            NPC.knockBackResist = 0;
            NPC.alpha = 100;
            NPC.value = 2500; // life / 2 in HM : was 160
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.CrazedDemonSpiritBanner>();

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.HasGhostAfterimages = true;

            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 1000;
                NPC.defense = 85;
                NPC.damage = 180;
                NPC.value = 4000; // life / 2.5
                NPC.scale = 1.15f;
                demonDamage = 35;
            }

        }
        const int AttackIdleTime = 180;
        const int NormalAttackComboGapTime = 30;
        const int EnragedAttackComboGapTime = 15;
        int attackDelay = AttackIdleTime;
        int queuedAttackPatterns;
        int lastAttackPattern = -1;

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player p = spawnInfo.Player;

            //Ensuring it can't spawn if two already exist.
            int count = 0;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (Main.npc[i].type == NPC.type)
                {
                    count++;
                    if (count > 1)
                    {
                        return 0;
                    }
                }
            }

            bool nospecialbiome = !p.ZoneJungle && !p.ZoneCorrupt && !p.ZoneCrimson && !p.ZoneHallow && !p.ZoneMeteor && !p.ZoneDungeon; // Not necessary at all to use but needed to make all this work.
            bool sky = nospecialbiome && ((double)spawnInfo.SpawnTileY < Main.worldSurface * 0.44999998807907104);
            bool surface = nospecialbiome && !sky && (spawnInfo.SpawnTileY <= Main.worldSurface);
            bool underground = nospecialbiome && !surface && (spawnInfo.SpawnTileY <= Main.rockLayer);
            bool underworld = (p.ZoneUnderworldHeight);
            bool cavern = nospecialbiome && (spawnInfo.SpawnTileY >= Main.rockLayer) && (spawnInfo.SpawnTileY <= Main.rockLayer * 25);
            bool undergroundJungle = (spawnInfo.SpawnTileY >= Main.rockLayer) && (spawnInfo.SpawnTileY <= Main.rockLayer * 25) && p.ZoneJungle;
            bool undergroundEvil = (spawnInfo.SpawnTileY >= Main.rockLayer) && (spawnInfo.SpawnTileY <= Main.rockLayer * 25) && (p.ZoneCorrupt || p.ZoneCrimson);
            bool undergroundHoly = (spawnInfo.SpawnTileY >= Main.rockLayer) && (spawnInfo.SpawnTileY <= Main.rockLayer * 25) && p.ZoneHallow;

            if (p.ZoneMeteor && Main.hardMode && Main.rand.NextBool(25)) return 1;

            if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheSorrow>())))
            {
                if (underworld && Main.rand.NextBool(300)) return 1;
                else if (underworld && Main.hardMode && Main.rand.NextBool(15)) return 1;
            }

            if (tsorcRevampWorld.SuperHardMode)
            {
                if (spawnInfo.Player.ZoneCrimson)
                {
                    return 0.15f;
                }
                else return 0;
            }
            else return 0;
        }
        #endregion

        #region AI
        public override void AI()
        {

            if (NPC.life > NPC.lifeMax / 2)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.Torch, NPC.velocity.X, NPC.velocity.Y, 200, Color.Violet, 2f);
                Main.dust[dust].noGravity = true;
            }
            else if (NPC.life <= NPC.lifeMax / 2)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.Torch, NPC.velocity.X, NPC.velocity.Y, 200, Color.Violet, 3f);
                Main.dust[dust].noGravity = true;
            }


            #region Shooting code
            RunAttackStateMachine();
            #endregion
            if (NPC.justHit)
            {
                NPC.ai[2] = 0f;


                Color color = new Color();
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.Torch, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 200, color, 3f);
                Main.dust[dust].noGravity = true;
                dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.Torch, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 200, color, 3f);
                Main.dust[dust].noGravity = true;
                dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.Wraith, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 200, color, 3f);
                Main.dust[dust].noGravity = true;
                dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.PurpleTorch, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 200, color, 3f);
                Main.dust[dust].noGravity = true;



            }

            //Basically just modifications to the vanilla wraith code. I tried to clean it up and rename the variables to more useful things, but I could only do so much.
            #region Movement AI
            bool flag25 = false;

            //npc.ai[0] = a snapshot of its X position at some point in time
            //npc.ai[1] = a snapshot of its Y position at some point in time
            //npc.ai[2] = Counts up every frame to 60, then drops to 200
            if (NPC.ai[2] >= 0f)
            {

                int strayLimit = 16;


                bool flag26 = false;
                bool flag27 = false;

                //If it HAS NOT moved 16 units left or right since ai[0] was last set to its position
                if (NPC.position.X > (NPC.ai[0] - (float)strayLimit) && NPC.position.X < (NPC.ai[0] + (float)strayLimit))
                {
                    flag26 = true;
                }
                else
                {   //If the direction it's facing and the direction it's moving are different
                    if ((NPC.velocity.X < 0f && NPC.direction > 0) || (NPC.velocity.X > 0f && NPC.direction < 0))
                    {
                        flag26 = true;
                    }
                }
                strayLimit += 24;

                //
                //If it is within 40 tiles of where npc.ai[1] was last set to
                if (NPC.position.Y > NPC.ai[1] - (float)strayLimit && NPC.position.Y < NPC.ai[1] + (float)strayLimit)
                {
                    flag27 = true;
                }

                //If both of these are true
                if (flag26 && flag27)
                {
                    NPC.ai[2] += 1f;

                    if (NPC.ai[2] >= 30f && strayLimit == 16)
                    {
                        //This will NEVER be true, since num258 gets set to 40 first.
                        flag25 = true;
                    }

                    //Once it hits 60, turn the NPC around and set it back to -200
                    if (NPC.ai[2] >= 60f)
                    {
                        NPC.ai[2] = -200f;
                        NPC.direction *= -1;
                        NPC.velocity.X = NPC.velocity.X * -1f;
                        NPC.collideX = false;
                    }
                }
                else
                {
                    NPC.ai[0] = NPC.position.X;
                    NPC.ai[1] = NPC.position.Y;
                    NPC.ai[2] = 0f;
                }
                NPC.TargetClosest(true);
            }
            else
            {
                NPC.ai[2] += 1f;
                if (Main.player[NPC.target].position.X + (float)(Main.player[NPC.target].width / 2) > NPC.position.X + (float)(NPC.width / 2))
                {
                    NPC.direction = -1;
                }
                else
                {
                    NPC.direction = 1;
                }
            }

            //The centered X position of it
            int centeredXInTiles = (int)((NPC.position.X + (float)(NPC.width / 2)) / 16f) + NPC.direction * 2;
            //The centered Y position of it
            int centeredYInTiles = (int)((NPC.position.Y + (float)NPC.height) / 16f);
            bool isNotAboveSolidTile = true;
            //Literally nothing! This bool literally just exists to make this even harder to read. It gets set a few times, but never checked or used at all. For sanity's sake, i'm commenting them all out.
            //bool flag29 = false;

            //If it's above a solid tile or liquid within 3 blocks below its center, set flag 28 to false
            for (int tileIterator = centeredYInTiles; tileIterator < centeredYInTiles + 3; tileIterator++)
            {
                if (Main.tile[centeredXInTiles, tileIterator] == null)
                {
                    Main.tile[centeredXInTiles, tileIterator].ClearTile();
                }
                if ((Main.tile[centeredXInTiles, tileIterator].HasTile && Main.tileSolid[(int)Main.tile[centeredXInTiles, tileIterator].TileType]) || Main.tile[centeredXInTiles, tileIterator].LiquidAmount > 0)
                {
                    /**if (num269 <= num260 + 1)
				//	{
				//		flag29 = true;
				//	}**/
                    isNotAboveSolidTile = false;
                    break;
                }
            }

            //This will never run because flag25 will always be false! Why?
            if (flag25)
            {
                //	flag29 = false;
                isNotAboveSolidTile = true;
            }

            //If flag 28, then accelerate it down in the Y direction
            if (isNotAboveSolidTile)
            {
                NPC.velocity.Y = NPC.velocity.Y + 0.1f;
                if (NPC.velocity.Y > 3f)
                {
                    NPC.velocity.Y = 3f;
                }
            }
            else
            {
                if (NPC.directionY < 0 && NPC.velocity.Y > 0f)
                {
                    NPC.velocity.Y = NPC.velocity.Y - 0.1f;
                }
                if (NPC.velocity.Y < -4f)
                {
                    NPC.velocity.Y = -4f;
                }
            }
            //CollideX will always be false, this will never run!!
            if (NPC.collideX)
            {
                NPC.velocity.X = NPC.oldVelocity.X * -0.4f;
                if (NPC.direction == -1 && NPC.velocity.X > 0f && NPC.velocity.X < 1f)
                {
                    NPC.velocity.X = 1f;
                }
                if (NPC.direction == 1 && NPC.velocity.X < 0f && NPC.velocity.X > -1f)
                {
                    NPC.velocity.X = -1f;
                }
            }
            if (NPC.collideY)
            {
                NPC.velocity.Y = NPC.oldVelocity.Y * -0.25f;
                if (NPC.velocity.Y > 0f && NPC.velocity.Y < 1f)
                {
                    NPC.velocity.Y = 1f;
                }
                if (NPC.velocity.Y < 0f && NPC.velocity.Y > -1f)
                {
                    NPC.velocity.Y = -1f;
                }
            }


            float speedLimit = 2f;
            if (NPC.direction == -1 && NPC.velocity.X > -speedLimit)
            {

                NPC.velocity.X = NPC.velocity.X - 0.1f;

                if (NPC.velocity.X > speedLimit)
                {

                    NPC.velocity.X = NPC.velocity.X - 0.1f;

                }
                else
                {
                    if (NPC.velocity.X > 0f)
                    {
                        NPC.velocity.X = NPC.velocity.X + 0.05f;

                    }
                }
                if (NPC.velocity.X < -speedLimit)
                {
                    NPC.velocity.X = -speedLimit;

                }
            }
            else
            {
                if (NPC.direction == 1 && NPC.velocity.X < speedLimit)
                {
                    NPC.velocity.X = NPC.velocity.X + 0.1f;
                    if (NPC.velocity.X < -speedLimit)
                    {
                        NPC.velocity.X = NPC.velocity.X + 0.1f;
                    }
                    else
                    {
                        if (NPC.velocity.X < 0f)
                        {
                            NPC.velocity.X = NPC.velocity.X - 0.05f;
                        }
                    }
                    if (NPC.velocity.X > speedLimit)
                    {
                        NPC.velocity.X = speedLimit;
                    }
                }
            }
            if (NPC.directionY == -1 && (double)NPC.velocity.Y > -1.5)
            {
                NPC.velocity.Y = NPC.velocity.Y - 0.04f;
                if ((double)NPC.velocity.Y > 1.5)
                {
                    NPC.velocity.Y = NPC.velocity.Y - 0.05f;
                }
                else
                {
                    if (NPC.velocity.Y > 0f)
                    {
                        NPC.velocity.Y = NPC.velocity.Y + 0.03f;
                    }
                }
                if ((double)NPC.velocity.Y < -1.5)
                {
                    NPC.velocity.Y = -1.5f;
                }
            }
            else
            {
                if (NPC.directionY == 1 && (double)NPC.velocity.Y < 1.5)
                {
                    NPC.velocity.Y = NPC.velocity.Y + 0.04f;
                    if ((double)NPC.velocity.Y < -1.5)
                    {
                        NPC.velocity.Y = NPC.velocity.Y + 0.05f;
                    }
                    else
                    {
                        if (NPC.velocity.Y < 0f)
                        {
                            NPC.velocity.Y = NPC.velocity.Y - 0.03f;
                        }
                    }
                    if ((double)NPC.velocity.Y > 1.5)
                    {
                        NPC.velocity.Y = 1.5f;
                    }
                }
            }
            #endregion

            Lighting.AddLight((int)NPC.position.X / 16, (int)NPC.position.Y / 16, 0.4f, 0f, 0.25f);
            return;
        }

        void RunAttackStateMachine()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            NPC.TargetClosest(true);
            Player target = Main.player[NPC.target];

            if (!target.active || target.dead || !Collision.CanHit(NPC.position, NPC.width, NPC.height, target.position, target.width, target.height))
            {
                return;
            }

            if (attackDelay > 0)
            {
                attackDelay--;
                return;
            }

            bool enraged = NPC.life <= NPC.lifeMax / 2;
            if (queuedAttackPatterns <= 0)
            {
                queuedAttackPatterns = enraged ? Main.rand.Next(2, 5) : Main.rand.Next(1, 4);
            }

            FireRandomAttackPattern(target);
            queuedAttackPatterns--;
            attackDelay = queuedAttackPatterns > 0 ? (enraged ? EnragedAttackComboGapTime : NormalAttackComboGapTime) : AttackIdleTime;
            NPC.netUpdate = true;
        }

        void FireRandomAttackPattern(Player target)
        {
            int pattern = Main.rand.Next(10);
            if (pattern == lastAttackPattern)
            {
                pattern = (pattern + Main.rand.Next(1, 10)) % 10;
            }
            lastAttackPattern = pattern;

            switch (pattern)
            {
                case 0:
                    FireDemonSpiritNeedle(target);
                    break;
                case 1:
                    FireDemonSpiritFork(target);
                    break;
                case 2:
                    FireDemonSpiritSplit(target);
                    break;
                case 3:
                    FireDemonSpiritCross(target);
                    break;
                case 4:
                    FireDemonSpiritSurround(target);
                    break;
                case 5:
                    FirePurpleCrushShot(target);
                    break;
                case 6:
                    FirePurpleCrushFan(target);
                    break;
                case 7:
                    FirePurpleCrushCage(target);
                    break;
                case 8:
                    FirePurpleCrushWall(target);
                    break;
                default:
                    FirePurpleCrushSpiral(target);
                    break;
            }
        }

        void FireDemonSpiritNeedle(Player target)
        {
            Vector2 velocity = AimAtTarget(target, 10f, 20f);
            SpawnProjectile(NPC.Center, velocity, ModContent.ProjectileType<Projectiles.Enemy.DemonSpirit>(), demonDamage, 120);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
        }

        void FireDemonSpiritFork(Player target)
        {
            Vector2 velocity = AimAtTarget(target, 8.8f, 10f);
            for (int i = -1; i <= 1; i++)
            {
                SpawnProjectile(NPC.Center, velocity.RotatedBy(MathHelper.ToRadians(15f * i)), ModContent.ProjectileType<Projectiles.Enemy.DemonSpirit>(), demonDamage, 120);
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
        }

        void FireDemonSpiritSplit(Player target)
        {
            Vector2 velocity = AimAtTarget(target, 8f, 8f);
            Vector2 perpendicular = velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2) * 22f;

            SpawnProjectile(NPC.Center + perpendicular, velocity.RotatedBy(MathHelper.ToRadians(-10f)), ModContent.ProjectileType<Projectiles.Enemy.DemonSpirit>(), demonDamage, 135);
            SpawnProjectile(NPC.Center - perpendicular, velocity.RotatedBy(MathHelper.ToRadians(10f)), ModContent.ProjectileType<Projectiles.Enemy.DemonSpirit>(), demonDamage, 135);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
        }

        void FireDemonSpiritCross(Player target)
        {
            Vector2 velocity = AimAtTarget(target, 7.4f, 5f);
            for (int i = 0; i < 4; i++)
            {
                SpawnProjectile(NPC.Center, velocity.RotatedBy(MathHelper.PiOver2 * i), ModContent.ProjectileType<Projectiles.Enemy.DemonSpirit>(), demonDamage, 120);
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
        }

        void FireDemonSpiritSurround(Player target)
        {
            Vector2 velocity = AimAtTarget(target, 7f, 5f);
            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2 * i) * 32f;
                SpawnProjectile(NPC.Center + offset, (target.Center - (NPC.Center + offset)).SafeNormalize(Vector2.UnitY) * 7f, ModContent.ProjectileType<Projectiles.Enemy.DemonSpirit>(), demonDamage, 135);
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
        }

        void FirePurpleCrushShot(Player target)
        {
            Vector2 velocity = AimAtTarget(target, 2.4f, 0f);
            SpawnProjectile(NPC.Center, velocity, ModContent.ProjectileType<Projectiles.Enemy.PurpleCrush>(), demonDamage, 170);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9, NPC.Center);
            SpawnPurpleCrushCastDust();
        }

        void FirePurpleCrushFan(Player target)
        {
            Vector2 velocity = AimAtTarget(target, 2.2f, 0f);
            for (int i = -1; i <= 1; i++)
            {
                SpawnProjectile(NPC.Center, velocity.RotatedBy(MathHelper.ToRadians(20f * i)), ModContent.ProjectileType<Projectiles.Enemy.PurpleCrush>(), demonDamage, 170);
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9, NPC.Center);
            SpawnPurpleCrushCastDust();
        }

        void FirePurpleCrushCage(Player target)
        {
            Vector2 velocity = AimAtTarget(target, 2f, 0f);
            for (int i = -1; i <= 1; i += 2)
            {
                SpawnProjectile(NPC.Center + new Vector2(0, 20f * i), velocity.RotatedBy(MathHelper.ToRadians(26f * i)), ModContent.ProjectileType<Projectiles.Enemy.PurpleCrush>(), demonDamage, 180);
                SpawnProjectile(NPC.Center + new Vector2(20f * i, 0), velocity.RotatedBy(MathHelper.ToRadians(-26f * i)), ModContent.ProjectileType<Projectiles.Enemy.PurpleCrush>(), demonDamage, 180);
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9, NPC.Center);
            SpawnPurpleCrushCastDust();
        }

        void FirePurpleCrushWall(Player target)
        {
            Vector2 velocity = AimAtTarget(target, 1.9f, 0f);
            Vector2 perpendicular = velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2) * 22f;
            for (int i = -2; i <= 2; i++)
            {
                SpawnProjectile(NPC.Center + perpendicular * i, velocity, ModContent.ProjectileType<Projectiles.Enemy.PurpleCrush>(), demonDamage, 180);
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9, NPC.Center);
            SpawnPurpleCrushCastDust();
        }

        void FirePurpleCrushSpiral(Player target)
        {
            Vector2 velocity = AimAtTarget(target, 2.1f, 0f);
            float rotationOffset = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < 4; i++)
            {
                SpawnProjectile(NPC.Center, velocity.RotatedBy(rotationOffset + MathHelper.PiOver2 * i), ModContent.ProjectileType<Projectiles.Enemy.PurpleCrush>(), demonDamage, 190);
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item9, NPC.Center);
            SpawnPurpleCrushCastDust();
        }

        Vector2 AimAtTarget(Player target, float speed, float randomSpread)
        {
            Vector2 aimPoint = target.Center;
            if (randomSpread > 0)
            {
                aimPoint += new Vector2(Main.rand.NextFloat(-randomSpread, randomSpread), Main.rand.NextFloat(-randomSpread, randomSpread));
            }

            Vector2 velocity = aimPoint - NPC.Center;
            if (velocity.LengthSquared() < 1f)
            {
                velocity = Vector2.UnitY;
            }
            velocity.Normalize();
            return velocity * speed;
        }

        void SpawnProjectile(Vector2 position, Vector2 velocity, int type, int damage, int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int projectileIndex = Projectile.NewProjectile(NPC.GetSource_FromThis(), position, velocity, type, damage, 0f, Main.myPlayer);
            Main.projectile[projectileIndex].timeLeft = timeLeft;
        }

        void SpawnPurpleCrushCastDust()
        {
            int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.VilePowder, NPC.velocity.X, NPC.velocity.Y, 200, Color.Red, 1f);
            Main.dust[dust].noGravity = true;
        }
        #endregion

        #region Gore
        public override void OnKill()
        {

            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.CrazedDemonSpirit.Death"), 175, 75, 255);

            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 50; i++)
                {
                    {
                        Color color = new Color();
                        int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.UnholyWater, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                        Main.dust[dust].noGravity = true;
                        dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.PurpleTorch, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                        Main.dust[dust].noGravity = true;
                        dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.Wraith, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                        Main.dust[dust].noGravity = true;
                        dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, DustID.PurpleTorch, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                        Main.dust[dust].noGravity = true;
                    }
                }
            }
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(new CommonDrop(ModContent.ItemType<Items.Tools.GreatMagicShieldScroll>(), 100, 1, 1, 3));
            npcLoot.Add(ItemDropRule.Common(ItemID.BloodMoonStarter, 25));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 20));
            npcLoot.Add(ItemDropRule.Common(ItemID.ManaRegenerationPotion, 25));
            npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 20));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.AbyssRule, ModContent.ItemType<FlameOfTheAbyss>()));
        }
        #region Frames
        public override void FindFrame(int currentFrame)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if (NPC.velocity.X < 0)
            {
                NPC.spriteDirection = -1;
            }
            else
            {
                NPC.spriteDirection = 1;
            }
            NPC.rotation = NPC.velocity.X * 0.08f;
            NPC.frameCounter += 1.0;
            if (NPC.frameCounter >= 8.0)
            {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
            }
        }
        #endregion

    }
}
