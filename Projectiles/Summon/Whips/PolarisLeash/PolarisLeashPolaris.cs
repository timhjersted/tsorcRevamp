using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Buffs.Weapons.Summon.WhipDebuffs;

namespace tsorcRevamp.Projectiles.Summon.Whips.PolarisLeash
{

    public class PolarisLeashPolaris : ModProjectile
    {
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 18;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.SummonMeleeSpeed;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().CursorPosition = Main.MouseWorld; //this must be LocalPlayer because it's only supposed to set your own cursor position to that value, since it's a player instanced value

            if (Main.netMode == NetmodeID.MultiplayerClient && Main.GameUpdateCount % 6 == 0)
            {
                ModPacket cursorPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                cursorPacket.Write(tsorcPacketID.SyncOwnerCursor);
                cursorPacket.Write((byte)player.whoAmI);
                cursorPacket.WriteVector2(player.GetModPlayer<tsorcRevampPlayer>().CursorPosition);
                cursorPacket.Send();
            }

            Projectile.velocity = Projectile.Center.DirectionTo(modPlayer.CursorPosition) * 10;

            if (player.dead || !player.active)
            {
                player.ClearBuff(ModContent.BuffType<PolarisLeashBuff>());
            }
            if (player.HasBuff(ModContent.BuffType<PolarisLeashBuff>()))
            {
                Projectile.timeLeft = 2;
            }
            Dust.NewDust(Projectile.Center, 10, 10, DustID.IceRod, 0f, 0f, 150, Color.MediumSpringGreen, 1f);
            Lighting.AddLight(Projectile.Center, Color.MediumSpringGreen.ToVector3() * 1f);
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<PolarisLeashDebuff>(), (int)(ModdedWhipProjectile.DefaultWhipDebuffDuration * 60 * Main.player[Projectile.owner].GetModPlayer<tsorcRevampPlayer>().SummonTagDuration));
            Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI;
        }
    }
}
