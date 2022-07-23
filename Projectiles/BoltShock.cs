using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class BoltShock : ModProjectile
    {
        //WIP for the new bolt tome attack that Neph made

        public override void SetDefaults()
        {
            Projectile.hostile = true;
        }
        public override void AI()
        {

            
        }
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(Mod.Find<ModBuff>("ElectrocutedBuff").Type, 120);
            }

            
        }
        public override void Kill(int timeLeft)
        {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width / 2), Projectile.position.Y + (float)(Projectile.height / 2), Projectile.velocity.X, Projectile.velocity.Y, ModContent.ProjectileType<Bolt1Bolt>(), (this.Projectile.damage), 3.5f, Projectile.owner);
            
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit53 with { Volume = 0.8f, PitchVariance = 0.3f }, Projectile.Center);
        }

        public static Texture2D BoltStart;
        public static Texture2D BoltMiddle1;
        public static Texture2D BoltMiddle2;
        public static Texture2D BoltEnd;

        public override bool PreDraw(ref Color lightColor)
        {
            BoltStart = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/BoltBounceStart", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            BoltStart = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/BoltBounceStart", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            BoltStart = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/BoltBounceStart", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            BoltStart = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/BoltBounceStart", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            BoltStart = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/BoltBounceStart", ReLogic.Content.AssetRequestMode.ImmediateLoad);

            Main.instance.LoadProjectile(Projectile.type);
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;

            // Redraw the projectile with the color not influenced by light
            Vector2 drawOrigin = new Vector2(texture.Width * 0.5f, Projectile.height * 0.5f);
            

            return true;
        }
    }

}
