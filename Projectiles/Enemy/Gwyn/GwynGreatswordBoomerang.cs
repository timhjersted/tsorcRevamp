using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gwyn's hurled greatsword (shares the SwordOfGwyn item art): spins across the arena trailing
    ///fire, arcs at its apex, and returns to his hand — a full-lane horizontal wall that punishes
    ///edge-campers. While it flies, Gwyn is weaponless (the punish window if you're close).
    ///ai[0] = owner NPC whoAmI, ai[1] = outbound flight ticks before the return begins.
    ///</summary>
    class GwynGreatswordBoomerang : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/Broadswords/SwordOfGwyn";

        const float ReturnAccel = 1.1f;
        const float ReturnTopSpeed = 19f;
        const float CatchRange = 52f;

        int ParentIndex => (int)Projectile.ai[0];
        int OutboundTicks => (int)Projectile.ai[1] > 0 ? (int)Projectile.ai[1] : 50;
        bool Returning => Projectile.localAI[0] > OutboundTicks;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 60;
            Projectile.height = 60;
            Projectile.tileCollide = false; // a god's flaming blade doesn't stop for terrain
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 360;
            Projectile.light = 0.7f;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation += 0.38f * (Projectile.velocity.X >= 0f ? 1f : -1f);

            NPC parent = ParentIndex >= 0 && ParentIndex < Main.maxNPCs ? Main.npc[ParentIndex] : null;
            if (Returning)
            {
                if (parent == null || !parent.active)
                {
                    Projectile.Kill();
                    return;
                }
                //Accelerating home to his hand
                Vector2 toHand = parent.Center - Projectile.Center;
                if (toHand.Length() < CatchRange)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab with { Volume = 0.6f, Pitch = -0.3f }, Projectile.Center);
                    Projectile.Kill();
                    return;
                }
                Vector2 desired = toHand.SafeNormalize(Vector2.UnitX) * ReturnTopSpeed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.06f);
                if (Projectile.velocity.Length() < ReturnTopSpeed)
                {
                    Projectile.velocity += toHand.SafeNormalize(Vector2.Zero) * ReturnAccel * 0.25f;
                }
                Projectile.timeLeft = System.Math.Max(Projectile.timeLeft, 30); // never expire mid-return
            }
            else
            {
                //Outbound: slight decay so it visibly stalls at the apex before turning back
                Projectile.velocity *= 0.985f;
            }

            //Blazing spin trail
            for (int i = 0; i < 3; i++)
            {
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, type, 0f, 0f, 60, default, 1.4f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = Projectile.velocity * 0.15f + Main.rand.NextVector2Circular(1f, 1f);
            }
            Lighting.AddLight(Projectile.Center, 0.9f, 0.5f, 0.15f);

            if (Main.rand.NextBool(14))
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.35f, Pitch = -0.2f }, Projectile.Center);
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 6 * 60);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation,
                texture.Size() / 2f, 0.85f, SpriteEffects.None, 0);
            return false;
        }
    }
}
