using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Grave hand: a shadow claw erupting from the ground where the dead lie. Dark simmer telegraph
    ///for ai[0] ticks, then the hand claws upward — pure dust sculpture (dense shadowflame column
    ///with brighter "finger" glints at the top), damaging + Slowed while it grips. Spawned with its
    ///bottom on the ground.
    ///</summary>
    class NecroGraveHand : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const int RiseTicks = 6;
        const int HoldTicks = 40;
        const int SinkTicks = 8;

        int TelegraphTicks => (int)Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : 28;
        int Timer => (int)Projectile.localAI[0];
        bool Erupted => Timer > TelegraphTicks;
        bool Sinking => Timer > TelegraphTicks + RiseTicks + HoldTicks;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 22;
            Projectile.height = 46;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 120;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = TelegraphTicks + RiseTicks + HoldTicks + SinkTicks;
        }

        public override bool? CanDamage()
        {
            return Erupted && !Sinking;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;
            float bottom = Projectile.position.Y + Projectile.height;

            if (!Erupted)
            {
                //The grave stirring: dark simmer on the ground
                for (int i = 0; i < 2; i++)
                {
                    Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(Projectile.width), bottom - 8f);
                    int dust = Dust.NewDust(pos, 4, 4, DustID.Shadowflame, 0f, -1f, 120, default, 1f);
                    Main.dust[dust].noGravity = true;
                }
                return;
            }
            if (Timer == TelegraphTicks + 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath2 with { Volume = 0.35f, Pitch = 0.4f }, Projectile.Center);
                for (int i = 0; i < 8; i++)
                {
                    int dust = Dust.NewDust(new Vector2(Projectile.position.X, bottom - 12f), Projectile.width, 12, DustID.Shadowflame, Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-4f, -1f), 80, default, 1.3f);
                    Main.dust[dust].noGravity = true;
                }
            }
            if (!Sinking)
            {
                //The hand: dense shadow column with grasping finger-glints at the top
                float riseProgress = MathHelper.Min(1f, (Timer - TelegraphTicks) / (float)RiseTicks);
                float handTop = bottom - Projectile.height * riseProgress;
                for (int i = 0; i < 3; i++)
                {
                    float edge = Main.rand.NextBool() ? 2f : Projectile.width - 2f;
                    Vector2 pos = new Vector2(Projectile.position.X + edge, handTop + Main.rand.NextFloat(bottom - handTop));
                    int dust = Dust.NewDust(pos, 2, 2, DustID.Shadowflame, 0f, 0f, 90, default, 1.1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= 0.15f;
                }
                if (Main.rand.NextBool(2))
                {
                    //Fingers curling at the top
                    Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(Projectile.width), handTop + Main.rand.NextFloat(8f));
                    int finger = Dust.NewDust(pos, 4, 4, DustID.Shadowflame, 0f, -1.5f, 60, default, 1.3f);
                    Main.dust[finger].noGravity = true;
                }
                Lighting.AddLight(Projectile.Center, 0.2f, 0.1f, 0.3f);
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Slow, 90); //the dead grab your ankles
        }
    }
}
