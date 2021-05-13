using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class SentenzaShot : ModProjectile
    {
        public override void SetDefaults()
        {
            projectile.width = 12;
            projectile.height = 12;
            projectile.friendly = true;
            projectile.aiStyle = 0;
            projectile.ranged = true;
            projectile.tileCollide = true;
            projectile.timeLeft = 30;
            projectile.scale = 0.85f;
            projectile.extraUpdates = 1;
        }
        public override void AI()
        {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2; // projectile faces sprite right

            if (projectile.owner == Main.myPlayer && projectile.timeLeft == 28)
            {
                for (int i = 0; i < 15; i++)
                {
                    int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 73, projectile.velocity.X * 0, projectile.velocity.Y * 0, 70, default(Color), .75f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= Main.rand.NextFloat(0.3f, 3f);
                }
            }
            {
                int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 14, projectile.velocity.X * -0.3f, projectile.velocity.Y * -0.3f, 30, default(Color), 1f);
                Main.dust[dust].noGravity = true;
            }
            {
                int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 27, projectile.velocity.X * -0.3f, projectile.velocity.Y * -0.3f, 30, default(Color), 1f);
                Main.dust[dust].noGravity = true;
            }
            if (projectile.owner == Main.myPlayer && projectile.timeLeft <= 6)
            {
                projectile.alpha += 28;

                if (projectile.alpha > 225)
                {
                    projectile.alpha = 225;
                }
            }
        }

        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if ((target.type == NPCID.SandsharkHallow) | (target.type == NPCID.Pixie) | (target.type == NPCID.Unicorn) | (target.type == NPCID.RainbowSlime) | (target.type == NPCID.Gastropod) | (target.type == NPCID.LightMummy) | (target.type == NPCID.DesertGhoulHallow) | (target.type == NPCID.IlluminantSlime) | (target.type == NPCID.IlluminantBat) | (target.type == NPCID.EnchantedSword) | (target.type == NPCID.BigMimicHallow) | (target.type == NPCID.DesertLamiaLight))
            {
                damage += 10;
            }
        }

       /* public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
        }*/

        public override void Kill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                int option = Main.rand.Next(3);
                if (option == 0)
                {
                    Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/RicochetUno").WithVolume(.6f).WithPitchVariance(.3f), projectile.Center);
                }
                else if (option == 1)
                {
                    Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/RicochetDos").WithVolume(.6f).WithPitchVariance(.3f), projectile.Center);
                }
                else if (option == 2)
                {
                    Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/RicochetTres").WithVolume(.6f).WithPitchVariance(.3f), projectile.Center);
                }
            }
        }
    }
}
