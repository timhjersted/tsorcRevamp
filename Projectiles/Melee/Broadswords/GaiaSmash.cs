using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee.Broadswords
{
    public class GaiaSmash : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 100;
            Projectile.height = 180;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 2; 
            Projectile.DamageType = DamageClass.Melee;
            Projectile.hide = true;
        }

        public override void AI()
        {
            for (int i = 0; i < 50; i++)
            {
                float spawnX = Main.rand.NextFloat(-25f, 25f); 
                float spawnY = Main.rand.NextFloat(20f, 40f); 

                Vector2 spawnPos = Projectile.Center + new Vector2(spawnX, spawnY);

                float angle = -MathHelper.PiOver2 + Main.rand.NextFloat(-0.18f, 0.18f); 

                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(-0.5f, 8.5f);

                int[] dustChoices = new int[]
                {
                    DustID.Stone,
                    DustID.Dirt,
                    DustID.Iron, 
                    DustID.Copper,
                    DustID.GoldFlame
                };

                int chosenDust = dustChoices[Main.rand.Next(dustChoices.Length)];

                Dust d = Dust.NewDustPerfect(
                    spawnPos,
                    chosenDust,
                    velocity,
                    80,
                    default,
                    Main.rand.NextFloat(1.5f, 1.7f)
                );

                d.noGravity = false;
            }
        }
    }
}
