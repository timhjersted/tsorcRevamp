using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    internal class BasiliskLeechTongue : ModProjectile
    {
        public const float Gravity = 0.16f;

        private const int StateFlying = 0;
        private const int StateAttached = 1;
        private const int StateRetracting = 2;
        private const float MaxLength = 620f;
        private const int MaxFlightTicks = 105;
        private const int DrainInterval = 30;
        private const int HurtSoundInterval = 300;
        private const float HeadAttachOffset = 10f;
        private const float TongueSegmentSpacing = 8f;
        private const int GrabPadding = 18;

        private int targetWho = -1;
        private int attachLife;
        private int launchTimer;
        private int drainTimer;
        private int hurtSoundTimer;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/BasiliskSucker";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 99999999;
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 420;
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

            Projectile.timeLeft = 420;
            Projectile.rotation += 0.05f * Projectile.direction;
            AnimateSucker();

            switch ((int)Projectile.ai[1])
            {
                case StateAttached:
                    AttachedAI(owner);
                    break;
                case StateRetracting:
                    RetractAI(owner);
                    break;
                default:
                    FlyingAI(owner);
                    break;
            }
        }

        private void AnimateSucker()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 6)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
            }
        }

        private void FlyingAI(NPC owner)
        {
            launchTimer++;
            Projectile.velocity.Y += Gravity;

            if (TryAttachToPlayer())
            {
                return;
            }

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), DustID.PinkTorch, -Projectile.velocity * 0.08f, 100, Color.HotPink, 0.9f);
                dust.noGravity = true;
            }

            if (launchTimer > MaxFlightTicks || Projectile.Distance(GetMouthPosition(owner)) > MaxLength)
            {
                StartRetract();
            }
        }

        private void AttachedAI(NPC owner)
        {
            if (targetWho < 0 || targetWho >= Main.maxPlayers)
            {
                StartRetract();
                return;
            }

            Player player = Main.player[targetWho];
            if (!player.active || player.dead || owner.life <= 0 || attachLife - owner.life >= owner.lifeMax * 0.2f)
            {
                StartRetract();
                return;
            }

            Projectile.Center = GetPlayerHeadAnchor(player);
            Projectile.velocity = Vector2.Zero;
            player.AddBuff(BuffID.Confused, 60, false);

            hurtSoundTimer++;
            if (hurtSoundTimer >= HurtSoundInterval)
            {
                hurtSoundTimer = 0;
                PlayLeechHurtSound(player);
            }

            drainTimer++;
            if (drainTimer >= DrainInterval)
            {
                drainTimer = 0;
                DrainPlayer(owner, player);
            }

            if (Main.rand.NextBool(4))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(12f, 12f), DustID.PinkTorch, Main.rand.NextVector2Circular(1.2f, 1.2f), 80, Color.DeepPink, 1.1f);
                dust.noGravity = true;
            }
        }

        private void RetractAI(NPC owner)
        {
            Projectile.hostile = false;
            Projectile.tileCollide = false;

            if (TryAttachToPlayer())
            {
                return;
            }

            Vector2 mouth = GetMouthPosition(owner);
            Vector2 toMouth = mouth - Projectile.Center;
            if (toMouth.Length() < 14f)
            {
                Projectile.Kill();
                return;
            }

            Projectile.velocity = Vector2.Lerp(Projectile.velocity, toMouth.SafeNormalize(Vector2.Zero) * 18f, 0.28f);
        }

        private void DrainPlayer(NPC owner, Player player)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int drain = System.Math.Max(1, Projectile.damage);
            PlayerDeathReason deathReason = PlayerDeathReason.ByProjectile(-1, Projectile.whoAmI);
            player.statLife -= drain;

            if (Main.netMode != NetmodeID.Server)
            {
                CombatText.NewText(player.Hitbox, CombatText.DamagedFriendly, drain);
            }

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, player.whoAmI);
            }

            if (player.statLife <= 0)
            {
                player.KillMe(deathReason, drain, player.Center.X < owner.Center.X ? -1 : 1, false);
            }

            if (owner.active && owner.life > 0 && owner.life < owner.lifeMax)
            {
                int heal = System.Math.Min(drain, owner.lifeMax - owner.life);
                owner.life += heal;
                owner.HealEffect(heal);
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, owner.whoAmI);
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (!CanGrabPlayer)
            {
                return;
            }

            AttachToPlayer(target);
        }

        public override bool CanHitPlayer(Player target)
        {
            return CanGrabPlayer;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!CanGrabPlayer)
            {
                return false;
            }

            Rectangle grabBox = Projectile.Hitbox;
            grabBox.Inflate(GrabPadding, GrabPadding);
            if (grabBox.Intersects(targetHitbox))
            {
                return true;
            }

            float collisionPoint = 0f;
            Vector2 previousCenter = Projectile.Center - Projectile.velocity;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), previousCenter, Projectile.Center, Projectile.width + GrabPadding * 2f, ref collisionPoint);
        }

        private bool CanGrabPlayer => (int)Projectile.ai[1] == StateFlying || (int)Projectile.ai[1] == StateRetracting;

        private bool TryAttachToPlayer()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return false;
            }

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

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            if ((int)Projectile.ai[1] == StateAttached)
            {
                overPlayers.Add(index);
            }
        }

        private void AttachToPlayer(Player target)
        {
            targetWho = target.whoAmI;
            if (TryGetOwner(out NPC owner))
            {
                attachLife = owner.life;
            }

            Projectile.ai[1] = StateAttached;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.velocity = Vector2.Zero;
            Projectile.Center = GetPlayerHeadAnchor(target);
            drainTimer = 0;
            hurtSoundTimer = HurtSoundInterval - 1;
            Projectile.netUpdate = true;
            SoundEngine.PlaySound(SoundID.NPCHit13 with { Volume = 0.55f, Pitch = 0.25f }, Projectile.Center);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            StartRetract();
            return false;
        }

        private void StartRetract()
        {
            if ((int)Projectile.ai[1] == StateRetracting)
            {
                return;
            }

            Projectile.ai[1] = StateRetracting;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.netUpdate = true;
            SoundEngine.PlaySound(SoundID.NPCHit8 with { Volume = 0.35f, Pitch = -0.25f }, Projectile.Center);
        }

        private bool TryGetOwner(out NPC owner)
        {
            int ownerIndex = (int)Projectile.ai[0];
            if (ownerIndex >= 0 && ownerIndex < Main.maxNPCs)
            {
                owner = Main.npc[ownerIndex];
                if (owner.active)
                {
                    return true;
                }
            }

            owner = null;
            return false;
        }

        public static Vector2 GetMouthPosition(NPC npc)
        {
            int direction = npc.spriteDirection == 0 ? npc.direction : npc.spriteDirection;
            return npc.Center + new Vector2(direction * npc.width * 0.38f, -npc.height * 0.22f);
        }

        private static Vector2 GetPlayerHeadAnchor(Player player)
        {
            return player.Top + new Vector2(player.width * 0.5f, HeadAttachOffset);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (TryGetOwner(out NPC owner))
            {
                DrawTongue(GetMouthPosition(owner), Projectile.Center);
            }

            return true;
        }

        private void DrawTongue(Vector2 start, Vector2 end)
        {
            Texture2D segmentTexture = ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/BasiliskTonque").Value;
            Vector2 direction = end - start;
            Vector2 normal = direction.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2);

            DrawTendril(segmentTexture, start, end, normal, -5f, 0.00f, Color.DeepPink);
            DrawTendril(segmentTexture, start, end, normal, 0f, 1.75f, Color.HotPink);
            DrawTendril(segmentTexture, start, end, normal, 5f, 3.45f, Color.LightPink);
        }

        private void DrawTendril(Texture2D segmentTexture, Vector2 start, Vector2 end, Vector2 normal, float offset, float phase, Color color)
        {
            float distance = Vector2.Distance(start, end);
            int segments = System.Math.Clamp((int)(distance / TongueSegmentSpacing), 2, 90);
            Vector2 origin = segmentTexture.Size() / 2f;
            Vector2 previous = GetStringPoint(start, end, normal, offset, phase, 0f);

            for (int i = 1; i <= segments; i++)
            {
                float progress = i / (float)segments;
                Vector2 point = GetStringPoint(start, end, normal, offset, phase, progress);
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

        private Vector2 GetStringPoint(Vector2 start, Vector2 end, Vector2 normal, float offset, float phase, float progress)
        {
            float pulse = Main.GlobalTimeWrappedHourly * 11f + Projectile.identity * 0.09f + phase;
            float taper = (float)System.Math.Sin(progress * MathHelper.Pi);
            float wave = (float)System.Math.Sin(progress * MathHelper.TwoPi * 2.25f + pulse) * 5f * taper;
            return Vector2.Lerp(start, end, progress) + normal * (offset + wave);
        }

        private void PlayLeechHurtSound(Player player)
        {
            if (player.whoAmI != Main.myPlayer)
            {
                return;
            }

            int voiceIndex = Main.rand.Next(1, 4);
            float pitchOffset = Main.rand.Next(-1, 2) * 0.08f;

            if (player.Male)
            {
                SoundEngine.PlaySound(new SoundStyle($"tsorcRevamp/Sounds/DarkSouls/Voices/Male/m-hurt-{voiceIndex}") with { Volume = 0.5f, Pitch = pitchOffset });
            }
            else
            {
                SoundEngine.PlaySound(new SoundStyle($"tsorcRevamp/Sounds/DarkSouls/Voices/Female/f-hurt-{voiceIndex}") with { Volume = 0.5f, Pitch = pitchOffset });
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(targetWho);
            writer.Write(attachLife);
            writer.Write(launchTimer);
            writer.Write(drainTimer);
            writer.Write(hurtSoundTimer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            targetWho = reader.ReadInt32();
            attachLife = reader.ReadInt32();
            launchTimer = reader.ReadInt32();
            drainTimer = reader.ReadInt32();
            hurtSoundTimer = reader.ReadInt32();
        }
    }
}
