using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Bases;
using tsorcRevamp.Content.Projectiles;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol
{
    public class StardustDelivery : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));
        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.timeLeft = 900;
            Projectile.extraUpdates = 300;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            float projSpeed = 1f;
            float Scale = 0.5f;
            int Alpha = 0;

            Lighting.AddLight(Projectile.Center, Color.Navy.ToVector3() * 2f);
            Dust.NewDust(Projectile.TopLeft, Projectile.width, Projectile.height, DustID.UltraBrightTorch, 0, 0, Alpha, default, Scale);

            Projectile.velocity = Projectile.Center.DirectionTo(player.Center) * projSpeed;

            if (Projectile.Hitbox.Intersects(player.Hitbox))
            {
                Projectile.Kill();
            }
        }
        public override void OnKill(int timeLeft)
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<RuneterraGauntletPlayer>();
            Rectangle PlayerRect = Utils.CenteredRectangle(player.Center, player.Size);
            modPlayer.CenterOfTheUniverseStardustCount += 1;
            if (modPlayer.CenterOfTheUniverseStardustCount >= 10)
            {
                CombatText.NewText(PlayerRect, Color.Navy, LangUtils.GetTextValue("Items.CenterOfTheUniverse.MeteorReady"));
            }
            else
            {
                CombatText.NewText(PlayerRect, Color.Navy, LangUtils.GetTextValue("Items.CenterOfTheUniverse.StackGained", modPlayer.CenterOfTheUniverseStardustCount));
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}