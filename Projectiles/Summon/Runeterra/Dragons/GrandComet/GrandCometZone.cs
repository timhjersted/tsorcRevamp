using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;

namespace tsorcRevamp.Projectiles.Summon.Runeterra.Dragons.GrandComet
{
    public class GrandCometZone : ModProjectile
    {
        public float Timer = 0;
        public int TransparencyDivisor = 360;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
        }
        public override void SetDefaults()
        {
            Projectile.width = 350;
            Projectile.height = 350;
            Projectile.friendly = true;
            Projectile.timeLeft = 10000;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
        }
        public int DustRingTimerBonusTimer = 0;
        public int DustRingTimerBonus = 0;
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (Projectile.ai[0] == 0 && Timer < TransparencyDivisor)
            {
                Timer++;
            }
            else if (Projectile.ai[0] != 0)
            {
                Timer -= 4;
                if (player.ownedProjectileCounts[ModContent.ProjectileType<GrandCometExplosion>()] < 1)
                {
                    Projectile Explosion = Projectile.NewProjectileDirect(Projectile.GetSource_None(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<GrandCometExplosion>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                }
            }
            if (Projectile.timeLeft < 9900 && Timer < 0)
            {
                Projectile.Kill();
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<AwestruckDebuff>(), CenterOfTheUniverse.AwestruckDebuffDuration * 60);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = new Color(Timer / TransparencyDivisor, Timer / TransparencyDivisor, Timer / TransparencyDivisor, Timer / TransparencyDivisor);
            return base.PreDraw(ref lightColor);
        }
        public override void OnKill(int timeLeft)
        {
            Timer = 0;
        }
    }
}