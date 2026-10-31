using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy.Darkmoon
{
    public class DarkmoonSliver : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 120; Projectile.hostile = true; Projectile.penetrate = 1;
            Projectile.tileCollide = false; Projectile.aiStyle = -1; Projectile.timeLeft = 52; Projectile.extraUpdates = 0;
            Projectile.DamageType = DamageClass.Magic;
        }
        public override bool? CanDamage() => Projectile.ai[0] < 40;
        public override void AI()
        {
            Projectile.ai[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.ai[0] == 40) Projectile.netUpdate = true;
            if (Projectile.ai[0] >= 40) Projectile.velocity *= 0.85f;
            if (!Main.dedServ && (int)Projectile.ai[0] % 3 == 0) DarkmoonVisuals.Motes(Projectile.Center, Color.LightBlue, 1, 5);
        }
        public override bool? Colliding(Rectangle broadphase, Rectangle target)
        {
            Vector2 axis = Projectile.rotation.ToRotationVector2();
            for (int i = 0; i < 12; i++)
            {
                float u = (i + 0.5f) / 12f, radius = 7f * (float)Math.Pow(4 * u * (1 - u), 2);
                Vector2 position = Projectile.Center + axis * ((u - 0.5f) * 96);
                float contact = 0;
                if (Collision.CheckAABBvLineCollision(target.TopLeft(), target.Size(), position - Projectile.velocity - axis * 4, position + axis * 4, Math.Max(1, radius * 2), ref contact)) return true;
            }
            return false;
        }
        public override bool PreDraw(ref Color lightColor)
        { DarkmoonVisuals.DrawSliver(Projectile.Center, Projectile.rotation, Projectile.ai[0] >= 40 ? MathHelper.Clamp(Projectile.timeLeft / 12f, 0, 1) : 1); return false; }
        public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 180);
        public override void OnKill(int timeLeft) => DarkmoonVisuals.Motes(Projectile.Center, Color.LightBlue, 40, 12);
    }
    public class DarkmoonExplosion : ModProjectile
    {
        public const int Radius = 160, VisualTicks = 60, DamageStart = 6, DamageEndExclusive = 21;
        readonly bool[] hit = new bool[Main.maxPlayers];
        public override string Texture => "Terraria/Images/Projectile_0";
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = Radius * 2; Projectile.hostile = true; Projectile.penetrate = -1;
            Projectile.tileCollide = false; Projectile.aiStyle = -1; Projectile.timeLeft = VisualTicks; Projectile.extraUpdates = 0;
            Projectile.DamageType = DamageClass.Magic;
        }
        public static bool DamagingAt(int age) => age >= DamageStart && age < DamageEndExclusive;
        public override bool? CanDamage() => DamagingAt((int)Projectile.ai[0]);
        public override bool CanHitPlayer(Player target) => !hit[target.whoAmI];
        public override void OnHitPlayer(Player target, Player.HurtInfo info) => hit[target.whoAmI] = true;
        public override bool? Colliding(Rectangle broadphase, Rectangle target)
        {
            Vector2 closest = Vector2.Clamp(Projectile.Center, target.TopLeft(), target.BottomRight());
            return Vector2.DistanceSquared(Projectile.Center, closest) <= Radius * Radius;
        }
        public override void AI()
        {
            int age = (int)Projectile.ai[0];
            if (!Main.dedServ)
            {
                if (age == 0)
                {
                    DarkmoonVisuals.Motes(Projectile.Center, Color.LightBlue, 80, 48);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.8f, Pitch = 0.2f }, Projectile.Center);
                    UsefulFunctions.ScreenShake(Projectile.Center, 3f, 15, distanceFalloff: 650);
                }
                if (age >= 21 && age < 39) DarkmoonVisuals.Motes(Projectile.Center, Color.Silver, 3, 160);
                Lighting.AddLight(Projectile.Center, new Vector3(0.3f, 0.65f, 1f) * MathHelper.Clamp(1 - age / 60f, 0, 1));
            }
            Projectile.ai[0]++;
            if (Projectile.ai[0] == 6 || Projectile.ai[0] == 21) Projectile.netUpdate = true;
        }
        public override bool PreDraw(ref Color lightColor)
        { DarkmoonVisuals.DrawExplosion(Projectile.Center, MathHelper.Clamp(Projectile.ai[0] / 60f, 0, 1), Projectile.ai[2]); return false; }
    }
}
