using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Sandstorm : ModProjectile
    {

        public override string Texture => "tsorcRevamp/Projectiles/Sand";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Sandstorm");
        }
        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.scale = 1f;
            Projectile.alpha = 255;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 3600;
            Projectile.friendly = true;
            Projectile.penetrate = 4;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 6;
        }
        public override void AI()
        {
            if (Projectile.timeLeft > 60)
            {
                Projectile.timeLeft = 60;
            }
            if (Projectile.ai[0] > 7f)
            {
                float num152 = 1f;
                if (Projectile.ai[0] == 8f)
                {
                    num152 = 0.25f;
                }
                else
                {
                    if (Projectile.ai[0] == 9f)
                    {
                        num152 = 0.5f;
                    }
                    else
                    {
                        if (Projectile.ai[0] == 10f)
                        {
                            num152 = 0.75f;
                        }
                    }
                }
                Projectile.ai[0] += 1f;
                if (Main.rand.NextBool(2))
                {
                    for (int i = 0; i < 1; i++)
                    {
                        int num155 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 10, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default(Color), 1f);
                        int dust2 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y - 10), Projectile.width, Projectile.height, 10, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default(Color), 1f);
                        int dust3 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y + 10), Projectile.width, Projectile.height, 10, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default(Color), 1f);
                        if (!Main.rand.NextBool(3))
                        {
                            Main.dust[num155].noGravity = true;
                            Main.dust[num155].scale *= 3f;
                            Dust dustnum155 = Main.dust[num155];
                            dustnum155.velocity.X *= 2f;
                            Dust dustnum155_2 = Main.dust[num155];
                            dustnum155_2.velocity.Y *= 2f;
                            Main.dust[dust2].noGravity = true;
                            Main.dust[dust2].scale *= 3f;
                            Dust dustDust2 = Main.dust[dust2];
                            dustDust2.velocity.X *= 2f;
                            Dust dustDust2_2 = Main.dust[dust2];
                            dustDust2_2.velocity.Y *= 2f;
                            Main.dust[dust3].noGravity = true;
                            Main.dust[dust3].scale *= 3f;
                            Dust dustDust3 = Main.dust[dust2];
                            dustDust3.velocity.X *= 2f;
                            Dust dustDust3_2 = Main.dust[dust3];
                            dustDust3_2.velocity.Y *= 2f;
                            if (Main.rand.NextBool(5) && Main.myPlayer == Projectile.owner)
                            {
                                Vector2 Position = new Vector2(dustnum155.position.X, dustnum155.position.Y);
                                Vector2 Velocity = new Vector2(dustnum155.velocity.X, dustnum155.velocity.Y);
                                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Position, Velocity, ModContent.ProjectileType<Sand>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 60, 0.5f);
                            }
                            if (Main.rand.NextBool(5) && Main.myPlayer == Projectile.owner)
                            {
                                Vector2 Position = new Vector2(dustDust2.position.X, dustDust2.position.Y);
                                Vector2 Velocity = new Vector2(dustDust2.velocity.X, dustDust2.velocity.Y);
                                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Position, Velocity, ModContent.ProjectileType<Sand>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 60, 0.5f);
                            }
                            if (Main.rand.NextBool(5) && Main.myPlayer == Projectile.owner)
                            {
                                Vector2 Position = new Vector2(dustDust3.position.X, dustDust3.position.Y);
                                Vector2 Velocity = new Vector2(dustDust3.position.X, dustDust3.position.Y);
                                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Position, Velocity, ModContent.ProjectileType<Sand>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 60, 0.5f);
                            }
                        }
                        Main.dust[num155].scale *= 1.5f;
                        Dust dust155 = Main.dust[num155];
                        dust155.velocity.X *= 1.2f;
                        Dust dust155_2 = Main.dust[num155];
                        dust155_2.velocity.Y *= 1.2f;
                        Main.dust[num155].scale *= num152;
                        Main.dust[dust2].scale *= 1.5f;
                        Dust dustDust2_3 = Main.dust[dust2];
                        dustDust2_3.velocity.X *= 1.2f;
                        Dust dustDust2_4 = Main.dust[dust2];
                        dustDust2_4.velocity.Y *= 1.2f;
                        Main.dust[dust2].scale *= num152;
                        Main.dust[dust3].scale *= 1.5f;
                        Dust dustDust3_3 = Main.dust[dust3];
                        dustDust3_3.velocity.X *= 1.2f;
                        Dust dustDust3_4 = Main.dust[dust3];
                        dustDust3_4.velocity.Y *= 1.2f;
                        Main.dust[dust3].scale *= num152;
                        if (Main.rand.NextBool(5) && Main.myPlayer == Projectile.owner)
                        {
                            Vector2 Position = new Vector2(dust155.position.X, dust155.position.Y);
                            Vector2 Velocity = new Vector2(dust155.velocity.X, dust155.velocity.Y);
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Position, Velocity, ModContent.ProjectileType<Sand>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 60, 0.5f);
                        }
                        if (Main.rand.NextBool(5) && Main.myPlayer == Projectile.owner)
                        {
                            Vector2 Position = new Vector2(dustDust2_3.position.X, dustDust2_3.position.Y);
                            Vector2 Velocity = new Vector2(dustDust2_3.velocity.X, dustDust2_3.velocity.Y);
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Position, Velocity, ModContent.ProjectileType<Sand>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 60, 0.5f);
                        }
                        if (Main.rand.NextBool(5) && Main.myPlayer == Projectile.owner)
                        {
                            Vector2 Position = new Vector2(dustDust3_3.position.X, dustDust3_3.position.Y);
                            Vector2 Velocity = new Vector2(dustDust3_3.velocity.X, dustDust3_3.velocity.Y);
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Position, Velocity, ModContent.ProjectileType<Sand>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 60, 0.5f);
                        }
                    }
                }
            }
            else
            {
                Projectile.ai[0] += 1f;
            }
            Projectile.rotation += 0.3f * (float)Projectile.direction;
            return;
        }
    }
}
