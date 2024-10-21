using Microsoft.Xna.Framework;
using System.Drawing;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic
{
    public class EnergyStrikeScrollProjectile : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 12;
        }

        public override void SetDefaults()
        {
            Projectile.width = 44;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.MagicSummonHybrid;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Projectile.SentryLifeTime + 1;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 30;
            Projectile.scale = 2;
        }
        public override void OnSpawn(IEntitySource source)
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().CursorPosition = Main.MouseWorld;

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket cursorPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                cursorPacket.Write(tsorcPacketID.SyncOwnerCursor);
                cursorPacket.Write((byte)player.whoAmI);
                cursorPacket.WriteVector2(player.GetModPlayer<tsorcRevampPlayer>().CursorPosition);
                cursorPacket.Send();
            }
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            Projectile.velocity = Vector2.Zero;
            if (Projectile.timeLeft > Projectile.SentryLifeTime)
            {
                 Projectile.velocity = Projectile.Center.DirectionTo(modPlayer.CursorPosition).SafeNormalize(Vector2.Zero) * Projectile.Center.Distance(modPlayer.CursorPosition);
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].type == Type && Main.projectile[i].timeLeft < Projectile.timeLeft)
                {
                    Main.projectile[i].Kill();
                }
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 3)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 12)
            {
                Projectile.frame = 0;
                return;
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            //Projectile.velocity = Vector2.Zero;
            return false;
        }
    }
}
