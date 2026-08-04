using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class InkGeyser : ModProjectile
    {

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Okiku/PoisonSmog";

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.timeLeft = 240;
            Projectile.hostile = true;
        }


        float timer = 0;
        bool targetSet = false;
        Vector2 targetPos;
        public override void AI()
        {
            if (timer < 120)
            {
                timer++;
            }
            else
            {
                if (!targetSet)
                {
                    targetSet = true;
                    targetPos = Main.player[(int)Projectile.ai[0]].Center;
                }
                if (Main.GameUpdateCount % 5 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 projVelocity = UsefulFunctions.Aim(Projectile.Center, targetPos, 12);
                    projVelocity = projVelocity.RotatedBy(Main.rand.NextFloat(-0.1f, 0.1f));
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, projVelocity, ModContent.ProjectileType<Projectiles.Enemy.InkJet>(), Projectile.damage, 0, Projectile.owner);
                }
            }

            //Core swirl
            for (int j = 0; j < 6f * (timer / 120f); j++)
            {
                Vector2 dir = Main.rand.NextVector2Circular(64, 64);
                Vector2 dustPos = Projectile.Center + dir;
                Vector2 dustVel = new Vector2(5, 0).RotatedBy(dir.ToRotation() + MathHelper.Pi / 2);
                Dust thisDust = Dust.NewDustPerfect(dustPos, DustID.Asphalt, dustVel, 0, default, 1.2f);
                thisDust.noGravity = true;
                thisDust.shader = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.BlackDye), Main.LocalPlayer);
            }
            //Edge ring
            for (int j = 0; j < 16; j++)
            {
                Vector2 dir = Main.rand.NextVector2CircularEdge(65, 65);
                Vector2 dustPos = Projectile.Center + dir;
                Vector2 dustVel = new Vector2(10, 0).RotatedBy(dir.ToRotation() + MathHelper.Pi / 2);
                int DustType = DustID.Asphalt;
                if (Main.GameUpdateCount % 5 == 0)
                {
                    DustType = DustID.CursedTorch;
                }
                Dust.NewDustPerfect(dustPos, DustType, dustVel, 0, default, 1).noGravity = true;
            }
        }

        public override bool? CanDamage() => timer >= 120f;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            const float radius = 40f;
            Vector2 closest = Vector2.Clamp(Projectile.Center,
                new Vector2(targetHitbox.Left, targetHitbox.Top),
                new Vector2(targetHitbox.Right, targetHitbox.Bottom));
            return Vector2.DistanceSquared(Projectile.Center, closest) <= radius * radius;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            bool active = timer >= 120f;
            float progress = active
                ? MathHelper.Clamp(1f - Projectile.timeLeft / 120f, 0f, 1f)
                : MathHelper.Clamp(timer / 120f, 0f, 1f);
            EnemyVFX.DrawQuaraInkGeyser(Projectile.Center, progress, active);
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            EnemyShaderBurst.Spawn(Projectile.GetSource_Death(), Projectile.Center, EnemyVFXBurstKind.QuaraInkBurst);
        }


        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            int buffLengthMod = 1;
            if (Main.expertMode)
            {
                buffLengthMod = 2;
            }
            //Ink sticks to a soaked target: Wet doubles the ink durations
            int wetMod = target.HasBuff(BuffID.Wet) ? 2 : 1;

            target.AddBuff(BuffID.BrokenArmor, 180 * wetMod / buffLengthMod, false);
            target.AddBuff(BuffID.Blackout, 600 * wetMod / buffLengthMod, false);
        }
    }
}
