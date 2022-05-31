using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
	class OldHalberd : ModProjectile
	{

		public override void SetDefaults()
		{
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.aiStyle = 19;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 3600;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.ownerHitCheck = false;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.tileCollide = false;
			Projectile.hide = true;
			Projectile.scale = 1f;
			Projectile.ownerHitCheck = true;
		}
		public float moveFactor
		{ //controls spear speed
			get => Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		public override void AI()
		{
			Player pOwner = Main.player[Projectile.owner];
			Vector2 ownercenter = pOwner.RotatedRelativePoint(pOwner.MountedCenter, true);
			Projectile.direction = pOwner.direction;
			pOwner.heldProj = Projectile.whoAmI;
			pOwner.itemTime = pOwner.itemAnimation;
			Projectile.position.X = ownercenter.X - (float)(Projectile.width / 2);
			Projectile.position.Y = ownercenter.Y - (float)(Projectile.height / 2);

			if (!pOwner.frozen)
			{
				if (moveFactor == 0f)
				{ //when initially thrown
					moveFactor = 2.4f; //move forward
					Projectile.netUpdate = true;
				}
				if (pOwner.itemAnimation < pOwner.itemAnimationMax / 2)
				{ //after x animation frames, return
					moveFactor -= 2.26f;
				}
				else
				{ //extend spear
					moveFactor += 2.4f;
				}

			}

			if (pOwner.itemAnimation == 4)
			{
				Projectile.Kill();
			}

			Projectile.position += Projectile.velocity * moveFactor;
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(135f);
			Projectile.spriteDirection = Projectile.direction;
			if (Projectile.spriteDirection == -1)
			{
				Projectile.rotation -= MathHelper.ToRadians(90f);
			}


		}

	}

}
