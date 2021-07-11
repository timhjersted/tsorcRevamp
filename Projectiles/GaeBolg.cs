using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

//using tsorcRevamp.Dusts;

namespace tsorcRevamp.Projectiles {
    class GaeBolg : ModProjectile {

        public override void SetDefaults() {
			projectile.width = 45;
			projectile.height = 45;
			projectile.aiStyle = 19;
			projectile.penetrate = 10;
			projectile.timeLeft = 3600;
			projectile.friendly = true; //can hit enemies
			projectile.hostile = false; //can hit player / friendly NPCs
			projectile.ownerHitCheck = false;
			projectile.melee = true;
			projectile.tileCollide = false;
			projectile.hide = true;
			//projectile.usesLocalNPCImmunity = true;
			//projectile.localNPCHitCooldown = 5;
			projectile.scale = 1.3f;
			
		}
		public float moveFactor { //controls spear speed
			get => projectile.ai[0];
			set => projectile.ai[0] = value;
        }

        public override void AI() {
			Player pOwner = Main.player[projectile.owner];
			Vector2 ownercenter = pOwner.RotatedRelativePoint(pOwner.MountedCenter, true);
			projectile.direction = pOwner.direction;
			pOwner.heldProj = projectile.whoAmI;
			pOwner.itemTime = pOwner.itemAnimation;
			projectile.position.X = ownercenter.X - (float)(projectile.width / 2);
			projectile.position.Y = ownercenter.Y - (float)(projectile.height / 2);

			if (!pOwner.frozen) {
				if (moveFactor == 0f) { //when initially thrown
					moveFactor = 3f; //move forward
					projectile.netUpdate = true;
				}
				if (pOwner.itemAnimation < pOwner.itemAnimationMax / 2) { //after x animation frames, return
					moveFactor -= 1.7f;
                }
				else { //extend spear
					moveFactor += 2.25f;
                }

            }

			if (pOwner.itemAnimation == 0) {
				projectile.Kill();
			}

			projectile.position += projectile.velocity * moveFactor;
			projectile.rotation = projectile.velocity.ToRotation() + MathHelper.ToRadians(135f);
			
			if (projectile.spriteDirection == -1) {
				projectile.rotation -= MathHelper.ToRadians(90f);
            }


			if (Main.rand.Next(5) == 0) {
				Dust.NewDust(projectile.position, projectile.width, projectile.height, 15, 0f, 0f, 150, default, 1.4f);
			}
			int num116 = Dust.NewDust(projectile.position, projectile.width, projectile.height, 15, projectile.velocity.X * 0.2f + (float)(projectile.direction * 3), projectile.velocity.Y * 0.2f, 100, default, 1.2f);
			Main.dust[num116].velocity /= 2f;
			num116 = Dust.NewDust(projectile.position - projectile.velocity * 2f, projectile.width, projectile.height, 15, 0f, 0f, 150, default, 1.4f);
			Main.dust[num116].velocity /= 5f;

		}
        
    }
    
}
