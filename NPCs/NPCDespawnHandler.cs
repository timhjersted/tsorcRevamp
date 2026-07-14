using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using tsorcRevamp.Buffs;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Buffs.Runeterra.Magic;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Buffs.Weapons;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Buffs.Weapons.Summon.WhipDebuffs;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Accessories.Defensive;
using tsorcRevamp.Items.Accessories.Damage;
using tsorcRevamp.Items.Armors.Melee;
using tsorcRevamp.Items.Debug;
using tsorcRevamp.Items.ItemCrates;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.VanillaItems;
using tsorcRevamp.Items.Weapons.Magic.Runeterra;
using tsorcRevamp.Items.Weapons.Ranged;
using tsorcRevamp.Items.Weapons.Ranged.Runeterra;
using tsorcRevamp.Items.Weapons.Ranged.Specialist;
using tsorcRevamp.Items.Weapons.Summon;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Items.Weapons.Summon.Whips;
using tsorcRevamp.Items.Weapons.Throwing;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;
using tsorcRevamp.Projectiles.Ranged;
using tsorcRevamp.Projectiles.Summon;
using tsorcRevamp.Projectiles.Summon.Archer;
using tsorcRevamp.Projectiles.Summon.SamuraiBeetle;
using tsorcRevamp.Projectiles.Summon.Whips;
using tsorcRevamp.Projectiles.Summon.Whips.Dominatrix;
using tsorcRevamp.Projectiles.Summon.Whips.EnchantedWhip;
using tsorcRevamp.Projectiles.Summon.Whips.PolarisLeash;
using tsorcRevamp.Projectiles.VFX;
using tsorcRevamp.Utilities;
using tsorcRevamp;

namespace tsorcRevamp.NPCs
{
    ///<summary> 
    ///Handles boss despawning and targeting.
    ///This exists to simplify AI code.
    ///Create an instance of this class in SetDefaults, call targetAndDespawn(npcID) at the start of their AI, and removing any existing targeting or despawning.
    ///</summary>
    public class NPCDespawnHandler
    {
        ///<summary> 
        ///Handles all targeting and despawning.
        ///</summary> 
        ///<param name="despawnFlavorText">The custom text this boss displays when it despawns</param>
        ///<param name="textColor">The color of the despawn text</param>
        ///<param name="DustType">The ID of the dust this NPC should create an explosion of upon despawning</param>
        ///<param name="range">The boss will despawn if any player gets further away than this. -1 means infinite range.</param>
        public NPCDespawnHandler(string despawnFlavorText, Color textColor, int DustType, float range = -1)
        {
            despawnText = despawnFlavorText;
            despawnTextColor = textColor;
            despawnDustType = DustType;

            if (range > 0) //Pre-emptively square it so we don't have to do so later
            {
                range *= range;
            }
            despawnRange = range;
        }

        ///<summary> 
        ///Handles all targeting and despawning.
        ///</summary> 
        ///<param name="DustType">The ID of the dust this NPC should create an explosion of upon despawning</param>
        ///<param name="range">The boss will despawn if any player gets further away than this. -1 means infinite range.</param>
        public NPCDespawnHandler(int DustType, float range = -1)
        {
            despawnDustType = DustType;
            if (range > 0)
            {
                range *= range;
            }
            despawnRange = range;
        }

        readonly string despawnText;
        readonly Color despawnTextColor;
        readonly int despawnDustType;
        bool hasTargeted = false;
        int targetCount = 0;
        readonly int[] targetIDs = new int[256];
        readonly bool[] targetAlive = new bool[256];
        int despawnTime = -1;
        float despawnRange;
        int OutOfBoundsTimer = 600;

        public bool IsDespawning => despawnTime >= 0;

