using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Items.Weapons.Melee.Spears;

//using tsorcRevamp.Dusts;

namespace tsorcRevamp.Projectiles.Melee.Spears
{
    class AncientBloodLanceProj : ModdedSpearProjectile
    {
        public override float HoldoutRangeMin => 62f;
        public override float HoldoutRangeMax => 186f;
        public override float HitboxSize => 1;
        public override float Scale => 1;
        public override int dustID => DustID.Blood;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<CrimsonBurn>(), AncientBloodLance.BurnDuration * 60);


            float radius = 192f; //= 12 tiles 
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && npc != target && npc.Distance(Projectile.Center) <= radius)
                {
                    // Apply Crimson Burn to nearby enemies 
                    npc.AddBuff(ModContent.BuffType<CrimsonBurn>(), AncientBloodLance.BurnDuration * 60);
                }
            }
            
            // if the player has crimson drain, has a chance to heal 1 hp on hit
            Player player = Main.player[Projectile.owner];
            if (player.HasBuff(ModContent.BuffType<CrimsonDrain>()) && Main.rand.NextBool(2))
            {
                player.statLife += 1;
                player.HealEffect(1); 
            }
        }
    }
}
