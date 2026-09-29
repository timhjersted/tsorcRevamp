namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    // Shares the grey star's flight, stick and impact effects, at half its usual draw size.
    public class ShadowNinjaStarProj : EnemyNinjaStarProj
    {
        public override string Texture => "tsorcRevamp/Content/Projectiles/Enemy/Weapons/EnemyNinjaStarProj";

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.scale *= 0.5f;
        }
    }
}
