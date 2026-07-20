using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies{
	// Sprite by Omnir, from Omnir's Nostalgia Pack: https://forums.terraria.org/index.php?threads/omnirs-nostalgia-pack.11875/
	public class Hydra : ModNPC
	{
        float npcAcSPD = 0.6f; //How fast they accelerate.
        float npcSPD = 2.2f; //Max speed

        float npcEnrAcSPD = .9f; //How fast they accelerate, enraged.
        float npcEnrSPD = 5f; //Max speed, enraged.

        // ── Attack chooser state machine ───────────────────────────────────────
        // Chase runs FighterAI and rolls to start an attack. AimLock holds the NPC's facing
        // steady for AimLockTicks before the attack actually fires (it can still walk forward
        // or backward per FighterAI's canWalkBackwards - only the facing direction is locked),
        // so the attack direction is telegraphed and readable. RecoveryTicks then gates how soon
        // the next attack can be chosen.
        private enum AttackID
        {
            ConsecratedLight,
            SmiteMark,
        }

        // SmiteMark is ground-targeted (not directional), so it skips AimLock and drives its
        // own two-phase loop instead: SmiteMark (appear -> track -> lock -> fire, SmiteFireTick
        // ticks total) then SmiteIntervalTicks of quiet before the next mark in the chain.
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
        const int SmiteLockTick = 30; // ticks BEFORE firing that the mark's position locks (i.e. locks at tick SmiteFireTick - SmiteLockTick)
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

			// Phase 2: SmartFighter4AI movement + beast levers (migrated off MNPC). MaxJumpPower above the 8
			// default so this huge beast clears ledges. minSurfaceWidth (in AI) is now the SUPPORT CORE (center
			// tiles that need solid ground); the ~10.6-tile sprite's edges clip/sink into terrain (Phase 3). Tune.
			tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
			g.NavSearchRadius = 24;
			g.MaxJumpPower = 10f;
			g.MaxJumpBoost = 6f;
			// On-hit evasion: lumber back to reset spacing, or telegraph a hyper-armored charge back in.
			EvasiveProfile.HeavyBeast(g);
			// Phase 1 (beast positioner): never stand still — oscillate in a large band when it can't path; wander
			// off if it can't reach you AND you stop hitting it for ~10s. Tune the band to taste.
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
            bool oSky = (y < (Main.maxTilesY * 0.1f));
            bool oSurface = (y >= (Main.maxTilesY * 0.1f) && y < (Main.maxTilesY * 0.2f));
            bool oUnderSurface = (y >= (Main.maxTilesY * 0.2f) && y < (Main.maxTilesY * 0.3f));
            bool oUnderground = (y >= (Main.maxTilesY * 0.3f) && y < (Main.maxTilesY * 0.4f));
            bool oCavern = (y >= (Main.maxTilesY * 0.4f) && y < (Main.maxTilesY * 0.6f));
            bool oMagmaCavern = (y >= (Main.maxTilesY * 0.6f) && y < (Main.maxTilesY * 0.8f));
            bool oUnderworld = (y >= (Main.maxTilesY * 0.8f));
            bool oBorders = (y < (Main.maxTilesY * 0.03f) || x < (Main.maxTilesX * 0.03f) || y > (Main.maxTilesY * 0.97f) || x > (Main.maxTilesX * 0.97f));
            int tile = (int)Main.tile[x, y].TileType;
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
        //Spawns in the Jungle, mostly Underground and in the Cavern.

        void TryStartAttack(Player player)
        {
            if (attackCooldown > 0 || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (NPC.velocity.Y != 0f) // only commit to a cast while grounded, so a ground-sweep/mark reads correctly
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
                    // Chance to chain more marks (1-5) the more health it's already lost.
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

        // 1 guaranteed mark, then a fresh roll for each additional one (up to MaxSmiteMarks) -
        // chance of chaining scales with how much of its life is already gone.
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
                smiteMarkPosition = player.Center; // tracks the player until it locks in place
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

            bool enraged = (NPC.life < (float)NPC.lifeMax * .2f); //  speed up at low life
            float accel = enraged ? npcEnrAcSPD : npcAcSPD; //  how fast it can speed up
            float topSpeed = enraged ? npcEnrSPD : npcSPD; //  max walking speed, also affects jump length

            // SmartFighter4AI movement. minSurfaceWidth:4 keeps this 10-tile-wide beast off narrow ledges;
            // canWalkBackwards lets it keep moving away/toward the player while AimLock below holds its facing.
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
    }
}
