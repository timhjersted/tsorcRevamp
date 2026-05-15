using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.Projectiles.Enemy
{
    class FrozenSaw : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Frozen Orb");

        }
        public override void SetDefaults()
        {
            Projectile.aiStyle = 0;
            Projectile.hostile = true;
            Projectile.height = 34;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.width = 34;
            Projectile.timeLeft = 150;
            Projectile.light = .3f;
            Main.projFrames[Projectile.type] = 4;
            Projectile.coldDamage = true;
        }

        public override bool PreKill(int timeLeft)
        {
            Projectile.type = 645;
            return true;
        }

        public override void AI()
        {
            Projectile.rotation++;
            if (Projectile.velocity.X <= 10 && Projectile.velocity.Y <= 10 && Projectile.velocity.X >= -10 && Projectile.velocity.Y >= -10)
            {
                Projectile.velocity.X *= 1.01f;
                Projectile.velocity.Y *= 1.01f;
            }

            if (Main.rand.NextBool(2))
            {

                Lighting.AddLight((int)Projectile.position.X / 16, (int)Projectile.position.Y / 16, 0.1f, 0.5f, 0.8f);
                return;

            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 2)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 4)
            {
                Projectile.frame = 0;
            }

        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            int buffLengthMod = 1;
            if (Main.expertMode)
            {
                buffLengthMod = 2;
            }

            target.AddBuff(BuffID.BrokenArmor, 300 / buffLengthMod);
            if (Main.rand.NextBool(10))
            {
                target.AddBuff(BuffID.Silenced, 180 / buffLengthMod);
                target.AddBuff(BuffID.Weak, 180 / buffLengthMod);
                target.AddBuff(BuffID.Slow, 300 / buffLengthMod);
            }
        }

        public override bool PreDraw(Player player, ref Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                Color.White, 
                Projectile.rotation,
                frame.Size() / 2f,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            return false;
        }
    }
}
