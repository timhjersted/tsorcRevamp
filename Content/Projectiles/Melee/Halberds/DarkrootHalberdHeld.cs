using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Melee.Halberds;

namespace tsorcRevamp.Content.Projectiles.Melee.Halberds
{
    public class DarkrootHalberdHeld : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Content/Items/Weapons/Melee/Halberds/DarkrootHalberd";
        int Step => (int)Projectile.ai[0];
        int Age => (int)Projectile.ai[1];
        int Duration => Math.Max(1, (int)Projectile.ai[2]);
        bool Owner => Projectile.owner == Main.myPlayer;
        int facing, ordinal;
        float entry = DarkrootHalberdMotion.Carry;
        bool buffered, completed, geometryReady, previousLive, playedSwing, pushing;
        Vector2 hand, point, neck, edge, oldPoint, oldNeck, oldEdge;
        float worldAngle, extension;
        readonly Vector2[] trail = new Vector2[8];
        int trailCount;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 300;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.ownerHitCheck = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 120;
        }

        public override bool ShouldUpdatePosition() => false;
        float MotionTime => DarkrootHalberdMotion.Time(Age, Duration, Step);
        public override bool? CanDamage() => geometryReady && previousLive ? null : false;
        public override bool? CanHitNPC(NPC target) => target.friendly ? false : null;

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead || player.noItems || player.CCed
                || player.HeldItem.type != ModContent.ItemType<DarkrootHalberd>()
                || player.GetModPlayer<tsorcRevampPlayer>().isDodging
                || player.GetModPlayer<tsorcRevampPlayer>().isSwordflipping)
            { Projectile.Kill(); return; }

            if (Age >= Duration && Owner && !AdvanceState(player)) return;
            Projectile.timeLeft = 120;
            int age = Age;
            Projectile.Center = player.MountedCenter;
            if (facing == 0) facing = Projectile.velocity.X < 0 ? -1 : 1;
            if (Owner && Step <= 2 && Age < Duration * DarkrootHalberdMotion.Windup(Step) / DarkrootHalberdMotion.BaseDuration(Step) - 8)
            {
                Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * facing);
                if (Vector2.DistanceSquared(Projectile.velocity, aim) > 0.0001f)
                { Projectile.velocity = aim; facing = aim.X < 0 ? -1 : 1; Projectile.netUpdate = true; }
            }
            player.ChangeDir(facing);
            player.heldProj = Projectile.whoAmI;
            player.itemTime = player.itemAnimation = 2;
            float time = MotionTime;
            float poseTime = DarkrootHalberdMotion.Time(age + 1, Duration, Step);
            float angle = DarkrootHalberdMotion.Angle(Step, poseTime, entry);
            worldAngle = Projectile.velocity.ToRotation() + facing * angle;
            extension = DarkrootHalberdMotion.Extension(Step, poseTime);
            float armAngle = worldAngle - MathHelper.PiOver2;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armAngle);
            hand = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, armAngle);
            // Both hands stay on a reachable point of the supplied wooden shaft, including its rear-facing sweep.
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full,
                DarkrootHalberdMotion.RearArm(hand - player.MountedCenter, worldAngle, facing, extension, out _));

            oldPoint = point; oldNeck = neck; oldEdge = edge;
            point = hand + DarkrootHalberdMotion.Offset(DarkrootHalberdMotion.Point, worldAngle, facing, extension);
            neck = hand + DarkrootHalberdMotion.Offset(DarkrootHalberdMotion.Neck, worldAngle, facing, extension);
            edge = hand + DarkrootHalberdMotion.Offset(DarkrootHalberdMotion.Edge, worldAngle, facing, extension);
            bool live = DarkrootHalberdMotion.Live(Step, time);
            if (!geometryReady || Vector2.DistanceSquared(oldPoint, point) > 128 * 128)
            { oldPoint = point; oldNeck = neck; oldEdge = edge; }
            geometryReady = true;
            previousLive = live;
            if (live)
            {
                if (!playedSwing && !Main.dedServ) { SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.3f, Volume = 0.8f }, hand); playedSwing = true; }
                for (int i = trail.Length - 1; i > 0; i--) trail[i] = trail[i - 1];
                trail[0] = point; trailCount = Math.Min(trail.Length, trailCount + 1);
            }
            else trailCount = Math.Max(0, trailCount - 1);

            float push = Step == 2 ? DarkrootHalberdMotion.PushDistance(time, poseTime) : 0;
            if (Owner && push > 0)
            {
                // Prefix/speed changes preserve the total 32px impulse, and vanilla tile movement still stops it.
                Vector2 safe = Collision.TileCollision(player.position, new Vector2(facing * push, 0), player.width, player.height);
                player.velocity.X = safe.X;
                pushing = true;
            }
            else if (Owner && pushing) { player.velocity.X = 0; pushing = false; }
            if (Step == DarkrootHalberdMotion.Spin)
                player.velocity.X = MathHelper.Clamp(player.velocity.X, -player.maxRunSpeed * 0.4f, player.maxRunSpeed * 0.4f);
            if (Owner && Step <= 2 && Age >= Duration - 12 && player.controlUseItem) buffered = true;

            if (Age < Duration) Projectile.ai[1]++;
        }

        bool AdvanceState(Player player)
        {
            float angle = DarkrootHalberdMotion.Angle(Step, DarkrootHalberdMotion.BaseDuration(Step), entry);
            if (Step == DarkrootHalberdMotion.SpinWindup)
            { Begin(player, DarkrootHalberdMotion.Spin, angle); return true; }
            if (Step == DarkrootHalberdMotion.Spin)
            {
                if (player.controlUseTile && DarkrootHalberd.TryPay(player, DarkrootHalberdMotion.Spin))
                    Begin(player, DarkrootHalberdMotion.Spin, MathHelper.WrapAngle(angle));
                else Begin(player, DarkrootHalberdMotion.SpinRecovery, MathHelper.WrapAngle(angle));
                return true;
            }
            if (Step == DarkrootHalberdMotion.SpinRecovery) { completed = true; Projectile.Kill(); return false; }
            var combo = player.GetModPlayer<DarkrootHalberdPlayer>();
            combo.Complete(Step);
            if ((buffered || player.controlUseItem) && DarkrootHalberd.TryPay(player, combo.NextStep))
            { Begin(player, combo.NextStep, angle); return true; }
            completed = true; Projectile.Kill(); return false;
        }

        void Begin(Player player, int step, float from)
        {
            Projectile.ai[0] = step;
            Projectile.ai[1] = 0;
            Projectile.ai[2] = DarkrootHalberd.Duration(player, step);
            entry = from;
            ordinal++;
            buffered = playedSwing = previousLive = false;
            trailCount = 0;
            Array.Clear(Projectile.localNPCImmunity, 0, Projectile.localNPCImmunity.Length);
            Projectile.netUpdate = true;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
            => modifiers.SourceDamage *= DarkrootHalberdMotion.DamageMultiplier(Step);

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!geometryReady) return false;
            float collisionPoint = 0;
            for (int i = 0; i <= 8; i++)
            {
                float fraction = i / 8f;
                Vector2 a = Vector2.Lerp(oldNeck, neck, fraction);
                Vector2 b = Vector2.Lerp(oldPoint, point, fraction);
                Vector2 c = Vector2.Lerp(oldEdge, edge, fraction);
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), a, b, 10, ref collisionPoint)
                    || Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), a, c, 12, ref collisionPoint)) return true;
            }
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (!Owner) return;
            Player player = Main.player[Projectile.owner];
            if (!completed || Step >= DarkrootHalberdMotion.SpinWindup) player.GetModPlayer<DarkrootHalberdPlayer>().ResetCombo();
            player.itemTime = player.itemAnimation = 0;
        }

        public override void SendExtraAI(BinaryWriter writer)
        { writer.Write(facing); writer.Write(entry); writer.Write(ordinal); }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            facing = reader.ReadInt32(); entry = reader.ReadSingle(); int incoming = reader.ReadInt32();
            if (incoming != ordinal)
            {
                ordinal = incoming; playedSwing = previousLive = geometryReady = false; trailCount = 0;
                Array.Clear(Projectile.localNPCImmunity, 0, Projectile.localNPCImmunity.Length);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!geometryReady) return false;
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            for (int i = 1; i < trailCount; i++)
                Utils.DrawLine(Main.spriteBatch, trail[i - 1], trail[i], new Color(90, 150, 105) * (0.22f * (1 - i / (float)trailCount)), new Color(90, 150, 105) * 0.05f, 3);
            Vector2 grip = DarkrootHalberdMotion.GripAt(extension);
            SpriteEffects flip = SpriteEffects.None;
            float native = DarkrootHalberdMotion.NativeAngle;
            if (facing < 0) { flip = SpriteEffects.FlipVertically; grip.Y = texture.Height - grip.Y; native = -native; }
            Main.EntitySpriteDraw(texture, hand - Main.screenPosition, null, lightColor,
                worldAngle - native, grip, DarkrootHalberdMotion.DrawScale, flip, 0);
            return false;
        }
    }
}
