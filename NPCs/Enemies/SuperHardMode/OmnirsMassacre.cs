using Terraria.Audio;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tsorcRevamp.Items.Materials;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
	public class OmnirsMassacre : ModNPC
	{
        float npcAcSPD = 1.5f; //How fast they accelerate.
        float npcSPD = 2.65f; //Max speed (Phase2: 50% slower, was 5.3f)

        float npcEnrAcSPD = 2.1f; //How fast they accelerate.
        float npcEnrSPD = 3.55f; //Max speed (Phase2: 50% slower, was 7.1f)

        // ── Attack state machine ──
        private enum MassacreAttackState { Idle, FireBreathWindup, FireBreathing, RainWindup, Raining }
        private MassacreAttackState _attackState = MassacreAttackState.Idle;
        private int _attackTimer;
        private int _attackCooldown;

        private const int FireBreathWindupTicks = 150;   // 2.5 s telegraph (smoulder ~1 s + grow ~1.5 s)
        private const int FireBreathDurationTicks = 70;  // length of the breath stream
        private const int FireBreathCooldownTicks = 300; // ~5 s before another breath
        private const int FireBreathDamage = 36;
        private const float FireBreathTriggerRange = 620f;

        // Rain of fire: 2 s purple-glow windup (with Darkness), then 20 fireballs 25 t apart, each
        // aimed at where the player WAS 25 t earlier, falling from a wide band 15-20 tiles up.
        private int _rainCooldown;
        private int _glowTimer; // > 0 = draw the purple pre-attack glow
        private int _rainShotsRemaining;
        private const int RainWindupTicks = 120;     // 2 s
        private const int RainShots = 20;
        private const int RainShotSpacing = 25;
        private const int RainCooldownTicks = 720;   // 12 s minimum between casts
        private const int RainFireballDamage = 33;
        private const float RainTriggerRange = 900f;

        // Ring buffer of the target's recent positions for the 25-tick-delayed rain targeting.
        private readonly Vector2[] _playerHistory = new Vector2[30];
        private int _historyIndex;

        // Mouth: ~70 px forward of center (facing-relative) and 5 px up.
        private Vector2 MouthPosition => NPC.Center + new Vector2(NPC.spriteDirection * 70f, -5f);

        public override void SetDefaults()
		{
			NPC.width = 140;
			NPC.height = 130;
			NPC.damage = 110;
			NPC.defense = 65;
			NPC.lifeMax = 15000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 400000;
			NPC.npcSlots = 5;
			NPC.knockBackResist = 0f;
			Main.npcFrameCount[NPC.type] = 16;
			AnimationType = 28;
			NPC.lavaImmune = true;
			NPC.buffImmune[BuffID.Venom] = true;
			NPC.buffImmune[BuffID.Confused] = true;
			NPC.buffImmune[BuffID.CursedInferno] = true;
			NPC.buffImmune[BuffID.OnFire] = true;

			// Phase 2: SmartFighter4AI movement + beast levers. minSurfaceWidth (in AI) is now the SUPPORT CORE
			// (center tiles that need solid ground); the ~8.75-tile sprite's edges clip/sink into terrain (Phase 3).
			// NavSearchRadius enables A*; MaxJumpPower above the 8 default for a strong jump given its size
			// (NPC.gravity is read-only, so no true heavy "weighty" fall). Tune to taste.
			tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
			g.NavSearchRadius = 24;
			g.MaxJumpPower = 10f;
			g.MaxJumpBoost = 6f;
			// On-hit evasion: lumber back to reset spacing, or telegraph a hyper-armored charge back in.
			EvasiveProfile.HeavyBeast(g);
			// HeavyBeast's 6× dash assumed a ~0.5 walk speed; Massacre walks at ~2.65, so 6× blinked
			// across the screen.  Drop it to a readable, dodgeable charge (~6.6 px/frame).
			g.EvasiveDashSpeedMult = 2.5f;
			// Phase 1 (beast positioner): never stand still — oscillate in a large band when it can't path (it has
			// FireBreath + Rain to punish from range); wander off if it can't reach you AND you stop hitting it for
			// ~10s. Massacre walks fast (~2.65), so bump the back-off speeds above the slow defaults. Tune to taste.
			g.KiteRangeMin = 14f;
			g.KiteRangeMax = 32f;
			g.PatrolMode = NPCs.PatrolMode.Wander;
			g.BeastRetreatSpeedCalm = 2f;
			g.BeastRetreatSpeedHit = 6f;
		}

		public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
		{
			tsorcRevampAIs.EvasiveOnHit(NPC, true);
		}
		public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
		{
			tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
		}
        //Spawns in the Underworld on hardmode.
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (tsorcRevampWorld.SuperHardMode)
            {
                if ((spawnInfo.Player.ZoneUnderworldHeight || (spawnInfo.Player.ZoneOverworldHeight && !Main.dayTime)) && Main.rand.NextBool(45))
                {
                    return 1;
                }
                else return 0;
            }
            else return 0;
        }
        public void teleport(bool pre)
		{
			if (Main.netMode != 2)
			{
				SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
				for (int m = 0; m < 25; m++)
				{
					int dustID = Dust.NewDust(NPC.position, NPC.width, NPC.height, 6, 0, 0, 100, Color.White, 2f);
					Main.dust[dustID].noGravity = true;
					Main.dust[dustID].velocity = new Vector2(MathHelper.Lerp(-1f, 1f, (float)Main.rand.NextDouble()), MathHelper.Lerp(-1f, 1f, (float)Main.rand.NextDouble()));
					Main.dust[dustID].velocity *= 7f;
				}
			}
		}
		public override void AI()  //  warrior ai
		{
            bool enraged = (NPC.life < (float)NPC.lifeMax*.2f);  //  speed up at low life
			int shotRate = enraged?100:70;
			float accel=enraged? npcEnrAcSPD:npcAcSPD;  //  how fast it can speed up
			float topSpeed=enraged? npcEnrSPD:npcSPD;  //  max walking speed, also affects jump length
            tsorcRevampAIs.FighterAI(NPC, topSpeed: topSpeed, acceleration: accel, canPounce: false, canDodgeroll: false, canTeleport: true, minSurfaceWidth: 4, canWalkBackwards: true); // support core (center 4 tiles); sprite edges clip/sink (Phase 3)
            Vector2 angle = Main.player[NPC.target].Center - NPC.Center;
            angle.Y = angle.Y - (Math.Abs(angle.X) * .1f);
            angle.X += (float)Main.rand.Next(-20, 21);
            angle.Y += (float)Main.rand.Next(-20, 21);
            angle.Normalize();
            if (NPC.lavaWet) NPC.velocity.Y-=2;
			float distance = NPC.Distance(Main.player[NPC.target].Center);
            #region shoot and walk
            if (Main.netMode != 1 && !Main.player[NPC.target].dead) // can generalize this section to moving+Projectile code // can generalize this section to moving+Projectile code
            {
                if (NPC.justHit)
                    NPC.ai[2] = 0f; // reset throw countdown when hit

                #region Charge
                if (NPC.velocity.Y == 0f && Main.rand.Next(550) == 1)
                {
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    NPC.velocity.X = (float)(Math.Cos(rotation) * 7) * -1;
                    NPC.velocity.Y = (float)(Math.Sin(rotation) * 7) * -1;
                    NPC.ai[1] = 1f;
                    NPC.netUpdate = true;
                }
                #endregion
            }
            #endregion
            if (NPC.velocity.Y == 0f && Main.rand.Next(550) == 1)
            {
                Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                NPC.velocity.X = (float)(Math.Cos(rotation) * 7) * -1;
                NPC.velocity.Y = (float)(Math.Sin(rotation) * 7) * -1;
                NPC.localAI[3] = 1f;
                NPC.netUpdate = true;
            }

            UpdateAttacks(distance);
		}

        // ── Attack FSM ──────────────────────────────────────────────────────────
        // Idle picks an attack; fire breath (mouth telegraph → FireBreath stream) and rain of fire
        // (2 s purple-glow + Darkness windup → 20 delayed-aim fireballs).  Runs alongside FighterAI.
        private void UpdateAttacks(float distance)
        {
            if (_attackCooldown > 0) _attackCooldown--;
            if (_rainCooldown > 0) _rainCooldown--;
            if (_glowTimer > 0) _glowTimer--;

            Player target = Main.player[NPC.target];

            // Record the target's position each tick for the rain's 25-tick-delayed aim.
            _playerHistory[_historyIndex % _playerHistory.Length] = target.Center;
            _historyIndex++;

            bool canAttack = !target.dead && NPC.HasValidTarget;

            switch (_attackState)
            {
                case MassacreAttackState.Idle:
                    if (!canAttack || NPC.velocity.Y != 0f)
                        break;

                    // Rain of fire fires as soon as it's off its 12 s cooldown and the player's near.
                    if (_rainCooldown == 0 && distance < RainTriggerRange && Main.rand.Next(120) == 0)
                    {
                        _attackState = MassacreAttackState.RainWindup;
                        _attackTimer = RainWindupTicks;
                        _rainCooldown = RainCooldownTicks;
                        _glowTimer = RainWindupTicks;
                        if (!Main.dedServ)
                            SoundEngine.PlaySound(SoundID.Item170 with { Volume = 0.7f }, NPC.Center);
                        break;
                    }

                    // Fire breath.
                    if (_attackCooldown == 0 && distance < FireBreathTriggerRange
                        && Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1)
                        && Main.rand.Next(90) == 0)
                    {
                        _attackState = MassacreAttackState.FireBreathWindup;
                        _attackTimer = FireBreathWindupTicks;
                        if (!Main.dedServ)
                            SoundEngine.PlaySound(SoundID.Item119 with { Volume = 0.7f }, NPC.Center);
                    }
                    break;

                case MassacreAttackState.FireBreathWindup:
                    NPC.velocity.X *= 0.7f; // plant to breathe
                    EmitMouthEmber(FireBreathWindupTicks - _attackTimer);
                    if (--_attackTimer <= 0)
                    {
                        _attackState = MassacreAttackState.FireBreathing;
                        _attackTimer = FireBreathDurationTicks;
                    }
                    break;

                case MassacreAttackState.FireBreathing:
                    NPC.velocity.X *= 0.85f;
                    if (Main.netMode != NetmodeID.MultiplayerClient && _attackTimer % 4 == 0)
                    {
                        Vector2 mouth = MouthPosition;
                        Vector2 vel = UsefulFunctions.Aim(mouth, target.Center, 9f) + Main.rand.NextVector2Circular(1.5f, 1.5f);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), mouth, vel,
                            ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), FireBreathDamage, 0f, Main.myPlayer);
                    }
                    if (--_attackTimer <= 0)
                    {
                        _attackState = MassacreAttackState.Idle;
                        _attackCooldown = FireBreathCooldownTicks;
                    }
                    break;

                case MassacreAttackState.RainWindup:
                    NPC.velocity.X *= 0.6f; // plant + charge up (purple glow drawn in PreDraw)
                    ApplyDarkness(target);
                    if (--_attackTimer <= 0)
                    {
                        _attackState = MassacreAttackState.Raining;
                        _rainShotsRemaining = RainShots;
                        _attackTimer = 1; // first fireball next tick
                    }
                    break;

                case MassacreAttackState.Raining:
                    NPC.velocity.X *= 0.9f;
                    ApplyDarkness(target);
                    if (--_attackTimer <= 0)
                    {
                        SpawnRainFireball();
                        _attackTimer = RainShotSpacing;
                        if (--_rainShotsRemaining <= 0)
                            _attackState = MassacreAttackState.Idle;
                    }
                    break;
            }
        }

        // Keep refreshing Darkness on the target for the windup + the whole rain.
        private void ApplyDarkness(Player target)
        {
            if (target.active && !target.dead)
                target.AddBuff(BuffID.Darkness, 40);
        }

        // One rain fireball: falls from a random spot in a 30-tile-wide, 5-tile-tall band 15-20 tiles
        // up, aimed at where the player was ~25 ticks ago (so a moving player can outrun the volley).
        private void SpawnRainFireball()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int delayedIdx = ((_historyIndex - 1 - RainShotSpacing) % _playerHistory.Length + _playerHistory.Length) % _playerHistory.Length;
            Vector2 targetPos = _playerHistory[delayedIdx];
            if (targetPos == Vector2.Zero)
                targetPos = Main.player[NPC.target].Center;

            float spawnX = NPC.Center.X + Main.rand.NextFloat(-240f, 240f);          // 30-tile width
            float spawnY = NPC.Center.Y - Main.rand.NextFloat(15f * 16f, 20f * 16f); // 15-20 tiles up
            Vector2 spawn = new Vector2(spawnX, spawnY);
            Vector2 vel = UsefulFunctions.Aim(spawn, targetPos, 9f);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), spawn, vel,
                ModContent.ProjectileType<Projectiles.Enemy.DragonMeteor>(), RainFireballDamage, 2f, Main.myPlayer);
        }

        // Red mouth ember during the telegraph: small/steady for the first ~1 s (smoulder), then
        // swelling over the remaining ~1.5 s before the breath releases.
        private void EmitMouthEmber(int elapsed)
        {
            if (Main.dedServ) return;
            float t = MathHelper.Clamp(elapsed / (float)FireBreathWindupTicks, 0f, 1f);
            float scale = t < 0.4f ? 0.45f : MathHelper.Lerp(0.6f, 1.9f, (t - 0.4f) / 0.6f);
            Vector2 mouth = MouthPosition;
            int count = t < 0.4f ? 1 : 2;
            for (int i = 0; i < count; i++)
            {
                Dust d = Dust.NewDustPerfect(mouth + Main.rand.NextVector2Circular(3f, 3f), DustID.RedTorch,
                    Main.rand.NextVector2Circular(0.6f, 0.6f), 0, default, scale);
                d.noGravity = true;
            }
            Lighting.AddLight(mouth, 0.6f * scale, 0.05f, 0.05f);
        }

        // Purple glow behind the sprite during the rain-of-fire windup: the NPC's own frame drawn as
        // a pulsing radial halo, fading with the windup timer.
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (_glowTimer > 0)
            {
                Texture2D tex = TextureAssets.Npc[Type].Value;
                Rectangle src = NPC.frame;
                Vector2 origin = src.Size() / 2f;
                float fade = MathHelper.Clamp(_glowTimer / (float)RainWindupTicks, 0f, 1f);
                float pulse = 0.6f + 0.4f * (float)Math.Sin(Main.GameUpdateCount * 0.18f);
                Color glow = new Color(150, 40, 210) * (0.55f * fade * pulse);
                SpriteEffects fx = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                for (int i = 0; i < 6; i++)
                {
                    Vector2 off = new Vector2(6f, 0f).RotatedBy(MathHelper.TwoPi * i / 6f);
                    spriteBatch.Draw(tex, NPC.Center - Main.screenPosition + off, src, glow,
                        NPC.rotation, origin, NPC.scale, fx, 0f);
                }
            }
            return true;
        }
		public override void OnKill()
		{
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsTibianJuggernautGore1").Type, 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsTibianJuggernautGore2").Type, 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsTibianJuggernautGore3").Type, 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsTibianJuggernautGore3").Type, 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsTibianJuggernautGore3").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsTibianJuggernautGore3").Type, 1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsTibianJuggernautGore2").Type, 1f);
        }
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BlueTitanite>(), 1, 2, 3));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RedTitanite>(), 1, 2, 3));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<WhiteTitanite>(), 1, 2, 3));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CursedSoul>(), 1, 4, 6));
        }
    }
}
