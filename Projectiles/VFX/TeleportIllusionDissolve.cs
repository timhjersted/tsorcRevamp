using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.VFX
{
    /// <summary>Networked one-shot visual used when a teleport illusion expires.</summary>
    public class TeleportIllusionDissolve : ModProjectile
    {
        public override string Texture => "tsorcRevamp/NPCs/Puppets/PuppetPlaceholder";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 10;
        }

        public override void AI()
        {
            if (Main.dedServ || Projectile.localAI[0] != 0f)
            {
                return;
            }

            Projectile.localAI[0] = 1f;
            for (int i = 1; i <= 3; i++)
            {
                Vector2 goreVelocity = Main.rand.NextVector2Circular(2.2f, 2.2f) + new Vector2(0f, -1.5f);
                Gore.NewGore(Projectile.GetSource_FromThis(), Projectile.Center, goreVelocity,
                    Mod.Find<ModGore>($"Cloud Gore {i}").Type, Main.rand.NextFloat(0.8f, 1.15f));
            }

            for (int i = 0; i < 32; i++)
            {
                bool blue = Main.rand.NextBool();
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(14f, 24f),
                    blue ? DustID.BlueTorch : DustID.Cloud,
                    Main.rand.NextVector2Circular(3.2f, 3.2f), 120,
                    blue ? Color.LightBlue : Color.White, Main.rand.NextFloat(1.1f, 1.8f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
        public override bool? CanDamage() => false;
    }
}
