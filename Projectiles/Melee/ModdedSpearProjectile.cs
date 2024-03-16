using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee
{
    public abstract class ModdedSpearProjectile : ModProjectile
    {
        // Define the range of the Spear Projectile. These are overridable properties, in case you'll want to make a class inheriting from Projectile one.
        public abstract float HoldoutRangeMin {  get; }
        public abstract float HoldoutRangeMax { get; }
        /// <summary>
        /// Scales the size of the hitbox at the tip of the spear
        /// </summary>
        public abstract float HitboxSize { get; }
        public abstract float Scale { get; }
        /// <summary>
        /// Set to -2 for no dust
        /// </summary>
        public abstract int dustID { get; }
        public override void SetDefaults()
        {
            Projectile.width = (int)(18 * HitboxSize);
            Projectile.height = (int)(18 * HitboxSize);
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.scale = Scale;
            Projectile.hide = true;
            Projectile.aiStyle = 19;
            Projectile.ownerHitCheck = true;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override bool PreAI()
        {
            Player player = Main.player[Projectile.owner]; // Since we access the owner player instance so much, it's useful to create a helper local variable for Projectile
            int duration = player.itemAnimationMax; // Define the duration the projectile will exist in frames

            player.heldProj = Projectile.whoAmI; // Update the player's held projectile id

            // Reset projectile time left if necessary
            if (Projectile.timeLeft > duration)
            {
                Projectile.timeLeft = duration;
            }

            Projectile.velocity = Vector2.Normalize(Projectile.velocity); // Velocity isn't used in Projectile spear implementation, but we use the field to store the spear's attack direction.

            float halfDuration = duration * 0.5f;
            float progress;

            // Here 'progress' is set to a value that goes from 0.0 to 1.0 and back during the item use animation.
            if (Projectile.timeLeft < halfDuration)
            {
                progress = Projectile.timeLeft / halfDuration;
            }
            else
            {
                progress = (duration - Projectile.timeLeft) / halfDuration;
            }

            // Move the projectile from the HoldoutRangeMin to the HoldoutRangeMax and back, using SmoothStep for easing the movement
            Projectile.Center = player.MountedCenter + Vector2.SmoothStep(Projectile.velocity * HoldoutRangeMin, Projectile.velocity * HoldoutRangeMax, progress);

            // Apply proper rotation to the sprite.
            if (Projectile.spriteDirection == -1)
            {
                // If sprite is facing left, rotate 45 degrees
                Projectile.rotation += MathHelper.ToRadians(45f);
            }
            else
            {
                // If sprite is facing right, rotate 135 degrees
                Projectile.rotation += MathHelper.ToRadians(135f);
            }

            // Avoid spawning dusts on dedicated servers
            if (!Main.dedServ && dustID != -2)
            {
                // These dusts are added later, for the 'ExampleMod' effect
                if (Main.rand.NextBool(3))
                {
                    Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, dustID, Projectile.velocity.X * 2f, Projectile.velocity.Y * 2f, Alpha: 128, Scale: 1.2f);
                }

                if (Main.rand.NextBool(4))
                {
                    Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, dustID, Alpha: 128, Scale: 0.3f);
                }
            }
            return false;
        }
    }
}
