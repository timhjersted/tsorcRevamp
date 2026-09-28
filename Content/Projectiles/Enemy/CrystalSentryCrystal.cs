using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    public class CrystalSentryCrystal : ModProjectile
    {
        // Homing shot: single 74x68 CrystalCoreSmall sprite, spun at 0.45 scale.
        // Death child ai[0]=1: vanilla10x20 IceSpike, TOP leads, .65 scale, twenty-tick forming tell.
        public override string Texture => "tsorcRevamp/Content/Projectiles/Enemy/Crystal/CrystalCoreSmall";
        private bool DeathShard => Projectile.ai[0] == 1;
        private bool UpwardLaunch => Projectile.ai[0] == 2;
        private int WarningTicks => DeathShard ? 20 : 0;
        private float Age => Projectile.ai[1];
        public override void SetStaticDefaults() => Main.projFrames[Type] = 1;
        public override void SetDefaults()
        {
            Projectile.width = 12; Projectile.height = 12; Projectile.hostile = true;
            Projectile.DamageType = DamageClass.Magic; Projectile.penetrate = 1;
            Projectile.timeLeft = 180; Projectile.tileCollide = true; Projectile.ignoreWater = true;
        }
        public override bool ShouldUpdatePosition() => Age > WarningTicks;
        public override bool? CanDamage() => Age > WarningTicks && Age < WarningTicks + (DeathShard ? 105 : 135);
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!DeathShard)
            {
                Rectangle core = new((int)Projectile.Center.X - 17, (int)Projectile.Center.Y - 16, 34, 32);
                return core.Intersects(targetHitbox);
            }
            Vector2 axis = Projectile.velocity.SafeNormalize(Vector2.UnitY) * 5f;
            float point = 0;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center - axis, Projectile.Center + axis, 5f, ref point);
        }
        public override void AI()
        {
            Projectile.ai[1]++;
            Projectile.tileCollide = Age > WarningTicks;
            int homingStart = UpwardLaunch ? 7 : 1;
            if (!DeathShard && Age >= homingStart && Age < homingStart + 60
                && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int targetIndex = (int)Projectile.ai[2];
                if (targetIndex >= 0 && targetIndex < Main.maxPlayers
                    && Main.player[targetIndex].active && !Main.player[targetIndex].dead)
                {
                    Vector2 desired = (Main.player[targetIndex].Center - Projectile.Center)
                        .SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitY)) * 8f;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.13f)
                        .SafeNormalize(desired) * 8f;
                    if (Age % 6 == 0) Projectile.netUpdate = true;
                }
            }
            Projectile.rotation = DeathShard ? Projectile.velocity.ToRotation() + MathHelper.PiOver2
                : Projectile.rotation + 0.1f;
            if (!Main.dedServ)
            {
                if (Age <= WarningTicks)
                {
                    CrystalKnightShard.GatherDust(Projectile.Center, Age / WarningTicks, 14);
                    if (Age % 4 == 0) CrystalKnightShard.FrostDust(Projectile.Center
                        + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 22, Vector2.Zero, 0.6f);
                }
                else if (Age % 2 == 0) CrystalKnightShard.FrostDust(Projectile.Center, -Projectile.velocity * 0.08f, 0.75f);
                if (Age == WarningTicks + 1) SoundEngine.PlaySound(SoundID.Item30 with { Volume = DeathShard ? 0.12f : 0.5f, Pitch = 0.5f }, Projectile.Center);
                Lighting.AddLight(Projectile.Center, 0.08f, 0.25f, 0.35f);
            }
            if (Age >= WarningTicks + (DeathShard ? 120 : 150)) Projectile.Kill();
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // Every crystal hit adds frost, including the sentry's destruction shards.
            Buffs.Debuffs.FrostBuildup.Apply(target);
            if (!DeathShard)
            {
                target.AddBuff(BuffID.Frostburn, 180);
                target.AddBuff(ModContent.BuffType<Buffs.Debuffs.TornWings>(), 540);
            }
        }
        public override void OnKill(int timeLeft) => CrystalKnightShard.Shatter(Projectile.Center, 70);
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = DeathShard ? TextureAssets.Projectile[ProjectileID.IceSpike].Value : TextureAssets.Projectile[Type].Value;
            float opacity = MathHelper.Clamp((WarningTicks + (DeathShard ? 120 : 150) - Age) / 15f, 0, 1);
            float form = WarningTicks > 0 ? MathHelper.Clamp(Age / WarningTicks, 0, 1) : 1;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null,
                Color.Lerp(lightColor, Color.LightCyan, 0.6f) * opacity * (0.3f + form * 0.7f),
                Projectile.rotation, texture.Size() / 2f,
                (DeathShard ? 0.65f : 0.45f) * MathHelper.Lerp(0.2f, 1f, form), SpriteEffects.None, 0);
            return false;
        }
    }
}
