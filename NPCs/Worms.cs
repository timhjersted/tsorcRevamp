using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs
{
    class Worms : GlobalNPC
    {
        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (tsorcRevamp.DestroyerSegments.Contains(npc.type)) //destroyer sword/item dmg reduction (flat, can't alter item dmg because it's a permanent stat)
            { //could do some trickery with modifydamage in global item but this makes more sense
                modifiers.FinalDamage *= 0.85f;
            }
            if (tsorcRevamp.JungleWyvernSegments.Contains(npc.type)) 
            {
                modifiers.FinalDamage *= 0.9f;
            }
            if (tsorcRevamp.EaterOfWorldsSegments.Contains(npc.type))
            {
                modifiers.FinalDamage *= 0.95f;
            }
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (tsorcRevamp.DestroyerSegments.Contains(npc.type))
            {
                if (projectile.IsMinionOrSentryRelated) //destroyer minion dmg reduction (flat, can't alter minion damage because they're mostly permanent projectiles)
                {
                    modifiers.FinalDamage *= 0.7f;
                }
            }
            if (tsorcRevamp.JungleWyvernSegments.Contains(npc.type)) 
            {
                if (projectile.IsMinionOrSentryRelated)
                {
                    modifiers.FinalDamage *= 0.8f;
                }
            }
            if (tsorcRevamp.JungleWyvernSegments.Contains(npc.type))
            {
                if (projectile.IsMinionOrSentryRelated)
                {
                    modifiers.FinalDamage *= 0.9f;
                }
            }
        }
        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            /*if (DestroyerSegments.Contains(npc.type))
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].type == NPCID.TheDestroyer || Main.npc[i].type == NPCID.TheDestroyerBody || Main.npc[i].type == NPCID.TheDestroyerTail)
                    {
                        Main.npc[i].immune[player.whoAmI] = 5;
                    }
                }
            }*/
            base.OnHitByItem(npc, player, item, hit, damageDone);
        }
        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (tsorcRevamp.DestroyerSegments.Contains(npc.type))
            {
                if (!projectile.IsMinionOrSentryRelated)
                {
                    projectile.damage = (int)(projectile.damage * 0.8f);
                }
                /*for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (DestroyerSegments.Contains(Main.npc[i].type))
                    {
                        Main.npc[i].immune[projectile.owner] = 5;
                    }
                }*/
            }
            if (tsorcRevamp.JungleWyvernSegments.Contains(npc.type))
            {
                if (!projectile.IsMinionOrSentryRelated)
                {
                    projectile.damage = (int)(projectile.damage * 0.85f);
                }
            }
            if (tsorcRevamp.JungleWyvernSegments.Contains(npc.type))
            {
                if (!projectile.IsMinionOrSentryRelated)
                {
                    projectile.damage = (int)(projectile.damage * 0.92f);
                }
            }
            base.OnHitByProjectile(npc, projectile, hit, damageDone);
        }
    }
}
