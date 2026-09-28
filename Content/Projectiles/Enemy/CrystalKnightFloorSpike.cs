using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>Locked floor mark: 45t frost tell, 8t rise, 8t fall, 12t harmless withdrawal.</summary>
    public class CrystalKnightFloorSpike : ModProjectile
    {
        // LargeCrystalShaft: five 312x48 horizontal variants. Rotate the first shaft 90 degrees;
        // its 72px drawn height leaves roughly 58px above the floor when 20% is buried.
        public override string Texture => "tsorcRevamp/Content/Projectiles/Enemy/Crystal/LargeCrystalShaft";
        private const float ShaftScale = 0.23f;
        private const float DrawnHeight = 312f * ShaftScale;
        private const float BuriedHeight = DrawnHeight * 0.2f;
        public override void SetDefaults()
        {
            Projectile.width = 16; Projectile.height = 12; Projectile.hostile = true;
            Projectile.friendly = false; Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1; Projectile.timeLeft = 90; Projectile.tileCollide = false;
            Projectile.ignoreWater = true; // Deliberate stationary floor hazard: terrain validated by the owner.
            Projectile.hide = true;
        }
        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => Projectile.ai[1] < 0 && Projectile.ai[1] >= -16;
        private float RiseProgress
        {
            get
            {
                float age = -Projectile.ai[1];
                if (age <= 0f) return 0f;
                if (age <= 8f) return age / 8f;
                if (age <= 16f) return (16f - age) / 8f;
                return 0f;
            }
        }
        private float ShaftTipY => Projectile.Bottom.Y + BuriedHeight - RiseProgress * DrawnHeight;
        public override void AI()
        {
            if (Projectile.ai[1] > 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient && !CrystalKnightShard.OwnerCasting(Projectile))
                { Projectile.Kill(); return; }
                if (!Main.dedServ)
                {
                    float progress = 1f - Projectile.ai[1] / 45f;
                    CrystalKnightShard.GatherDust(Projectile.Bottom - new Vector2(0f, 8f), progress, 24f);
                    if (Main.GameUpdateCount % 3 == 0)
                        for (int i = -4; i <= 4; i++)
                            CrystalKnightShard.FrostDust(Projectile.Bottom + new Vector2(i * 12f, -2f),
                                new Vector2(0f, -0.25f), 0.75f);
                }
            }
            Projectile.ai[1]--;
            if (Projectile.ai[1] == 0 && Main.netMode != NetmodeID.MultiplayerClient) Projectile.netUpdate = true;
            if (!Main.dedServ && Projectile.ai[1] == -1)
            {
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.45f }, Projectile.Center);
                for (int i = 0; i < 24; i++) CrystalKnightShard.FrostDust(Projectile.Bottom,
                    new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-4f, -1f)), 0.8f);
            }
            if (Projectile.ai[1] <= -28) Projectile.Kill();
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float tipY = ShaftTipY;
            if (tipY >= Projectile.Bottom.Y) return false;
            Rectangle visibleCrystal = new((int)Projectile.Center.X - 8, (int)tipY, 16,
                (int)System.Math.Ceiling(Projectile.Bottom.Y - tipY));
            return visibleCrystal.Intersects(targetHitbox);
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Frostburn, 180);
            target.AddBuff(ModContent.BuffType<Buffs.Debuffs.TornWings>(), 540);
            Buffs.Debuffs.FrostBuildup.Apply(target);
        }
        public override void OnKill(int timeLeft) => CrystalKnightShard.Shatter(Projectile.Bottom - new Vector2(0f, 12f), Projectile.ai[1] > 0 ? 16 : 45);
        public override void DrawBehind(int index, System.Collections.Generic.List<int> behindNPCsAndTiles,
            System.Collections.Generic.List<int> behindNPCs, System.Collections.Generic.List<int> behindProjectiles,
            System.Collections.Generic.List<int> overPlayers, System.Collections.Generic.List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            float progress = RiseProgress;
            if (progress <= 0f) return false; // The 45-tick floor dust is the entire tell.
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = new(0, 0, 312, 48);
            float fade = Projectile.ai[1] < -16 ? MathHelper.Clamp((28f + Projectile.ai[1]) / 12f, 0f, 1f) : 1f;
            Color color = Color.Lerp(lightColor, Color.LightCyan, 0.5f) * fade;
            // The source's right edge becomes the buried base after +90 degrees. Move the
            // full-size shaft vertically as it rises; tiles mask the buried 20%.
            Vector2 bottom = new(Projectile.Center.X, ShaftTipY + DrawnHeight);
            Main.EntitySpriteDraw(texture, bottom - Main.screenPosition, frame, color, MathHelper.PiOver2,
                new Vector2(frame.Width, frame.Height * 0.5f), ShaftScale, SpriteEffects.None, 0);
            return false;
        }
    }
}
