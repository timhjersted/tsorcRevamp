using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Ice Prison pillar: one segment of the icicle cage that erupts in a ring around the player.
    ///Rises after ai[0] telegraph ticks, stands as a damaging column, then at ai[1] ticks every
    ///pillar shatters INWARD — three shards fly at the nearest player. Escape through the gap
    ///the ring leaves open before the shatter.
    ///</summary>
    class GigasPrisonPillar : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        int RiseTicks => (int)Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : 20;
        int ShatterTick => (int)Projectile.ai[1] > 0 ? (int)Projectile.ai[1] : 70;
        int Timer => (int)Projectile.localAI[0];
        bool Risen => Timer > RiseTicks;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 24;
            Projectile.height = 96;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 300;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = ShatterTick + 1;
        }

        public override bool? CanDamage()
        {
            return Risen;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;

            if (!Risen)
            {
                //Frost gathering along the pillar line
                for (int i = 0; i < 2; i++)
                {
                    Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(Projectile.width), Projectile.position.Y + Main.rand.NextFloat(Projectile.height));
                    int dust = Dust.NewDust(pos, 4, 4, DustID.Frost, 0f, -1f, 120, default, 1.1f);
                    Main.dust[dust].noGravity = true;
                }
                if (Timer == RiseTicks)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.5f, Pitch = -0.2f }, Projectile.Center);
                }
                return;
            }

            //Standing column of ice: dense edges, crystalline core
            for (int i = 0; i < 3; i++)
            {
                float edge = Main.rand.NextBool() ? 2f : Projectile.width - 2f;
                Vector2 pos = new Vector2(Projectile.position.X + edge, Projectile.position.Y + Main.rand.NextFloat(Projectile.height));
                int dust = Dust.NewDust(pos, 2, 2, DustID.IceTorch, 0f, 0f, 80, default, 1.1f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.15f;
            }
            if (Main.rand.NextBool(3))
            {
                Vector2 pos = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-4f, 4f), Projectile.position.Y + Main.rand.NextFloat(Projectile.height));
                int core = Dust.NewDust(pos, 2, 2, DustID.IceRod, 0f, 0f, 60, default, 0.9f);
                Main.dust[core].noGravity = true;
                Main.dust[core].velocity *= 0.1f;
            }
            Lighting.AddLight(Projectile.Center, 0.25f, 0.4f, 0.6f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Chilled, 2 * 60);
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.6f, Pitch = -0.3f }, Projectile.Center);
            for (int i = 0; i < 14; i++)
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(Projectile.width), Projectile.position.Y + Main.rand.NextFloat(Projectile.height));
                int dust = Dust.NewDust(pos, 4, 4, DustID.Ice, Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-2f, 2f), 60, default, 1.2f);
                Main.dust[dust].noGravity = true;
            }
            //Shatter inward: shards hunt the nearest player
            if (Main.netMode != NetmodeID.MultiplayerClient && Timer >= ShatterTick)
            {
                Player target = UsefulFunctions.GetClosestPlayer(Projectile.Center);
                if (target != null && !target.dead)
                {
                    Vector2 baseDir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                    for (int i = -1; i <= 1; i++)
                    {
                        Vector2 vel = baseDir.RotatedBy(i * 0.18f) * 8f;
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + new Vector2(0f, Main.rand.NextFloat(-30f, 30f)), vel,
                            ModContent.ProjectileType<GigasIceShard>(), (int)(Projectile.damage * 0.7f), 1f, Main.myPlayer);
                    }
                }
            }
        }
    }
}
