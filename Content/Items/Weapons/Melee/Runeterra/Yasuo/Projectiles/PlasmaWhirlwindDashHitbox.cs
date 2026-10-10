using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Melee.Runeterra.Yasuo.Buffs;
using tsorcRevamp.Content.Items.Weapons.Melee.Runeterra.Yasuo.Sounds.PlasmaWhirlwind;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Runeterra.Yasuo.Projectiles
{
    class PlasmaWhirlwindDashHitbox : ModProjectile
    {
        public bool Hit;
        public bool Initialized;
        public override void SetDefaults()
        {
            Projectile.width = Player.defaultWidth;
            Projectile.height = Player.defaultHeight;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ContinuouslyUpdateDamageStats = true;
            Projectile.tileCollide = false;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = PlasmaWhirlwind.DashCooldown * 60 - 1;
        }
        public override void OnSpawn(IEntitySource source)
        {
            Projectile.OriginalCritChance = SteelTempest.BaseCritChanceBonus;
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!Initialized)
            {
                Main.npc[(int)Projectile.ai[0]].AddBuff(ModContent.BuffType<PlasmaWhirlwindDashCooldown>(), PlasmaWhirlwind.DashCooldown * 60);
                Initialized = true;
            }
            Projectile.Center = player.Center;
            if (player.HasBuff(ModContent.BuffType<PlasmaWhirlwindDash>()))
            {
                Projectile.timeLeft = 2;
            }
            Projectile.CritChance *= 2;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!Hit)
            {
                Hit = true;
                SoundEngine.PlaySound(new SoundStyle(UsefulFunctions.RefactorableFilepath(typeof(PlasmaWhirlwindSound)) + "_DashHit") with { Volume = 1.5f });
            }
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage.Flat += Math.Min(target.lifeMax * PlasmaWhirlwind.DashAndSlashPercentHealthDamage / 100f, PlasmaWhirlwind.HealthDamageCap);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
