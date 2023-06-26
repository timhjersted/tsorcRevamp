using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using tsorcRevamp.Buffs.Runeterra.Ranged;
using tsorcRevamp.NPCs;
using Terraria.Audio;

namespace tsorcRevamp.Projectiles.Ranged.Runeterra
{
	public class RadioactiveDart : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5; // The length of old position to be recorded
			ProjectileID.Sets.TrailingMode[Projectile.type] = 0; // The recording mode
		}

		public override void SetDefaults()
		{
			Projectile.width = 2;
			Projectile.height = 2;
			Projectile.aiStyle = ProjAIStyleID.SmallFlying;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 300;
			Projectile.light = 0.5f;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = true;
            Projectile.extraUpdates = 3;

            AIType = ProjectileID.Bat;
		}

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (hit.Crit)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/OmegaSquadRifle/ShotCrit") with { Volume = 0.5f }, target.Center);
            }
            else
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/OmegaSquadRifle/ShotHit") with { Volume = 0.5f }, target.Center);
            }
            target.GetGlobalNPC<tsorcRevampGlobalNPC>().lastHitPlayerRanger = Main.player[Projectile.owner];
            target.AddBuff(ModContent.BuffType<IrradiatedDebuff>(), 2 * 60);
        }
	}
}