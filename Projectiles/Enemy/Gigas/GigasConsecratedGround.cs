using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gigas consecrated ground: a lingering patch of golden fire left by slam landings and solar
    ///boulder impacts. Same invisible-projectile/dust approach as AcidPool, in holy gold.
    ///ai[0] = custom lifetime in ticks (0 = default).
    ///</summary>
    class GigasConsecratedGround : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public const int PatchWidth = 64;  //4 tiles
        public const int PatchHeight = 16;
        public const int PatchLifetime = 5 * 60;

        int Lifetime => Projectile.ai[0] > 0f ? (int)Projectile.ai[0] : PatchLifetime;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = PatchWidth;
            Projectile.height = PatchHeight;
            Projectile.tileCollide = false;
            Projectile.aiStyle = 0;
            Projectile.penetrate = -1;
            Projectile.timeLeft = PatchLifetime;
            Projectile.light = 0.4f;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;

            //Low burning layer of golden flame on the surface
            for (int i = 0; i < 2; i++)
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(PatchWidth), Projectile.position.Y + Main.rand.NextFloat(PatchHeight * 0.6f));
                int dust = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, 0f, -1f, 100, default, 1.3f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.2f;
                Main.dust[dust].velocity.Y = -1f;
            }
            //Occasional bright sparkle drifting up — fades as the patch ages
            float remaining = Projectile.timeLeft / (float)Lifetime;
            if (Main.rand.NextFloat() < 0.5f * remaining)
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(PatchWidth), Projectile.position.Y - 8f + Main.rand.NextFloat(10f));
                int sparkle = Dust.NewDust(pos, 4, 4, DustID.GoldCoin, 0f, -1.5f, 0, default, 0.9f);
                Main.dust[sparkle].noGravity = true;
                Main.dust[sparkle].velocity *= 0.2f;
                Main.dust[sparkle].velocity.Y = -1.4f;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 3 * 60);
        }
    }
}
