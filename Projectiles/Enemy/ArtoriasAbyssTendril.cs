using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy
{
    // Abyss-corrupted cousin of BasiliskLeechTongue: reaches out, and on contact gently pulls the
    // target toward the owner before retracting, tagging them with Suppressed/Tired and a touch of
    // curse buildup instead of the basilisk's HP drain. Fully generic (works for any owner NPC, not
    // just Artorias).
    //
    // The shaft is drawn as three overlapping wavy strands (same technique as
    // BasiliskLeechTongue.DrawTendril) using AbyssTendrilChain.png (a copy of BasiliskTonque.png,
    // to be recolored black), plus a sparse black/white dust trail layered on top. The tip reuses
    // AbyssSucker.png (a copy of BasiliskSucker.png, recolored dark via GetAlpha) as a placeholder
    // until a dedicated sprite exists.
    class ArtoriasAbyssTendril : ModProjectile
    {
        const int StateFlying     = 0;
        const int StateYanking    = 1;
        const int StateRetracting = 2;

        const float MaxLength      = 550f;
        const int   MaxFlightTicks = 70;
        const int   YankTicks      = 24;
        const float YankPullSpeed  = 3.2f; // gentle - not a hard/fast yank
        const int   GrabPadding    = 16;

        int targetWho = -1;
        int launchTimer;
        int yankTimer;

        const int GrabDebuffTicks = 10 * 60;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/AbyssSucker";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 0f;
        }

        public override void AI()
        {
            if (!TryGetOwner(out NPC owner))
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 240;
            AnimateSucker();
            SpawnShaftDust(owner);

            switch ((int)Projectile.ai[1])
            {
                case StateYanking:
                    YankAI(owner);
                    break;
                case StateRetracting:
                    RetractAI(owner);
                    break;
                default:
                    FlyingAI(owner);
                    break;
            }
        }

        void AnimateSucker()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 6)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
        }

        void FlyingAI(NPC owner)
        {
            launchTimer++;

            if (TryAttachToPlayer())
                return;

            if (Main.rand.NextBool(2))
            {
                Color tint = Main.rand.NextBool() ? Color.Black : Color.White;
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    DustID.Smoke, -Projectile.velocity * 0.05f, 120, tint, 0.9f);
                d.noGravity = true;
            }

            if (launchTimer > MaxFlightTicks || Projectile.Distance(GetOriginPosition(owner)) > MaxLength)
                StartRetract();
        }

        void YankAI(NPC owner)
        {
            if (targetWho < 0 || targetWho >= Main.maxPlayers)
            {
                StartRetract();
                return;
            }

            Player player = Main.player[targetWho];
            if (!player.active || player.dead)
            {
                StartRetract();
                return;
            }

            Vector2 toOwner = owner.Center - player.Center;
            if (toOwner.LengthSquared() > 1f)
            {
                Vector2 pullDir = toOwner.SafeNormalize(Vector2.Zero);
                player.velocity = Vector2.Lerp(player.velocity, pullDir * YankPullSpeed, 0.12f);
            }

            // Keep the tendril visually stretched between the owner's hand and the target.
            Projectile.Center = Vector2.Lerp(GetOriginPosition(owner), player.Center, 0.85f);
            Projectile.velocity = Vector2.Zero;

            if (Main.rand.NextBool(3))
            {
                Color tint = Main.rand.NextBool() ? Color.Black : Color.White;
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    DustID.Smoke, Vector2.Zero, 120, tint, 1f);
                d.noGravity = true;
            }

            yankTimer++;
            if (yankTimer >= YankTicks)
                StartRetract();
        }

        void RetractAI(NPC owner)
        {
            Vector2 origin = GetOriginPosition(owner);
            Vector2 toOrigin = origin - Projectile.Center;
            if (toOrigin.Length() < 16f)
            {
                Projectile.Kill();
                return;
            }

            Projectile.velocity = Vector2.Lerp(Projectile.velocity, toOrigin.SafeNormalize(Vector2.Zero) * 16f, 0.3f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (!CanGrabPlayer)
                return;

            target.AddBuff(ModContent.BuffType<Suppressed>(), GrabDebuffTicks, false);
            target.AddBuff(ModContent.BuffType<Tired>(), GrabDebuffTicks, false);
            // Long duration (matches how CurseBuildup is applied elsewhere) so it stays active
            // between separate grabs - ReApply is what actually accumulates CurseLevel per hit.
            target.AddBuff(ModContent.BuffType<CurseBuildup>(), 300 * 60, false);

            AttachToPlayer(target);
        }

        public override bool CanHitPlayer(Player target)
        {
            return CanGrabPlayer;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!CanGrabPlayer)
                return false;

            Rectangle grabBox = Projectile.Hitbox;
            grabBox.Inflate(GrabPadding, GrabPadding);
            if (grabBox.Intersects(targetHitbox))
                return true;

            float collisionPoint = 0f;
            Vector2 previousCenter = Projectile.Center - Projectile.velocity;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                previousCenter, Projectile.Center, Projectile.width + GrabPadding * 2f, ref collisionPoint);
        }

        bool CanGrabPlayer => (int)Projectile.ai[1] == StateFlying;

        bool TryAttachToPlayer()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return false;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead && Colliding(Projectile.Hitbox, player.Hitbox) == true)
                {
                    AttachToPlayer(player);
                    return true;
                }
            }
            return false;
        }

        void AttachToPlayer(Player target)
        {
            targetWho = target.whoAmI;
            Projectile.ai[1] = StateYanking;
            Projectile.hostile = false; // stop re-damaging while the yank is in progress
            yankTimer = 0;
            Projectile.netUpdate = true;
            SoundEngine.PlaySound(SoundID.NPCHit13 with { Volume = 0.5f, Pitch = -0.3f }, Projectile.Center);
        }

        void StartRetract()
        {
            if ((int)Projectile.ai[1] == StateRetracting)
                return;

            Projectile.ai[1] = StateRetracting;
            Projectile.hostile = false;
            Projectile.netUpdate = true;
        }

        void SpawnShaftDust(NPC owner)
        {
            if (Main.dedServ)
                return;

            Vector2 origin = GetOriginPosition(owner);
            Vector2 tip = Projectile.Center;
            int segments = (int)MathHelper.Clamp(Vector2.Distance(origin, tip) / 20f, 1, 30);

            for (int i = 0; i <= segments; i++)
            {
                if (!Main.rand.NextBool(2))
                    continue;

                Vector2 pos = Vector2.Lerp(origin, tip, i / (float)segments);
                Color tint = Main.rand.NextBool() ? Color.Black : Color.White;
                Dust d = Dust.NewDustPerfect(pos, DustID.Smoke, Vector2.Zero, 130, tint, 0.75f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (TryGetOwner(out NPC owner))
            {
                DrawChain(GetOriginPosition(owner), Projectile.Center);
            }
            return true; // let vanilla's default draw still render the tip sprite/frame on top
        }

        // Three overlapping wavy strands, same technique as BasiliskLeechTongue.DrawTendril -
        // each offset a few pixels either side of the straight line and phase-shifted so they
        // undulate independently, reading as a single thicker, living chain rather than one line.
        void DrawChain(Vector2 start, Vector2 end)
        {
            Texture2D segmentTexture = ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/AbyssTendrilChain").Value;
            Vector2 direction = end - start;
            Vector2 normal = direction.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2);

            DrawStrand(segmentTexture, start, end, normal, -5f, 0.00f, Color.White);
            DrawStrand(segmentTexture, start, end, normal, 0f, 1.75f, Color.White);
            DrawStrand(segmentTexture, start, end, normal, 5f, 3.45f, Color.White);
        }

        void DrawStrand(Texture2D segmentTexture, Vector2 start, Vector2 end, Vector2 normal, float offset, float phase, Color color)
        {
            const float SegmentSpacing = 8f;
            float distance = Vector2.Distance(start, end);
            int segments = System.Math.Clamp((int)(distance / SegmentSpacing), 2, 90);
            Vector2 origin = segmentTexture.Size() / 2f;
            Vector2 previous = GetStrandPoint(start, end, normal, offset, phase, 0f);

            for (int i = 1; i <= segments; i++)
            {
                float progress = i / (float)segments;
                Vector2 point = GetStrandPoint(start, end, normal, offset, phase, progress);
                Vector2 tangent = point - previous;
                if (tangent.LengthSquared() > 0.01f)
                {
                    float rotation = tangent.ToRotation() + MathHelper.PiOver2;
                    float scale = 0.95f + 0.08f * (float)System.Math.Sin(progress * MathHelper.TwoPi + phase);
                    Main.EntitySpriteDraw(segmentTexture, point - Main.screenPosition, null, color * 0.95f, rotation, origin, scale, SpriteEffects.None, 0f);
                }
                previous = point;
            }
        }

        Vector2 GetStrandPoint(Vector2 start, Vector2 end, Vector2 normal, float offset, float phase, float progress)
        {
            float pulse = Main.GlobalTimeWrappedHourly * 11f + Projectile.identity * 0.09f + phase;
            float taper = (float)System.Math.Sin(progress * MathHelper.Pi);
            float wave = (float)System.Math.Sin(progress * MathHelper.TwoPi * 2.25f + pulse) * 5f * taper;
            return Vector2.Lerp(start, end, progress) + normal * (offset + wave);
        }

        static Vector2 GetOriginPosition(NPC npc)
        {
            int direction = npc.spriteDirection == 0 ? npc.direction : npc.spriteDirection;
            return npc.Center + new Vector2(direction * (npc.width * 0.5f + 10f), -npc.height * 0.3f);
        }

        bool TryGetOwner(out NPC owner)
        {
            int ownerIndex = (int)Projectile.ai[0];
            if (ownerIndex >= 0 && ownerIndex < Main.maxNPCs)
            {
                owner = Main.npc[ownerIndex];
                if (owner.active)
                    return true;
            }

            owner = null;
            return false;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            // Dark near-black tint on the (currently pink) placeholder sprite until a dedicated
            // black tendril-tip sprite replaces it.
            return new Color(35, 32, 40);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            if ((int)Projectile.ai[1] == StateFlying)
                behindNPCs.Add(index);
            else
                overPlayers.Add(index); // wrapped toward/pulling the target - draw in front
        }
    }
}
