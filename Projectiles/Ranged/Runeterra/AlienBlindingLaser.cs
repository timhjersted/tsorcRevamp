using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using tsorcRevamp.Buffs.Runeterra.Ranged;
using System;
using tsorcRevamp.NPCs;
using tsorcRevamp.Items.Weapons.Ranged.Runeterra;
using Terraria.Audio;

namespace tsorcRevamp.Projectiles.Ranged.Runeterra
{
	public class AlienBlindingLaser : ModProjectile
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
			Projectile.light = 0.75f;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = true;
			Projectile.extraUpdates = 3;

			AIType = ProjectileID.Bat;
        }
        public override void OnSpawn(IEntitySource source)
        {
            var player = Main.player[Projectile.owner];
            player.AddBuff(ModContent.BuffType<AlienBlindingLaserCooldown>(), AlienGun.BlindingLaserCooldown * 60);
			Projectile.CritChance += AlienGun.BlindingLaserBonusCritChance;
			player.statMana -= (int)(AlienGun.BaseLaserManaCost * player.manaCost);
			player.ManaEffect(-(int)(AlienGun.BaseLaserManaCost * player.manaCost));
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
		}
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
			SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Ranged/AlienGun/BlindingLaserHit") with { Volume = 1f });
            target.GetGlobalNPC<tsorcRevampGlobalNPC>().lastHitPlayerRanger = Main.player[Projectile.owner];
            target.AddBuff(ModContent.BuffType<ElectrifiedDebuff>(), 2 * 60);
            target.AddBuff(BuffID.Confused, 2 * 60);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
			modifiers.SourceDamage *= AlienGun.BlindingLaserDmgMult;
            modifiers.FinalDamage.Flat += Math.Min(target.lifeMax * AlienGun.BlindingLaserPercentHPDmg / 100f, AlienGun.BlindingLaserHPDmgCap);
        }
    }
}