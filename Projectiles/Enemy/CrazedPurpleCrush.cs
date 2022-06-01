using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class CrazedPurpleCrush : ModProjectile
    {
        public override void SetDefaults()
        {

            Projectile.width = 16;
            //projectile.aiStyle = 24;
            Projectile.hostile = true;
            Projectile.height = 16;
            Projectile.scale = 1;
            Projectile.tileCollide = false;
            Projectile.damage = 25;
            //projectile.aiPretendType = 94;
            //projectile.timeLeft = 100;
            Projectile.light = 0.8f;
            Main.projFrames[Projectile.type] = 1;

            drawOriginOffsetX = 12;
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Purple Crush");
        }

        public override bool PreKill(int timeLeft)
        {
            Projectile.type = 44; //killpretendtype
            return true;
        }

        public override void AI()
        {

            Color color = new Color();
            int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y - 10), Projectile.width, Projectile.height, DustID.Shadowflame, 0, 0, 100, color, 1.0f);
            Main.dust[dust].noGravity = true;

            Projectile.rotation++;

            if (Projectile.velocity.X <= 10 && Projectile.velocity.Y <= 10 && Projectile.velocity.X >= -10 && Projectile.velocity.Y >= -10)
            {
                //     projectile.velocity.X *= 1.01f;
                //     projectile.velocity.Y *= 1.01f;
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            int buffLengthMod = 1;
            if (Main.expertMode)
            {
                buffLengthMod = 2;
            }
            target.AddBuff(BuffID.Poisoned, 300 / buffLengthMod, false); //poisoned
            target.AddBuff(ModContent.BuffType<Buffs.Crippled>(), 300 / buffLengthMod, false); //crippled
            target.AddBuff(BuffID.Bleeding, 300 / buffLengthMod, false); //bleeding
        }
    }
}