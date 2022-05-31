using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

//using tsorcRevamp.Dusts;

namespace tsorcRevamp.Projectiles {
    class ForgottenRadiantLance : ModProjectile {

        public override void SetDefaults() {
			Projectile.width = 45;
			Projectile.height = 45;
			Projectile.aiStyle = 19;
			Projectile.penetrate = 13;
			Projectile.timeLeft = 3600;
			Projectile.friendly = true; //can hit enemies
			Projectile.hostile = false; //can hit player / friendly NPCs
			Projectile.ownerHitCheck = false;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.tileCollide = false;
			Projectile.hide = true;
			//projectile.usesLocalNPCImmunity = true;
			//projectile.localNPCHitCooldown = 5;
			Projectile.scale = 1.3f;
			
		}
		public float moveFactor { //controls spear speed
			get => Projectile.ai[0];
			set => Projectile.ai[0] = value;
        }

        public override void AI() {
			Player pOwner = Main.player[Projectile.owner];
			Vector2 ownercenter = pOwner.RotatedRelativePoint(pOwner.MountedCenter, true);
			Projectile.direction = pOwner.direction;
			pOwner.heldProj = Projectile.whoAmI;
			pOwner.itemTime = pOwner.itemAnimation;
			Projectile.position.X = ownercenter.X - (float)(Projectile.width / 2);
			Projectile.position.Y = ownercenter.Y - (float)(Projectile.height / 2);

			if (!pOwner.frozen) {
				if (moveFactor == 0f) { //when initially thrown
					moveFactor = 3.4f; //move forward
					Projectile.netUpdate = true;
				}
				if (pOwner.itemAnimation < pOwner.itemAnimationMax / 2) { //after x animation frames, return
					moveFactor -= 3.2f;
                }
				else { //extend spear
					moveFactor += 3.4f;
                }

            }

			if (pOwner.itemAnimation == 0) {
				Projectile.Kill();
			}

			Projectile.position += Projectile.velocity * moveFactor;
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(135f);
			
			if (Projectile.spriteDirection == -1) {
				Projectile.rotation -= MathHelper.ToRadians(90f);
            }


        }
        
    }
    
}
