using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class EnemySpellGreatPoisonStrikeBall : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Enemy Spell Great Poison Strike Ball");
        }
        public override void SetDefaults()
        {
            Projectile.aiStyle = 23;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.light = 0.8f;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.scale = 1f;
            Projectile.tileCollide = true;
        }


        #region Kill
        public override void OnKill(int timeLeft)
        {
            if (!Projectile.active)
                return;

            Projectile.timeLeft = 0;

            if (Projectile.owner == Main.myPlayer)
            {
                int poisonball = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    new Vector2(Projectile.position.X + Projectile.width, Projectile.position.Y + Projectile.height),
                    Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatPoisonStrike>(),
                    Projectile.damage,
                    1f,
                    Projectile.owner);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    new Vector2(Projectile.position.X + Projectile.width, Projectile.position.Y + Projectile.height),
                    Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatPoisonStrike>(),
                    Projectile.damage,
                    1f,
                    Projectile.owner);

                Vector2 dustOrigin = new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y);
                int dustIndex = Dust.NewDust(dustOrigin, Projectile.width, Projectile.height, 44, 0f, 0f, 100, default, 2f);
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].velocity *= 2f;

                dustIndex = Dust.NewDust(dustOrigin, Projectile.width, Projectile.height, 44, 0f, 0f, 100, default, 1f);

                if (Main.netMode == 2)
                {
                    NetMessage.SendData(27, -1, -1, null, poisonball, 0f, 0f, 0f, 0);
                }
            }

            Projectile.active = false;
        }
        #endregion

    }
}
