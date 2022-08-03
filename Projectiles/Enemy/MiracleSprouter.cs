using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class MiracleSprouter : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5; // The length of old position to be recorded
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0; // The recording mode
        }

        public override void SetDefaults()
        {
            Projectile.height = 15;
            Projectile.width = 15;
            Projectile.scale = 1.2f;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.hostile = true;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.position, 0.1f, .35f, .25f);
            Projectile.rotation = (float)Math.Atan2((double)Projectile.velocity.X, (double)Projectile.velocity.Y);
            int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 75, 0, 0, 100, default, 2.0f);
            Main.dust[dust].noGravity = true;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int projIndex = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X, Projectile.position.Y, 0, -3f, ModContent.ProjectileType<MiracleVines>(), Projectile.damage, 0f, Main.myPlayer); Projectile.active = false;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item60 with { Volume = 0.5f, Pitch = -0.1f }, Projectile.position);
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, projIndex);
                    NetMessage.SendData(MessageID.KillProjectile, -1, -1, null, this.Projectile.whoAmI);
                }
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Main.instance.LoadProjectile(Projectile.type);
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;

            // Redraw the projectile with the color not influenced by light
            Vector2 drawOrigin = new Vector2(texture.Width * 0.5f, Projectile.height * 0.5f);
            for (int k = 0; k < Projectile.oldPos.Length; k++)
            {
                Vector2 drawPos = (Projectile.oldPos[k] - Main.screenPosition) + drawOrigin + new Vector2(0f, Projectile.gfxOffY);
                Color color = Projectile.GetAlpha(lightColor) * ((Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length);
                Main.EntitySpriteDraw(texture, drawPos, null, color, Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None, 0);
            }

            return true;
        }
    }
}
