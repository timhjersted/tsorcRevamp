using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;
using tsorcRevamp.Content.Projectiles.Melee;

//using tsorcRevamp.Dusts;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Spears.BloodLance
{
    class BloodLanceProj : ModdedSpearProjectile
    {
        public override float HoldoutRangeMin => 62f;
        public override float HoldoutRangeMax => 186f;
        public override float HitboxSize => 1;
        public override float Scale => 1;
        public override int dustID => DustID.Blood;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<CrimsonBurn>(), BloodLanceItem.BurnDuration * 60);


            float radius = 192f; //= 12 tiles
            foreach (var npc in Main.ActiveNPCs)
            {
                if (!npc.friendly && npc != target && npc.Distance(Projectile.Center) <= radius)
                {
                    // Apply Crimson Burn to nearby enemies 
                    npc.AddBuff(ModContent.BuffType<CrimsonBurn>(), BloodLanceItem.BurnDuration * 60);
                }
            }
            
            Player player = Main.player[Projectile.owner];
            if (Main.rand.Next(100) < BloodLanceItem.OnHitHealChance)
            {
                player.statLife += BloodLanceItem.OnHitHealAmount;
                player.HealEffect(BloodLanceItem.OnHitHealAmount); 
            }
            else if (target.HasBuff<CrimsonBurn>())
            {
                player.statLife += BloodLanceItem.OnHitHealAmount;
                player.HealEffect(BloodLanceItem.OnHitHealAmount); 
            }
        }
    }
}
