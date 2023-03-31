using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy
{
    class HypnoticDisrupter : ModProjectile
    {

        public override void SetDefaults()
        {
            //projectile.aiStyle = 18;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.penetrate = 1;
            Projectile.hostile = true;
            Projectile.timeLeft = 360;
            Projectile.scale = 1f;
            Projectile.tileCollide = false;
            Projectile.light = 1;
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Hypnotic Disrupter");
        }

        public override void AI()
        {




            if (Projectile.ai[1] > 0 && Projectile.timeLeft > 300)
            {
                Projectile.timeLeft = 300;
                Projectile.tileCollide = true;
            }
            Projectile.rotation += 3f;

            if (Main.player[(int)Projectile.ai[0]].position.X < Projectile.position.X)
            {
                if (Projectile.velocity.X > -10) Projectile.velocity.X -= 0.1f;
            }

            if (Main.player[(int)Projectile.ai[0]].position.X > Projectile.position.X)
            {
                if (Projectile.velocity.X < 10) Projectile.velocity.X += 0.2f;
            }

            if (Main.player[(int)Projectile.ai[0]].position.Y < Projectile.position.Y)
            {
                if (Projectile.velocity.Y > -10) Projectile.velocity.Y -= 0.1f;
            }

            if (Main.player[(int)Projectile.ai[0]].position.Y > Projectile.position.Y)
            {
                if (Projectile.velocity.Y < 10) Projectile.velocity.Y += 0.1f;
            }


            Color color = new Color();
            int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y - 10), Projectile.width, Projectile.height, DustID.Shadowflame, 0, 0, 50, color, 2.0f);
            Main.dust[dust].noGravity = true;

            if (Main.rand.NextBool(2))
            {
                Lighting.AddLight((int)(Projectile.position.X / 16f), (int)(Projectile.position.Y / 16f), 0.7f, 0.2f, 0.2f);
            }
        }
        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            int buffLengthMod = 1;
            if (!Main.expertMode) //surely that was the wrong way round
            {
                buffLengthMod = 2;
            }

            target.AddBuff(BuffID.Bleeding, 600 / buffLengthMod, false); //bleeding
            target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 600 / buffLengthMod, false); //you take knockback

            if (Projectile.ai[1] < 1)
            {
                target.AddBuff(ModContent.BuffType<Crippled>(), 300 / buffLengthMod, false);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return true;
        }
    }
}