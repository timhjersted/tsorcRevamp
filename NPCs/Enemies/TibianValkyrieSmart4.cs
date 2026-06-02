using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    // Test 4 enemy — routes through SmartFighter4AI, which keeps SF3's A* pathfinding
    // but replaces the static jump-arc table with PHYSICS-BASED jumps:
    //   - ComputeJumpArc solves the exact projectile arc from npc.gravity + jump limits
    //   - jumps fire only when genuinely makeable; sized to land ~1 tile onto the target
    //   - the airborne handler preserves the computed launch velocity (SF3 erased it,
    //     which is why SF3 fell short of wide gaps)
    // Same stats / texture / attack as the other Valkyrie testbeds for direct A/B comparison.
    //
    // GRAVITY EXPERIMENT: npc.gravity is set explicitly below. Lower = floatier / longer
    // jumps (nimble), higher = heavier / shorter jumps. SF4 reads this same value so the
    // arc prediction matches the engine's integration exactly.
    class TibianValkyrieSmart4 : ModNPC
    {
        public override string Texture => "tsorcRevamp/NPCs/Enemies/TibianValkyrie";

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = Main.npcFrameCount[NPCID.Skeleton];
        }

        public override void SetDefaults()
        {
            AnimationType = NPCID.Skeleton;
            NPC.aiStyle = -1;
            NPC.height = 40;
            NPC.width = 20;
            NPC.lifeMax = 90;
            NPC.damage = 22;
            NPC.scale = 1f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = .5f;
            NPC.value = 0;
            NPC.defense = 4;
            // NOTE: NPC.gravity is read-only in this tModLoader version, so effective gravity is
            // requested via SmartFighter4AI.Run(gravity: ...) below instead of set here.

            int spearDamage = 10;
            if (Main.hardMode)
            {
                NPC.lifeMax = 260;
                NPC.damage = 40;
                NPC.defense = 12;
                spearDamage = 20;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 700;
                NPC.damage = 70;
                NPC.defense = 30;
                spearDamage = 35;
            }

            // Spear throw velocity bumped 8 -> 11: the ballistic solver under-threw (landed at the
            // NPC's feet) when the player was above and far. More launch speed extends the arc so
            // shots reach an elevated player; if still short, the player is simply out of range.
            UsefulFunctions.AddAttack(NPC, 190, ModContent.ProjectileType<Projectiles.Enemy.BlackKnightSpear>(), spearDamage, 11, shootSound: SoundID.Item17);

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavigationTier = 0;
            globalNPC.MaxJumpPower = 9f;
            globalNPC.MaxJumpBoost = 5f;
            globalNPC.WeakTeleport = false;
            globalNPC.CanStopToFire = false;

            globalNPC.Aggression = 0.6f;
            globalNPC.Patience = 1.2f;
            globalNPC.Agility = 0.25f;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) => 0f;

        public override void AI()
        {
            SmartFighter4AI.Run(NPC, topSpeed: 1.55f, acceleration: 0.10f, attackRange: 700f);

            // Keep the NPC from vanishing via vanilla's timeLeft despawn while it's actively
            // pursuing a living target within a generous range. Without this, an NPC that gets
            // stuck (e.g. below a rope while the player climbs away) slips out of the player's
            // active rectangle and vanilla despawns it in ~10s. Refreshing timeLeft each frame
            // lets it persist so its behavior stays observable; it still despawns if the player
            // truly leaves the area (> ~4000px).
            Player target = Main.player[NPC.target];
            if (target != null && target.active && !target.dead && NPC.Distance(target.Center) < 4000f)
            {
                if (NPC.timeLeft < 600) NPC.timeLeft = 600;
            }

            if (!NPC.downedBoss1)
            {
                NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().AttackList[0].damage = 7;
            }
        }

        // Custom framing for this sheet's layout (15 frames):
        //   frame 0      = jump
        //   frame 6 (7th)= standing / idle (feet together)
        //   frames 1..14 = walk cycle
        // With AnimationType = Skeleton, vanilla held the last walk frame at rest, which looked
        // like a wide-legged walk pose. This forces the proper idle/jump frames.
        private const int IdleFrameIndex = 6;
        private const int JumpFrameIndex = 0;
        public override void FindFrame(int frameHeight)
        {
            int frameCount = Main.npcFrameCount[NPC.type];
            // Airborne -> jump frame.
            if (NPC.velocity.Y != 0f)
            {
                NPC.frame.Y = JumpFrameIndex * frameHeight;
                return;
            }
            // Idle: grounded and essentially stationary -> standing frame.
            // NOTE: do NOT reset frameCounter here. The AI frequently dips velocity below this threshold
            // for a frame or two while maneuvering; zeroing the counter on every dip pinned the walk cycle
            // to frame 1 ("stuck walking"). Holding the counter lets the cycle resume seamlessly.
            if (System.Math.Abs(NPC.velocity.X) < 0.2f)
            {
                NPC.frame.Y = IdleFrameIndex * frameHeight;
                return;
            }
            // Walking -> the standard vanilla fighter walk cycle over frames 1..frameCount-1, speed-paced.
            // This matches how the other Valkyrie testbeds animate (AnimationType = Skeleton, no override);
            // the custom walk math here previously broke it. Only the idle/jump frames above are intentional
            // custom overrides — the walk itself is back to the known-good vanilla pacing.
            NPC.frameCounter += 1.0 + System.Math.Abs(NPC.velocity.X) * 0.5;
            if (NPC.frameCounter > 6.0)
            {
                NPC.frame.Y += frameHeight;
                NPC.frameCounter = 0.0;
            }
            int walkIndex = NPC.frame.Y / frameHeight;
            if (walkIndex < 1 || walkIndex >= frameCount)
            {
                NPC.frame.Y = frameHeight; // wrap to the first walk frame (frame 0 is reserved for the jump pose)
            }
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone) =>
            SmartFighter4AI.OnHit(NPC);

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone) =>
            SmartFighter4AI.OnHit(NPC);

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer >= 140)
            {
                Texture2D spearTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Enemies/TibianValkyrie_Spear");
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition,
                    new Rectangle(NPC.frame.X, NPC.frame.Y, 76, 58), drawColor, NPC.rotation,
                    new Vector2(38, 34), NPC.scale, effects, 0);
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 5; i++)
            {
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X += Main.rand.Next(-50, 51) * 0.06f;
                dust.velocity.Y += Main.rand.Next(-50, 51) * 0.06f;
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (NPC.life <= 0 && !Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Valkyrie Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Valkyrie Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Valkyrie Gore 3").Type, 1f);
            }
        }
    }
}
