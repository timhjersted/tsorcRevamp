using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Weapons;

namespace tsorcRevamp.NPCs.Puppets
{
    /// <summary>
    /// A short-lived, fire-tinted Owl Companion used only by Owl Father's spectral-form
    /// bombardment. Its committed dive and the impact explosion share one damage value, while the
    /// explosion owns the wider circular follow-through.
    /// </summary>
    public class OwlFireDiveCompanion : ModNPC
    {
        private const int FlightTicks = 90;
        private const int MaxDiveTicks = 100;
        private const float DiveSpeed = 12.8f; // 20% slower than the original 16px/tick dive
        private const int SpriteFrameSize = 40;
        private const int SpawnBurstDustCount = 50;
        private static readonly int[] FlyFrames = { 9, 10, 11, 12 };

        private Vector2 _spawnAnchor;
        private int _syncTimer;
        private bool _diveHootPlayed;

        public override string Texture => "tsorcRevamp/NPCs/Puppets/Owl";

        private int TargetPlayerIndex => (int)NPC.ai[0];
        private int Age => (int)NPC.ai[1];
        private bool IsDiving => Age >= FlightTicks;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 13;
        }

        public override void SetDefaults()
        {
            NPC.width = 32;
            NPC.height = 32;
            NPC.lifeMax = 1;
            NPC.dontTakeDamage = true;
            NPC.damage = 0;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.aiStyle = -1;
            NPC.knockBackResist = 0f;
            NPC.DeathSound = SoundID.NPCDeath2 with { Volume = 0.4f };
            NPC.value = 0f;
            NPC.npcSlots = 0f;
            NPC.lavaImmune = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            _spawnAnchor = NPC.Center;
            SpawnSummonBurst();
        }

