using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class CursedFlamelash : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.scale = 1.1f;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 420;
        }

        bool spawnedTrail;
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, TorchID.Cursed);

            Main.player[Projectile.owner].manaRegenDelay = 10;

            if (!spawnedTrail)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float encodedIdentity = UsefulFunctions.EncodeID(Projectile.identity, Projectile.owner);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.CursedFlamelashTrail>(), Projectile.damage, 0, Main.myPlayer, 0, encodedIdentity);
                }
                Projectile.damage = 0;
                spawnedTrail = true;
                Projectile.netUpdate = true;
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                //Main.NewText("Client Host HostProjectile whoami: " + Projectile.whoAmI + " identity: " + Projectile.identity + " type: " + Projectile.type);
            }
            else
            {
                //UsefulFunctions.BroadcastText("Server Host HostProjectile whoami: " + Projectile.whoAmI + " identity: " + Projectile.identity + " type: " + Projectile.type);
            }

            if (Projectile.owner == Main.myPlayer)
            {
                UsefulFunctions.SmoothHoming(Projectile, Main.MouseWorld, 1f, 20, null, true, 0.2f);
            }
        }


        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
