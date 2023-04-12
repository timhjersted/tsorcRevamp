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
                if (Main.GameUpdateCount % 5 == 0)
                {
                    Vector2 projVelocity = UsefulFunctions.GenerateTargetingVector(Projectile.Center, targetPos, 12);
                    projVelocity = projVelocity.RotatedBy(Main.rand.NextFloat(-0.1f, 0.1f));
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, projVelocity, ModContent.ProjectileType<Projectiles.Enemy.InkJet>(), Projectile.damage, 0, Projectile.owner);
                }
            }

            for (int j = 0; j < 10f * (timer / 120f); j++)
            {
                Vector2 dir = Main.rand.NextVector2Circular(64, 64);
                Vector2 dustPos = Projectile.Center + dir;
                Vector2 dustVel = dir.RotatedBy(MathHelper.Pi / 1.3f);
                dustVel /= 16f;
                Dust thisDust = Dust.NewDustPerfect(dustPos, DustID.Asphalt, dustVel, 0, default, 2f);
                thisDust.noGravity = true;
                thisDust.shader = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.BlackDye), Main.LocalPlayer);
            }
            for (int j = 0; j < 100; j++)
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


        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            int buffLengthMod = 1;
            if (Main.expertMode)
            {
                buffLengthMod = 2;
            }

            target.AddBuff(BuffID.BrokenArmor, 180 / buffLengthMod, false);
            target.AddBuff(BuffID.Blackout, 600 / buffLengthMod, false);
        }
    }
}