        private void SpawnSummonBurst()
        {
            if (Main.dedServ)
                return;

            // A deliberately dense one-shot flash so every airborne summon reads before its
            // 90-tick patrol begins. GoldFlame supplies the yellow core and OrangeTorch gives the
            // outer burst its hotter orange edge.
            for (int i = 0; i < SpawnBurstDustCount; i++)
            {
                float angle = MathHelper.TwoPi * i / SpawnBurstDustCount + Main.rand.NextFloat(-0.08f, 0.08f);
                float speed = Main.rand.NextFloat(1.8f, 5.6f);
                Vector2 velocity = angle.ToRotationVector2() * speed + new Vector2(0f, -0.8f);
                Dust dust = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(7f, 7f),
                    i % 2 == 0 ? DustID.GoldFlame : DustID.OrangeTorch, velocity, 70, default,
                    Main.rand.NextFloat(0.75f, 1.3f));
                dust.noGravity = true;
            }
        }

        public override bool CheckActive() => false;

        public override void AI()
        {
            UpdateVisuals();
            SpawnFlightDust();

            // Only the server commits flight steering, tile contact, and the blast spawn. Clients
            // receive position/AI updates and merely animate the synced phase.
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (TargetPlayerIndex < 0 || TargetPlayerIndex >= Main.maxPlayers)
            {
                NPC.active = false;
                return;
            }

            Player target = Main.player[TargetPlayerIndex];
            if (!target.active || target.dead)
            {
                NPC.active = false;
                return;
            }

            if (Age < FlightTicks)
                TickFlight();
            else
                TickDive(target);

            NPC.ai[1]++;
            if (++_syncTimer >= 10 || Age == FlightTicks)
            {
                _syncTimer = 0;
                NPC.netUpdate = true;
            }
        }

        private void TickFlight()
        {
            NPC.noTileCollide = true;
            NPC.damage = 0;
            float phase = NPC.ai[2] * 0.74f + Age * 0.11f;
            Vector2 hoverPoint = _spawnAnchor + new Vector2(
                (float)Math.Sin(phase) * 52f,
                (float)Math.Cos(phase * 1.35f) * 22f);
            Vector2 desiredVelocity = (hoverPoint - NPC.Center) * 0.14f;
            NPC.velocity = Vector2.Lerp(NPC.velocity, desiredVelocity, 0.16f);
        }

        private void TickDive(Player target)
        {
            NPC.noTileCollide = false;
            NPC.damage = OwlFireDiveExplosion.ImpactDamage;
            int diveAge = Age - FlightTicks;
            Vector2 aimPoint = target.Center + target.velocity * MathHelper.Clamp(10f - diveAge * 0.14f, 0f, 10f);
            Vector2 desiredVelocity = (aimPoint - NPC.Center).SafeNormalize(Vector2.UnitY) * DiveSpeed;
            NPC.velocity = Vector2.Lerp(NPC.velocity, desiredVelocity, diveAge < 18 ? 0.16f : 0.07f);

            if (NPC.collideX || NPC.collideY || Collision.SolidCollision(NPC.position, NPC.width, NPC.height)
                || diveAge >= MaxDiveTicks)
            {
                Explode();
            }
        }

        private void Explode()
        {
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                ModContent.ProjectileType<OwlFireDiveExplosion>(), OwlFireDiveExplosion.ImpactDamage, 2.5f, Main.myPlayer);
            NPC.active = false;
            NPC.netUpdate = true;
        }

        private void UpdateVisuals()
        {
            if (IsDiving && !_diveHootPlayed)
            {
                _diveHootPlayed = true;
                if (!Main.dedServ)
                    SoundEngine.PlaySound(SoundID.Zombie112 with
                    {
                        Volume = 0.78f,
                        PitchVariance = 0.08f
                    }, NPC.Center);
            }

            if (Math.Abs(NPC.velocity.X) > 0.05f)
                NPC.direction = NPC.velocity.X < 0f ? -1 : 1;
            NPC.spriteDirection = -NPC.direction; // Owl.png faces screen-right.
            NPC.rotation = IsDiving
                ? NPC.velocity.ToRotation() + (NPC.direction < 0 ? MathHelper.Pi : 0f)
                : 0f;
            NPC.frame.Y = (IsDiving ? 8 : FlyFrames[(int)((Main.GameUpdateCount / 5 + (ulong)NPC.ai[2]) % 4)])
                * SpriteFrameSize;
        }

        private void SpawnFlightDust()
        {
            if (Main.dedServ)
                return;

            Lighting.AddLight(NPC.Center, 0.55f, 0.22f, 0.04f);
            if (!Main.rand.NextBool(IsDiving ? 2 : 4))
                return;

            Vector2 velocity = IsDiving
                ? -NPC.velocity * 0.08f + Main.rand.NextVector2Circular(0.8f, 0.8f)
                : new Vector2(Main.rand.NextFloat(-0.45f, 0.45f), Main.rand.NextFloat(-1.5f, -0.45f));
            Dust dust = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(8f, 8f),
                Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.Torch, velocity, 90, default,
                Main.rand.NextFloat(0.7f, 1.15f));
            dust.noGravity = true;
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => IsDiving;

        public override void OnHitPlayer(Player target, Player.HurtInfo hurt)
        {
            // Contact is the committed 18-damage dive. The fire blast immediately follows, but the
            // standard player immunity window prevents it from applying a second damage event to
            // this same contact while it can still catch anyone else inside its visible radius.
            target.AddBuff(BuffID.OnFire, 4 * 60);
            if (Main.netMode != NetmodeID.MultiplayerClient)
                Explode();
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TextureAssets.Npc[Type].Value;
            Rectangle source = new Rectangle(0, NPC.frame.Y, texture.Width, SpriteFrameSize);
            Vector2 origin = source.Size() * 0.5f;
            Vector2 drawPosition = NPC.Center - Main.screenPosition + new Vector2(0f, NPC.gfxOffY);
            SpriteEffects effects = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float orbit = Main.GlobalTimeWrappedHourly * 4f + NPC.ai[2] * 0.61f;

            // Three six-pixel copies produce the compact spectral fire halo requested for the
            // birds; the actual Owl sprite remains readable at half opacity in the center.
            for (int i = 0; i < 3; i++)
            {
                Vector2 offset = (orbit + MathHelper.TwoPi * i / 3f).ToRotationVector2() * 6f;
                Main.EntitySpriteDraw(texture, drawPosition + offset, source,
                    new Color(255, 178, 32) * 0.34f, NPC.rotation, origin, 1.02f, effects, 0);
            }
            Color core = Color.Lerp(drawColor, new Color(255, 190, 52), 0.68f) * 0.5f;
            Main.EntitySpriteDraw(texture, drawPosition, source, core, NPC.rotation, origin, 1f, effects, 0);
            return false;
        }
    }
}
