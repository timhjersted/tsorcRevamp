using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gigas lightning sweep: a long, low horizontal blade of golden lightning hugging the ground in
    ///front of the giant. Telegraphs with a line of sparkles along its full length for ai[0] ticks
    ///(dodge by jumping over it or rolling through), then flashes into the damaging sweep.
    ///Spawned as a fixed rectangle by the NPC; ai[1] = direction (only used for dust drift).
    ///</summary>
    class GigasSweepBeam : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public const int BeamLength = 720; //45 tiles
        public const int BeamHeight = 48;  //3 tiles — jumpable
        const int StrikeTicks = 12;

        int TelegraphTicks => (int)Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : 40;
        int Direction => (int)Projectile.ai[1] >= 0 ? 1 : -1;
        bool Striking => Projectile.localAI[0] > TelegraphTicks;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = BeamLength;
            Projectile.height = BeamHeight;
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

            if (!Striking)
            {
                //Sparkle line along the beam path, brighter toward the strike
                float progress = Projectile.localAI[0] / (float)TelegraphTicks;
                int count = 2 + (int)(progress * 3f);
                for (int i = 0; i < count; i++)
                {
                    Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(BeamLength), Projectile.position.Y + Main.rand.NextFloat(BeamHeight));
                    int sparkle = Dust.NewDust(pos, 4, 4, DustID.GoldCoin, 0f, 0f, 0, default, 0.9f + progress * 0.4f);
                    Main.dust[sparkle].noGravity = true;
                    Main.dust[sparkle].velocity = new Vector2(Direction * 0.5f, -0.6f);
                }
                if (Projectile.localAI[0] >= TelegraphTicks)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Thunder with { Volume = 0.5f, Pitch = 0.2f }, Projectile.Center);
                }
                return;
            }

            //Strike: dense golden lightning filling the band
            for (int i = 0; i < 22; i++)
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(BeamLength), Projectile.position.Y + Main.rand.NextFloat(BeamHeight));
                int dust = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, Direction * 3f, 0f, 50, default, Main.rand.NextFloat(1.6f, 2.4f));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = new Vector2(Direction * Main.rand.NextFloat(2f, 6f), Main.rand.NextFloat(-1f, 1f));
            }
            //White-hot core streaks
            for (int i = 0; i < 5; i++)
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(BeamLength), Projectile.Center.Y + Main.rand.NextFloat(-8f, 8f));
                int core = Dust.NewDust(pos, 2, 2, DustID.AncientLight, Direction * 5f, 0f, 20, Color.LightGoldenrodYellow, 1.5f);
                Main.dust[core].noGravity = true;
            }
            for (int seg = 0; seg < 6; seg++)
            {
                Lighting.AddLight(new Vector2(Projectile.position.X + BeamLength * (seg + 0.5f) / 6f, Projectile.Center.Y), 1f, 0.9f, 0.4f);
            }
        }
    }
}
