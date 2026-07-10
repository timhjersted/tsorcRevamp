using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gwyn's Lord's Grasp: a hand of the First Flame that reaches out and seizes. Dust-drawn flaming
    ///claw that flies toward the player; on contact it immolates them (big damage, heavy On Fire!,
    ///hard knockback — the "seize and hurl"). Bypasses the comfort of range: it's the shield-turtle /
    ///spacing punish. ai[0] = parent NPC whoAmI (unused reach ref).
    ///</summary>
    class GwynFlameGrasp : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 46;
            Projectile.height = 46;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 55; // the reach window
            Projectile.light = 0.7f;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 0.99f;
            //A grasping claw of fire — five "fingers" of flame trailing the reach
            for (int i = 0; i < 6; i++)
            {
                Vector2 spread = Main.rand.NextVector2Circular(Projectile.width * 0.5f, Projectile.height * 0.5f);
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                int dust = Dust.NewDust(Projectile.Center + spread - new Vector2(2f), 4, 4, type, 0f, 0f, 40, default, 1.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = Projectile.velocity * 0.15f;
            }
            Lighting.AddLight(Projectile.Center, 1f, 0.5f, 0.15f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            //Seized and immolated
            target.AddBuff(BuffID.OnFire, 10 * 60);
            target.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 8f - new Vector2(0f, 4f);
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.5f, Pitch = 0.2f }, Projectile.Center);
            for (int i = 0; i < 14; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(4f, 4f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, vel.X, vel.Y, 40, default, 1.4f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
