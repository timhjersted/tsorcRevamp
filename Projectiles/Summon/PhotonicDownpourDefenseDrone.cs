using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;

namespace tsorcRevamp.Projectiles.Summon
{
	public class PhotonicDownpourDefenseDrone : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true; // This is necessary for right-click targeting
			Main.projPet[Projectile.type] = true; // Denotes that this projectile is a pet or minion
			ProjectileID.Sets.MinionSacrificable[Projectile.type] = true; // This is needed so your minion can properly spawn when summoned and replaced when other minions are summoned
		}

		public sealed override void SetDefaults()
		{
			Main.projFrames[Projectile.type] = 2;
			Projectile.width = 40;
			Projectile.height = 50;
			Projectile.tileCollide = false; // Makes the minion go through tiles freely

			// These below are needed for a minion weapon
			Projectile.friendly = true; // Only controls if it deals damage to enemies on contact (more on that later)
			Projectile.minion = true; // Declares this as a minion (has many effects)
			Projectile.DamageType = DamageClass.Summon; // Declares the damage type (needed for it to deal damage)
			Projectile.minionSlots = 1; // Amount of slots this minion occupies from the total minion slots available to the player (more on that later)
			Projectile.penetrate = -1; // Needed so the minion doesn't despawn on collision with enemies or tiles

			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 15;
		}

		public override bool? CanCutTiles()
		{
			return false;
		}

		public override bool MinionContactDamage()
		{
			return false;
		}

		bool indexSet = false;
		List<float> foundIndicies = new List<float>();
		int timer = 0;
		int firingCooldown;
		public override void AI()
		{
			timer++;
			Player owner = Main.player[Projectile.owner];

			if (!indexSet)
			{
				for (int i = 0; i < Main.maxProjectiles; i++)
				{
					if (Main.projectile[i].active && Main.projectile[i].type == Projectile.type && Main.projectile[i].owner == Projectile.owner && Main.projectile[i].whoAmI != Projectile.whoAmI)
					{
						foundIndicies.Add(Main.projectile[i].ai[0]);
					}
				}

				for (int i = 0; i < 12; i++)
				{
					if (foundIndicies.Contains(i))
					{
						continue;
					}
					else
					{
						Projectile.ai[0] = i;
						break;
					}
				}
				indexSet = true;
			}

			CheckActive(owner);

			float rotationTime = 820;
			float percentage = MathHelper.TwoPi * ((Main.GameUpdateCount % rotationTime) / rotationTime);

			float radius = 200;
			int ownedCount = owner.ownedProjectileCounts[ModContent.ProjectileType<PhotonicDownpourDefenseDrone>()];
			Vector2 target = owner.Center + new Vector2(radius, 0).RotatedBy(percentage + MathHelper.TwoPi * Projectile.ai[0] / ownedCount);
			UsefulFunctions.SmoothHoming(Projectile, target, 0.5f, 20);

			bool foundProjTarget = false;
			Vector2 targetCoords = new Vector2(99999, 99999);
			if (owner.whoAmI == Main.myPlayer && Main.GameUpdateCount % 60 == Projectile.ai[0] * 60f / ownedCount)
			{
				for(int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].Center.Distance(Projectile.Center) < targetCoords.Distance(Projectile.Center) && UsefulFunctions.IsProjectileSafeToFuckWith(i))
                    {
						foundProjTarget = true;
						targetCoords = Main.projectile[i].Center;
					}
                }
			}

            if (foundProjTarget)
            {
				Vector2 velocity = UsefulFunctions.Aim(Projectile.Center, targetCoords, 22);
				Projectile.rotation = velocity.ToRotation() + MathHelper.PiOver2;
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<DefenseDroneFire>(), Projectile.damage, 0, Main.myPlayer, 1);
				foundProjTarget = false;
			}
		}

		// This is the "active check", makes sure the minion is alive while the player is alive, and despawns if not
		private bool CheckActive(Player owner)
		{
			if (owner.dead || !owner.active)
			{
				owner.ClearBuff(ModContent.BuffType<PhotonicDownpourBuff>());

				return false;
			}

			if (owner.HasBuff(ModContent.BuffType<PhotonicDownpourBuff>()))
			{
				Projectile.timeLeft = 2;
			}

			return true;
		}

		public static Texture2D glowmask;
		public override bool PreDraw(ref Color lightColor)
		{
			UsefulFunctions.EnsureLoaded(ref glowmask, Texture + "Glowmask");
			SpriteEffects spriteEffects = SpriteEffects.None;

			Rectangle sourceRectangle = new Rectangle(0, 0, TextureAssets.Projectile[Projectile.type].Value.Width, TextureAssets.Projectile[Projectile.type].Value.Height);
			Vector2 origin = sourceRectangle.Size() / 2f;
			Color drawColor = Projectile.GetAlpha(lightColor);
			Main.EntitySpriteDraw(TextureAssets.Projectile[Projectile.type].Value,
				Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
				sourceRectangle, drawColor, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);
			Main.EntitySpriteDraw(glowmask,
				Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
				sourceRectangle, Color.White, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);

			return false;
		}
	}
}