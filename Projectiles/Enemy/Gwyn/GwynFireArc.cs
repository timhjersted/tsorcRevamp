using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///A crescent of cinder-fire thrown off Gwyn's heavier greatsword swings, so "melee" reaches a
    ///tile or two past the blade. Dust-drawn (no sprite), short-lived, travels in the swing
    ///direction with a slight fan. ai[0] = direction (-1/1), ai[1] = vertical bias (for overhead /
    ///rising swings). Applies On Fire!.
    ///</summary>
    class GwynFireArc : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 22; //~a couple tiles of reach then it dies
            Projectile.light = 0.5f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            int dir = (int)Projectile.ai[0] >= 0 ? 1 : -1;
            float vBias = Projectile.ai[1];
            //Drifts outward along the swing, slowing — a crescent flung off the edge
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.velocity = new Vector2(dir * 6f, vBias);
            }
            Projectile.localAI[0]++;
            Projectile.velocity *= 0.9f;

            //Fiery crescent: dense flame biased to the leading edge
            for (int i = 0; i < 4; i++)
            {
                Vector2 pos = Projectile.Center + new Vector2(dir * Main.rand.NextFloat(0f, Projectile.width * 0.5f), Main.rand.NextFloat(-Projectile.height * 0.5f, Projectile.height * 0.5f));
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                int dust = Dust.NewDust(pos, 4, 4, type, dir * 1f, 0f, 60, default, Main.rand.NextFloat(1.2f, 1.8f));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = new Vector2(dir * Main.rand.NextFloat(0.5f, 2f), Main.rand.NextFloat(-1f, 1f));
            }
            Lighting.AddLight(Projectile.Center, 0.6f, 0.35f, 0.1f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 4 * 60);
        }
    }
}
