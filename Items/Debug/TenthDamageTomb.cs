using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Debug
{
    public class TenthDamageTomb : ModItem
    {
        public override string Texture => "Terraria/Images/Item_" + ItemID.Tombstone;

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 30;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.UseSound = SoundID.Item14;
            Item.rare = ItemRarityID.Master;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer)
            {
                return true;
            }

            Rectangle clickArea = Utils.CenteredRectangle(Main.MouseWorld, new Vector2(32f));
            NPC target = null;
            float nearest = float.MaxValue;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC candidate = Main.npc[i];
                if (!candidate.active || candidate.friendly || candidate.dontTakeDamage
                    || candidate.lifeMax <= 0 || !candidate.Hitbox.Intersects(clickArea))
                {
                    continue;
                }

                float distance = Vector2.DistanceSquared(candidate.Center, Main.MouseWorld);
                if (distance < nearest)
                {
                    nearest = distance;
                    target = candidate;
                }
            }

            if (target == null)
            {
                return true;
            }

            int damage = Math.Max(1, (int)Math.Ceiling(target.lifeMax * 0.10f));
            NPC.HitInfo hit = new NPC.HitInfo
            {
                Damage = damage,
                HitDirection = target.Center.X >= player.Center.X ? 1 : -1,
                Knockback = 0f,
                Crit = false,
                DamageType = DamageClass.Default,
            };

            target.lastInteraction = player.whoAmI;
            target.playerInteraction[player.whoAmI] = true;
            target.StrikeNPC(hit);

            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                NetMessage.SendStrikeNPC(target, hit, player.whoAmI);
            }

            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "DebugDamage", "Click an enemy to deal 10% of its maximum life"));
        }
    }
}
