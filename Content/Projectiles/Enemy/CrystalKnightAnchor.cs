using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>Damaging flying core -> embedded or floating 120t charge -> five outward shards -> 15t withdrawal.</summary>
    public class CrystalKnightAnchor : ModProjectile
    {
        // CrystalCore: 92x102 radial star at 0.42 scale; player contact uses its 38x43 draw area.
        public override string Texture => "tsorcRevamp/Content/Projectiles/Enemy/Crystal/CrystalCore";
        private const byte Warning = 0, Flight = 1, Anchored = 2, Withdrawal = 3;
        private const float CoreScale = 0.42f;
        private const float CoreHeight = 102f * CoreScale;
        private const float EmbedDepth = CoreHeight * 0.25f;
        private byte _phase;
        private bool _burstPlayed;
        private bool _airborne;
        private int _targetIndex = -1;
        private float _settledRotation;
        private Vector2 _surface, _normal;
        private int _warningTotal = 30;
        private Vector2 Endpoint => _airborne ? _surface : _surface + _normal * (CoreHeight * 0.5f - EmbedDepth);
        private bool Authority => Main.netMode != NetmodeID.MultiplayerClient;
        public bool IsWaitingToFire => _phase == Warning;

        public static void Spawn(NPC owner, Vector2 origin, Vector2 surface, Vector2 normal, int damage, int warning, int sequence,
            bool airborne = false, int targetIndex = -1)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            int index = Projectile.NewProjectile(owner.GetSource_FromAI(), origin, Vector2.Zero,
                ModContent.ProjectileType<CrystalKnightAnchor>(), damage, 2f, Main.myPlayer, owner.whoAmI, warning, sequence);
            if (index >= Main.maxProjectiles) return;
            var anchor = (CrystalKnightAnchor)Main.projectile[index].ModProjectile;
            anchor._surface = surface; anchor._normal = normal; anchor._warningTotal = warning;
            anchor._airborne = airborne; anchor._targetIndex = targetIndex;
            Main.projectile[index].netUpdate = true;
        }
        public override void SetDefaults()
        {
            // A 20px tile probe can reach the endpoint without colliding with the floor;
            // Colliding below still uses the full visible core for player contact.
            Projectile.width = 20; Projectile.height = 20;
            Projectile.hostile = true; Projectile.friendly = false; Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1; Projectile.timeLeft = 420; Projectile.ignoreWater = true;
            Projectile.netImportant = true; // Persistent anchors must also reach joining clients.
            Projectile.tileCollide = false;
        }
        public override bool? CanDamage() => _phase == Flight || _phase == Anchored;
        public override bool ShouldUpdatePosition() => _phase == Flight;
        private bool SurfaceIntact() => _airborne || Collision.SolidCollision(_surface - _normal * 4f - Vector2.One, 2, 2);
        private void Enter(byte phase, int ticks)
        {
            _phase = phase; Projectile.ai[1] = ticks;
            Projectile.hide = phase >= Anchored && !_airborne;
            Projectile.netUpdate = true;
        }
        public override void AI()
        {
            // Once launched the core is independent: its 120-tick charge and child fan
            // continue while CrystalKnight recovers or begins another attack.
            if (Authority && _phase == Warning && !CrystalKnightShard.OwnerCasting(Projectile))
            { Projectile.Kill(); return; }
            Projectile.hide = _phase >= Anchored && !_airborne;
            if (_phase == Withdrawal && !_burstPlayed)
            {
                _burstPlayed = true;
                if (!Main.dedServ) CrystalKnightShard.Shatter(Projectile.Center, 280);
            }
            Projectile.tileCollide = _phase == Flight;
            if (_phase == Warning)
            {
                Vector2 aim = (Endpoint - Projectile.Center).SafeNormalize(Vector2.UnitY);
                Projectile.rotation = (420 - Projectile.timeLeft) * 0.07f;
                if (!Main.dedServ)
                {
                    float progress = 1f - Projectile.ai[1] / System.Math.Max(1, _warningTotal);
                    CrystalKnightShard.GatherDust(Projectile.Center, progress, 20f);
                    CrystalKnightShard.GatherDust(Endpoint, progress, 14f);
                    if (Main.GameUpdateCount % 4 == 0)
                        for (int i = 1; i <= 4; i++)
                            CrystalKnightShard.FrostDust(Projectile.Center + aim * (i * 12f), Vector2.Zero, 0.6f);
                }
                if (Projectile.ai[1] > 0) Projectile.ai[1]--;
                if (Authority && Projectile.ai[1] <= 0)
                {
                    if (_airborne && _targetIndex >= 0 && _targetIndex < Main.maxPlayers
                        && Main.player[_targetIndex].active && !Main.player[_targetIndex].dead)
                    {
                        Vector2 candidate = Main.player[_targetIndex].Center - new Vector2(0f, 12f * 16f);
                        if (candidate.Y > 32f && candidate.Y < Main.maxTilesY * 16f - 32f
                            && !Collision.SolidCollision(candidate - new Vector2(10f), 20, 20)
                            && Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height,
                                candidate - new Vector2(10f), 20, 20))
                            _surface = candidate; // Lock the air position when the core actually fires.
                    }
                    Projectile.velocity = (Endpoint - Projectile.Center).SafeNormalize(aim) * 10f;
                    Projectile.tileCollide = true;
                    Enter(Flight, 90);
                }
                return;
            }
            if (_phase == Flight)
            {
                Projectile.rotation += 0.11f;
                if (!Main.dedServ && Main.GameUpdateCount % 2 == 0)
                    CrystalKnightShard.FrostDust(Projectile.Center, -Projectile.velocity * 0.1f, 1f);
                if (Authority && Projectile.Distance(Endpoint) <= 12f)
                {
                    if (!SurfaceIntact() || Collision.SolidCollision(Endpoint - new Vector2(10f), 20, 20))
                    { Projectile.Kill(); return; }
                    Projectile.Center = Endpoint; Projectile.velocity = Vector2.Zero;
                    _settledRotation = Projectile.rotation;
                    if (!_airborne) Projectile.rotation = 0f;
                    Projectile.tileCollide = false;
                    Enter(Anchored, 120); // Two seconds measured from THIS crystal's actual impact.
                    return;
                }
                if (Projectile.ai[1] > 0) Projectile.ai[1]--;
                if (Authority && Projectile.ai[1] <= 0) Projectile.Kill();
                return;
            }
            // Landed cores stay fixed; unsupported cores rotate throughout the charge.
            // Derive the pose from the synced timer so late-joining clients see the same angle.
            Projectile.rotation = _airborne
                ? _settledRotation + (_phase == Anchored ? 120f - Projectile.ai[1] : 135f - Projectile.ai[1]) * 0.08f
                : 0f;
            if (Authority && _phase == Anchored && !SurfaceIntact()) { Projectile.Kill(); return; }
            if (_phase == Anchored && !Main.dedServ)
            {
                float progress = 1f - Projectile.ai[1] / 120f;
                if (Main.GameUpdateCount % 3 == 0)
                    CrystalKnightShard.GatherDust(Projectile.Center, progress, 12f);
                if (Projectile.ai[1] <= 30 && Main.GameUpdateCount % 3 == 0)
                    for (int lane = -2; lane <= 2; lane++)
                    {
                        Vector2 direction = _normal.RotatedBy(lane * MathHelper.PiOver4);
                        for (int j = 1; j <= 5; j++)
                            CrystalKnightShard.FrostDust(Projectile.Center + _normal * 20f + direction * (j * 16f), Vector2.Zero, 0.6f);
                    }
                if (Projectile.ai[1] == 120)
                    SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.45f }, Projectile.Center);
                if (Projectile.ai[1] <= 30 && Projectile.ai[1] % 10 == 0)
                    SoundEngine.PlaySound(SoundID.Item30 with { Pitch = 0.6f, Volume = 0.15f }, Projectile.Center);
                Lighting.AddLight(Projectile.Center, 0.1f, 0.25f + progress * 0.15f, 0.4f);
            }
            if (Projectile.ai[1] > 0) Projectile.ai[1]--;
            if (!Authority || Projectile.ai[1] > 0) return;
            if (_phase == Anchored)
            {
                for (int lane = -2; lane <= 2; lane++)
                {
                    Vector2 direction = _normal.RotatedBy(lane * MathHelper.PiOver4);
                    CrystalKnightShard.SpawnFromAnchor(Projectile, Projectile.Center + _normal * 24f,
                        direction * 8f, (int)(Projectile.damage * 0.9f), (int)Projectile.ai[2]);
                }
                Enter(Withdrawal, 15);
            }
            else Projectile.Kill();
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Unexpected walls cancel the seed. Clients wait for the authoritative kill packet.
            return Authority;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Frostburn, 180);
            target.AddBuff(ModContent.BuffType<Buffs.Debuffs.TornWings>(), 540);
            Buffs.Debuffs.FrostBuildup.Apply(target);
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Rectangle core = new((int)Projectile.Center.X - 19, (int)Projectile.Center.Y - 21, 38, 43);
            return core.Intersects(targetHitbox);
        }
        public override void OnKill(int timeLeft) => CrystalKnightShard.Shatter(Projectile.Center,
            _phase == Warning || _phase == Withdrawal ? 16 : 70);
        public override void DrawBehind(int index, System.Collections.Generic.List<int> behindNPCsAndTiles,
            System.Collections.Generic.List<int> behindNPCs, System.Collections.Generic.List<int> behindProjectiles,
            System.Collections.Generic.List<int> overPlayers, System.Collections.Generic.List<int> overWiresUI)
        {
            if (_phase >= Anchored && !_airborne) behindNPCsAndTiles.Add(index);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            float growth = _phase == Warning ? MathHelper.Lerp(0.25f, 1f,
                1f - Projectile.ai[1] / System.Math.Max(1, _warningTotal)) : 1f;
            float fade = _phase == Withdrawal || _phase == Flight && Projectile.ai[1] < 15
                ? MathHelper.Clamp(Projectile.ai[1] / 15f, 0f, 1f) : 1f;
            float charge = _phase == Anchored ? 1f - Projectile.ai[1] / 120f : 0f;
            Color color = Color.Lerp(lightColor, Color.LightCyan, 0.5f + charge * 0.45f)
                * fade * (_phase == Warning ? 0.35f + growth * 0.4f : 1f);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, color,
                Projectile.rotation, texture.Size() / 2f, CoreScale * growth, SpriteEffects.None, 0);
            return false;
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(_phase); writer.Write(_surface.X); writer.Write(_surface.Y);
            writer.Write(_normal.X); writer.Write(_normal.Y); writer.Write((short)_warningTotal);
            writer.Write(_airborne); writer.Write((short)_targetIndex); writer.Write(_settledRotation);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            _phase = reader.ReadByte(); _surface = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            _normal = new Vector2(reader.ReadSingle(), reader.ReadSingle()); _warningTotal = reader.ReadInt16();
            _airborne = reader.ReadBoolean(); _targetIndex = reader.ReadInt16(); _settledRotation = reader.ReadSingle();
        }
    }
}
