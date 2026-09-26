using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Enemies;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Projectiles.Enemy.Attraidies
{
    // Spec: real 56x56 Rune Blade (tip upper-right), grip (9,47), tip (54,1), scale 128/64.35.
    // 40t staff tell + 40t harmless staff sweep precede birth; blue gather at the exact pivot.
    // 220deg Weighted 8/32 k6 cut, 15 live ticks, then harmless settle + 16t blue dissolve.
    // Offset +/-30px vertically, +24px forward. Raw damage12, no debuff, no child attacks.
    // Spectral blade deliberately ignores tiles; collision is a swept blade segment, not the VFX.
    public class IllusionRuneBlade : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Content/Items/Weapons/Melee/Broadswords/RuneBlade";
        private const string SlashTexture = "tsorcRevamp/Content/Items/Weapons/Melee/Broadswords/BroadswordRework/Common/Melee/Slash";
        public static readonly Color Blue = new Color(65, 165, 255);
        private bool _born, _ending;
        private Vector2 _previousPivot;
        private int Facing => Projectile.ai[1] >= 0f ? 1 : -1;
        private bool Underhand => Math.Abs(Projectile.ai[1]) > 1.5f;
        private float Age => Projectile.ai[2];
        private float Angle(float age) => RuneBladeConjuration.WorldAngle(RuneBladeConjuration.Sweep(Underhand, age), Facing);
        private float Fade => MathHelper.Clamp(Projectile.timeLeft / (float)RuneBladeConjuration.FadeTicks, 0f, 1f);
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 12;
            Projectile.hostile = true; Projectile.friendly = false;
            Projectile.tileCollide = false; Projectile.penetrate = -1;
            Projectile.timeLeft = RuneBladeConjuration.SweepTicks + RuneBladeConjuration.FadeTicks;
            Projectile.alpha = 50; Projectile.DamageType = DamageClass.Magic;
        }
        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => !_ending && RuneBladeConjuration.Live(Age) ? null : false;
        private void Dissolve()
        {
            if (_ending) return;
            _ending = true;
            Projectile.timeLeft = Math.Min(Projectile.timeLeft, RuneBladeConjuration.FadeTicks);
            Projectile.netUpdate = true;
        }
        public override void AI()
        {
            _previousPivot = Projectile.Center;
            int index = (int)Projectile.ai[0];
            bool ownerValid = index >= 0 && index < Main.maxNPCs && Main.npc[index].active
                && Main.npc[index].ModNPC is AttraidiesIllusion owner && owner.RuneBladeChannelActive;
            if (ownerValid && !_ending)
                Projectile.Center = Main.npc[index].Center + RuneBladeConjuration.Offset(Underhand, Facing);
            else if (Main.netMode != NetmodeID.MultiplayerClient) Dissolve();
            // Sweep ordinary caster movement with the angular cut; never turn a network
            // correction/teleport into a long invisible collision segment.
            if (Vector2.DistanceSquared(_previousPivot, Projectile.Center) > 144f) _previousPivot = Projectile.Center;
            if (!_born && !Main.dedServ)
            {
                _born = true;
                Burst(48);
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.6f }, Projectile.Center);
            }
            if (!_ending) Projectile.ai[2]++;
            Projectile.rotation = Angle(Age);
            Projectile.alpha = 50 + (int)(205f * (1f - Fade));
            if (Main.dedServ) return;
            Lighting.AddLight(Projectile.Center, Blue.ToVector3() * 0.4f * Fade);
            if (RuneBladeConjuration.Live(Age) || Projectile.timeLeft <= RuneBladeConjuration.FadeTicks)
            {
                Vector2 point = Projectile.Center + Projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(18f, RuneBladeConjuration.Reach);
                Dust dust = Dust.NewDustPerfect(point, DustID.TintableDustLighted,
                    Main.rand.NextVector2Circular(1.5f, 1.5f), 60, Blue, 1.1f);
                dust.noGravity = true;
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (_ending || !RuneBladeConjuration.Live(Age)) return false;
            float start = Angle(Math.Max(0f, Age - 1f)), end = Angle(Age);
            int samples = Math.Max(1, (int)Math.Ceiling(Math.Abs(end - start) / MathHelper.ToRadians(4f)));
            for (int i = 0; i <= samples; i++)
            {
                Vector2 direction = MathHelper.Lerp(start, end, i / (float)samples).ToRotationVector2();
                Vector2 pivot = Vector2.Lerp(_previousPivot, Projectile.Center, i / (float)samples);
                float collisionPoint = 0f;
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                    pivot + direction * 14f, pivot + direction * RuneBladeConjuration.Reach,
                    10f, ref collisionPoint)) return true;
            }
            return false;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D sword = TextureAssets.Projectile[Type].Value;
            SpriteEffects swordFlip = Facing == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Vector2 grip = new Vector2(Facing == 1 ? 9f : sword.Width - 9f, 47f);
            float swordRotation = Projectile.rotation + (Facing == 1 ? MathHelper.PiOver4 : -MathHelper.Pi * 1.25f);
            // Same three-frame Slash strip and direction/scale language as PuppetNPC.DrawSlashToLayer.
            // Attach to the summoned blade's pivot and actual 128px reach, rather than the caster's hand.
            if (!_ending && Age <= RuneBladeConjuration.Curve.LiveTicks + 6f)
            {
                Texture2D slash = ModContent.Request<Texture2D>(SlashTexture).Value;
                float progress = MathHelper.Clamp(Age / RuneBladeConjuration.Curve.LiveTicks, 0f, 1f);
                Rectangle frame = new Rectangle(0, Math.Min(2, (int)(progress * 3f)) * (slash.Height / 3), slash.Width, slash.Height / 3);
                float opacity = MathHelper.Clamp(Age / 4f, 0f, 1f)
                    * MathHelper.Clamp((RuneBladeConjuration.Curve.LiveTicks + 6f - Age) / 6f, 0f, 1f) * 0.65f;
                SpriteEffects slashFlip = (Facing > 0) ^ Underhand ? SpriteEffects.FlipVertically : SpriteEffects.None;
                Main.EntitySpriteDraw(slash, Projectile.Center - Main.screenPosition, frame, Blue * opacity,
                    Projectile.rotation, frame.Size() * 0.5f, RuneBladeConjuration.Reach / 30f, slashFlip);
            }
            Main.EntitySpriteDraw(sword, Projectile.Center - Main.screenPosition, null,
                Color.White * ((255f - Projectile.alpha) / 255f), swordRotation, grip,
                RuneBladeConjuration.Reach / 64.35f, swordFlip);
            return false;
        }
        private void Burst(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 point = Projectile.Center + Angle(Age).ToRotationVector2() * Main.rand.NextFloat(8f, RuneBladeConjuration.Reach);
                Dust dust = Dust.NewDustPerfect(point, DustID.TintableDustLighted,
                    Main.rand.NextVector2Circular(3f, 3f), 60, Blue, Main.rand.NextFloat(0.8f, 1.4f));
                dust.noGravity = true;
            }
        }
        public override void OnKill(int timeLeft) { if (!Main.dedServ) Burst(24); }
        public override void SendExtraAI(BinaryWriter writer) { writer.Write(_ending); writer.Write((short)Projectile.timeLeft); }
        public override void ReceiveExtraAI(BinaryReader reader) { _ending = reader.ReadBoolean(); Projectile.timeLeft = reader.ReadInt16(); }
    }
}
