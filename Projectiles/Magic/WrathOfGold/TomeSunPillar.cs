using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic
{
    ///<summary>
    ///Wrath of Gold right tap: a friendly column of judgment light slamming down at the cursor's
    ///ground. Brief rising-mote telegraph, then the beam — multi-tick damage inside the column via
    ///a local hit cooldown. ai[0] = telegraph ticks. Player mirror of the boss's Sun Pillar.
    ///</summary>
    class TomeSunPillar : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public const int PillarWidth = 44;
        public const int PillarHeight = 480;
        const int StrikeTicks = 15;

        int TelegraphTicks => (int)Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : 20;
        bool Striking => Projectile.localAI[0] > TelegraphTicks;

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.width = PillarWidth;
            Projectile.height = PillarHeight;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = TelegraphTicks + StrikeTicks;
        }

        public override bool? CanDamage()
        {
            return Striking;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;
            float groundY = Projectile.position.Y + Projectile.height;

            if (!Striking)
            {
                //Bright ground marker + faint motes rising through the column
                for (int i = 0; i < 2; i++)
                {
                    Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(PillarWidth), groundY - 10f);
                    int dust = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, 0f, -1.5f, 100, default, 1.3f);
                    Main.dust[dust].noGravity = true;
                }
                if (Main.rand.NextBool(2))
                {
                    Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(PillarWidth), Projectile.position.Y + Main.rand.NextFloat(PillarHeight));
                    int mote = Dust.NewDust(pos, 4, 4, DustID.GoldCoin, 0f, -2f, 0, default, 0.9f);
                    Main.dust[mote].noGravity = true;
                    Main.dust[mote].velocity = new Vector2(0f, -2.5f);
                }
                if (Projectile.localAI[0] >= TelegraphTicks)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Thunder with { Volume = 0.4f, Pitch = 0.4f }, new Vector2(Projectile.Center.X, groundY));
                }
                return;
            }

            //Strike: dense golden column with a white-hot core
            for (int i = 0; i < 9; i++)
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(PillarWidth), Projectile.position.Y + Main.rand.NextFloat(PillarHeight));
                int dust = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, 0f, 0f, 60, default, Main.rand.NextFloat(1.5f, 2.2f));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-4f, -1f));
            }
            for (int i = 0; i < 3; i++)
            {
                Vector2 pos = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-6f, 6f), Projectile.position.Y + Main.rand.NextFloat(PillarHeight));
                int core = Dust.NewDust(pos, 2, 2, DustID.AncientLight, 0f, 0f, 20, Color.LightGoldenrodYellow, 1.5f);
                Main.dust[core].noGravity = true;
                Main.dust[core].velocity *= 0.2f;
            }
            for (int seg = 0; seg < 4; seg++)
            {
                Lighting.AddLight(new Vector2(Projectile.Center.X, Projectile.position.Y + PillarHeight * (seg + 0.5f) / 4f), 0.9f, 0.8f, 0.35f);
            }
        }
    }
}
