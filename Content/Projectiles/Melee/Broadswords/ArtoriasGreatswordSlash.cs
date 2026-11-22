using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords;
using AbyssSlash = tsorcRevamp.Content.Projectiles.Enemy.AbyssSlash;

namespace tsorcRevamp.Content.Projectiles.Melee.Broadswords
{
    /// <summary>
    /// The Artorias Greatsword's swing crescent: the boss's own AbyssSlash (same sprite, same ArtoriasVFX swipe
    /// shader, same dust), made friendly magic damage and capped to ArtoriasGreatsword.SlashRangeTiles of travel.
    /// Pierces up to three enemies, one hit each.
    /// </summary>
    class ArtoriasGreatswordSlash : AbyssSlash
    {
        public const float Speed = 13f;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(AbyssSlash));

        // Range cap as a lifetime: 20 tiles at 13 px/tick is 25 ticks.
        static int Lifetime => (int)(ArtoriasGreatsword.SlashRangeTiles * 16f / Speed);

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = Lifetime;
        }
    }
}
