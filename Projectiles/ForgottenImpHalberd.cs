using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace tsorcRevamp.Projectiles {
    class ForgottenImpHalberd : ModProjectile {

        public override void SetDefaults() {
            projectile.width = 23;
            projectile.height = 23;
			projectile.aiStyle = 19;
            projectile.scale = 1.3f;
            projectile.timeLeft = 3600;
            projectile.friendly = true;
            projectile.hide = true;
            projectile.ownerHitCheck = true;
            projectile.melee = true;
            projectile.tileCollide = false;
            projectile.penetrate = 5;
        }

		public float moveFactor {
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
					moveFactor = 1.96f; //move forward (2.4% of projectile scaled sprite size)
					projectile.netUpdate = true;
				}
				if (pOwner.itemAnimation < pOwner.itemAnimationMax / 2) { //after x animation frames, return
					moveFactor -= 1.8f; //2.2% of projctile scaled sprite size
				}
				else { //extend spear
					moveFactor += 1.96f; //(2.4% of projectile scaled sprite size)
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
