using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Buffs;
using tsorcRevamp.Content.Items.ConsumableSoul;
using tsorcRevamp.Content.Items.Weapons.Magic.GreatSoulArrowStaff;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

public class DarkSoulNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    /// <summary>
    /// Whoever killed this npc
    /// </summary>
    public Player Killer = Main.player.Last();

    public override void OnKill(NPC npc)
    {
        var modPlayer = Killer.GetModPlayer<DarkSoulPlayer>();
            if (npc.lifeMax > 5 && npc.value >= 10f || npc.boss)
            { //stop zero-value souls from dropping (the 'or boss' is for expert mode support)
                
                float multiplier = modPlayer.SoulsMultiplier() *
                             npc.GetGlobalNPC<GreatSoulArrowStaffNPC>().CheckSoulstruckAmplifier();

                int darkSoulQuantity = (int)(multiplier * npc.value);

                #region Bosses drop souls once
                if (npc.boss)
                {
                    if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(npc.type)))
                    {
                        darkSoulQuantity = 0;
                    }
                    else
                    {
                        // check whether the SHM boss was killed
                        if (npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Fiends.WaterFiendKraken>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Fiends.FireFiendMarilith>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Fiends.EarthFiendLich>()
                            || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>()
                            || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.OolacileSerpent.GreatSerpentHead>()
                            || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.AbysmalOolacileSorcerer>()
                            || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Artorias>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Blight>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>()
                            || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.DarkCloud>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Witchking>()) /*|| npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Gwyn>()) gwyn CLOSES the abyss portal!*/
                        {
                            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.SHM.BossDeath"), Color.Orange); 
                        }

                        if (npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>())
                        {
                            tsorcRevampWorld.isHellkiteDragonDead = true;
                        }

                        if (npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.OolacileSerpent.GreatSerpentHead>())
                        {
                            tsorcRevampWorld.isOolacileSerpentDead = true;
                        }

                        if (((npc.type == NPCID.EaterofWorldsHead) || (npc.type == NPCID.EaterofWorldsBody) || (npc.type == NPCID.EaterofWorldsTail)) && Main.invasionType == 0)
                        {
                            Main.StartInvasion();
                        }

                        if ((npc.type == ModContent.NPCType<NPCs.Bosses.TheSorrow>()) && Main.invasionType == 0)
                        {
                            Main.StartInvasion(3);
                        }

                        tsorcRevampWorld.PopulatePairedBosses();
                        //Paired bosses have to have their slain entries work different
                        if (tsorcRevampWorld.PairedBosses.Contains(npc.type))
                        {
                            for (int i = 0; i < tsorcRevampWorld.PairedBosses.Count; i++)
                            {
                                if (tsorcRevampWorld.PairedBosses[i] == npc.type)
                                {
                                    int pairedNPCOffset = -1;
                                    if (i % 2 == 0)
                                    {
                                        pairedNPCOffset = 1;
                                    }

                                    //If the other boss is not alive, then add them both. If not, don't.
                                    if (!NPC.AnyNPCs(tsorcRevampWorld.PairedBosses[i + pairedNPCOffset]))
                                    {
                                        tsorcRevampWorld.NewSlain.Add(new NPCDefinition(npc.type), 1);
                                        tsorcRevampWorld.NewSlain.Add(new NPCDefinition(tsorcRevampWorld.PairedBosses[i + pairedNPCOffset]), 1);
                                    }

                                    break;
                                }
                            }
                        }
                        else
                        {
                            tsorcRevampWorld.NewSlain.Add(new NPCDefinition(npc.type), 1);
                        }

                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.WorldData); //Slain only exists on the server. This tells the server to run NetSend(), which syncs this data with clients
                        }
                    }
                }
                #endregion

                #region EoW drops souls in a unique way
                if (((npc.type == NPCID.EaterofWorldsHead) || (npc.type == NPCID.EaterofWorldsBody) || (npc.type == NPCID.EaterofWorldsTail)))
                {

                    darkSoulQuantity = 24; //*72 for soul drops per eater, 1728 souls per one whole eater

                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<DarkSoulItem>(), darkSoulQuantity);
                }
                #endregion

                if (darkSoulQuantity > 0)
                {
                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<DarkSoulItem>(), darkSoulQuantity);
                }


                // Consumable Soul drops ahead - Current numbers give aprox. +20% souls

                float chance = 0.01f + (0.0005f * Main.LocalPlayer.GetModPlayer<DarkSoulPlayer>().ConsSoulChanceMult);
                //Main.NewText(chance);

                if (!(npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail || npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.Creeper))
                {

                    if ((npc.value >= 1) && (npc.value <= 200) && (Main.rand.NextFloat() < chance)) // 1% chance of all enemies between enemyValue 1 and 200 dropping FadingSoul aka 1/75
                    {
                        Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<FadingSoul>(), 1); // Zombies and eyes are 6 and 7 enemyValue, so will only drop FadingSoul
                    }

                    if ((npc.value >= 15) && (npc.value <= 2000) && (Main.rand.NextFloat() < chance)) // 1% chance of all enemies between enemyValue 10 and 2000 dropping LostUndeadSoul aka 1/75
                    {
                        Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<LostUndeadSoul>(), 1); // Most pre-HM enemies fall into this category
                    }

                    if ((npc.value >= 55) && (npc.value <= 10000) && (Main.rand.NextFloat() < chance)) // 1% chance of all enemies between enemyValue 50 and 10000 dropping NamelessSoldierSoul aka 1/75
                    {
                        Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<NamelessSoldierSoul>(), 1); // Most HM enemies fall into this category
                    }

                    if ((npc.value >= 150) && (npc.value <= 10000) && (Main.rand.NextFloat() < chance) && Main.hardMode) // 1% chance of all enemies between enemyValue 150 and 10000 dropping ProudKnightSoul aka 1/75
                    {
                        Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<ProudKnightSoul>(), 1);
                    }
                }
                //End consumable souls drops
            }
    }
}