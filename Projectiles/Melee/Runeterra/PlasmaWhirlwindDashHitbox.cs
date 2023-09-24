using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Melee;
using tsorcRevamp.Items.Weapons.Melee.Runeterra;

namespace tsorcRevamp.Projectiles.Melee.Runeterra
{
    class PlasmaWhirlwindDashHitbox : ModProjectile
    {
        public bool Hit = false;
        public override void SetDefaults()
        {
            Projectile.width = Player.defaultWidth;
            Projectile.height = Player.defaultHeight;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ContinuouslyUpdateDamageStats = true;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.position = player.Center;
            if (player.HasBuff(ModContent.BuffType<PlasmaWhirlwindDash>()))
            {
                Projectile.timeLeft = 2;
            }
            Projectile.CritChance *= 2;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<PlasmaWhirlwindDashCooldown>(), PlasmaWhirlwind.DashCooldown * 60);
            if (!Hit)
            {
                Hit = true;
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Melee/PlasmaWhirlwind/DashHit") with { Volume = 1f });
            }
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage.Flat += Math.Min(target.lifeMax * PlasmaWhirlwind.PercentHealthDamage / 100f, PlasmaWhirlwind.HealthDamageCap);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
