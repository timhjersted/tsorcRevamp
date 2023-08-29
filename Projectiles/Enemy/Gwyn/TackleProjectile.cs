using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Gwyn;

class TackleProjectile : ModProjectile
{

    public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Gwyn");
    }
    public override void SetDefaults()
    {
        Projectile.height = 58;
        Projectile.width = 58;
        Projectile.light = 0.8f;
        Projectile.penetrate = 99999;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 160;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.damage = 40;
    }

    internal float AI_Timer
    {
        get => Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    internal float AI_Owner
    {
        get => Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }


    public override void AI()
    {
        NPC owner = Main.npc[(int)AI_Owner];
        Projectile.Center = owner.Center;
        Projectile.velocity = owner.velocity;
        if (Projectile.velocity.Length() > 30 && Main.netMode != NetmodeID.Server)
        {
            for (int i = 0; i < 4; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.RuneWizard);
                dust.noGravity = true;
            }
        }
    }
}
