using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Buffs.Weapons.Summon.WhipDebuffs;

namespace tsorcRevamp.Projectiles.Summon.Whips
{

	public class PolarisLeashPolaris : ModProjectile
    {
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
		{
			Projectile.width = 22;
			Projectile.height = 18;
			Projectile.tileCollide = false;
			Projectile.DamageType = DamageClass.SummonMeleeSpeed;
			Projectile.friendly = true;
			Projectile.penetrate = -1;
			Projectile.extraUpdates = 1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 30;
		}

		public override void AI()
		{
            if (Main.myPlayer == Projectile.owner && Main.MouseWorld != Projectile.Center)
            {
                Projectile.Center = Main.MouseWorld;
                Projectile.netUpdate = true;
            }
            Player player = Main.player[Projectile.owner];
			if (player.dead || !player.active)
			{
				player.ClearBuff(ModContent.BuffType<PolarisLeashBuff>());
			}
			if (player.HasBuff(ModContent.BuffType<PolarisLeashBuff>()))
			{
				Projectile.timeLeft = 2;
			}
			Dust.NewDust(Projectile.Center, 10, 10, DustID.IceRod, 0f, 0f, 150, default(Color), 1f);
		}
		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(ModContent.BuffType<PolarisLeashDebuff>(), (int)(4 * 60 * Main.player[Projectile.owner].GetModPlayer<tsorcRevampPlayer>().SummonTagDuration));
			Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI;
		}
	}
}
