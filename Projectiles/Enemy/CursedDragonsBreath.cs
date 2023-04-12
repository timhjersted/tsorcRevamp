using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy
{
    class CursedDragonsBreath : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            AIType = 85;
            Projectile.alpha = 255;
            Projectile.aiStyle = 23;
            Projectile.damage = 80;
            Projectile.timeLeft = 3600;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = 3;
            Projectile.tileCollide = true;
            Projectile.MaxUpdates = 2;
        }

        public override bool PreKill(int timeLeft)
        {
            Projectile.type = 85;
            return true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheHunter>())) && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheRage>())))
            {
                target.AddBuff(ModContent.BuffType<PowerfulCurseBuildup>(), 36000);
            }
        
         
                target.AddBuff(BuffID.Poisoned, 3600);
                target.AddBuff(BuffID.Weak, 300);
        }

    }
}
