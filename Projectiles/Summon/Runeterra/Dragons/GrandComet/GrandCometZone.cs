using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Projectiles.Enemy.Okiku;
using static Humanizer.In;

namespace tsorcRevamp.Projectiles.Summon.Runeterra.Dragons.GrandComet
{
    public class GrandCometZone : ModProjectile
    {
        public float Timer = 0;
        public float DustRingTimer = 0;
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
            Projectile.timeLeft = 100000;
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
                DustRingTimer += 6 + DustRingTimerBonus;
                if (player.ownedProjectileCounts[ModContent.ProjectileType<GrandCometShockwaveTrail>()] < 1)
                {
                    Projectile Shockwave = Projectile.NewProjectileDirect(Projectile.GetSource_None(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<GrandCometShockwaveTrail>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                }
                int DustSpeed = 3;
                int DustCount = 30;
                int DustRadiusBonus = 100;
                int DustRadiusIncrease = 2;
                /*for (int i = 0; i < 40; i++)
                {
                    UsefulFunctions.DustRing(Projectile.Center, DustRingTimer + DustRadiusBonus, DustID.PurpleCrystalShard, DustCount, DustSpeed);
                    DustRadiusBonus += DustRadiusIncrease;
                }*/
                DustRingTimerBonusTimer++;
                if (DustRingTimerBonusTimer == 5)
                {
                    DustRingTimerBonusTimer = 0;
                    DustRingTimerBonus++;
                }
            }

            if (DustRingTimer >= 3000)
            {
                Projectile.Kill();
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.ai[0] == 1)
            {
                float distance = Vector2.Distance(Projectile.Center, targetHitbox.Center.ToVector2());
                if (distance <= DustRingTimer)
                {
                    return true;
                }
            }
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<AwestruckDebuff>(), CenterOfTheUniverse.AwestruckDebuffDuration * 60);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = new Color(3, 3, 3, Timer / TransparencyDivisor);
            return base.PreDraw(ref lightColor);
        }
    }
}