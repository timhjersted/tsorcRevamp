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
    /// <summary>Harmless seed -> embedded120t charge -> three outward shards ->15t withdrawal.</summary>
    public class CrystalKnightAnchor : ModProjectile
    {
        // IceSpike:10x20, one frame, point at top. Seed draw scale2, collision18x36.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.IceSpike;
        private const byte Warning = 0, Flight = 1, Anchored = 2, Withdrawal = 3;
        private byte _phase;
        private Vector2 _surface, _normal;
        private int _warningTotal = 30;
        private Vector2 Endpoint => _surface + _normal * 20f;
        private bool Authority => Main.netMode != NetmodeID.MultiplayerClient;

        public static void Spawn(NPC owner, Vector2 origin, Vector2 surface, Vector2 normal, int damage, int warning, int sequence)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            int index = Projectile.NewProjectile(owner.GetSource_FromAI(), origin, Vector2.Zero,
                ModContent.ProjectileType<CrystalKnightAnchor>(), damage, 2f, Main.myPlayer, owner.whoAmI, warning, sequence);
            if (index >= Main.maxProjectiles) return;
            var anchor = (CrystalKnightAnchor)Main.projectile[index].ModProjectile;
            anchor._surface = surface; anchor._normal = normal; anchor._warningTotal = warning;
            Main.projectile[index].netUpdate = true;
        }
        public override void SetDefaults()
        {
            Projectile.width = 18; Projectile.height = 36;
            Projectile.hostile = false; Projectile.friendly = false; Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1; Projectile.timeLeft = 420; Projectile.ignoreWater = true;
            Projectile.netImportant = true; // Persistent, harmless anchors must also reach joining clients.
            Projectile.tileCollide = false;
        }
        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => _phase == Flight;
        private bool SurfaceIntact() => Collision.SolidCollision(_surface - _normal * 4f - Vector2.One, 2, 2);
        private void Enter(byte phase, int ticks)
        {
            _phase = phase; Projectile.ai[1] = ticks;
            Projectile.netUpdate = true;
        }
        public override void AI()
        {
            if (Authority && _phase != Withdrawal && !CrystalKnightShard.OwnerCasting(Projectile))
            { Projectile.Kill(); return; }
            Projectile.tileCollide = _phase == Flight;
            if (_phase == Warning)
            {
                Vector2 aim = (Endpoint - Projectile.Center).SafeNormalize(Vector2.UnitY);
                Projectile.rotation = aim.ToRotation() + MathHelper.PiOver2;
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
                    Projectile.velocity = aim * 10f;
                    Projectile.tileCollide = true;
                    Enter(Flight, 90);
                }
                return;
            }
            if (_phase == Flight)
            {
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
                if (!Main.dedServ && Main.GameUpdateCount % 2 == 0)
                    CrystalKnightShard.FrostDust(Projectile.Center, -Projectile.velocity * 0.1f, 1f);
                if (Authority && Projectile.Distance(Endpoint) <= 12f)
                {
                    if (!SurfaceIntact() || Collision.SolidCollision(Endpoint - new Vector2(9f, 18f), 18, 36))
                    { Projectile.Kill(); return; }
                    Projectile.Center = Endpoint; Projectile.velocity = Vector2.Zero;
                    Projectile.rotation = (-_normal).ToRotation() + MathHelper.PiOver2;
                    Projectile.tileCollide = false;
                    Enter(Anchored, 120); // Two seconds measured from THIS crystal's actual impact.
                    return;
                }
                if (Projectile.ai[1] > 0) Projectile.ai[1]--;
                if (Authority && Projectile.ai[1] <= 0) Projectile.Kill();
                return;
            }
            // Derive the embedded pose from synced geometry, including a late-joining client.
            Projectile.rotation = (-_normal).ToRotation() + MathHelper.PiOver2;
            if (Authority && _phase == Anchored && !SurfaceIntact()) { Projectile.Kill(); return; }
            if (_phase == Anchored && !Main.dedServ)
            {
                float progress = 1f - Projectile.ai[1] / 120f;
                if (Main.GameUpdateCount % 3 == 0)
                    CrystalKnightShard.GatherDust(Projectile.Center, progress, 12f);
                if (Projectile.ai[1] <= 30 && Main.GameUpdateCount % 3 == 0)
                    for (int lane = -1; lane <= 1; lane++)
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
                NPC owner = Main.npc[(int)Projectile.ai[0]];
                for (int lane = -1; lane <= 1; lane++)
                {
                    Vector2 direction = _normal.RotatedBy(lane * MathHelper.PiOver4);
                    CrystalKnightShard.Spawn(owner, Projectile.Center + _normal * 20f,
                        direction * 8f, (int)(Projectile.damage * 0.9f), 0, (int)Projectile.ai[2], false);
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
        public override void OnKill(int timeLeft) => CrystalKnightShard.Shatter(Projectile.Center, _phase == Warning ? 16 : 70);
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[ProjectileID.IceSpike].Value;
            float growth = _phase == Warning ? MathHelper.Lerp(0.25f, 1f,
                1f - Projectile.ai[1] / System.Math.Max(1, _warningTotal)) : 1f;
            float fade = _phase == Withdrawal || _phase == Flight && Projectile.ai[1] < 15
                ? MathHelper.Clamp(Projectile.ai[1] / 15f, 0f, 1f) : 1f;
            float charge = _phase == Anchored ? 1f - Projectile.ai[1] / 120f : 0f;
            Color color = Color.Lerp(lightColor, Color.LightCyan, 0.5f + charge * 0.45f)
                * fade * (_phase == Warning ? 0.35f + growth * 0.4f : 1f);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, color,
                Projectile.rotation, texture.Size() / 2f, 2f * growth, SpriteEffects.None, 0);
            return false;
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(_phase); writer.Write(_surface.X); writer.Write(_surface.Y);
            writer.Write(_normal.X); writer.Write(_normal.Y); writer.Write((short)_warningTotal);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            _phase = reader.ReadByte(); _surface = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            _normal = new Vector2(reader.ReadSingle(), reader.ReadSingle()); _warningTotal = reader.ReadInt16();
        }
    }
}
