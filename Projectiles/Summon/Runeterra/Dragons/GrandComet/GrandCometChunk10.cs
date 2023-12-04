using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Items.Weapons.Throwing;

namespace tsorcRevamp.Projectiles.Summon.Runeterra.Dragons.GrandComet
{
    class GrandCometChunk10 : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 42;
            Projectile.height = 46;
            Projectile.friendly = true;
            Projectile.timeLeft = 240;
            Projectile.aiStyle = ProjAIStyleID.ThrownProjectile;
            Projectile.DamageType = DamageClass.Summon;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<AwestruckDebuff>(), CenterOfTheUniverse.AwestruckDebuffDuration * 60);
        }
    }
}
