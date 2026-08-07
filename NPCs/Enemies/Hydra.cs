using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    // Sprite by Omnir, from Omnir's Nostalgia Pack: https://forums.terraria.org/index.php?threads/omnirs-nostalgia-pack.11875/
    public class Hydra : ModNPC
    {
        public override string Texture => "tsorcRevamp/NPCs/Enemies/Hydra_Headless";

        private static ReLogic.Content.Asset<Texture2D> neckTexture;
        private static ReLogic.Content.Asset<Texture2D> headTexture;

        float npcAcSPD = 0.6f; // How fast they accelerate.
        float npcSPD = 2.2f; // Max speed

        float npcEnrAcSPD = .9f; // How fast they accelerate, enraged.
        float npcEnrSPD = 5f; // Max speed, enraged.

        // ── Attack chooser state machine ───────────────────────────────────────
        private enum AttackID
        {
            ConsecratedLight,
            SmiteMark,
        }

        private enum Phase
        {
            Chase,
            AimLock,
            SmiteMark,
            SmiteInterval,
        }

        private static readonly AttackID[] AvailableAttacks = { AttackID.ConsecratedLight, AttackID.SmiteMark };

        const int AimLockTicks = 25;
        const int RecoveryTicks = 90;

        const int SmiteFireTick = 60; // ticks from a mark appearing until its bolt fires
        const int SmiteLockTick = 30; // ticks BEFORE firing that the mark's position locks
        const int SmiteIntervalTicks = 60; // delay between consecutive marks in a chain, after firing
        const int MaxSmiteMarks = 5;
        const int SmiteDamage = 45;

        Phase phase = Phase.Chase;
        int phaseTimer;
        AttackID currentAttack;
        int attackCooldown;
        int lockedDirection = 1;

        int smiteMarksRemaining;
        int smiteMarkTimer;
        bool smiteMarkLocked;
        Vector2 smiteMarkPosition;

        // Mouth transition progress: 0.0 (closed) to 1.0 (fully open)
        private float mouthOpenProgress = 0f;

        public override void SetDefaults()
        {
            NPC.width = 170;
            NPC.height = 130;
            NPC.damage = 50;
            NPC.defense = 10;
            NPC.lifeMax = 2350;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 2400f;
            NPC.npcSlots = 100;
            NPC.scale = 1.1f;
            NPC.knockBackResist = 0.1f;
            Main.npcFrameCount[NPC.type] = 16;
            AnimationType = 28;
            NPC.lavaImmune = true;
            NPC.buffImmune[BuffID.Venom] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
            NPC.buffImmune[BuffID.OnFire] = true;

            tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            g.NavSearchRadius = 24;
            g.MaxJumpPower = 10f;
            g.MaxJumpBoost = 6f;
            EvasiveProfile.HeavyBeast(g);
            g.KiteRangeMin = 12f;
            g.KiteRangeMax = 30f;
            g.PatrolMode = NPCs.PatrolMode.Wander;

            attackCooldown = 120;
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) { return 0f; }

        public float CanSpawnLegacy(NPCSpawnInfo s)
        {
            int x = s.SpawnTileX;
            int y = s.SpawnTileY;
            bool oSurface = (y >= (Main.maxTilesY * 0.1f) && y < (Main.maxTilesY * 0.2f));
            bool oUnderSurface = (y >= (Main.maxTilesY * 0.2f) && y < (Main.maxTilesY * 0.3f));
            bool oUnderground = (y >= (Main.maxTilesY * 0.3f) && y < (Main.maxTilesY * 0.4f));
            bool oCavern = (y >= (Main.maxTilesY * 0.4f) && y < (Main.maxTilesY * 0.6f));
            bool oUnderworld = (y >= (Main.maxTilesY * 0.8f));
            bool oBorders = (y < (Main.maxTilesY * 0.03f) || x < (Main.maxTilesX * 0.03f) || y > (Main.maxTilesY * 0.97f) || x > (Main.maxTilesX * 0.97f));
            Player p = s.Player;
            if ((p.townNPCs > 2f && !Main.bloodMoon) || Main.pumpkinMoon || Main.snowMoon || !p.ZoneJungle || oUnderworld || oBorders)
            {
                return 0f;
            }
            if (oSurface || oUnderSurface || oUnderground || oCavern)
            {
                if (Main.rand.Next(12000) == 1) return 1f;
                else if (Main.hardMode && Main.rand.Next(50) == 1) return 1f;
                else if ((oUnderground || oCavern) && Main.rand.Next(800) == 1) return 1f;
                else if (Main.hardMode && (oUnderground || oCavern) && Main.rand.Next(30) == 1) return 1f;
                else if (Main.bloodMoon && Main.rand.Next(120) == 1) return 1f;
                return 0f;
            }
            return 0f;
        }

        void TryStartAttack(Player player)
        {
            if (attackCooldown > 0 || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (NPC.velocity.Y != 0f)
            {
                return;
            }
            if (!Main.rand.NextBool(90))
            {
                return;
            }

            currentAttack = AvailableAttacks[Main.rand.Next(AvailableAttacks.Length)];
            NPC.netUpdate = true;

            switch (currentAttack)
            {
                case AttackID.ConsecratedLight:
                    lockedDirection = player.Center.X >= NPC.Center.X ? 1 : -1;
                    phase = Phase.AimLock;
                    phaseTimer = 0;
                    break;
                case AttackID.SmiteMark:
                    smiteMarksRemaining = RollSmiteMarkCount();
                    BeginNextSmiteMark(player);
                    break;
            }
        }

        void TickAimLock()
        {
            NPC.direction = lockedDirection;
            NPC.spriteDirection = lockedDirection;

            phaseTimer++;
            if (phaseTimer == 1)
            {
                tsorcRevampAIs.SpawnTelegraphFlash(NPC, new Color(255, 220, 90));
            }

            if (phaseTimer >= AimLockTicks)
            {
                FireCurrentAttack();
                attackCooldown = RecoveryTicks;
                phase = Phase.Chase;
                phaseTimer = 0;
            }
        }

        void FireCurrentAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            switch (currentAttack)
            {
                case AttackID.ConsecratedLight:
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.UnitY, ModContent.ProjectileType<Projectiles.Enemy.EnemyConsecratedLight>(), 35, 0f, Main.myPlayer, 0f, NPC.whoAmI);
                    break;
            }
        }

        int RollSmiteMarkCount()
        {
            float lifeLostFraction = MathHelper.Clamp(1f - (NPC.life / (float)NPC.lifeMax), 0f, 1f);
            int count = 1;
            while (count < MaxSmiteMarks && Main.rand.NextFloat() < lifeLostFraction)
            {
                count++;
            }
            return count;
        }

        void BeginNextSmiteMark(Player player)
        {
            smiteMarkTimer = 0;
            smiteMarkLocked = false;
            smiteMarkPosition = player.Center;
            phase = Phase.SmiteMark;
            phaseTimer = 0;
            tsorcRevampAIs.SpawnTelegraphFlash(NPC, new Color(255, 220, 90));
        }

        void TickSmiteMark(Player player)
        {
            smiteMarkTimer++;

            if (!smiteMarkLocked)
            {
                smiteMarkPosition = player.Center;
                if (smiteMarkTimer >= SmiteFireTick - SmiteLockTick)
                {
                    smiteMarkLocked = true;
                    tsorcRevampAIs.SpawnTelegraphFlash(NPC, new Color(255, 220, 90), smiteMarkPosition);
                }
            }

            SpawnSmiteMarkDust(smiteMarkPosition, smiteMarkTimer / (float)SmiteFireTick);

            if (smiteMarkTimer >= SmiteFireTick)
            {
                FireConsecratedLightning(smiteMarkPosition);
                smiteMarksRemaining--;

                if (smiteMarksRemaining > 0)
                {
                    phase = Phase.SmiteInterval;
                    phaseTimer = 0;
                }
                else
                {
                    attackCooldown = RecoveryTicks;
                    phase = Phase.Chase;
                    phaseTimer = 0;
                }
            }
        }

        void TickSmiteInterval(Player player)
        {
            phaseTimer++;
            if (phaseTimer >= SmiteIntervalTicks)
            {
                BeginNextSmiteMark(player);
            }
        }

        void SpawnSmiteMarkDust(Vector2 position, float progress)
        {
            if (Main.netMode == NetmodeID.Server || Main.rand.NextFloat() >= 0.3f + progress * 0.5f)
            {
                return;
            }
            Vector2 edge = Main.rand.NextVector2CircularEdge(40f, 40f) * (1f - progress * 0.4f);
            int dust = Dust.NewDust(position + edge, 2, 2, DustID.GoldFlame, 0f, 0f, 100, default, 1f + progress);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity = -edge * 0.03f;
        }

        void FireConsecratedLightning(Vector2 position)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Vector2 spawnPosition = position - new Vector2(0f, 236f);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPosition, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.ConsecratedLightning>(), SmiteDamage, 0f, Main.myPlayer);
        }

        public override void AI()
        {
            NPC.TargetClosest(true);
            Player player = Main.player[NPC.target];

            bool enraged = (NPC.life < (float)NPC.lifeMax * .2f);
            float accel = enraged ? npcEnrAcSPD : npcAcSPD;
            float topSpeed = enraged ? npcEnrSPD : npcSPD;

            tsorcRevampAIs.FighterAI(NPC, topSpeed: topSpeed, acceleration: accel, canTeleport: true, doorBreakingDamage: 2, minSurfaceWidth: 4, canWalkBackwards: true, canPounce: true);

            if (attackCooldown > 0)
            {
                attackCooldown--;
            }

            switch (phase)
            {
                case Phase.Chase:
                    TryStartAttack(player);
                    break;
                case Phase.AimLock:
                    TickAimLock();
                    break;
                case Phase.SmiteMark:
                    TickSmiteMark(player);
                    break;
                case Phase.SmiteInterval:
                    TickSmiteInterval(player);
                    break;
            }

            // Update mouth open progress transition state
            float targetMouth = 0f;
            if (phase == Phase.AimLock)
            {
                float progress = phaseTimer / (float)AimLockTicks;
                targetMouth = progress < 0.7f ? 0.5f : 1.0f;
            }
            else if (phase == Phase.SmiteMark)
            {
                targetMouth = smiteMarkLocked ? 1.0f : 0.5f;
            }
            else if (phase == Phase.SmiteInterval)
            {
                targetMouth = 0f;
            }

            mouthOpenProgress = MathHelper.Lerp(mouthOpenProgress, targetMouth, 0.15f);
        }

        public override void OnKill()
        {
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("HydraGore1").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("HydraGore2").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("HydraGore3").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("HydraGore2").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("HydraGore3").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("HydraGore1").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("HydraGore1").Type, 1.1f);
        }

        // ── Drawing Engine ─────────────────────────────────────────────────────────────
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            neckTexture ??= ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Enemies/Hydra_Neck");
            headTexture ??= ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Enemies/Hydra_Head");

            // Base neck anchor offsets on body frame 0 (relative to center, facing left)
            // Neck 0 (Purple / Back): offset (-13, -34)
            // Neck 1 (Red / Middle): offset (-7, -36)
            // Neck 2 (Orange / Front): offset (-1, -36)
            Vector2[] neckBaseOffsetsLeft = new Vector2[]
            {
                new Vector2(-13f, -34f),
                new Vector2(-7f, -36f),
                new Vector2(-1f, -36f)
            };

            // 1. Draw Purple Neck & Head (Back layer)
            DrawNeckAndHead(spriteBatch, screenPos, drawColor, neckIndex: 0, neckBaseOffsetsLeft[0]);

            // 2. Draw Red Neck & Head (Middle layer)
            DrawNeckAndHead(spriteBatch, screenPos, drawColor, neckIndex: 1, neckBaseOffsetsLeft[1]);

            // 3. Draw Hydra_Headless Body (Center layer)
            DrawBody(spriteBatch, screenPos, drawColor);

            // 4. Draw Orange Neck & Head (Front layer)
            DrawNeckAndHead(spriteBatch, screenPos, drawColor, neckIndex: 2, neckBaseOffsetsLeft[2]);

            return false;
        }

        private void DrawBody(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D bodyTex = ModContent.Request<Texture2D>(Texture).Value;
            int frameHeight = bodyTex.Height / Main.npcFrameCount[NPC.type];
            Rectangle sourceRect = new Rectangle(0, NPC.frame.Y, bodyTex.Width, frameHeight);
            Vector2 origin = new Vector2(bodyTex.Width / 2f, frameHeight / 2f);

            SpriteEffects effects = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 drawPos = NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY);

            spriteBatch.Draw(bodyTex, drawPos, sourceRect, drawColor, NPC.rotation, origin, NPC.scale, effects, 0f);
        }

        private void DrawNeckAndHead(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, int neckIndex, Vector2 baseOffsetLeft)
        {
            if (neckTexture == null || headTexture == null) return;

            Texture2D neckTex = neckTexture.Value;
            Texture2D headTex = headTexture.Value;

            // Facing direction adjustments
            bool facingRight = NPC.spriteDirection == 1;
            Vector2 baseOffset = facingRight ? new Vector2(-baseOffsetLeft.X, baseOffsetLeft.Y) : baseOffsetLeft;

            Vector2 currentPos = NPC.Center + baseOffset;
            float time = (float)Main.timeForVisualEffects;

            // Independent idle sway per neck
            float[] swayFreq = { 0.05f, 0.045f, 0.055f };
            float[] swayPhase = { 0.0f, 2.1f, 4.2f };
            float swayAngle = MathF.Sin(time * swayFreq[neckIndex] + swayPhase[neckIndex]) * 0.08f;

            // Initial direction extending horizontally rightward (if facing left) or leftward (if facing right)
            // Facing left (spriteDirection == -1): starts pointing right (+X = 0 rad), curves counterclockwise/upward (-rad)
            // Facing right (spriteDirection == 1): starts pointing left (-X = PI rad), curves clockwise/upward (+rad)
            float baseAngle = facingRight ? MathHelper.Pi : 0f;
            float currentAngle = baseAngle + swayAngle;

            const int segmentCount = 22;
            const float segmentLength = 5.5f;
            const float maxBendStep = 0.0872665f; // 5 degrees max bend per segment step

            // Curve progress: angle steps smoothly toward backwards "C" target angle
            // Target total bend is ~160-180 degrees (Pi radians)
            float totalTargetBend = facingRight ? MathHelper.Pi : -MathHelper.Pi;
            float bendPerStep = MathHelper.Clamp(totalTargetBend / segmentCount, -maxBendStep, maxBendStep);

            Vector2 lastPos = currentPos;

            // Render neck segments
            for (int i = 0; i < segmentCount; i++)
            {
                currentAngle += bendPerStep;
                Vector2 segmentDir = currentAngle.ToRotationVector2();
                currentPos += segmentDir * segmentLength;

                Vector2 drawPos = currentPos - screenPos;
                Vector2 neckOrigin = new Vector2(neckTex.Width / 2f, neckTex.Height / 2f);

                // Draw neck segment without individual rotation so top/bottom black borders remain horizontal
                spriteBatch.Draw(neckTex, drawPos, null, drawColor, 0f, neckOrigin, NPC.scale, SpriteEffects.None, 0f);

                lastPos = currentPos;
            }

            // Head frame selection: 0 = closed, 1 = half open, 2 = wide open
            int headFrame;
            if (mouthOpenProgress < 0.25f)
                headFrame = 0;
            else if (mouthOpenProgress < 0.75f)
                headFrame = 1;
            else
                headFrame = 2;

            int headFrameHeight = headTex.Height / 3;
            Rectangle headSourceRect = new Rectangle(0, headFrame * headFrameHeight, headTex.Width, headFrameHeight);

            // Head attaches to last neck position, oriented following current terminal neck angle or target
            SpriteEffects headEffects = facingRight ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 headOrigin = facingRight ? new Vector2(headTex.Width - 6f, headFrameHeight / 2f) : new Vector2(6f, headFrameHeight / 2f);
            float headRotation = facingRight ? currentAngle - MathHelper.Pi : currentAngle;

            Vector2 headDrawPos = lastPos - screenPos;
            spriteBatch.Draw(headTex, headDrawPos, headSourceRect, drawColor, headRotation, headOrigin, NPC.scale, headEffects, 0f);
        }
    }
}
