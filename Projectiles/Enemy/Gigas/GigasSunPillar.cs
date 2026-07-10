using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gigas sun pillar: a column of judgment light. Spawned with its bottom on the ground under the
    ///player. Telegraphs with faint rising motes for ai[0] ticks, then the beam slams down — dense
    ///golden column, damaging for the strike window only. Stationary; dodge sideways.
    ///</summary>
    class GigasSunPillar : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public const int PillarWidth = 44;
        public const int PillarHeight = 480;
        const int StrikeTicks = 25;

        int TelegraphTicks => (int)Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : 45;
        bool Striking => Projectile.localAI[0] > TelegraphTicks;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = PillarWidth;
            Projectile.height = PillarHeight;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 300;
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
                //Ground marker: a bright simmering pool of light at the base
                for (int i = 0; i < 2; i++)
                {
                    Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(PillarWidth), groundY - 10f);
                    int dust = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, 0f, -1.5f, 100, default, 1.3f);
                    Main.dust[dust].noGravity = true;
                }
                //Faint motes rising through the whole column — the read for "get out of the light"
                if (Main.rand.NextBool(2))
                {
                    Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(PillarWidth), Projectile.position.Y + Main.rand.NextFloat(PillarHeight));
                    int mote = Dust.NewDust(pos, 4, 4, DustID.GoldCoin, 0f, -2f, 0, default, 0.9f);
                    Main.dust[mote].noGravity = true;
                    Main.dust[mote].velocity = new Vector2(0f, -2.5f);
                }
                Lighting.AddLight(new Vector2(Projectile.Center.X, groundY - 20f), 0.5f, 0.45f, 0.15f);

                if (Projectile.localAI[0] >= TelegraphTicks)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Thunder with { Volume = 0.45f, Pitch = 0.3f }, new Vector2(Projectile.Center.X, groundY));
                }
                return;
            }

            //Strike: dense column of light along the full height
            for (int i = 0; i < 10; i++)
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(PillarWidth), Projectile.position.Y + Main.rand.NextFloat(PillarHeight));
                int dust = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, 0f, 0f, 60, default, Main.rand.NextFloat(1.5f, 2.2f));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-4f, -1f));
            }
            //White-hot core
            for (int i = 0; i < 3; i++)
            {
                Vector2 pos = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-6f, 6f), Projectile.position.Y + Main.rand.NextFloat(PillarHeight));
                int core = Dust.NewDust(pos, 2, 2, DustID.AncientLight, 0f, 0f, 20, Color.LightGoldenrodYellow, 1.6f);
                Main.dust[core].noGravity = true;
                Main.dust[core].velocity *= 0.2f;
            }
            //Impact splash at the base
            if (Main.rand.NextBool(2))
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(PillarWidth), groundY - 8f);
                int splash = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, Main.rand.NextFloat(-3f, 3f), -2f, 80, default, 1.5f);
                Main.dust[splash].noGravity = true;
            }
            for (int seg = 0; seg < 4; seg++)
            {
                Lighting.AddLight(new Vector2(Projectile.Center.X, Projectile.position.Y + PillarHeight * (seg + 0.5f) / 4f), 1f, 0.9f, 0.4f);
            }
        }
    }
}
