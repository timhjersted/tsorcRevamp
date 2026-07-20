using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic
{
    ///<summary>
    ///Wrath of Gold right hold, released: a long, low blade of golden lightning sweeping the ground
    ///in front of the caster — the player mirror of the boss's Lightning of the First Sun. The
    ///controller's charge was the telegraph; this strikes immediately, hitting each enemy once.
    ///ai[0] = direction (-1/1, dust drift only).
    ///</summary>
    class TomeGoldSweep : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public const int BeamLength = 640; //40 tiles
        public const int BeamHeight = 60;
        const int StrikeTicks = 10;

        int Direction => (int)Projectile.ai[0] >= 0 ? 1 : -1;

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.width = BeamLength;
            Projectile.height = BeamHeight;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; //one hit per enemy
            Projectile.aiStyle = 0;
            Projectile.timeLeft = StrikeTicks;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;

            //Dense golden lightning filling the band
            for (int i = 0; i < 20; i++)
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(BeamLength), Projectile.position.Y + Main.rand.NextFloat(BeamHeight));
                int dust = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, Direction * 3f, 0f, 50, default, Main.rand.NextFloat(1.6f, 2.4f));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = new Vector2(Direction * Main.rand.NextFloat(2f, 6f), Main.rand.NextFloat(-1f, 1f));
            }
            for (int i = 0; i < 5; i++)
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(BeamLength), Projectile.Center.Y + Main.rand.NextFloat(-8f, 8f));
                int core = Dust.NewDust(pos, 2, 2, DustID.AncientLight, Direction * 5f, 0f, 20, Color.LightGoldenrodYellow, 1.5f);
                Main.dust[core].noGravity = true;
            }
            for (int seg = 0; seg < 6; seg++)
            {
                Lighting.AddLight(new Vector2(Projectile.position.X + BeamLength * (seg + 0.5f) / 6f, Projectile.Center.Y), 0.9f, 0.8f, 0.35f);
            }
        }
    }
}
