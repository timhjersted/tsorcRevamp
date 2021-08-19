using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

//using tsorcRevamp.Dusts;

namespace tsorcRevamp.Projectiles {
    class AncientBloodLance : ModProjectile {

		bool LegacyMode = ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

		public override void SetDefaults() {
			projectile.width = LegacyMode ? 45 : 25;
			projectile.height = LegacyMode ? 45 : 25;
			projectile.aiStyle = 19;
			projectile.penetrate = 4;
			projectile.timeLeft = 3600;
			projectile.friendly = true; //can hit enemies
			projectile.hostile = false; //can hit player / friendly NPCs
			projectile.ownerHitCheck = false;
			projectile.melee = true;
			projectile.tileCollide = false;
			projectile.hide = true;
			//projectile.usesLocalNPCImmunity = true;
			//projectile.localNPCHitCooldown = 5;
			projectile.scale = 1f;
			
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
					moveFactor = 2.59f; //move forward (2.4% of projectile scaled sprite size)
					projectile.netUpdate = true;
				}
				if (pOwner.itemAnimation < pOwner.itemAnimationMax / 2) { //after x animation frames, return
					moveFactor -= 2.37f; //2.2% of projctile scaled sprite size
                }
				else { //extend spear
					moveFactor += 2.59f; //(2.4% of projectile scaled sprite size)
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
