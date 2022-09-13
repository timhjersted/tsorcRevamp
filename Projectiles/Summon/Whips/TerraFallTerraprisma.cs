using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Summon.Whips
{
    public class TerraFallTerraprisma : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Terraprisma");
        }
        public override void SetDefaults()
        {
            Projectile.width = 33;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.CloneDefaults(ProjectileID.EmpressBlade);
            Projectile.aiStyle = ProjAIStyleID.Terraprisma;
        }

    }
}