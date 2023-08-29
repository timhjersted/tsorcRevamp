using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.DarkCloud;

class DarkUltimaWeaponDummyProjectile : ModProjectile
{
    public override void SetDefaults()
    {
        Projectile.aiStyle = -1;
        Projectile.width = 30;
        Projectile.height = 30;
        Projectile.hostile = true;
        Projectile.penetrate = 9999;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false;
    }
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Dark Ultima Weapon");
    }
    public NPC Sword
    {
        get => Main.npc[(int)Projectile.ai[0]];
        set => Main.npc[(int)Projectile.ai[0]] = value;
    }
    public NPC DarkCloud
    {
        get => Main.npc[(int)Sword.ai[0]];
        set => Main.npc[(int)Sword.ai[0]] = value;
    }
    public Vector2 SwordCenter
    {
        //Compensating for the sword's janky as hell hitbox.
        get => Sword.Center + new Vector2(0, -62);
    }

    public override void AI()
    {
        if (Sword == null || Sword.active == false || DarkCloud == null || DarkCloud.active == false)
        {
            Projectile.Kill();
        }

        if (Projectile.ai[1] == 0)
        {
            Projectile.Center = SwordCenter + new Vector2(16, 0).RotatedBy(Sword.rotation + MathHelper.ToRadians(-45));
        }
        if (Projectile.ai[1] == 1)
        {
            Projectile.Center = SwordCenter + new Vector2(32, 0).RotatedBy(Sword.rotation + MathHelper.ToRadians(-45));
        }
        if (Projectile.ai[1] == 2)
        {
            Projectile.Center = SwordCenter + new Vector2(48, 0).RotatedBy(Sword.rotation + MathHelper.ToRadians(-45));
        }
        else if (Projectile.ai[1] == 3)
        {
            Projectile.Center = SwordCenter + new Vector2(64, 0).RotatedBy(Sword.rotation + MathHelper.ToRadians(-45));
        }
        else if (Projectile.ai[1] == 4)
        {
            Projectile.Center = SwordCenter;
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        return false;
    }
}