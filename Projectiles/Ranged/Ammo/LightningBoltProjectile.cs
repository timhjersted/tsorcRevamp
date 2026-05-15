using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.Projectiles.Ranged.Ammo
{
    public class LightningBoltProjectile : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.aiStyle = 1;
            Projectile.scale = 1.1f;
            Projectile.friendly = true;
            Projectile.height = 20;
            Projectile.penetrate = 2;
            Projectile.tileCollide = true;
            Projectile.width = 10;
            AIType = ProjectileID.WoodenArrowFriendly;
            Projectile.aiStyle = 1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
        }

        public override void AI()
        {
            Projectile.velocity.Y = Projectile.velocity.Y;

            Lighting.AddLight(Projectile.Center, 0.2f, 0.4f, 1f);

            if (Main.rand.NextBool(5))
            {
                Dust dust = Dust.NewDustDirect(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height,
                    221,
                    Projectile.velocity.X * 0.2f,
                    Projectile.velocity.Y * 0.2f,
                    100,
                    default,
                    1.2f
                );

                dust.noGravity = true;
                dust.velocity *= 0.5f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextFloat() <= 0.33f)
            {
                int boltDamage = (int)(damageDone * 0.25f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.position,
                    Projectile.velocity,
                    ModContent.ProjectileType<Bolt1Bolt>(),
                    boltDamage,
                    Projectile.knockBack,
                    Projectile.owner
                );

                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit53, Projectile.position);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Main.rand.NextFloat() <= 0.33f)
            {
                int boltDamage = (int)(Projectile.damage * 0.25f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.position,
                    Projectile.velocity,
                    ModContent.ProjectileType<Bolt1Bolt>(),
                    boltDamage,
                    Projectile.knockBack,
                    Projectile.owner
                );

                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit53, Projectile.position);
            }

            return true;
        }

        public override void OnKill(int timeLeft)
        {
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
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