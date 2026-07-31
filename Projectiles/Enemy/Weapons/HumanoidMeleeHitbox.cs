using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;
using tsorcRevamp.Projectiles;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    /// <summary>Invisible, source-NPC-anchored hitbox for the shared humanoid hop attacks.</summary>
    public class HumanoidMeleeHitbox : ModProjectile
    {
        private const float SourceOverlap = 22f; // original 6px overlap + 16px so visible fighters reach the shield before rebounding

        public override string Texture => "tsorcRevamp/NPCs/Puppets/PuppetPlaceholder";

        public override void SetDefaults()
        {
            Projectile.width = 64;
            Projectile.height = 44;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 60;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.hide = true;
            Projectile.ignoreWater = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Vector2 center = Projectile.Center;
            Projectile.width = (int)System.Math.Max(1f, Projectile.ai[0]);
            Projectile.height = (int)System.Math.Max(1f, Projectile.ai[1]);
            Projectile.Center = center;
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override void AI()
        {
            tsorcGlobalProjectile globalProjectile = Projectile.GetGlobalProjectile<tsorcGlobalProjectile>();
            if (!globalProjectile.TryGetSourceNPC(out NPC sourceNPC))
            {
                Projectile.Kill();
                return;
            }

            tsorcRevampGlobalNPC sourceGlobalNPC = sourceNPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (!sourceGlobalNPC.CombatMeleeActive)
            {
                Projectile.Kill();
                return;
            }

            int direction = Projectile.velocity.X < 0f ? -1 : 1;
            Projectile.Center = sourceNPC.Center + new Vector2(direction * (Projectile.width * 0.5f - SourceOverlap), -4f);
            Projectile.timeLeft = 2;
        }

        public override bool? CanDamage()
        {
            tsorcGlobalProjectile globalProjectile = Projectile.GetGlobalProjectile<tsorcGlobalProjectile>();
            return globalProjectile.TryGetSourceNPC(out NPC sourceNPC)
                && sourceNPC.GetGlobalNPC<tsorcRevampGlobalNPC>().InCombatMeleeHitWindow;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            tsorcGlobalProjectile globalProjectile = Projectile.GetGlobalProjectile<tsorcGlobalProjectile>();
            if (globalProjectile.TryGetSourceNPC(out NPC sourceNPC) && sourceNPC.ModNPC is IHumanoidMeleeHitEffects hitEffects)
            {
                hitEffects.OnHumanoidMeleeHit(target);
            }
        }
    }
}
