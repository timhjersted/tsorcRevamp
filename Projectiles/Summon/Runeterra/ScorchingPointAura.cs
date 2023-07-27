using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Projectiles.Summon.Runeterra
{
	public class ScorchingPointAura : ModProjectile
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


			currentAngle += (angularSpeed / (modPlayer.MinionCircleRadius * 0.001f + 1f));

			Vector2 offset = new Vector2(modPlayer.MinionCircleRadius, modPlayer.MinionCircleRadius);

			UsefulFunctions.DustRing(owner.Center, modPlayer.MinionCircleRadius + 10f, DustID.InfernoFork, 5, 10);

			//Dust.NewDust(owner.Center - offset * 0.75f, (int)(modPlayer.MinionCircleRadius * 1.5f), (int)(modPlayer.MinionCircleRadius * 1.5f), DustID.Torch, 0, 0, 1000, default, 0.75f);

			Projectile.Center = owner.Center;
        }
		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Player owner = Main.player[Projectile.owner];
            float distance = Vector2.Distance(owner.Center, targetHitbox.Center.ToVector2());
			if (distance <= (owner.GetModPlayer<tsorcRevampPlayer>().MinionCircleRadius + 34f))
			{
				return true;
            }
            return false;
        }


		private bool CheckActive(Player owner)
		{
			if (owner.dead || !owner.active)
			{
				owner.ClearBuff(ModContent.BuffType<CenterOfTheHeat>());

				return false;
			}

			if (!owner.HasBuff(ModContent.BuffType<CenterOfTheHeat>()))
			{
				currentAngle = 0;
				Projectile.Kill();
				ScorchingPoint.projectiles.Clear();
			}

			if (owner.HasBuff(ModContent.BuffType<CenterOfTheHeat>()) && ScorchingPoint.processedProjectilesCount > 4)
			{
				Projectile.timeLeft = 2;
			}

			return true;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<tsorcRevampGlobalNPC>().lastHitPlayerSummoner = Main.player[Projectile.owner];
            target.AddBuff(ModContent.BuffType<ScorchingDebuff>(), 2 * 60);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
			modifiers.SourceDamage *= (float)ScorchingPoint.processedProjectilesCount / 5f;
        }
    }
}