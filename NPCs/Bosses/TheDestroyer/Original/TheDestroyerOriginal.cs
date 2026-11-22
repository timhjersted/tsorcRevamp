using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Utilities;
using tsorcRevamp.Utilities.Balance;

namespace tsorcRevamp.NPCs.Bosses.TheDestroyer.Original
{
    public abstract class TheDestroyerOriginalSegment : ModNPC
    {
        internal abstract int VanillaType { get; }

        // These are the existing tsorc replacements for the three vanilla sprites.
        public override string Texture => Mod.Name + "/NPCs/Bosses/TheDestroyer/NPC_" + VanillaType;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = Main.npcFrameCount[VanillaType];
            if (VanillaType != NPCID.TheDestroyer)
            {
                NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers { Hide = true });
            }

            NPCID.Sets.ImmuneToAllBuffs[Type] = true;
        }

        public override void SetDefaults()
        {
            NPC.CloneDefaults(VanillaType);
            NPC.aiStyle = -1;
            NPC.value = 0f;
            NPC.scale = 1.25f;
            NPC.width = 38;
            NPC.height = 38;
            NPC.lifeMax = 135000;

            switch (VanillaType)
            {
                case NPCID.TheDestroyer:
                    NPC.damage = Main.expertMode ? 40 : 60;
                    NPC.defense = 25;
                    NPC.rarity = 23;
                    NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().DespawnHandler =
                        new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.Destroyer.DespawnHandler"), Color.Orange, DustID.Torch);
                    break;
                case NPCID.TheDestroyerBody:
                    NPC.damage = 60;
                    NPC.defense = 45;
                    break;
                case NPCID.TheDestroyerTail:
                    NPC.damage = 80;
                    NPC.defense = 5;
                    break;
            }
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            // The vanilla mechanical-worm multipliers dispatch by NPC ID.
            NPC.lifeMax = (int)(NPC.lifeMax * 0.75f * balance * bossAdjustment);
            if (VanillaType == NPCID.TheDestroyer)
            {
                NPC.damage *= 2;
            }
            else
            {
                NPC.damage = (int)(NPC.damage * 0.85f);
            }

            NPC.scale *= 1.05f;
        }

        public override bool CheckActive()
        {
            // Keep the legacy worm intact off-screen. Terraria's inactivity exemption only recognizes the vanilla IDs.
            return false;
        }

        public override void AI()
        {
            TheDestroyer.RunMovement(NPC, ModContent.NPCType<TheDestroyerOriginal>(),
                ModContent.NPCType<TheDestroyerOriginalBody>(), ModContent.NPCType<TheDestroyerOriginalTail>(), true);
            if (NPC.active && TheDestroyerOriginal.FromLaserHost(NPC.whoAmI) is TheDestroyerOriginal head)
            {
                head.RunLegacyAttacks(NPC);
            }
        }

        public override void FindFrame(int frameHeight)
        {
            if (VanillaType == NPCID.TheDestroyerBody)
            {
                NPC.frame.Y = NPC.ai[2] == 0f ? 0 : frameHeight;
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, 156);
                }

                return;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && VanillaType == NPCID.TheDestroyerBody
                && NPC.ai[2] == 0f && Main.rand.NextBool(Terraria.NPC.IsMechQueenUp ? 50 : 25))
            {
                NPC.ai[2] = 1f;
                NPC.netUpdate = true;
                int probeIndex = NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Bottom.Y, NPCID.Probe);
                if (Main.netMode == NetmodeID.Server && probeIndex < Main.maxNPCs)
                {
                    NetMessage.SendData(MessageID.SyncNPC, number: probeIndex);
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            float glow = 0f;
            if (TheDestroyerOriginal.FromLaserHost(NPC.whoAmI) is TheDestroyerOriginal head)
            {
                glow = head.destroyerGlowPercent;
                if (head.destroyerReachedHeight || glow > 1f)
                {
                    glow = 1f;
                }

                if (head.destroyerChargeTimer > 920)
                {
                    glow = (head.destroyerChargeTimer - 920f) / 60f;
                }
            }

            TheDestroyer.DrawHull(NPC, spriteBatch, screenPos, drawColor, glow, VanillaType);
            return false;
        }
    }

    public sealed class TheDestroyerOriginalBody : TheDestroyerOriginalSegment
    {
        internal override int VanillaType => NPCID.TheDestroyerBody;
    }

    public sealed class TheDestroyerOriginalTail : TheDestroyerOriginalSegment
    {
        internal override int VanillaType => NPCID.TheDestroyerTail;
    }

    public sealed class TheDestroyerOriginal : TheDestroyerOriginalSegment, IDebugAttackLabel
    {
        internal override int VanillaType => NPCID.TheDestroyer;

        internal int destroyerAttackIndex;
        internal Vector2 destroyerLaserSafeAngle = Vector2.One;
        internal bool destroyerReachedHeight;
        internal int destroyerChargeTimer;
        private float destroyerRotation;
        private bool laserToggle;
        internal float laserRotation;
        private float destroyerTargetLerp;
        private bool destroyerJustSpawned = true;
        private List<Projectile> horizontalLasers;
        private List<Projectile> verticalLasers;
        private List<Vector2> intersections;
        internal float destroyerGlowPercent;

        public string DebugAttackLabel => destroyerAttackIndex switch
        {
            0 => "Old Destroyer · Confinement",
            1 => "Old Destroyer · Moving Grid",
            2 => "Old Destroyer · Laser Circle",
            _ => "Old Destroyer · Hull-Glow Spin"
        };

        public override bool PreKill()
        {
            // This command-spawned version participates in balance logging. Skip the loot transaction.
            BalanceLog.NotifyKilled(NPC);
            return false;
        }

        internal static TheDestroyerOriginal FromLaserHost(int hostIndex)
        {
            if (hostIndex < 0 || hostIndex >= Main.maxNPCs || !Main.npc[hostIndex].active)
            {
                return null;
            }

            NPC host = Main.npc[hostIndex];
            if (host.ModNPC is TheDestroyerOriginal head)
            {
                return head;
            }

            if (host.ModNPC is TheDestroyerOriginalSegment && host.realLife >= 0 && host.realLife < Main.maxNPCs
                && Main.npc[host.realLife].active)
            {
                return Main.npc[host.realLife].ModNPC as TheDestroyerOriginal;
            }

            return null;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)destroyerAttackIndex);
            writer.Write((short)destroyerChargeTimer);
            writer.Write(destroyerReachedHeight);
            writer.Write(destroyerJustSpawned);
            writer.Write(destroyerRotation);
            writer.Write(destroyerTargetLerp);
            writer.Write(laserRotation);
            writer.WriteVector2(destroyerLaserSafeAngle);
            writer.Write(destroyerGlowPercent);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            destroyerAttackIndex = reader.ReadByte();
            destroyerChargeTimer = reader.ReadInt16();
            destroyerReachedHeight = reader.ReadBoolean();
            destroyerJustSpawned = reader.ReadBoolean();
            destroyerRotation = reader.ReadSingle();
            destroyerTargetLerp = reader.ReadSingle();
            laserRotation = reader.ReadSingle();
            destroyerLaserSafeAngle = reader.ReadVector2();
            destroyerGlowPercent = reader.ReadSingle();
        }

        internal void RunLegacyAttacks(NPC npc)
        {
            if (npc.ModNPC is TheDestroyerOriginalBody && Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (!destroyerJustSpawned)
                {

                    if ((destroyerAttackIndex == 3 && destroyerReachedHeight))
                    {
                        Vector2 dustPos = Main.npc[(int)npc.ai[3]].velocity;
                        dustPos = dustPos.SafeNormalize(Vector2.UnitX);
                        dustPos *= 48;
                        dustPos = dustPos.RotatedBy(MathHelper.PiOver2);
                        if (!Main.dedServ)
                        {
                            Dust.NewDustPerfect(npc.Center + dustPos, DustID.Torch, Main.npc[(int)npc.ai[3]].velocity.RotatedBy(MathHelper.PiOver2) / 6, Scale: 3).noGravity = true;
                        }

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (destroyerChargeTimer > -180 && destroyerChargeTimer <= 740 && destroyerChargeTimer % 120 == 0 && laserToggle)
                            {
                                Vector2 projVel = UsefulFunctions.Aim(npc.Center, Main.player[npc.target].Center + Main.rand.NextVector2CircularEdge(220, 220), 1);
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, projVel, ModContent.ProjectileType<EnemyLingeringLaser>(), 80, 0, Main.myPlayer, 2000 + npc.target, npc.whoAmI);
                            }

                            //Shoot confining lasers above it to keep the player from circling
                            if (destroyerChargeTimer > -60 && (Main.GameUpdateCount + npc.whoAmI * 5) % 300 == 0)
                            {
                                Vector2 projVel = new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 0.05f));
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, projVel, ModContent.ProjectileType<EnemyLingeringLaser>(), 40, 0, Main.myPlayer, -4, npc.whoAmI);
                            }
                        }
                    }

                    if (destroyerChargeTimer < 60 && destroyerChargeTimer > 0)
                    {
                        Vector2 dustPos = Main.npc[(int)npc.ai[3]].velocity;
                        dustPos = dustPos.SafeNormalize(Vector2.UnitX);
                        dustPos *= 48;
                        dustPos = dustPos.RotatedBy(MathHelper.ToRadians(6 * destroyerChargeTimer));
                        if (!Main.dedServ)
                        {
                            Dust.NewDustPerfect(npc.Center + dustPos, DustID.Torch, Main.npc[(int)npc.ai[3]].velocity.RotatedBy(MathHelper.PiOver2) / 6, Scale: 3).noGravity = true;
                        }
                    }

                    //Aims in a ring around the player
                    if (destroyerAttackIndex == 0)
                    {
                        if (destroyerChargeTimer == 0)
                        {
                            Vector2 projVel = UsefulFunctions.Aim(npc.Center, Main.player[npc.target].Center + (Main.player[npc.target].velocity * 45), 1);

                            int style = -1;

                            //Aims in a ring around the player to constrain their movement.
                            //Impossible for any player other than the one being targeted to dodge, so it doesn't happen in multiplayer.
                            if (destroyerAttackIndex == 0)
                            {
                                if (Main.netMode == NetmodeID.SinglePlayer && Main.player[npc.target].active)
                                {
                                    style = npc.target;
                                }
                                else
                                {
                                    style = -1;
                                }
                            }

                            //Cancel the attack if it's too close to a "safe angle", which ensures the player can always avoid the attack
                            if (UsefulFunctions.CompareAngles(projVel, destroyerLaserSafeAngle) > MathHelper.PiOver4 / 2 && UsefulFunctions.CompareAngles(-projVel, destroyerLaserSafeAngle) > MathHelper.PiOver4 / 2 && Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, projVel, ModContent.ProjectileType<EnemyLingeringLaser>(), 40, 0, Main.myPlayer, style, npc.whoAmI);
                            }
                        }
                    }

                    if (Main.GameUpdateCount % 150 == 0 && (destroyerAttackIndex == 0 || destroyerAttackIndex == 2))
                    {
                        Vector2 projVel = UsefulFunctions.Aim(npc.Center, Main.player[npc.target].Center, 1);
                        if (UsefulFunctions.CompareAngles(projVel, destroyerLaserSafeAngle) > MathHelper.Pi / 6f && UsefulFunctions.CompareAngles(-projVel, destroyerLaserSafeAngle) > MathHelper.Pi / 6f && Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, projVel, ModContent.ProjectileType<EnemyLingeringLaser>(), 40, 0, Main.myPlayer, 1000 + npc.target, npc.whoAmI);
                        }
                    }

                    laserToggle = !laserToggle;
                }
            }

            if (npc.ModNPC is TheDestroyerOriginal)
            {
                int previousAttack = destroyerAttackIndex;
                bool previousReachedHeight = destroyerReachedHeight;
                bool previouslySpawning = destroyerJustSpawned;
                destroyerChargeTimer++;

                if (!destroyerJustSpawned)
                {
                    //Don't let the custom AI override its "run away" code
                    if (!Main.player[npc.target].dead)
                    {
                        if (destroyerAttackIndex == 3)
                        {
                            destroyerGlowPercent += (1f / 180f);
                            if (!destroyerReachedHeight)
                            {
                                Vector2 targetPoint = Main.player[npc.target].Center;
                                targetPoint.Y -= 400;

                                if (Vector2.Distance(npc.Center, targetPoint) < 70)
                                {
                                    destroyerReachedHeight = true;
                                    destroyerRotation = MathHelper.Pi;
                                    destroyerChargeTimer = -300;
                                }
                                else
                                {
                                    if (destroyerTargetLerp < 1)
                                    {
                                        destroyerTargetLerp += 0.001f;
                                    }
                                    npc.velocity = Vector2.Lerp(npc.velocity, UsefulFunctions.Aim(npc.Center, targetPoint, 15), destroyerTargetLerp);
                                }
                            }
                            //Don't do anything until it's set up
                            else
                            {
                                //Spawn dust
                                Vector2 dustPos = npc.velocity;
                                dustPos = dustPos.SafeNormalize(Vector2.UnitX);
                                dustPos *= 48;
                                dustPos = dustPos.RotatedBy(MathHelper.PiOver2);
                                if (!Main.dedServ)
                                {
                                    Dust.NewDustPerfect(npc.Center + dustPos, DustID.Torch, npc.velocity.RotatedBy(MathHelper.PiOver2) / 6, Scale: 3).noGravity = true;
                                }

                                //Accelerate over time
                                float factor = (400 + destroyerChargeTimer) / 400f;
                                if (destroyerChargeTimer > 0)
                                {
                                    factor = 1;
                                }

                                //Start slowing down near the end of the attack
                                if (destroyerChargeTimer > 920)
                                {
                                    factor = 0.2f + (0.8f * (980 - destroyerChargeTimer) / 60f);
                                }

                                //Rotate in a circle a decent distance from the player
                                destroyerRotation += 0.09f * factor;
                                npc.velocity = new Vector2(60 * factor, 0).RotatedBy(destroyerRotation);

                                if (destroyerChargeTimer == 980)
                                {
                                    destroyerRotation = MathHelper.Pi;
                                    destroyerAttackIndex = 0;
                                    destroyerChargeTimer = -240;
                                    destroyerReachedHeight = false;
                                    destroyerTargetLerp = 0;

                                    //Clean up
                                    for (int i = 0; i < Main.maxProjectiles; i++)
                                    {
                                        if (Main.projectile[i].active && Main.projectile[i].ModProjectile is EnemyLingeringLaser
                                            && FromLaserHost((int)Main.projectile[i].ai[1]) == this)
                                        {
                                            Main.projectile[i].Kill();
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            destroyerGlowPercent = 0.01f;

                            //Circle of lasers
                            if (destroyerAttackIndex == 2)
                            {
                                if (destroyerChargeTimer % 180 == 90)
                                {
                                    if (Main.netMode != NetmodeID.MultiplayerClient)
                                    {
                                        laserRotation = Main.rand.NextVector2Circular(10f, 10f).ToRotation();
                                        npc.netUpdate = true;
                                        float subRotation = 0;
                                        for (int i = 0; i < 3; i++)
                                        {
                                            subRotation += 2 * MathHelper.Pi / 3;

                                            Projectile.NewProjectile(npc.GetSource_FromThis(), Main.player[npc.target].Center, new Vector2(0, 1).RotatedBy(laserRotation + subRotation), ModContent.ProjectileType<EnemyLingeringLaser>(), 40, 0, Main.myPlayer, -3, npc.whoAmI);
                                        }
                                    }
                                }
                            }

                            //Moving square array                        
                            if (destroyerAttackIndex == 1)
                            {
                                if (destroyerChargeTimer % 290 == 1 && destroyerChargeTimer < 580
                                    && Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    horizontalLasers = new List<Projectile>();
                                    verticalLasers = new List<Projectile>();
                                    Vector2 randomVector = Main.rand.NextVector2Circular(10, 10);
                                    if (randomVector.Y > 0)
                                    {
                                        //Never let the movement direction of the grid be *down*, makes it very hard to dodge
                                        randomVector.Y *= -1;
                                    }
                                    laserRotation = randomVector.ToRotation();
                                    npc.netUpdate = true;

                                    Vector2 startPos = new Vector2(0, -3200).RotatedBy(laserRotation);
                                    Vector2 step = new Vector2(220, 220).RotatedBy(laserRotation);
                                    Vector2 laserVel = new Vector2(-1, 1).RotatedBy(laserRotation); //Aim down left
                                    if (Main.netMode != NetmodeID.MultiplayerClient)
                                    {
                                        for (int i = 0; i < 15; i++)
                                        {
                                            horizontalLasers.Add(Projectile.NewProjectileDirect(npc.GetSource_FromThis(), Main.player[npc.target].Center + startPos, laserVel, ModContent.ProjectileType<EnemyLingeringLaser>(), 40, 0, Main.myPlayer, -1, npc.whoAmI));
                                            startPos += step;
                                        }

                                        startPos = new Vector2(0, -3200).RotatedBy(laserRotation);
                                        step = new Vector2(-220, 220).RotatedBy(laserRotation);
                                        laserVel = new Vector2(1, 1).RotatedBy(laserRotation); //Aim down right
                                        for (int i = 0; i < 15; i++)
                                        {
                                            verticalLasers.Add(Projectile.NewProjectileDirect(npc.GetSource_FromThis(), Main.player[npc.target].Center + startPos, laserVel, ModContent.ProjectileType<EnemyLingeringLaser>(), 40, 0, Main.myPlayer, -1, npc.whoAmI));
                                            startPos += step;
                                        }

                                        intersections = new List<Vector2>();
                                        foreach (Projectile hLaser in horizontalLasers)
                                        {
                                            foreach (Projectile vLaser in verticalLasers)
                                            {
                                                Vector2[] collisions = Collision.CheckLinevLine(hLaser.position, hLaser.position + hLaser.velocity * 3000, vLaser.position, vLaser.position + vLaser.velocity * 3000);
                                                if (collisions.Length == 1) //2 lines can only ever intersect once, unless they're parallel and on top of each other. There would be bigger problems if that was the case.
                                                {
                                                    if (!intersections.Contains(collisions[0]))
                                                    {
                                                        intersections.Add(collisions[0]);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                if (!Main.dedServ && intersections != null && destroyerChargeTimer % 290 < 150 && destroyerChargeTimer < 580)
                                {
                                    Vector2 translationOffset = new Vector2(3, 0).RotatedBy(laserRotation) * (destroyerChargeTimer % 290 - 1);

                                    Rectangle screenRect = new Rectangle((int)Main.screenPosition.X - 100, (int)Main.screenPosition.Y - 100, Main.screenWidth + 100, Main.screenHeight + 100);

                                    if (Main.netMode != NetmodeID.MultiplayerClient)
                                    {
                                        foreach (Vector2 intersection in intersections)
                                        {
                                            if (screenRect.Contains(intersection.ToPoint()) && Vector2.DistanceSquared(intersection + translationOffset, Main.player[npc.target].Center) < 250000)
                                            {
                                                for (int i = 0; i < 5; i++)
                                                {
                                                    Vector2 circularOffset = Main.rand.NextVector2CircularEdge(1, 1);
                                                    circularOffset.Normalize();
                                                    Vector2 velocity = new Vector2(0, 3).RotatedBy(laserRotation);
                                                    circularOffset *= (150 - (destroyerChargeTimer % 290));

                                                    Dust.NewDustPerfect(intersection + circularOffset + translationOffset, DustID.OrangeTorch, velocity, Scale: 1).noGravity = true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (destroyerChargeTimer >= 600)
                            {
                                destroyerChargeTimer = 0;
                                destroyerAttackIndex++;
                                if (destroyerAttackIndex == 3 && Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.TheDestroyer.HullGlow"), Color.OrangeRed);
                                }
                            }
                            if (Main.GameUpdateCount % 60 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Vector2 sum = Vector2.Zero;
                                float count = 0;
                                for (int i = 0; i < Main.maxNPCs; i++)
                                {
                                    NPC segment = Main.npc[i];
                                    if (segment.ModNPC is TheDestroyerOriginalBody && segment.realLife == npc.whoAmI)
                                    {
                                        sum += UsefulFunctions.Aim(segment.Center, Main.player[npc.target].Center, 5);
                                        count++;
                                    }
                                }

                                if (count > 0)
                                {
                                    sum /= count;
                                    destroyerLaserSafeAngle = sum.RotatedBy(MathHelper.PiOver2);
                                    npc.netUpdate = true;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (destroyerChargeTimer > 300)
                    {
                        destroyerChargeTimer = 0;
                        destroyerJustSpawned = false;
                    }
                }

                if (Main.netMode != NetmodeID.MultiplayerClient && (previousAttack != destroyerAttackIndex
                    || previousReachedHeight != destroyerReachedHeight || previouslySpawning != destroyerJustSpawned))
                {
                    npc.netUpdate = true;
                }
            }
        }

    }

    public sealed class OldDestroyerCommand : ModCommand
    {
        public override CommandType Type => CommandType.World;
        public override string Command => "olddestroyer";
        public override string Description => "Spawn The Old Destroyer for comparison in Debug Mode.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            Player player = caller.Player;
            if (player == null || !ModContent.GetInstance<tsorcRevampGameplayConfig>().DebugMode)
            {
                caller.Reply("Use /olddestroyer as a player with Debug Mode enabled.", Color.Orange);
                return;
            }

            int type = ModContent.NPCType<TheDestroyerOriginal>();
            if (NPC.AnyNPCs(type))
            {
                caller.Reply("The Old Destroyer is already active.", Color.Orange);
                return;
            }

            int spawnX = (int)player.Center.X + player.direction * 480;
            int spawnY = (int)player.Center.Y + 280;
            int spawnIndex = NPC.NewNPC(player.GetSource_FromThis(), spawnX, spawnY, type, Target: player.whoAmI);
            if (spawnIndex < Main.maxNPCs && Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, number: spawnIndex);
            }
        }
    }
}
