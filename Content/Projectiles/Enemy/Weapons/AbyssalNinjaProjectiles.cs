using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Puppets;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    // Encounter nonce prevents a recycled NPC slot from adopting the previous boss's hazards.
    public abstract class AbyssalNinjaShot : ModProjectile
    {
        protected abstract int Lifetime { get; }
        protected abstract bool IsBolt { get; }
        private const int FadeTicks = 12;
        public override void SendExtraAI(BinaryWriter writer) => writer.Write((short)Projectile.timeLeft);
        public override void ReceiveExtraAI(BinaryReader reader) => Projectile.timeLeft = Math.Clamp(reader.ReadInt16(), 1, Lifetime);
        public override void SetDefaults()
        {
            Projectile.width = IsBolt ? 10 : 28; Projectile.height = IsBolt ? 20 : 28;
            Projectile.hostile = true; Projectile.friendly = false; Projectile.penetrate = 1;
            Projectile.tileCollide = true; Projectile.timeLeft = Lifetime;
            Projectile.scale = IsBolt ? 1.1f : 0.5f;
        }
        internal static bool OwnerAlive(Projectile p)
        {
            int index = (int)p.ai[0];
            return index >= 0 && index < Main.maxNPCs && Main.npc[index].active
                && Main.npc[index].ModNPC is AbyssalNinja ninja && ninja.EncounterActive && ninja.EncounterId == (int)p.ai[2];
        }
        public override void AI()
        {
            if (!OwnerAlive(Projectile)) { Projectile.Kill(); return; }
            if (Projectile.localAI[0]++ == 0 && !Main.dedServ)
                SoundEngine.PlaySound((IsBolt ? SoundID.Item98 : SoundID.Item1) with { Volume = 0.65f }, Projectile.Center);
            if (Projectile.timeLeft <= FadeTicks)
            {
                Projectile.hostile = false;
                Projectile.Opacity = Projectile.timeLeft / (float)FadeTicks;
            }
            if (IsBolt) Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            else Projectile.rotation += Math.Sign(Projectile.velocity.X) * MathHelper.ToRadians(12);
            if (!Main.dedServ && Projectile.timeLeft % 3 == 0)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame, -Projectile.velocity * 0.07f,
                    120, new Color(150, 100, 220), 0.65f);
                dust.noGravity = true;
            }
        }
        public override bool? CanDamage() => Projectile.hostile && Projectile.timeLeft > FadeTicks ? null : false;
        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ) return;
            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.4f, Pitch = 0.3f }, Projectile.Center);
            for (int i = 0; i < 16; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Shadowflame,
                    Main.rand.NextVector2Circular(3, 3), 100, new Color(150, 100, 220), 0.8f);
                dust.noGravity = true;
            }
        }
    }
    // Single-frame star; spinning sprite is reused deliberately, no independent animation sheet.
    public class AbyssalNinjaStar : AbyssalNinjaShot
    {
        public override string Texture => "tsorcRevamp/Content/Projectiles/Melee/AbyssalStarProj";
        protected override int Lifetime => 87; // 75 live at 12px/t (900px), then 12 harmless fade.
        protected override bool IsBolt => false;
    }
    // The vertical bolt sprite points up; +Pi/2 aligns its leading edge with travel.
    public class AbyssalNinjaBolt : AbyssalNinjaShot
    {
        public override string Texture => "tsorcRevamp/Content/Projectiles/Ranged/Ammo/BoltProjectile";
        protected override int Lifetime => 72; // 60 live at 14px/t (840px), then 12 harmless fade.
        protected override bool IsBolt => true;
    }

    public class AbyssalMeridianSeam : ModProjectile
    {
        public override string Texture => "Terraria/Images/MagicPixel";
        internal Vector2 End;
        private const int ChargeTicks = 32;
        private const int LiveTicks = 8;
        private const int FadeTicks = 10;
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18; Projectile.timeLeft = ChargeTicks + LiveTicks + FadeTicks;
            Projectile.tileCollide = false; Projectile.penetrate = -1; Projectile.hostile = false;
            Projectile.friendly = false; Projectile.netImportant = true;
        }
        public override bool ShouldUpdatePosition() => false;
        public override void SendExtraAI(BinaryWriter writer) { writer.Write(End.X); writer.Write(End.Y); writer.Write(Projectile.ai[1]); }
        public override void ReceiveExtraAI(BinaryReader reader) { End = new Vector2(reader.ReadSingle(), reader.ReadSingle()); Projectile.ai[1] = reader.ReadSingle(); }
        public override void AI()
        {
            if (!AbyssalNinjaShot.OwnerAlive(Projectile)) { Projectile.Kill(); return; }
            int age = (int)Projectile.ai[1]++;
            Projectile.hostile = age >= ChargeTicks && age < ChargeTicks + LiveTicks;
            if (age >= ChargeTicks + LiveTicks + FadeTicks) { Projectile.Kill(); return; }
            if (!Main.dedServ && age == ChargeTicks)
            {
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.7f, Pitch = -0.4f }, Projectile.Center);
                for (int i = 0; i < 24; i++)
                {
                    Vector2 point = Vector2.Lerp(Projectile.Center, End, i / 23f);
                    Dust dust = Dust.NewDustPerfect(point, DustID.Shadowflame,
                        new Vector2(0, Main.rand.NextFloat(-3, 3)), 80, new Color(170, 110, 240), 1.1f);
                    dust.noGravity = true;
                }
            }
        }
        public override bool? CanDamage() => Projectile.hostile ? null : false;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float point = 0;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, End, 18f, ref point);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            int age = (int)Projectile.ai[1];
            float charge = MathHelper.Clamp(age / (float)ChargeTicks, 0, 1);
            float opacity = age < ChargeTicks ? 0.15f + 0.35f * charge : age < ChargeTicks + LiveTicks ? 1 : Math.Max(0, 1 - (age - ChargeTicks - LiveTicks) / (float)FadeTicks);
            Vector2 delta = End - Projectile.Center;
            int pieces = Math.Max(1, (int)(delta.Length() / 6));
            Vector2 normal = delta.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < pieces; i++)
            {
                float t0 = i / (float)pieces, t1 = (i + 1f) / pieces;
                Vector2 a = Vector2.Lerp(Projectile.Center, End, t0), b = Vector2.Lerp(Projectile.Center, End, t1);
                float flow = (float)Math.Sin(t0 * 51 - age * 0.3f);
                DrawBand(a + normal * flow * 5, b + normal * flow * 5, new Color(75, 20, 120) * opacity * 0.4f, 26);
                DrawBand(a, b, new Color(160, 85, 240) * opacity, Projectile.hostile ? 18 : 2);
                if (Projectile.hostile) DrawBand(a, b, new Color(225, 205, 255) * opacity * (0.7f + 0.2f * flow), 5);
            }
            return false;
        }
        private static void DrawBand(Vector2 a, Vector2 b, Color color, float width)
        {
            Vector2 d = b - a;
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, a - Main.screenPosition, null, color,
                d.ToRotation(), new Vector2(0, 0.5f), new Vector2(d.Length() + 1, width), SpriteEffects.None, 0);
        }
    }
}
