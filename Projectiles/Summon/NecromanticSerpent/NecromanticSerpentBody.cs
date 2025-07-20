using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Summon.NecromanticSerpent
{
    public class NecromanticSerpentBody : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
            Main.projPet[Type] = true;

            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
            ProjectileID.Sets.MinionSacrificable[Type] = false;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.DontAttachHideToAlpha[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 21;
            Projectile.height = 14;
            Projectile.ContinuouslyUpdateDamageStats = true;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.DamageType = NecromanticSerpentHead.DamageType;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.originalDamage = (int)NecromanticSerpentHead.OriginalDamage;
            Projectile.hide = true;
            Projectile.usesIDStaticNPCImmunity = true; 
            Projectile.idStaticNPCHitCooldown = 15; 
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            //Ichor
            target.AddBuff(BuffID.Ichor, 4 * 60);
            
            //33% to heal 1 hp
            Player player = Main.player[Projectile.owner];
            if (Main.rand.NextBool(3))
            {
                player.statLife += 1;
                player.HealEffect(1);
            }   
            
        }
        public override bool MinionContactDamage()
        {
            return true;
        }

        public override bool? CanCutTiles()
        {
            return false;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindProjectiles.Add(index);
        }
    }
}
