using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

//using tsorcRevamp.Dusts;

namespace tsorcRevamp.Projectiles {
    class DragoonLance : ModProjectile {

        public override void SetDefaults() {
			projectile.width = 45;
			projectile.height = 45;
			projectile.aiStyle = 19;
			projectile.penetrate = 1;
			projectile.timeLeft = 3600;
			projectile.friendly = true; //can hit enemies
			projectile.hostile = false; //can hit player / friendly NPCs
			projectile.ownerHitCheck = false;
			projectile.melee = true;
			projectile.tileCollide = false;
			projectile.hide = true;
			//projectile.usesLocalNPCImmunity = true;
			//projectile.localNPCHitCooldown = 5;
			projectile.scale = 1.1f;
			
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
					moveFactor = 10f; //move forward
					projectile.netUpdate = true;
				}
				if (pOwner.itemAnimation < pOwner.itemAnimationMax / 2) { //after x animation frames, return
					moveFactor -= 1.8f;
                }
				else { //extend spear
					moveFactor += 2f;
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


        }
        
    }
    
}