        ///<summary> 
        ///Handles all targeting and despawning.
        ///</summary>         
        ///<param name="npcID">The ID of the NPC in question.</param>
        public bool TargetAndDespawn(int npcID)
        {

            //When despawning, we set timeLeft to 240. If that's been done, we don't need to check for players or target anyone anymore.
            if (despawnTime < 0)
            {
                //Only run this once. Gets all active players and throws them into these arrays so we can track their status.
                if (!hasTargeted)
                {
                    foreach (Player player in Main.player)
                    {
                        //For some reason, Main.player always has 255 entries. This ensures we're only pulling real players from it.
                        if (player.active && player.name != "MPTestDummy")
                        {
                            targetIDs[targetCount] = player.whoAmI;
                            targetAlive[targetCount] = true;
                            targetCount++;
                        }
                    }
                    hasTargeted = true;
                }


                //Go through the target list. If everyone has died once, despawn. Else, target the closest one that has not yet died.
                //It's important that it only targets players who haven't died, because otherwise one living player could hide far away while the other repeatedly respawned and fought the boss.
                //With this, it will intentionally seek out those it has not yet killed instead.
                bool viableTarget = false;
                float closestPlayerDistance = float.MaxValue;
                float oldTarget = Main.npc[npcID].target;
                bool foundOutOfBoundsPlayer = false;

                //Iterate through all tracked players in the array
                for (int i = 0; i < targetCount; i++)
                {
                    //For each of them, check if they're dead. If so, mark it down in targetAlive.
                    if (Main.player[targetIDs[i]].dead && targetAlive[i])
                    {
                        targetAlive[i] = false;
                    }
                    else if (targetAlive[i] && Main.player[targetIDs[i]].active)
                    {
                        //If it found a player that hasn't been killed yet, then don't despawn
                        viableTarget = true;
                        //Check if they're the closest one, and if so target them
                        float distance = Vector2.DistanceSquared(Main.player[targetIDs[i]].position, Main.npc[npcID].position);
                        if (distance < closestPlayerDistance)
                        {
                            closestPlayerDistance = distance;
                            Main.npc[npcID].target = targetIDs[i];
                        }
                        if (despawnRange > 0 && !foundOutOfBoundsPlayer && Vector2.DistanceSquared(Main.player[targetIDs[i]].Center, tsorcRevampWorld.BossIDsAndCoordinates[Main.npc[npcID].type]) * 16 > despawnRange)
                        {
                            if (OutOfBoundsTimer == 600)
                            {
                                UsefulFunctions.BroadcastText(Main.npc[npcID].TypeName + " " + LangUtils.GetTextValue("NPCs.BossOutOfRange"), Color.Yellow);
                            }
                            OutOfBoundsTimer--;

                            //If players have been out of bounds for more than 10 seconds, then despawn the boss
                            if (OutOfBoundsTimer == 0)
                            {
                                for (int j = 0; j < targetAlive.Length; j++)
                                {
                                    targetAlive[j] = false;
                                }
                            }
                            foundOutOfBoundsPlayer = true;
                        }
                    }
                }

                //If a npc changes targets, sync it
                if (oldTarget != Main.npc[npcID].target)
                {
                    Main.npc[npcID].netUpdate = true;
                }

                //If there's no player that has not yet died, then despawn.
                if (!viableTarget)
                {
                    if (despawnText != null)
                    {
                        UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Player.AllDied"), Color.Yellow);
                        UsefulFunctions.BroadcastText(despawnText, despawnTextColor);
                    }
                    despawnTime = 240;
                }
            }
            else
            {
                //Adios
                if (despawnTime == 0)
                {
                    for (int i = 0; i < 60; i++)
                    {
                        int dustID = Dust.NewDust(Main.npc[npcID].position, Main.npc[npcID].width, Main.npc[npcID].height, despawnDustType, Main.rand.Next(-12, 12), Main.rand.Next(-12, 12), 150, default, 7f);
                        Main.dust[dustID].noGravity = true;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        UsefulFunctions.DespawnFlash(Main.npc[npcID].Center);
                    }
                    Main.npc[npcID].active = false;
                }
                else
                {
                    int dustID = Dust.NewDust(Main.npc[npcID].position, Main.npc[npcID].width, Main.npc[npcID].height, despawnDustType, Main.rand.Next(-12, 12), Main.rand.Next(-12, 12), 150, default, 1f);
                    Main.dust[dustID].noGravity = true;
                    despawnTime--;
                }

                //The frame before despawning, we return true to let the NPC's AI know it's about to get despawned. This allows it to do anything it needs to with that information (like re-actuating the pyramid)
                if (despawnTime == 1)
                {
                    return true;
                }
            }
            return false;
        }


    }
}
