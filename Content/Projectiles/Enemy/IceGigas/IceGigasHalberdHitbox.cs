using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.IceGigas
{
    // Invisible blade hitbox for the Ice Gigas's Halberd Cleave. It owns no shape of its own: every tick it asks the
    // Gigas for the halberd's grip→tip line (the same line PreDraw draws the sprite along) and collides with that line.
    // ai[0]: the Gigas's whoAmI. Spawned by the server at the strike's start; dies when the swing leaves the cleave.
    // Collision runs on the hit player's machine, where the Gigas's arm pose is derived from the same synced
    // State/AttackTimer, so the hit matches what that player sees.
    class IceGigasHalberdHitbox : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        ///<summary>Line thickness (px) for the blade collision: covers the axe blade, which sticks ~25px off the shaft.</summary>
        const float BladeThickness = 36f;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1; //one swing, one hit: the player's 40t post-hit immunity outlasts the live window
            Projectile.timeLeft = 60;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override void AI()
        {
            NPC owner = Main.npc[(int)Projectile.ai[0]];
            NPCs.Bosses.Gigas.IceGigas gigas = owner.ModNPC as NPCs.Bosses.Gigas.IceGigas;

            if (!owner.active || gigas == null || !gigas.HalberdOut)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Center;
        }

        public override bool? CanDamage()
        {
            NPC owner = Main.npc[(int)Projectile.ai[0]];
            NPCs.Bosses.Gigas.IceGigas gigas = owner.ModNPC as NPCs.Bosses.Gigas.IceGigas;
            bool bladeLive = owner.active && gigas != null && gigas.HalberdBladeLive;
            return bladeLive;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            NPC owner = Main.npc[(int)Projectile.ai[0]];
            NPCs.Bosses.Gigas.IceGigas gigas = owner.ModNPC as NPCs.Bosses.Gigas.IceGigas;

            if (!owner.active || gigas == null)
            {
                return false;
            }

            gigas.GetHalberdBlade(out Vector2 grip, out Vector2 tip);
            float collisionPoint = 0f;
            bool bladeTouches = Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), grip, tip, BladeThickness, ref collisionPoint);
            return bladeTouches;
        }
    }
}
