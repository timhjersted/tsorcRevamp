using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gigas halo sun: a small sun orbiting the giant's head. Harmless while orbiting; at its detach
    ///tick it launches at the player with lazy homing. If the Gigas is poise-broken while suns are
    ///still orbiting, the whole remaining halo scatters wildly instead (no homing) — the stagger
    ///visibly disrupts the spell. ai[0] = parent NPC whoAmI. ai[1] = detach tick + orbit-slot/10
    ///(fractional part encodes the sun's angle slot so it syncs in multiplayer).
    ///</summary>
    class GigasHaloSun : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float OrbitRadius = 58f;
        const float OrbitSpeed = 0.045f;
        const int Mode_Orbit = 0;
        const int Mode_Homing = 1;
        const int Mode_Scatter = 2;

        int ParentIndex => (int)Projectile.ai[0];
        int DetachTick => (int)Projectile.ai[1];
        float SlotAngle => (float)System.Math.Round((Projectile.ai[1] - DetachTick) * 10f) / 6f * MathHelper.TwoPi;
        int Mode { get => (int)Projectile.localAI[1]; set => Projectile.localAI[1] = value; }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 900;
            Projectile.light = 0.5f;
        }

        public override bool? CanDamage()
        {
            return Mode != Mode_Orbit;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;

            if (Mode == Mode_Orbit)
            {
                NPC parent = ParentIndex >= 0 && ParentIndex < Main.maxNPCs ? Main.npc[ParentIndex] : null;
                if (parent == null || !parent.active || parent.type != ModContent.NPCType<NPCs.Enemies.Gigas>())
                {
                    Detach(Mode_Homing);
                }
                else if (parent.GetGlobalNPC<tsorcRevampGlobalNPC>().StaggerTimer > 0)
                {
                    //Poise break: the halo scatters — the interrupted spell sprays harmlessly-aimed suns
                    Detach(Mode_Scatter);
                    Projectile.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(6f, 9f);
                    Projectile.timeLeft = 100;
                }
                else
                {
                    float angle = SlotAngle + Projectile.localAI[0] * OrbitSpeed;
                    Projectile.Center = parent.Center + new Vector2(0f, -70f) + angle.ToRotationVector2() * OrbitRadius;
                    Projectile.velocity = Vector2.Zero;
                    if (Projectile.localAI[0] >= DetachTick)
                    {
                        Detach(Mode_Homing);
                        Player target = UsefulFunctions.GetClosestPlayer(Projectile.Center);
                        if (target != null && !target.dead)
                        {
                            Projectile.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * 7f;
                        }
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.4f, Pitch = 0.6f }, Projectile.Center);
                    }
                }
            }
            else if (Mode == Mode_Homing)
            {
                Player target = UsefulFunctions.GetClosestPlayer(Projectile.Center);
                if (target != null && !target.dead)
                {
                    UsefulFunctions.SmoothHoming(Projectile, target.Center, 0.07f, 9f, target.velocity, false);
                }
            }

            //The sun itself: a bright golden core with drifting flame
            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, 0f, 0f, 80, default, 1.6f);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity *= 0.15f;
            if (Main.rand.NextBool(3))
            {
                int sparkle = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldCoin, 0f, -0.5f, 0, default, 0.9f);
                Main.dust[sparkle].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.6f, 0.5f, 0.2f);
        }

        void Detach(int mode)
        {
            Mode = mode;
            Projectile.tileCollide = true;
            if (Projectile.timeLeft > 240)
            {
                Projectile.timeLeft = 240;
            }
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, vel.X, vel.Y, 80, default, 1.3f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
