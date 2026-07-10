using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Lingering 3-tile-wide acid puddle left by VenomSpit impacts. Invisible projectile sold entirely by dust,
    ///borrowing the Abyss ground-mist approach (tsorcRevampPlayerUpdateLoops.SpawnAbyssMist: vanilla dust spawned
    ///just above the surface, no custom drawing) but much thicker: a dense boiling layer in the tile row sitting
    ///on the ground, and a sparser dissipating layer one tile higher so it reads as surface acid with rising gas.
    ///</summary>
    class AcidPool : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public const int PoolWidth = 48;  //3 tiles
        public const int PoolHeight = 16;
        public const int PoolLifetime = 3 * 60;

        //ai[0] = custom lifetime in ticks (0 = default PoolLifetime). AcidBody trails use 6s pools.
        int Lifetime => Projectile.ai[0] > 0f ? (int)Projectile.ai[0] : PoolLifetime;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = PoolWidth;
            Projectile.height = PoolHeight;
            Projectile.tileCollide = false;
            Projectile.aiStyle = 0;
            Projectile.penetrate = -1;
            Projectile.timeLeft = PoolLifetime;
            Projectile.light = 0.3f;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;

            //Dense boiling surface layer (the pool itself, in its own body row)
            for (int i = 0; i < 3; i++)
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(PoolWidth), Projectile.position.Y + Main.rand.NextFloat(PoolHeight * 0.6f));
                int dust = Dust.NewDust(pos, 4, 4, DustID.AncientLight, 0f, -1f, 120, Color.Purple, 1.2f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.2f;
                Main.dust[dust].velocity.Y = -0.8f;
            }
            if (Main.rand.NextBool(2))
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(PoolWidth), Projectile.position.Y + Main.rand.NextFloat(PoolHeight * 0.5f));
                int bubble = Dust.NewDust(pos, 4, 4, DustID.Venom, 0f, -1.2f, 100, default, 1.3f);
                Main.dust[bubble].noGravity = true;
                Main.dust[bubble].velocity *= 0.3f;
            }

            //Sparse dissipating gas one tile above -- fades as the pool ages
            float remaining = Projectile.timeLeft / (float)Lifetime;
            if (Main.rand.NextFloat() < 0.4f * remaining)
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(PoolWidth), Projectile.position.Y - 16f + Main.rand.NextFloat(12f));
                int gas = Dust.NewDust(pos, 4, 4, DustID.AncientLight, 0f, -1.5f, 200, Color.MediumPurple, 0.8f);
                Main.dust[gas].noGravity = true;
                Main.dust[gas].velocity *= 0.15f;
                Main.dust[gas].velocity.Y = -1.2f;
                Main.dust[gas].fadeIn = 0.2f;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Venom, 3 * 60);
        }
    }
}
