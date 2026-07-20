using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    public interface IPuppetSnareSource
    {
        void NotifySnareCaught(int playerIndex);
    }

    public class PuppetSnareBolt : ModProjectile
    {
        private const int FlightWebCount = 8;
        private const float FlightWebRadius = 24f;

        public override string Texture => "tsorcRevamp/Projectiles/Ranged/Ammo/BoltProjectile";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 20;
            Projectile.scale = 1.1f;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 300;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Ranged;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.velocity.Y += 0.02f;
            Projectile.velocity.Y = Math.Min(Projectile.velocity.Y, 16f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox)
        {
            Vector2[] nodes = GetFlightWebNodes();

            // Each visible cobweb is independently dangerous.
            const int nodeCollisionSize = 10;
            for (int i = 0; i < FlightWebCount; i++)
            {
                Rectangle nodeHitbox = new Rectangle(
                    (int)nodes[i].X - nodeCollisionSize / 2,
                    (int)nodes[i].Y - nodeCollisionSize / 2,
                    nodeCollisionSize,
                    nodeCollisionSize);
                if (nodeHitbox.Intersects(targetHitbox))
                    return true;
            }

            // Then cover the one-pixel threads with a small collision tolerance. This follows the
            // complete C silhouette while leaving its backward-facing opening genuinely open.
            Vector2 targetPosition = targetHitbox.TopLeft();
            Vector2 targetSize = targetHitbox.Size();
            for (int i = 0; i < FlightWebCount - 1; i++)
            {
                float collisionPoint = 0f;
                if (Collision.CheckAABBvLineCollision(
                    targetPosition,
                    targetSize,
                    nodes[i],
                    nodes[i + 1],
                    4f,
                    ref collisionPoint))
                {
                    return true;
                }
            }

            // Preserve the original bolt-center collision as a final fallback during the first few
            // deployment ticks, before the C has expanded to its full visual height.
            return projectileHitbox.Intersects(targetHitbox);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D lineTexture = TextureAssets.MagicPixel.Value;
            Texture2D webTexture = TextureAssets.Tile[TileID.Cobweb].Value;
            Rectangle webSource = new Rectangle(0, 0, 16, 16);

            Vector2[] nodes = GetFlightWebNodes();

            for (int i = 0; i < FlightWebCount - 1; i++)
            {
                PuppetSnareNet.DrawPixelLine(
                    lineTexture,
                    nodes[i],
                    nodes[i + 1],
                    Color.Black * 0.78f);
            }

            for (int i = 0; i < FlightWebCount; i++)
            {
                Color color = Lighting.GetColor(nodes[i].ToTileCoordinates());
                Main.EntitySpriteDraw(
                    webTexture,
                    nodes[i] - Main.screenPosition,
                    webSource,
                    color * 0.9f,
                    0f,
                    new Vector2(8f),
                    0.58f,
                    SpriteEffects.None);
            }

            // Let tModLoader draw the crossbow bolt itself over the deployed web payload.
            return true;
        }

        private Vector2[] GetFlightWebNodes()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 perpendicular = new Vector2(-forward.Y, forward.X);
            float deploy = MathHelper.SmoothStep(
                0.25f,
                1f,
                MathHelper.Clamp(Projectile.localAI[0] / 8f, 0f, 1f));
            float radius = FlightWebRadius * deploy;
            Vector2[] nodes = new Vector2[FlightWebCount];

            // Parabolic C: the belly leads with the bolt while both tips trail backward, away from
            // the target. This keeps the dangerous direction readable even as the bolt arcs.
            for (int i = 0; i < FlightWebCount; i++)
            {
                float t = -1f + 2f * i / (FlightWebCount - 1f);
                float forwardOffset = radius * (1f - 2f * t * t);
                float sideOffset = radius * t;
                nodes[i] = Projectile.Center
                    + forward * forwardOffset
                    + perpendicular * sideOffset;
            }
            return nodes;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            SpawnNet(target.whoAmI + 1);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SpawnNet(0);
            Projectile.Kill();
            return false;
        }

        private void SpawnNet(int encodedTarget)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<PuppetSnareNet>(),
                0,
                0f,
                Main.myPlayer,
                Projectile.ai[0],
                encodedTarget);
            SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.55f, Pitch = 0.35f }, Projectile.Center);
        }
    }

    /// <summary>Eight virtual cobweb nodes controlled by one projectile. ai[0] stores the source NPC;
    /// ai[1] stores target player + 1, or zero while the net is spread across the ground.</summary>
    public class PuppetSnareNet : ModProjectile
    {
        private const int NodeCount = 8;
        private const int AttachedLifetime = 3 * 60;
        private const int GroundLifetime = 4 * 60;
        private const int NodeSize = 18;

        private static readonly Vector2[] GroundOffsets =
        {
            new Vector2(-70f, 0f), new Vector2(-50f, -2f),
            new Vector2(-30f, 0f), new Vector2(-10f, -3f),
            new Vector2(10f, -3f), new Vector2(30f, 0f),
            new Vector2(50f, -2f), new Vector2(70f, 0f),
        };

        private static readonly Vector2[] WrappedOffsets =
        {
            new Vector2(-18f, -24f), new Vector2(0f, -29f),
            new Vector2(18f, -22f), new Vector2(22f, 0f),
            new Vector2(17f, 22f), new Vector2(0f, 28f),
            new Vector2(-18f, 22f), new Vector2(-22f, 0f),
        };

        private byte _aliveMask;
        private readonly float[] _nodeFade = new float[NodeCount];
        private bool _notifiedSource;
        private ulong _nextCutTick;

        public override string Texture => "Terraria/Images/MagicPixel";

        private bool Attached => Projectile.ai[1] > 0f;
        private int TargetPlayerIndex => (int)Projectile.ai[1] - 1;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = GroundLifetime;
            Projectile.netImportant = true;
            _aliveMask = byte.MaxValue;
            for (int i = 0; i < NodeCount; i++)
                _nodeFade[i] = 1f;
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (Attached)
                Projectile.timeLeft = AttachedLifetime;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(_aliveMask);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            byte receivedMask = reader.ReadByte();
            for (int i = 0; i < NodeCount; i++)
            {
                bool wasAlive = IsNodeAlive(i);
                bool nowAlive = (receivedMask & (1 << i)) != 0;
                if (wasAlive && !nowAlive)
                    _nodeFade[i] = 1f;
            }
            _aliveMask = receivedMask;
        }

        public override void AI()
        {
            UpdateNodeFades();
            if (Projectile.timeLeft <= 15 && _aliveMask != 0)
            {
                _aliveMask = 0;
                Projectile.netUpdate = true;
            }
            if (_aliveMask == 0)
            {
                Projectile.velocity = Vector2.Zero;
                return;
            }

            if (Attached)
            {
                int targetIndex = TargetPlayerIndex;
                if (targetIndex < 0 || targetIndex >= Main.maxPlayers)
                {
                    Projectile.Kill();
                    return;
                }

                Player target = Main.player[targetIndex];
                if (!target.active || target.dead)
                {
                    Projectile.Kill();
                    return;
                }

                Projectile.Center = target.Center;
                Projectile.velocity = Vector2.Zero;
                target.AddBuff(ModContent.BuffType<PuppetSnared>(), 2);
                NotifySource(targetIndex);
                return;
            }

            Projectile.velocity = Vector2.Zero;
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int playerIndex = 0; playerIndex < Main.maxPlayers; playerIndex++)
            {
                Player player = Main.player[playerIndex];
                if (!player.active || player.dead || !IntersectsLivingNode(player.getRect()))
                    continue;

                Projectile.ai[1] = playerIndex + 1;
                Projectile.Center = player.Center;
                Projectile.timeLeft = AttachedLifetime;
                Projectile.netUpdate = true;
                NotifySource(playerIndex);
                break;
            }
        }

        private void NotifySource(int playerIndex)
        {
            if (_notifiedSource || Main.netMode == NetmodeID.MultiplayerClient)
                return;
            _notifiedSource = true;

            int npcIndex = (int)Projectile.ai[0];
            if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
                return;
            NPC source = Main.npc[npcIndex];
            if (source.active && source.ModNPC is IPuppetSnareSource snareSource)
                snareSource.NotifySnareCaught(playerIndex);
        }

        internal bool TryCutNodes(Rectangle hitbox, Player player)
        {
            if (_aliveMask == 0 || Main.GameUpdateCount < _nextCutTick)
                return false;
            if (Attached && TargetPlayerIndex != player.whoAmI)
                return false;

            Rectangle expandedHitbox = hitbox;
            expandedHitbox.Inflate(8, 8);
            int cutCount = 0;
            for (int i = 0; i < NodeCount && cutCount < 2; i++)
            {
                if (!IsNodeAlive(i) || !NodeRectangle(i).Intersects(expandedHitbox))
                    continue;
                KillNode(i);
                cutCount++;
            }

            // A wrapped player should always be able to cut their way out even if a particular
            // weapon's first-frame hitbox points just beyond the body.
            if (cutCount == 0 && Attached)
            {
                int nearest = FindNearestLivingNode(hitbox.Center.ToVector2());
                if (nearest >= 0)
                {
                    KillNode(nearest);
                    cutCount = 1;
                }
            }

            if (cutCount <= 0)
                return false;

            _nextCutTick = Main.GameUpdateCount + 10;
            Projectile.netUpdate = true;
            SoundEngine.PlaySound(SoundID.Grass with { Volume = 0.55f, PitchVariance = 0.2f }, player.Center);
            if (_aliveMask == 0)
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, 15);
            return true;
        }

        private void KillNode(int index)
        {
            _aliveMask = (byte)(_aliveMask & ~(1 << index));
            _nodeFade[index] = 1f;
            if (!Main.dedServ)
            {
                Vector2 position = GetNodeWorldPosition(index);
                for (int i = 0; i < 5; i++)
                {
                    Dust dust = Dust.NewDustPerfect(
                        position,
                        DustID.Web,
                        Main.rand.NextVector2Circular(1.5f, 1.5f),
                        120,
                        default,
                        Main.rand.NextFloat(0.7f, 1.1f));
                    dust.noGravity = true;
                }
            }
        }

        private int FindNearestLivingNode(Vector2 point)
        {
            int nearest = -1;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < NodeCount; i++)
            {
                if (!IsNodeAlive(i))
                    continue;
                float distance = Vector2.DistanceSquared(point, GetNodeWorldPosition(i));
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = i;
                }
            }
            return nearest;
        }

        private bool IntersectsLivingNode(Rectangle rectangle)
        {
            for (int i = 0; i < NodeCount; i++)
            {
                if (IsNodeAlive(i) && NodeRectangle(i).Intersects(rectangle))
                    return true;
            }
            return false;
        }

        private Rectangle NodeRectangle(int index)
        {
            Vector2 center = GetNodeWorldPosition(index);
            return new Rectangle(
                (int)center.X - NodeSize / 2,
                (int)center.Y - NodeSize / 2,
                NodeSize,
                NodeSize);
        }

        private Vector2 GetNodeWorldPosition(int index)
        {
            if (Attached)
                return Projectile.Center + WrappedOffsets[index];

            Vector2 point = Projectile.Center + GroundOffsets[index];
            point.Y = PuppetGroundDustWave.FindGroundY(point.X, point.Y) - 8f;
            return point;
        }

        private bool IsNodeAlive(int index) => (_aliveMask & (1 << index)) != 0;

        private void UpdateNodeFades()
        {
            for (int i = 0; i < NodeCount; i++)
            {
                if (IsNodeAlive(i))
                    _nodeFade[i] = Math.Min(1f, _nodeFade[i] + 0.15f);
                else
                    _nodeFade[i] = Math.Max(0f, _nodeFade[i] - 0.08f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D lineTexture = TextureAssets.MagicPixel.Value;
            Texture2D webTexture = TextureAssets.Tile[TileID.Cobweb].Value;
            Rectangle webSource = new Rectangle(0, 0, 16, 16);
            Vector2[] positions = new Vector2[NodeCount];
            for (int i = 0; i < NodeCount; i++)
                positions[i] = GetNodeWorldPosition(i);

            int segmentCount = Attached ? NodeCount : NodeCount - 1;
            for (int i = 0; i < segmentCount; i++)
            {
                int next = (i + 1) % NodeCount;
                float opacity = Math.Min(_nodeFade[i], _nodeFade[next]) * 0.72f;
                if (opacity > 0f)
                    DrawPixelLine(lineTexture, positions[i], positions[next], Color.Black * opacity);
            }

            for (int i = 0; i < NodeCount; i++)
            {
                float opacity = _nodeFade[i];
                if (opacity <= 0f)
                    continue;
                Color color = Lighting.GetColor((int)(positions[i].X / 16f), (int)(positions[i].Y / 16f));
                Main.EntitySpriteDraw(
                    webTexture,
                    positions[i] - Main.screenPosition,
                    webSource,
                    color * opacity,
                    0f,
                    new Vector2(8f),
                    0.9f,
                    SpriteEffects.None);
            }
            return false;
        }

        internal static void DrawPixelLine(Texture2D texture, Vector2 start, Vector2 end, Color color)
        {
            Vector2 delta = end - start;
            float length = delta.Length();
            if (length <= 0.01f)
                return;
            Main.EntitySpriteDraw(
                texture,
                start - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                color,
                delta.ToRotation(),
                new Vector2(0f, 0.5f),
                new Vector2(length, 1f),
                SpriteEffects.None);
        }
    }
}
