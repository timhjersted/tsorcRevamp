using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Enemies.GhostFighter;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Projectiles.Enemy.Darkmoon
{
    // A projectile, not the summoner, owns both moons. No owner-alive cleanup belongs here.
    public class DarkmoonVigilController : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";
        public const int SliverExpertDamage = 256, ExplosionExpertDamage = 360;

        // Moon hunt (before the crash): top drift speed px/t, how close it gets before committing to the final
        // 18-tick glide onto the player, and the longest it hunts before committing anyway.
        const float HuntTopSpeed = 6.5f;
        const float HuntCommitDistance = 112f;
        const int HuntMaxTicks = 210;
        const float TerminalMaxGlide = 160f; // the 18-tick final glide covers at most this (about 9 px/t)
        enum Stage : byte { Dormant, Pursuit, ChargeTell, Hunt, Terminal, Exploded, Finished, Fade }
        sealed class Moon
        {
            public Stage Stage;
            public Vector2 Position, Velocity, Aim = Vector2.UnitX, CrashPoint;
            public int Timer, Fans, SafeAfter;
            public bool Locked, TerrainHold;
        }
        readonly Moon[] moons = { new Moon(), new Moon() };
        static int nextSerial;
        int clock, secondActivation = -1, reacquire;
        bool initialized, fading;
        public int Serial => (int)Projectile.ai[2];
        int Target => (int)Projectile.ai[0];
        bool Server => Main.netMode != NetmodeID.MultiplayerClient;
        public static int NewSerial() { nextSerial = (nextSerial + 1) & 0x7FFFFF; return nextSerial == 0 ? ++nextSerial : nextSerial; }
        public static DarkmoonVigilController Find(int serial)
        {
            if (serial == 0) return null;
            for (int i = 0; i < Main.maxProjectiles; i++)
                if (Main.projectile[i].active && Main.projectile[i].ModProjectile is DarkmoonVigilController c && c.Serial == serial) return c;
            return null;
        }
        static bool Clear(Vector2 center) => center.X > 64 && center.Y > 64 && center.X < Main.maxTilesX * 16 - 64 && center.Y < Main.maxTilesY * 16 - 64
            && !Collision.SolidCollision(center - new Vector2(56), 112, 112);
        public static bool CanForm(Vector2 anchor) => Clear(anchor + new Vector2(-80, -88)) && Clear(anchor + new Vector2(80, -88));
        GhostOfTheDarkmoonKnight Owner
        {
            get
            {
                int index = (int)Projectile.ai[1];
                return index >= 0 && index < Main.maxNPCs && Main.npc[index].active
                    && Main.npc[index].ModNPC is GhostOfTheDarkmoonKnight knight && knight.EncounterSerial == Serial ? knight : null;
            }
        }
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.aiStyle = -1; Projectile.timeLeft = 2400; Projectile.tileCollide = false;
            Projectile.penetrate = -1; Projectile.netImportant = true; Projectile.extraUpdates = 0;
        }
        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;
        public override void AI()
        {
            if (!initialized)
            {
                if (!Server) return;
                initialized = true;
                for (int i = 0; i < 2; i++) moons[i].Position = Projectile.Center + new Vector2(i == 0 ? -80 : 80, -88);
                moons[0].Stage = Stage.Pursuit;
                Projectile.netUpdate = true;
            }
            clock++;
            AddFormingBeams();
            if (!Server)
            {
                foreach (Moon m in moons)
                {
                    m.Timer++;
                    if (m.Stage == Stage.Terminal && m.Timer >= 18) m.Position = m.CrashPoint;
                    else m.Position += m.Velocity;
                }
                Projectile.Center = moons[1].Stage >= Stage.Pursuit && moons[1].Stage <= Stage.Terminal ? moons[1].Position : moons[0].Position;
                return;
            }
            bool valid = Target >= 0 && Target < Main.maxPlayers && Main.player[Target].active && !Main.player[Target].dead;
            if (!valid && !fading)
            {
                foreach (Moon m in moons) m.Velocity = Vector2.Zero;
                if (++reacquire >= 30)
                {
                    int best = -1; float distance = 1600 * 1600;
                    for (int i = 0; i < Main.maxPlayers; i++) if (Main.player[i].active && !Main.player[i].dead)
                    { float d = Vector2.DistanceSquared(moons[0].Position, Main.player[i].Center); if (d < distance) { best = i; distance = d; } }
                    if (best >= 0) { Projectile.ai[0] = best; reacquire = 0; Projectile.netUpdate = true; }
                    else { fading = true; foreach (Moon m in moons) { m.Stage = Stage.Fade; m.Timer = 0; } Projectile.netUpdate = true; }
                }
                return;
            }
            if (secondActivation >= 0 && clock >= secondActivation && moons[1].Stage == Stage.Dormant)
            { moons[1].Stage = Stage.Pursuit; moons[1].Timer = 0; Projectile.netUpdate = true; }
            for (int i = 0; i < 2; i++) Advance(moons[i], i);
            Projectile.Center = moons[1].Stage >= Stage.Pursuit && moons[1].Stage <= Stage.Terminal ? moons[1].Position : moons[0].Position;
            if (clock % 10 == 0) Projectile.netUpdate = true;
            if (moons[0].Stage == Stage.Finished && moons[1].Stage == Stage.Finished) Projectile.Kill();
        }
        void Trace(string kind, Moon m)
        {
            if (Server && ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
                Mod.Logger.Info("DARKMOON_VIGIL " + System.Text.Json.JsonSerializer.Serialize(new {
                    kind, serial = Serial, clock, moon = Array.IndexOf(moons, m),
                    stage = m.Stage.ToString(), age = m.Timer, fans = m.Fans,
                    x = m.Position.X, y = m.Position.Y, secondActivation }));
        }
        void Change(Moon m, Stage stage)
        { m.Stage = stage; m.Timer = 0; m.Locked = false; Trace("transition", m); Projectile.netUpdate = true; }
        void Advance(Moon m, int slot)
        {
            if (m.Stage == Stage.Dormant || m.Stage == Stage.Finished) { m.Velocity = Vector2.Zero; return; }
            m.Timer++;
            if (m.Stage == Stage.Fade) { if (m.Timer >= 20) Change(m, Stage.Finished); return; }
            if (m.Stage == Stage.Exploded) { if (m.Timer >= 60) Change(m, Stage.Finished); return; }
            Player target = Main.player[Target];
            if (m.Stage == Stage.Pursuit)
            {
                int release = 30 + m.Fans * 90;
                if (m.Fans < 4 && m.Timer >= release - 18)
                {
                    m.Velocity = Vector2.Zero;
                    if (!m.Locked)
                    { m.Aim = (target.Center - m.Position).SafeNormalize(Vector2.UnitX); m.Locked = true; Projectile.netUpdate = true; }
                    if (m.Timer >= release)
                    { FireFan(m); m.Fans++; m.Locked = false; Trace("transition", m); Projectile.netUpdate = true; }
                }
                else
                {
                    Vector2 hover = target.Center + new Vector2(slot == 0 ? -120 : 120, -96);
                    Vector2 desired = (hover - m.Position).SafeNormalize(Vector2.Zero) * Math.Min(1.4f, Vector2.Distance(hover, m.Position) * 0.03f);
                    m.Velocity = Approach(m.Velocity, desired, 0.04f);
                    Vector2 allowed = Collision.TileCollision(m.Position - new Vector2(56), m.Velocity, 112, 112);
                    m.Position += allowed; m.Velocity = allowed;
                }
                if (m.Timer >= 340) { m.Velocity = Vector2.Zero; Change(m, Stage.ChargeTell); }
            }
            else if (m.Stage == Stage.ChargeTell)
            {
                m.Velocity = Vector2.Zero;
                var owner = Owner;
                if (owner != null && owner.MeleeDangerRemaining > 0) m.SafeAfter = Math.Max(m.SafeAfter, clock + owner.MeleeDangerRemaining + 30);
                if (m.Timer >= 40 && clock >= m.SafeAfter) Change(m, Stage.Hunt);
            }
            else if (m.Stage == Stage.Hunt)
            {
                Vector2 lead = target.velocity * 8f;
                if (lead.Length() > 64) lead = lead.SafeNormalize(Vector2.Zero) * 64;
                Vector2 delta = target.Center + lead - m.Position;
                float distance = delta.Length();

                // A slow, relentless drift: speed builds 1.4 -> HuntTopSpeed px/t over 45 ticks. The turn tightens
                // as it closes (0.05 rad/t far, 0.15 rad/t point-blank) so it cannot orbit a player at its turn
                // radius instead of reaching them.
                float speed = MathHelper.Lerp(1.4f, HuntTopSpeed, MathHelper.Clamp(m.Timer / 45f, 0, 1));
                float closeness = 1f - MathHelper.Clamp(distance / 300f, 0f, 1f);
                float turnRate = MathHelper.Lerp(0.05f, 0.15f, closeness);
                float heading = m.Velocity.LengthSquared() > 0.01f ? m.Velocity.ToRotation() : delta.ToRotation();
                float desired = delta.ToRotation();
                heading += MathHelper.Clamp(MathHelper.WrapAngle(desired - heading), -turnRate, turnRate);
                m.Velocity = heading.ToRotationVector2() * speed;
                Vector2 allowed = Collision.TileCollision(m.Position - new Vector2(56), m.Velocity, 112, 112);
                m.Position += allowed;
                if (Vector2.DistanceSquared(allowed, m.Velocity) > 0.01f)
                { BeginTerminal(m, m.Position, true); return; }

                // Commit once it is nearly on top of the player (or after HuntMaxTicks): the last glide ends ON the
                // player's centre, under the 18t warning, instead of a fixed distance short of them.
                bool closeEnough = m.Timer >= 30 && distance <= HuntCommitDistance;
                if (closeEnough || m.Timer >= HuntMaxTicks)
                {
                    Vector2 toPlayer = target.Center - m.Position;
                    BeginTerminal(m, m.Position + toPlayer.SafeNormalize(Vector2.UnitX) * Math.Min(TerminalMaxGlide, toPlayer.Length()), false);
                }
            }
            else if (m.Stage == Stage.Terminal)
            {
                if (!m.TerrainHold)
                {
                    Vector2 allowed = Collision.TileCollision(m.Position - new Vector2(56), m.Velocity, 112, 112);
                    m.Position += allowed;
                    if (Vector2.DistanceSquared(allowed, m.Velocity) > 0.01f)
                    { BeginTerminal(m, m.Position, true); return; }
                }
                if (m.Timer >= 18) Explode(m, slot);
            }
        }
        void BeginTerminal(Moon m, Vector2 point, bool terrain)
        {
            Change(m, Stage.Terminal); m.TerrainHold = terrain; m.CrashPoint = point;
            m.Velocity = terrain ? Vector2.Zero : (point - m.Position) / 18f;
        }
        void FireFan(Moon m)
        {
            Trace("fan-release", m);
            for (int i = -1; i <= 1; i++)
            {
                Vector2 direction = m.Aim.RotatedBy(MathHelper.ToRadians(24) * i);
                Vector2 origin = m.Position + direction * 56f;
                if (Collision.SolidCollision(origin - new Vector2(6), 12, 12) || !Collision.CanHitLine(origin, 1, 1, origin + direction * 32, 1, 1)) continue;
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), origin, direction * 16f,
                    ModContent.ProjectileType<DarkmoonSliver>(), EnemyDamage.Projectile(SliverExpertDamage), 4f, Projectile.owner, 0, Serial);
            }
        }
        void Explode(Moon m, int slot)
        {
            Vector2 arrival = m.Velocity.SafeNormalize(Vector2.UnitY);
            m.Velocity = Vector2.Zero;
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), m.Position, Vector2.Zero, ModContent.ProjectileType<DarkmoonExplosion>(),
                EnemyDamage.Projectile(ExplosionExpertDamage), 6, Projectile.owner, 0, Serial, arrival.ToRotation());
            Change(m, Stage.Exploded);
            if (slot == 0) secondActivation = clock + 60 + 300;
            Trace("explosion", m);
        }
        static Vector2 Approach(Vector2 value, Vector2 target, float amount)
        { Vector2 delta = target - value; return delta.Length() <= amount ? target : value + delta.SafeNormalize(Vector2.Zero) * amount; }
        public bool QuietFor(int ticks)
        {
            if (!initialized || fading) return false;
            if (moons[0].Stage == Stage.Finished && moons[1].Stage == Stage.Dormant)
                return secondActivation - clock >= ticks;
            return false;
        }
        public bool CanMeleeStart(int tell, int live)
        {
            if (!initialized || fading) return true;
            foreach (Moon m in moons)
            {
                if (m.Stage == Stage.ChargeTell || m.Stage == Stage.Hunt || m.Stage == Stage.Terminal) return false;
                if (m.Stage == Stage.Exploded && m.Timer < 51) return false; // damage ends21 +30t fresh-roll budget
                if (m.Stage != Stage.Pursuit) continue;
                if (Target < 0 || Target >= Main.maxPlayers) return false;
                float flight = Vector2.Distance(m.Position, Main.player[Target].Center) / 16f;
                for (int f = 0; f < 4; f++)
                {
                    float arrival = 30 + f * 90 - m.Timer + flight;
                    if (arrival + 4 + 30 >= tell && arrival - 4 - 30 <= tell + live) return false;
                }
            }
            return true;
        }
        /// <summary>
        /// Clients: each fan about to fire shows its three beams forming at the moon's rim over the last 24 ticks,
        /// growing from a short dim spark to the full beam the shot leaves with. Mirrors the aim PreDraw uses.
        /// </summary>
        void AddFormingBeams()
        {
            if (Main.dedServ || !initialized)
            {
                return;
            }

            foreach (Moon m in moons)
            {
                if (m.Stage != Stage.Pursuit || m.Fans >= 4)
                {
                    continue;
                }

                int release = 30 + m.Fans * 90;
                int left = release - m.Timer;
                if (left > 24 || left < 0)
                {
                    continue;
                }

                Vector2 aim = m.Aim;
                if (!m.Locked && Target >= 0 && Target < Main.maxPlayers)
                {
                    aim = (Main.player[Target].Center - m.Position).SafeNormalize(Vector2.UnitX);
                }

                float forming = MathHelper.Clamp((24 - left) / 24f, 0.1f, 1f);
                for (int j = -1; j <= 1; j++)
                {
                    Vector2 direction = aim.RotatedBy(MathHelper.ToRadians(24) * j);
                    Vector2 origin = m.Position + direction * 56;
                    Vector2 head = origin + direction * (12f + 40f * forming);
                    DarkmoonVisuals.BeamStrip(head, direction, 40f + 130f * forming, 6f + 8f * forming, forming * 0.8f);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!initialized) return false;
            for (int i = 0; i < 2; i++)
            {
                Moon m = moons[i];
                if (m.Stage == Stage.Exploded || m.Stage == Stage.Finished) continue;
                float opacity = m.Stage == Stage.Dormant ? 0.4f : m.Stage == Stage.Fade ? MathHelper.Clamp(1 - m.Timer / 20f, 0, 1) : 1f;
                float charge = m.Stage >= Stage.ChargeTell && m.Stage <= Stage.Terminal ? 1f : 0f;
                DarkmoonVisuals.DrawMoon(m.Position, 1f, opacity, charge, i);
                if (m.Stage == Stage.Pursuit && m.Fans < 4)
                {
                    int release = 30 + m.Fans * 90, left = release - m.Timer;
                    if (left <= 24 && left >= 0)
                    {
                        Vector2 aim = m.Locked ? m.Aim : Target >= 0 && Target < Main.maxPlayers ? (Main.player[Target].Center - m.Position).SafeNormalize(Vector2.UnitX) : m.Aim;
                        for (int j = -1; j <= 1; j++)
                        {
                            Vector2 d = aim.RotatedBy(MathHelper.ToRadians(24) * j), origin = m.Position + d * 56;
                            DarkmoonVisuals.Line(origin, origin + d * 640, Color.LightBlue * (m.Locked ? 0.55f : 0.25f), 1f);
                        }
                    }
                }
                if (m.Stage == Stage.Terminal) DarkmoonVisuals.DrawWarning(m.CrashPoint, MathHelper.Clamp(m.Timer / 18f, 0, 1));
            }
            return false;
        }
        public override void SendExtraAI(BinaryWriter w)
        {
            w.Write(initialized); w.Write(clock); w.Write(secondActivation); w.Write(fading); w.Write(reacquire);
            foreach (Moon m in moons)
            {
                w.Write((byte)m.Stage); w.Write(m.Timer); w.Write(m.Fans); w.Write(m.SafeAfter); w.Write(m.Locked); w.Write(m.TerrainHold);
                Write(w, m.Position); Write(w, m.Velocity); Write(w, m.Aim); Write(w, m.CrashPoint);
            }
        }
        public override void ReceiveExtraAI(BinaryReader r)
        {
            initialized = r.ReadBoolean(); clock = r.ReadInt32(); secondActivation = r.ReadInt32(); fading = r.ReadBoolean(); reacquire = r.ReadInt32();
            foreach (Moon m in moons)
            {
                m.Stage = (Stage)r.ReadByte(); m.Timer = r.ReadInt32(); m.Fans = r.ReadInt32(); m.SafeAfter = r.ReadInt32(); m.Locked = r.ReadBoolean(); m.TerrainHold = r.ReadBoolean();
                m.Position = Read(r); m.Velocity = Read(r); m.Aim = Read(r); m.CrashPoint = Read(r);
            }
        }
        static void Write(BinaryWriter w, Vector2 v) { w.Write(v.X); w.Write(v.Y); }
        static Vector2 Read(BinaryReader r) => new Vector2(r.ReadSingle(), r.ReadSingle());
    }
}
