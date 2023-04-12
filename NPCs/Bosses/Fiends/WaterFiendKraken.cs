using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace tsorcRevamp.NPCs.Bosses.Fiends
{
    [AutoloadBossHead]
    class WaterFiendKraken : ModNPC
    {
        public override void SetDefaults()
        {
            Main.npcFrameCount[NPC.type] = 8;
            NPC.width = 110;
            NPC.height = 170;
            DrawOffsetY = 50;
            NPC.damage = trueContactDamage;
            NPC.defense = 35;
            NPC.aiStyle = -1;
            AnimationType = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = (int)(325000 * (1 + (0.25f * (Main.CurrentFrameFlags.ActivePlayersCount - 1))));
            NPC.timeLeft = 22500;
            NPC.friendly = false;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.knockBackResist = 0f;
            NPC.lavaImmune = true;
            NPC.boss = true;
            NPC.value = 600000;

            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
            NPC.buffImmune[BuffID.OnFire] = true;

            despawnHandler = new NPCDespawnHandler("Water Fiend Kraken submerges into the depths...", Color.DeepSkyBlue, 180);
            InitializeMoves();
        }

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Water Fiend Kraken");
        }

        int hypnoticDisruptorDamage = 35;
        int cursedFlamesDamage = 40;
        int geyserDamage = 30;
        int plasmaOrbDamage = 65;
        int trueContactDamage = 140; //Contact damage does not get multiplied by 4, hence the higher values
        int chargeContactDamage = 200;

        //If this is set to anything but -1, the boss will *only* use that attack ID
        int testAttack = -1;
        KrakenMove CurrentMove
        {
            get => MoveList[MoveIndex];
        }

        List<KrakenMove> MoveList;

        //Controls what move is currently being performed
        public int MoveIndex
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }

        //Used by moves to keep track of how long they've been going for
        public int MoveCounter
        {
            get => (int)NPC.ai[1];
            set => NPC.ai[1] = value;
        }

        public Player Target
        {
            get => Main.player[NPC.target];
        }

        int MoveTimer = 0;
        NPCDespawnHandler despawnHandler;

        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            Lighting.AddLight((int)NPC.Center.X / 16, (int)NPC.Center.Y / 16, 0.4f, 0f, 0.25f);

            if (testAttack != -1)
            {
                MoveIndex = testAttack;
            }
            if (MoveList == null)
            {
                InitializeMoves();
            }
            if (MoveIndex >= MoveList.Count)
            {
                MoveIndex = 0;
            }

            CurrentMove.Move();
        }

        Vector2 chargeVelocity = new Vector2(0, 0);
        float ChargeTimer = 0;
        float projectileTimer = -120;
        float projectileType = 0;
        bool charging = false;
        private void CursedFireSpam()
        {
            ChargeTimer++;
            if (ChargeTimer >= 500)
            {
                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, 29, NPC.velocity.X, NPC.velocity.Y, 200, new Color(), 5);
                Main.dust[dust].velocity = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.dust[dust].position, 5);
            }
            if (ChargeTimer == 600)
            {
                chargeVelocity = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 19);
                charging = true;
            }
            if (charging)
            {
                NPC.velocity = chargeVelocity;
                NPC.damage = chargeContactDamage;

                //Check if it's passed the player by at least 500 units while charging, and if so stop
                if (Vector2.Distance(NPC.Center, Target.Center) > 500)
                {
                    Vector2 vectorDiff = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 1);
                    double angleDiff = UsefulFunctions.CompareAngles(NPC.velocity, vectorDiff);

                    if (angleDiff > MathHelper.Pi / 2)
                    {
                        charging = false;
                        NPC.damage = trueContactDamage;
                        ChargeTimer = 0;
                        MoveCounter++;
                    }
                }
            }

            //Most of its movement is done here
            if (!charging)
            {
                FloatOminouslyTowardPlayer();
            }
            if (MoveCounter >= 3 && projectileTimer == 0)
            {
                NextAttack();
            }

            #region Projectiles and NPCs
            if (Main.netMode != NetmodeID.MultiplayerClient && !charging)
            {
                projectileTimer++;
                ArmorShaderData dustShader = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.MartianArmorDye), Main.LocalPlayer);
                if (projectileType == 9)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        Vector2 dir = Main.rand.NextVector2Circular(300, 300);
                        Vector2 dustPos = NPC.Center + dir;
                        Vector2 dustVel = new Vector2(10, 0).RotatedBy(dir.ToRotation() + MathHelper.Pi);
                        Dust thisDust = Dust.NewDustPerfect(dustPos, DustID.FireworkFountain_Blue, dustVel, Scale: 1);
                        thisDust.noGravity = true;
                        thisDust.shader = dustShader;
                    }
                }
                if (projectileTimer >= 0)
                {
                    float offset = MathHelper.ToRadians(-20 + 10 * projectileTimer);
                    if (projectileType == 9)
                    {
                        offset = MathHelper.ToRadians(-30 + 10 * projectileTimer);
                    }
                    if (projectileType < 6)
                    {
                        Vector2 projVector = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 10);
                        projVector = projVector.RotatedBy(offset);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyRedirectingShark>(), cursedFlamesDamage, 0f, Main.myPlayer, 0, NPC.target);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    }
                    if (projectileType >= 6 && projectileType != 9)
                    {
                        Vector2 projVector = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 5);
                        projVector = projVector.RotatedBy(offset);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.HypnoticDisrupter>(), hypnoticDisruptorDamage, 0f, Main.myPlayer, NPC.target, 1f);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    }
                    if (projectileType == 9)
                    {
                        Vector2 projVector = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 12);
                        projVector = projVector.RotatedBy(offset);
                        projVector += (Main.player[NPC.target].velocity / 2);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    }
                    if (projectileType != 9)
                    {
                        if (projectileTimer == 4)
                        {
                            projectileTimer = -120;
                            projectileType = Main.rand.Next(10);
                        }
                    }
                    else
                    {
                        if (projectileTimer == 6)
                        {
                            projectileTimer = -120;
                            projectileType = Main.rand.Next(10);
                        }
                    }
                }
            }
            #endregion

            //If low on life, start flooding the chamber constantly
            if (NPC.life < (NPC.lifeMax / 2))
            {

                //Don't start a new flood if it's below the normal water line, to avoid fucking with it as much as possible
                if (radius != 0 || ((NPC.Center.Y / 16) < 1713 && !UsefulFunctions.IsTileReallySolid(NPC.Center / 16)))
                {
                    radius++;
                    FloodArena();
                }
                if (radius > 300)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 projCenter = Target.Center;
                        projCenter += Main.rand.NextVector2CircularEdge(500, 500);

                        Projectile.NewProjectile(NPC.GetSource_FromThis(), projCenter.X, projCenter.Y, 0, 0, ModContent.ProjectileType<Projectiles.Enemy.InkGeyser>(), geyserDamage, 0f, Main.myPlayer, Target.whoAmI);
                    }
                    chamberFlooded = !chamberFlooded;
                    radius = 0;
                }
            }
        }

        float radius = 0;
        bool chamberFlooded;
        //Rectangle arena = new Rectangle(1557, 1639, 467, 103);
        List<Vector2> activeTiles;
        List<Vector2> nextTiles;
        int projType = 0;
        private void AquaWave()
        {
            NPC.velocity = Vector2.Zero;

            if (projType >= 8)
            {
                ArmorShaderData dustShader = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.MartianArmorDye), Main.LocalPlayer);
                for (int j = 0; j < 10; j++)
                {
                    Vector2 dir = Main.rand.NextVector2Circular(300, 300);
                    Vector2 dustPos = NPC.Center + dir;
                    Vector2 dustVel = new Vector2(10, 0).RotatedBy(dir.ToRotation() + MathHelper.Pi);
                    Dust thisDust = Dust.NewDustPerfect(dustPos, DustID.FireworkFountain_Blue, dustVel, Scale: 1);
                    thisDust.noGravity = true;
                    thisDust.shader = dustShader;
                }
            }

            if (Main.GameUpdateCount % 40 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {

                if (projType < 5)
                {
                    Vector2 projVector = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 10);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyRedirectingShark>(), cursedFlamesDamage, 0f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                }
                if (projType >= 5 && projType < 8)
                {

                    Vector2 projVector = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 5);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.HypnoticDisrupter>(), hypnoticDisruptorDamage, 0f, Main.myPlayer, NPC.target, 1f);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                }
                if (projType >= 8)
                {
                    Vector2 projVector = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 15);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                }
                projType = Main.rand.Next(10);
            }


            radius++;
            FloodArena();


            //Check if we're done
            if (radius > 300)
            {
                //Self-correcting: If the chamber starts out flooded then the flooding algorithm will by nature do nothing
                chamberFlooded = !chamberFlooded;
                radius = 0;

                MoveTimer++;

                if (MoveTimer >= 5)
                {
                    MoveTimer = 0;
                    NextAttack();
                }
            }
        }

        private void FloatOminouslyTowardPlayer()
        {
            Vector2 krakenMaxSpeed = new Vector2(3, 4);
            float krakenAccelerationX = 0.05f;
            float krakenAccelerationY = 0.05f;

            if (NPC.Center.X < Target.Center.X)
            {
                NPC.velocity.X += krakenAccelerationX;
            }
            else
            {
                NPC.velocity.X -= krakenAccelerationX;
            }

            //This is the part that makes it bob up and down as it moves
            //If it's moving up
            if (NPC.velocity.Y < 0)
            {
                //And it's not more than 120 units above the player
                if (Target.Center.Y - NPC.Center.Y <= 120)
                {
                    //Keep moving up
                    NPC.velocity.Y -= krakenAccelerationY;
                }
                //If we are more than 120 units above the player, start accelerating down
                else
                {
                    NPC.velocity.Y += krakenAccelerationY;
                }
            }
            else
            {
                //Do the same thing, but reversed if it's moving down. Could probably simplify this, but this format makes it clear what it's doing.
                if (Target.Center.Y - NPC.Center.Y <= -120)
                {
                    NPC.velocity.Y -= krakenAccelerationY;
                }
                else
                {
                    NPC.velocity.Y += krakenAccelerationY;
                }
            }

            NPC.velocity = Vector2.Clamp(NPC.velocity, -krakenMaxSpeed, krakenMaxSpeed);
        }

        Vector2 ArenaCenter = new Vector2(1820 * 16, 1702 * 16);
        private void DashToArenaMidline()
        {
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureMode || NPC.Center.Y < 1660 * 16 || NPC.Center.Y > 1744 * 16 || NPC.Center.X < 1560 * 16 || NPC.Center.X > 2011 * 16)
            {
                NextAttack();
                return;
            }
            MoveCounter++;
            int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, 29, NPC.velocity.X, NPC.velocity.Y, 200, new Color(), 5);
            Main.dust[dust].velocity = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.dust[dust].position, 5);
            if (MoveCounter > 60)
            {
                if (NPC.Center.Y < ArenaCenter.Y)
                {
                    NPC.velocity.Y = 12;
                }
                else
                {
                    NPC.velocity.Y = -12;
                }
            }
            if (Math.Abs(NPC.Center.Y - ArenaCenter.Y) < 16)
            {
                NextAttack();
            }
        }

        float cursedRadius = 1400;
        private void CursedBarrage()
        {
            MoveCounter++;
            NPC.velocity = Vector2.Zero;

            cursedRadius = 250 + ((1200 - MoveCounter) * 1.5f);

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i] != null && Main.player[i].active && NPC.Distance(Main.player[i].Center) > cursedRadius)
                {
                    Main.player[i].AddBuff(BuffID.Blackout, 5);
                    Main.player[i].AddBuff(BuffID.Venom, 5);
                }
            }

            for (int j = 0; j < 100; j++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(cursedRadius, cursedRadius);
                Vector2 dustPos = NPC.Center + dir;
                Vector2 dustVel = new Vector2(10, 0).RotatedBy(dir.ToRotation() + MathHelper.Pi / 2);
                Dust thisDust = Dust.NewDustPerfect(dustPos, DustID.Asphalt, dustVel, 0, default, 3);
                thisDust.noGravity = true;
                thisDust.shader = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.BlackDye), Main.LocalPlayer);
            }

            for (int j = 0; j < 10; j++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(130, 130);
                Vector2 dustPos = NPC.Center + dir;
                Vector2 dustVel = new Vector2(3, 0).RotatedBy(dir.ToRotation());
                Dust thisDust = Dust.NewDustPerfect(dustPos, DustID.Asphalt, dustVel, 0, default, 2);
                thisDust.noGravity = true;
                thisDust.shader = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.BlackDye), Main.LocalPlayer);
            }

            int cursedFlameCooldown = 60;
            if (chamberFlooded)
            {
                cursedFlameCooldown = 80;
            }
            if (MoveCounter % cursedFlameCooldown == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 projCenter = Main.rand.NextVector2CircularEdge(cursedRadius, cursedRadius) + NPC.Center;
                Vector2 projVector = UsefulFunctions.GenerateTargetingVector(projCenter, Main.player[NPC.target].Center, 10);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), projCenter.X, projCenter.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyRedirectingShark>(), cursedFlamesDamage, 0f, Main.myPlayer, 1, NPC.target);
            }

            int waterJetCooldown = 160;
            if (MoveCounter % waterJetCooldown == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 projCenter = Target.Center;
                projCenter += Main.rand.NextVector2CircularEdge(500, 500);

                Projectile.NewProjectile(NPC.GetSource_FromThis(), projCenter.X, projCenter.Y, 0, 0, ModContent.ProjectileType<Projectiles.Enemy.InkGeyser>(), geyserDamage, 0f, Main.myPlayer, Target.whoAmI);
            }
                        
            if (MoveCounter > 1200)
            {
                NextAttack();
            }
        }
        private void FloodArena()
        {

            //Don't flood anything if outside of adventure mode or far from the arena center
            if(!ModContent.GetInstance<tsorcRevampConfig>().AdventureMode || NPC.Center.Y < 1660 * 16 || NPC.Center.Y > 1744 * 16 || NPC.Center.X < 1560 * 16 || NPC.Center.X > 2011 * 16)
            {
                return;
            }

            Vector2 centerOver16 = NPC.Center / 16;
            //Initialize some things
            if (radius == 1)
            {
                activeTiles = new List<Vector2>();
                nextTiles = new List<Vector2>();
                activeTiles.Add(centerOver16);
            }
            //Perform the flooding algorithm
            else
            {
                //Most things here work in vector2s, so declaring these here simplifies calculations below
                Vector2 up = new Vector2(0, 1);
                Vector2 right = new Vector2(1, 0);
                Vector2 left = new Vector2(-1, 0);
                Vector2 down = new Vector2(0, -1);

                //Pick whether we're flooding or emptying
                int liquidLevel = 255;
                if (chamberFlooded)
                {
                    liquidLevel = 0;
                }

                //For every tile on the list
                for (int i = 0; i < activeTiles.Count; i++)
                {
                    //Set it to full/empty
                    Main.tile[(int)activeTiles[i].X, (int)activeTiles[i].Y].LiquidAmount = (byte)liquidLevel;

                    //And add any adjacent unchanged tiles to the nextTiles list
                    if (!nextTiles.Contains(activeTiles[i] + up) && Main.tile[(int)(activeTiles[i] + up).X, (int)(activeTiles[i] + up).Y].LiquidAmount != liquidLevel && !UsefulFunctions.IsTileReallySolid(activeTiles[i] + up))
                    {
                        nextTiles.Add(activeTiles[i] + up);
                    }
                    if (!nextTiles.Contains(activeTiles[i] + right) && Main.tile[(int)(activeTiles[i] + right).X, (int)(activeTiles[i] + right).Y].LiquidAmount != liquidLevel && !UsefulFunctions.IsTileReallySolid(activeTiles[i] + right))
                    {
                        nextTiles.Add(activeTiles[i] + right);
                    }
                    if (!nextTiles.Contains(activeTiles[i] + left) && Main.tile[(int)(activeTiles[i] + left).X, (int)(activeTiles[i] + left).Y].LiquidAmount != liquidLevel && !UsefulFunctions.IsTileReallySolid(activeTiles[i] + left))
                    {
                        nextTiles.Add(activeTiles[i] + left);
                    }
                    if (!nextTiles.Contains(activeTiles[i] + down) && Main.tile[(int)(activeTiles[i] + down).X, (int)(activeTiles[i] + down).Y].LiquidAmount != liquidLevel && !UsefulFunctions.IsTileReallySolid(activeTiles[i] + down))
                    {
                        nextTiles.Add(activeTiles[i] + down);
                    }
                }

                //Push tiles that got queued in nextTiles into activeTiles to be operated on next tick, then wipe it
                activeTiles = nextTiles;
                nextTiles = new List<Vector2>();


                //Dust effect
                ArmorShaderData shader = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.CyanGradientDye), Main.LocalPlayer);
                for (int i = 0; i < 90; i++)
                {
                    float offset = Main.rand.NextFloat(-720, 0);
                    Vector2 velocity = new Vector2(-16, 0);
                    if (radius < 20)
                    {
                        velocity *= radius / 20;
                    }
                    Vector2 positionOffset = Vector2.Zero;
                    positionOffset.X += radius * 16;
                    positionOffset.X += offset;
                    positionOffset.Y += offset;

                    Vector2 offset1 = positionOffset + NPC.Center;
                    Vector2 offset2 = positionOffset;
                    offset2.X *= -1;
                    offset2 += NPC.Center;
                    Vector2 offset3 = positionOffset;
                    offset3.Y *= -1;
                    offset3 += NPC.Center;
                    Vector2 offset4 = positionOffset;
                    offset4 *= -1;
                    offset4 += NPC.Center;

                    if (!UsefulFunctions.IsTileReallySolid(offset1 / 16))
                    {
                        Dust r = Dust.NewDustPerfect(offset1, 174, velocity, 10, default, 2);
                        r.noGravity = true;
                        r.shader = shader;
                    }
                    if (!UsefulFunctions.IsTileReallySolid(offset2 / 16))
                    {
                        Dust l = Dust.NewDustPerfect(offset2, 174, velocity, 10, default, 2);
                        l.noGravity = true;
                        l.shader = shader;
                    }
                    if (!UsefulFunctions.IsTileReallySolid(offset3 / 16))
                    {
                        Dust r = Dust.NewDustPerfect(offset3, 174, velocity, 10, default, 2);
                        r.noGravity = true;
                        r.shader = shader;
                    }
                    if (!UsefulFunctions.IsTileReallySolid(offset4 / 16))
                    {
                        Dust l = Dust.NewDustPerfect(offset4, 174, velocity, 10, default, 2);
                        l.noGravity = true;
                        l.shader = shader;
                    }
                }
            }
        }
        private void NextAttack()
        {
            MoveIndex++;
            if (MoveIndex > MoveList.Count)
            {
                MoveIndex = 0;
            }

            MoveCounter = 0;
        }
        private void InitializeMoves(List<int> validMoves = null)
        {
            MoveList = new List<KrakenMove> {
                new KrakenMove(CursedFireSpam, KrakenAttackID.CursedFireSpam, "Cursed Fire"),
                new KrakenMove(DashToArenaMidline, KrakenAttackID.CenterDash, "Dash to Center"),
                new KrakenMove(AquaWave, KrakenAttackID.AquaWave, "Aqua Wave"),
                new KrakenMove(CursedBarrage, KrakenAttackID.CursedBarrage, "Cursed Barrage"),
                };
        }

        private class KrakenAttackID
        {
            public const short CursedFireSpam = 0;
            public const short AquaWave = 1;
            public const short CenterDash = 2;
            public const short CursedBarrage = 3;
        }
        private class KrakenMove
        {
            public Action Move;
            public int ID;
            public Action<SpriteBatch, Color> Draw;
            public string Name;

            public KrakenMove(Action MoveAction, int MoveID, string AttackName, Action<SpriteBatch, Color> DrawAction = null)
            {
                Move = MoveAction;
                ID = MoveID;
                Draw = DrawAction;
                Name = AttackName;
            }
        }

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
            if (NPC.frameCounter >= 5.0)
            {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
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

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.KrakenBag>()));
        }
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 4").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 5").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 6").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 7").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Water Fiend Kraken Gore 8").Type, 1f);
            }
            if (!Main.expertMode)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.GuardianSoul>(), 1);
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Weapons.Melee.ForgottenRisingSun>(), 10);
                if (!tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(NPC.type)))
                {
                    Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DarkSoul>(), 30000);
                }
            }
        }
    }
}