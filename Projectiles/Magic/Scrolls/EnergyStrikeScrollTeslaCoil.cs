using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Projectiles.Magic.Scrolls
{
    public class EnergyStrikeScrollTeslaCoil : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
        }

        public float Timer
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }
        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 60;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.MagicSummonHybrid;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Projectile.SentryLifeTime + 1;
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

            Lighting.AddLight(Projectile.Center, Color.Blue.ToVector3() * 2f);

            Timer++;

            Projectile.velocity = Vector2.Zero;
            if (Projectile.timeLeft > Projectile.SentryLifeTime)
            {
                 Projectile.velocity = Projectile.Center.DirectionTo(modPlayer.CursorPosition) * Projectile.Center.Distance(modPlayer.CursorPosition);
            }

            if (Timer >= 20 && player.whoAmI == Main.myPlayer) // The magnet sphere uses a timer of 9
            {
                var npcs = Find5NPCsWithinRange(450);
                foreach (NPC npc in npcs)
                {
                    Projectile p = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<EnergyStrikeScrollZap>(), Projectile.damage, 0f, Main.myPlayer, 0, 0, npc.whoAmI);
                    p.originalDamage = Projectile.damage;
                    p.netUpdate = true;
                }
                Timer = 0;
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 3)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= Main.projFrames[Type])
            {
                Projectile.frame = 0;
                return;
            }
        }
        public List<NPC> Find5NPCsWithinRange(float maxDetectDistance)
        {
            List<NPC> npcs = [];
            int added = 0;

            float sqrMaxDetectDistance = maxDetectDistance * maxDetectDistance;
            for (int k = 0; k < Main.maxNPCs && added < 5; k++)
            {
                NPC target = Main.npc[k];
                if (target.CanBeChasedBy() || target.type == NPCID.TargetDummy)
                {
                    float sqrDistanceToTarget = Vector2.DistanceSquared(target.Center, Projectile.Center);

                    if (sqrDistanceToTarget < sqrMaxDetectDistance)
                    {
                        npcs.Add(target);
                        added++;
                    }
                }
            }

            return npcs;
        }
        public override bool? Colliding(Microsoft.Xna.Framework.Rectangle projHitbox, Microsoft.Xna.Framework.Rectangle targetHitbox)
        {
            return false;
        }
    }
}
