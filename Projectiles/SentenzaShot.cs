using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class SentenzaShot : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.aiStyle = 0;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 37;
            Projectile.scale = 0.85f;
            Projectile.extraUpdates = 1;
        }
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2; // projectile faces sprite right

            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft == 35)
            {
                for (int i = 0; i < 15; i++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 73, Projectile.velocity.X * 0, Projectile.velocity.Y * 0, 70, default(Color), .75f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= Main.rand.NextFloat(0.3f, 3f);
                }
            }
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 14, Projectile.velocity.X * -0.3f, Projectile.velocity.Y * -0.3f, 30, default(Color), 1f);
                Main.dust[dust].noGravity = true;
            }
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 27, Projectile.velocity.X * -0.3f, Projectile.velocity.Y * -0.3f, 30, default(Color), 1f);
                Main.dust[dust].noGravity = true;
            }

            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft == 3)
            {
                for (int d = 0; d < 10; d++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 27, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                }
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, new Rectangle(0, Projectile.frame, 14, 38), Color.White, Projectile.rotation, new Vector2(7, 0), Projectile.scale, SpriteEffects.None, 0);

            return false;
        }

        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if ((target.type == NPCID.SandsharkHallow) | (target.type == NPCID.Pixie) | (target.type == NPCID.Unicorn) | (target.type == NPCID.RainbowSlime) | (target.type == NPCID.Gastropod) | (target.type == NPCID.LightMummy) | (target.type == NPCID.DesertGhoulHallow) | (target.type == NPCID.IlluminantSlime) | (target.type == NPCID.IlluminantBat) | (target.type == NPCID.EnchantedSword) | (target.type == NPCID.BigMimicHallow) | (target.type == NPCID.DesertLamiaLight))
            {
                damage += 10;
            }
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            for (int d = 0; d < 15; d++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 27, Projectile.velocity.X, Projectile.velocity.Y, 30, default(Color), 1f);
                Main.dust[dust].noGravity = true;
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            for (int d = 0; d < 15; d++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 27, Projectile.velocity.X, Projectile.velocity.Y, 30, default(Color), 1f);
                Main.dust[dust].noGravity = true;
            }

            return true;
        }
        public override void Kill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                int option = Main.rand.Next(3);
                if (option == 0)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("Sounds/Custom/RicochetUno").WithVolume(.6f).WithPitchVariance(.3f), Projectile.Center);
                }
                else if (option == 1)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("Sounds/Custom/RicochetDos").WithVolume(.6f).WithPitchVariance(.3f), Projectile.Center);
                }
                else if (option == 2)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("Sounds/Custom/RicochetTres").WithVolume(.6f).WithPitchVariance(.3f), Projectile.Center);
                }
            }
        }
    }
}
