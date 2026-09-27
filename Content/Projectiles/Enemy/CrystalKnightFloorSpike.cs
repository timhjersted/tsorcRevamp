using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>Locked floor mark: 32t gathering, 16t live, 12t harmless withdrawal.</summary>
    public class CrystalKnightFloorSpike : ModProjectile
    {
        // Verified 10x20 single frame, tip up; draw bottom-anchored at exactly the 24x64 collision extent.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.IceSpike;
        public override void SetDefaults()
        {
            Projectile.width = 24; Projectile.height = 64; Projectile.hostile = true;
            Projectile.friendly = false; Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1; Projectile.timeLeft = 90; Projectile.tileCollide = false;
            Projectile.ignoreWater = true; // Deliberate stationary floor hazard: terrain validated by the owner.
        }
        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => Projectile.ai[1] < 0 && Projectile.ai[1] >= -16;
        public override void AI()
        {
            if (Projectile.ai[1] > 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient && !CrystalKnightShard.OwnerCasting(Projectile))
                { Projectile.Kill(); return; }
                if (!Main.dedServ)
                {
                    float progress = 1f - Projectile.ai[1] / 32f;
                    CrystalKnightShard.GatherDust(Projectile.Bottom - new Vector2(0f, 8f), progress, 12f);
                    if (Main.GameUpdateCount % 3 == 0)
                        for (int i = -1; i <= 1; i++)
                            CrystalKnightShard.FrostDust(Projectile.Bottom + new Vector2(i * 10f, -2f), new Vector2(0f, -0.25f), 0.75f);
                }
            }
            Projectile.ai[1]--;
            if (Projectile.ai[1] == 0 && Main.netMode != NetmodeID.MultiplayerClient) Projectile.netUpdate = true;
            if (!Main.dedServ && Projectile.ai[1] == -1)
            {
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.45f }, Projectile.Center);
                for (int i = 0; i < 12; i++) CrystalKnightShard.FrostDust(Projectile.Bottom,
                    new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-4f, -1f)), 0.8f);
            }
            if (Projectile.ai[1] <= -28) Projectile.Kill();
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // Tapered upright crystal, not a full invisible rectangle at its narrow tip.
            float collision = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Bottom - new Vector2(0f, 6f), Projectile.Top + new Vector2(0f, 5f), 18f, ref collision);
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Frostburn, 180);
            target.AddBuff(ModContent.BuffType<Buffs.Debuffs.TornWings>(), 540);
            Buffs.Debuffs.FrostBuildup.Apply(target);
        }
        public override void OnKill(int timeLeft) => CrystalKnightShard.Shatter(Projectile.Bottom - new Vector2(0f, 12f), Projectile.ai[1] > 0 ? 16 : 45);
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[ProjectileID.IceSpike].Value;
            float growth = Projectile.ai[1] >= 0 ? MathHelper.Lerp(0.05f, 0.25f, 1f - Projectile.ai[1] / 32f)
                : Projectile.ai[1] >= -16 ? 1f : MathHelper.Clamp((28f + Projectile.ai[1]) / 12f, 0f, 1f);
            Color color = Color.Lerp(lightColor, Color.LightCyan, 0.5f) * (Projectile.ai[1] >= 0 ? 0.4f : growth);
            Main.EntitySpriteDraw(texture, Projectile.Bottom - Main.screenPosition, null, color, 0f,
                new Vector2(texture.Width * 0.5f, texture.Height), new Vector2(24f / texture.Width, 64f / texture.Height * growth), SpriteEffects.None, 0);
            return false;
        }
    }
}
