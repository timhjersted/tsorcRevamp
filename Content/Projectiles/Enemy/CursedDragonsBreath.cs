using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy
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
            Projectile.DamageType = DamageClass.Magic;
        }

        // Powerful-curse telegraph: plum flames mixed into the orange stream, shown only once the breath actually builds
        // powerful curse (Hunter + Rage slain, same gate as OnHitPlayer). MaxUpdates = 2 runs this twice a tick,
        // so 1-in-6 per update is ~1-in-3 per tick.
        private const int AccentDustChanceDenominator = 6;

        public override void PostAI()
        {
            if (!Main.rand.NextBool(AccentDustChanceDenominator) || !BuildsPowerfulCurse())
            {
                return;
            }

            PowerfulCurseBuildup.SpawnAccentDust(Projectile.position, Projectile.width, Projectile.height, Projectile.velocity * 0.4f, Main.rand.NextFloat(1.6f, 2.4f));
        }

        private static bool BuildsPowerfulCurse()
        {
            bool hunterSlain = tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheHunter>()));
            bool rageSlain = tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheRage>()));

            return hunterSlain && rageSlain;
        }

        public override bool PreKill(int timeLeft)
        {
            Projectile.type = 85;
            return true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (BuildsPowerfulCurse())
            {
                target.AddBuff(ModContent.BuffType<PowerfulCurseBuildup>(), 36000);
            }

            target.AddBuff(BuffID.OnFire, 480);
        }

    }
}
