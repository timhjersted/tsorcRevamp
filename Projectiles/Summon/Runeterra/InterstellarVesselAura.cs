using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Projectiles.Summon.Runeterra
{
	public class InterstellarVesselAura : ModProjectile
	{
		public float angularSpeed = 0.03f;
		public float currentAngle = 0;

        public override void SetStaticDefaults()
		{
			ProjectileID.Sets.MinionShot[Projectile.type] = true;
            ProjectileID.Sets.SummonTagDamageMultiplier[Projectile.type] = ScorchingPoint.SummonTagDmgMult / 100f;
        }
		public sealed override void SetDefaults()
		{
			Projectile.tileCollide = false;

			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.penetrate = -1;

            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 100;
		}
		public override bool? CanCutTiles()
		{
			return false;
		}
		public override bool MinionContactDamage()
		{
			return true;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			tsorcRevampPlayer modPlayer = owner.GetModPlayer<tsorcRevampPlayer>();

			if (!CheckActive(owner))
			{
				return;
			}

			if (modPlayer.InterstellarBoost)
			{
				Projectile.idStaticNPCHitCooldown = 50;
			} else
			{
				Projectile.idStaticNPCHitCooldown = 100;
			}

			currentAngle += (angularSpeed / (modPlayer.MinionCircleRadius * 0.001f + 1f));

			Vector2 offset = new Vector2(modPlayer.MinionCircleRadius, modPlayer.MinionCircleRadius);

            UsefulFunctions.DustRing(owner.Center, modPlayer.MinionCircleRadius + 20f, DustID.GemTopaz, 5, 10);
            //Dust.NewDust(owner.Center - offset * 0.75f, (int)(modPlayer.MinionCircleRadius * 1.5f), (int)(modPlayer.MinionCircleRadius * 1.5f), DustID.DesertTorch, 0, 0, 1000, default, 0.75f);

			Projectile.Center = owner.Center;
        }
		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Player owner = Main.player[Projectile.owner];
            float distance = Vector2.Distance(owner.Center, targetHitbox.Center.ToVector2());
			if (distance <= (owner.GetModPlayer<tsorcRevampPlayer>().MinionCircleRadius + 60f))
			{
				return true;
			}
			else
			{
				return false;
			}
		}


		private bool CheckActive(Player owner)
		{
			if (owner.dead || !owner.active)
			{
				owner.ClearBuff(ModContent.BuffType<InterstellarCommander>());

				return false;
			}

			if (!owner.HasBuff(ModContent.BuffType<InterstellarCommander>()))
			{
				currentAngle = 0;
				Projectile.Kill();
				InterstellarVesselGauntlet.projectiles.Clear();
			}

			if (owner.HasBuff(ModContent.BuffType<InterstellarCommander>()) && InterstellarVesselGauntlet.processedProjectilesCount > 4)
			{
				Projectile.timeLeft = 2;
			}

			return true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<tsorcRevampGlobalNPC>().lastHitPlayerSummoner = Main.player[Projectile.owner];
            target.AddBuff(ModContent.BuffType<ShockedDebuff>(), 2 * 60);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Player owner = Main.player[Projectile.owner];
            modifiers.SourceDamage *= (float)InterstellarVesselGauntlet.processedProjectilesCount / 5f;
            if (owner.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost)
            {
                modifiers.SourceDamage *= 1.25f;
            }
        }
    }
}