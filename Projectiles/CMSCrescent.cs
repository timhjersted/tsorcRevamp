using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class CMSCrescent : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Melee/MLGSCrescent";

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = 3;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
        }
       

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Buffs.DispelShadow>(), 36000);
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                NetMessage.SendData(MessageID.AddNPCBuff, number: target.whoAmI, number2: ModContent.BuffType<Buffs.DispelShadow>(), number3: 36000);
                ModPacket shadowPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                shadowPacket.Write((byte)tsorcPacketID.DispelShadow);
                shadowPacket.Write(target.whoAmI);
                shadowPacket.Send();
            }
            if (Main.rand.NextBool(10))
            {
              target.AddBuff(BuffID.Ichor, 1200);
            }
            
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Color color = Color.White * .8f;

            if (Projectile.ai[0] > 2)
            {
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, texture.Bounds, color, Projectile.rotation, texture.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            }

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 20; i++)
            {
                int dust = Dust.NewDust(new Vector2(Projectile.position.X - 11, Projectile.position.Y - 11), Projectile.width + 22, Projectile.height + 22, 89, 0, 0, 70, default(Color), 1f);
                Main.dust[dust].noGravity = true;
            }
        }


        public override void AI()
        {
            Projectile.ai[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation();

            Projectile.velocity *= 1.2f;

            if (Projectile.ai[0] > 2)
            {

                Lighting.AddLight(Projectile.position, 0.0452f, 0.21f, 0.1f);

                for (int d = 0; d < 2; d++)
                {
                    int dust = Dust.NewDust(new Vector2(Projectile.position.X - 11, Projectile.position.Y - 11), Projectile.width + 22, Projectile.height + 22, 89, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = true;

                }

                for (int d = 0; d < 2; d++)
                {
                    int dust = Dust.NewDust(new Vector2(Projectile.position.X - 11, Projectile.position.Y - 11), Projectile.width + 22, Projectile.height + 22, 110, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                }

                if (++Projectile.frameCounter >= 3)
                {
                    Projectile.frameCounter = 0;
                    if (++Projectile.frame > 2)
                    {
                        Projectile.Kill();
                    }
                }
            }

        }
    }
}
