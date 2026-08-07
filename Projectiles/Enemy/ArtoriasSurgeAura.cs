using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class ArtoriasSurgeAura : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            int ownerIndex = (int)Projectile.ai[0];
            if (ownerIndex < 0 || ownerIndex >= Main.maxNPCs
                || !Main.npc[ownerIndex].active
                || Main.npc[ownerIndex].ModNPC is not Artorias artorias
                || !artorias.AbyssSurgeActive)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = artorias.NPC.Center;
            Projectile.timeLeft = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int ownerIndex = (int)Projectile.ai[0];
            if (ownerIndex < 0 || ownerIndex >= Main.maxNPCs || !Main.npc[ownerIndex].active)
            {
                return false;
            }

            Vector2 center = Main.npc[ownerIndex].Center + new Vector2(0f, -15f);
            ArtoriasVFX.DrawMantle(center, new Vector2(205f, 255f), 0.68f, 1.15f, 1f);
            return false;
        }
    }
}
