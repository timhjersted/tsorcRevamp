using Microsoft.Xna.Framework;
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
    // The shaft and directional grab tip are shader-built from moving black, violet, and pale
    // filaments. Sparse dust only breaks up their edges; it no longer carries the whole effect.
    class ArtoriasAbyssTendril : ModProjectile
    {
        const int StateFlying     = 0;
        const int StateYanking    = 1;
        const int StateRetracting = 2;

        const float MaxLength      = 550f;
        const int   MaxFlightTicks = 70;
        const int   YankTicks      = 24;
        const float YankPullSpeed  = 7.5f;
        const float ReleaseHorizontalSpeed = 15f;
        const float ReleaseVerticalSpeed = -10f;
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
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 1.1f);

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
                bool bright = Main.rand.NextBool(6);
                int type = bright ? DustID.SilverFlame
                    : Main.rand.NextBool(3) ? DustID.ShadowbeamStaff : DustID.Smoke;
                Color tint = bright ? new Color(224, 216, 248)
                    : type == DustID.Smoke ? new Color(10, 7, 18) : new Color(102, 38, 164);
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    type, -Projectile.velocity * 0.05f, 120, tint,
                    Main.rand.NextFloat(0.68f, 0.94f));
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

            // Keep the three-pronged tip visibly embedded at the target throughout the grab.
            Projectile.Center = player.Center;
            Projectile.velocity = Vector2.Zero;

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                bool bright = Main.rand.NextBool(7);
                int type = bright ? DustID.SilverFlame
                    : Main.rand.NextBool(3) ? DustID.ShadowbeamStaff : DustID.Smoke;
                Color tint = bright ? new Color(226, 220, 252)
                    : type == DustID.Smoke ? new Color(8, 6, 14) : new Color(110, 38, 174);
                Vector2 stabVelocity = (player.Center - owner.Center).SafeNormalize(Vector2.UnitX)
                    .RotatedByRandom(0.75f) * Main.rand.NextFloat(1.2f, 3.4f);
                Dust d = Dust.NewDustPerfect(player.Center + Main.rand.NextVector2Circular(7f, 9f),
                    type, stabVelocity, 120, tint, 0.92f);
                d.noGravity = true;
            }

            yankTimer++;
            if (yankTimer >= YankTicks)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float outward = player.Center.X < owner.Center.X ? -1f : 1f;
                    player.velocity = new Vector2(outward * ReleaseHorizontalSpeed, ReleaseVerticalSpeed);
                }
                StartRetract();
            }
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
            int segments = (int)MathHelper.Clamp(Vector2.Distance(origin, tip) / 48f, 1, 10);

            for (int i = 0; i <= segments; i++)
            {
                if (!Main.rand.NextBool(7))
                    continue;

                Vector2 pos = Vector2.Lerp(origin, tip, i / (float)segments);
                bool bright = Main.rand.NextBool(9);
                int type = bright ? DustID.SilverFlame
                    : Main.rand.NextBool(4) ? DustID.ShadowbeamStaff : DustID.Smoke;
                Color tint = bright ? new Color(230, 224, 255)
                    : type == DustID.Smoke ? new Color(7, 5, 13) : new Color(94, 30, 154);
                Dust d = Dust.NewDustPerfect(pos + Main.rand.NextVector2Circular(3f, 3f),
                    type, Main.rand.NextVector2Circular(0.35f, 0.35f), 130, tint, 0.72f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (TryGetOwner(out NPC owner))
            {
                int state = (int)Projectile.ai[1];
                float tension = state == StateYanking
                    ? 1f
                    : state == StateFlying
                        ? MathHelper.Clamp(launchTimer / (float)MaxFlightTicks, 0f, 0.72f)
                        : 0.28f;
                ArtoriasVFX.DrawTendril(GetOriginPosition(owner), Projectile.Center,
                    tension, state == StateRetracting ? 0.48f : 0.88f,
                    state == StateFlying || state == StateYanking);
            }
            return false;
        }

        static Vector2 GetOriginPosition(NPC npc)
        {
            if (npc.ModNPC is global::tsorcRevamp.NPCs.Bosses.SuperHardMode.Artorias artorias)
                return artorias.TendrilHandPosition;

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
