using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee.Broadswords
{
    class AncientFireSwordSlash : ModProjectile
    {
        public Vector2 Velocity;
        public int ProjectileLifetime;
        public int Frames = 6;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = Frames;
            ProjectileID.Sets.NoMeleeSpeedVelocityScaling[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 50;
            Projectile.penetrate = -1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ownerHitCheck = true;
            Projectile.DamageType = DamageClass.Melee;
            ProjectileLifetime = (int)(20 / Main.player[Projectile.owner].GetTotalAttackSpeed(DamageClass.Melee));
            Projectile.timeLeft = ProjectileLifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        public override void OnSpawn(IEntitySource source)
        {
            Velocity = Projectile.velocity;
            Projectile.spriteDirection = Main.player[Projectile.owner].direction;
            Projectile.velocity = Vector2.Zero;
            Projectile.damage /= 2;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 20; i++)
            {
                int dust = Dust.NewDust(new Vector2(Projectile.position.X - 11, Projectile.position.Y - 11), Projectile.width + 22, Projectile.height + 22, DustID.Torch, 0, 0, 70, default, 1f);
                Main.dust[dust].noGravity = true;
            }
        }

        public bool ReverseAnimation = false;
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.rotation = Velocity.ToRotation();
            if (Projectile.spriteDirection == -1)
            {
                Projectile.rotation -= (float)Math.PI;
            }

            player.heldProj = Projectile.whoAmI;

            // Keep locked onto the player, but extend further based on the given velocity (Requires ShouldUpdatePosition returning false to work)
            Vector2 playerCenter = player.RotatedRelativePoint(player.MountedCenter, reverseRotation: false, addGfxOffY: false);
            Projectile.Center = playerCenter + Velocity;

            int frameSpeed = (int)Math.Round((double)ProjectileLifetime / ((double)Frames * 2));
            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                if (!ReverseAnimation)
                {
                    Projectile.frame++;
                }
                else
                {
                    Projectile.frame--;
                }
                if (Projectile.frame >= Main.projFrames[Type])
                {
                    ReverseAnimation = true;
                    Projectile.frame -= 2;
                }
            }
        }
        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            hitbox.Inflate(14, 0);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.OnFire, 5 * 60);
            }
        }
    }
}
