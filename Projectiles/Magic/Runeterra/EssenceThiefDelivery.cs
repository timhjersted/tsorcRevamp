using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic.Runeterra
{
    public class EssenceThiefDelivery : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 8;
        }
        public override void SetDefaults()
        {
			Projectile.width = 20;
            Projectile.height = 32;
            Projectile.scale = 0f;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 900;
            Projectile.extraUpdates = 10;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            float projSpeed = 10f;

            switch (Projectile.ai[0])
            {
                case 0:
                    {
                        Lighting.AddLight(Projectile.Center, Color.LightSteelBlue.ToVector3() * 2f);
                        Dust.NewDust(Projectile.Center, 2, 2, DustID.BlueTorch, 0, 0, 150, default, 1.5f);
                        break;
                    }
                case 1:
                    {
                        Lighting.AddLight(Projectile.Center, Color.Firebrick.ToVector3() * 2f);
                        Dust.NewDust(Projectile.Center, 2, 2, DustID.Torch, 0, 0, 150, default, 1.5f);
                        break;
                    }
                case 2:
                    {
                        Lighting.AddLight(Projectile.Center, Color.Pink.ToVector3() * 2f);
                        Dust.NewDust(Projectile.Center, 2, 2, DustID.VenomStaff, 0, 0, 150, default, 1.5f);
                        break;
                    }
            }

            Projectile.velocity = (player.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * projSpeed;
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            int frameSpeed = 5;

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Projectile.type] || Projectile.frame <= Main.projFrames[Projectile.type] / 2)
                {
                    Projectile.frame = 4;
                }
            }
            if (Projectile.Hitbox.Intersects(player.Hitbox))
            {
                Projectile.Kill();
            }
        }
        public override void OnKill(int timeLeft)
        {
            Player player = Main.player[Projectile.owner];
            player.GetModPlayer<tsorcRevampPlayer>().EssenceThief += 1;
            CombatText.NewText(player.Hitbox, Color.Lime, 1, true);
        }
    }
}