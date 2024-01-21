using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Projectiles.VFX
{
    public class StackDelivery : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.timeLeft = 900;
            Projectile.extraUpdates = 900;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            float projSpeed = 1f;
            float Scale = 0.5f;
            int Alpha = 0;

            switch (Projectile.ai[0])
            {
                case 0: //Orb of Deception
                    {
                        Lighting.AddLight(Projectile.Center, Color.LightSteelBlue.ToVector3() * 2f);
                        Dust.NewDust(Projectile.TopLeft, Projectile.width, Projectile.height, DustID.Flare_Blue, 0, 0, Alpha, default, Scale);
                        break;
                    }
                case 1: //Orb of Flame
                    {
                        Lighting.AddLight(Projectile.Center, Color.Firebrick.ToVector3() * 2f);
                        Dust.NewDust(Projectile.TopLeft, Projectile.width, Projectile.height, DustID.Torch, 0, 0, Alpha, default, Scale);
                        break;
                    }
                case 2: //Orb of Spirituality
                    {
                        Lighting.AddLight(Projectile.Center, Color.Pink.ToVector3() * 2f);
                        Dust.NewDust(Projectile.TopLeft, Projectile.width, Projectile.height, DustID.VenomStaff, 0, 0, Alpha, default, Scale);
                        break;
                    }
                case 3: //Center of the Universe
                    {
                        Lighting.AddLight(Projectile.Center, Color.Navy.ToVector3() * 2f);
                        Dust.NewDust(Projectile.TopLeft, Projectile.width, Projectile.height, DustID.UltraBrightTorch, 0, 0, Alpha, default, Scale);
                        break;
                    }
            }

            Projectile.velocity = Projectile.Center.DirectionTo(player.Center) * projSpeed;

            if (Projectile.Hitbox.Intersects(player.Hitbox))
            {
                Projectile.Kill();
            }
        }
        public override void OnKill(int timeLeft)
        {
            Player player = Main.player[Projectile.owner];
            Rectangle PlayerRect = Utils.CenteredRectangle(player.Center, player.Size);
            switch (Projectile.ai[0])
            {
                case float EssenceThief when (EssenceThief >= 0 && EssenceThief < 3):
                    {
                        player.GetModPlayer<tsorcRevampPlayer>().EssenceThief += (int)Projectile.ai[1];
                        if (player.GetModPlayer<tsorcRevampPlayer>().EssenceThief >= 9)
                        {
                            CombatText.NewText(PlayerRect, Color.Lime, LangUtils.GetTextValue("Items.OrbOfDeception.OrbFilled"));
                        }
                        else
                        {
                            CombatText.NewText(PlayerRect, Color.Lime, LangUtils.GetTextValue("Items.OrbOfDeception.StackGained", player.GetModPlayer<tsorcRevampPlayer>().EssenceThief));
                        }
                        break;
                    }
                case 3:
                    {
                        player.GetModPlayer<tsorcRevampPlayer>().CenterOfTheUniverseStardustCount += (int)Projectile.ai[1];
                        if (player.GetModPlayer<tsorcRevampPlayer>().CenterOfTheUniverseStardustCount >= 10)
                        {
                            CombatText.NewText(PlayerRect, Color.Navy, LangUtils.GetTextValue("Items.CenterOfTheUniverse.MeteorReady"));
                        }
                        else
                        {
                            CombatText.NewText(PlayerRect, Color.Navy, player.GetModPlayer<tsorcRevampPlayer>().CenterOfTheUniverseStardustCount + LangUtils.GetTextValue("Items.CenterOfTheUniverse.StackGained", player.GetModPlayer<tsorcRevampPlayer>().CenterOfTheUniverseStardustCount));
                        }
                        break;
                    }
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}