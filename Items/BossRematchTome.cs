using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace tsorcRevamp.Items
{
    public class BossRematchTome : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Allows you to re-summon any boss you have defeated" +
                "\nLeft click to summon the selected boss, right click to select the one you want" +
                "\nBosses summoned in this way will not drop dark souls, guardian souls or stamina vessels" +
                "\nHowever, they will drop the rest of their normal loot" +
                "\n[Developer note: Not all bosses can be summoned this way yet, but the rest will be added over time]"); */
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.useTime = 1;
            Item.useAnimation = 1;
            Item.UseSound = SoundID.Item11;
            Item.useTurn = true;
            Item.noMelee = true;
            Item.value = 10000;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.rare = ItemRarityID.Green;
            Item.shootSpeed = 24f;
            Item.shoot = ModContent.ProjectileType<Projectiles.BlackFirelet>();
        }

        int index = 0;
        List<NPCDefinition> keys;
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string selectedBoss;
            if(tsorcRevampWorld.NewSlain == null || tsorcRevampWorld.NewSlain.Keys.Count == 0)
            {
                selectedBoss = "No rematchable bosses defeated!";
            }
            else
            {
                keys = new List<NPCDefinition>(tsorcRevampWorld.NewSlain.Keys);
                RemoveBannedBosses(keys);
                if(keys.Count == 0)
                {
                    selectedBoss = "No rematchable bosses defeated!";
                }
                else
                {
                    NPC temp = new NPC();
                    temp.SetDefaults(keys[index].Type);

                    selectedBoss = temp.GivenOrTypeName;

                    if (selectedBoss.Contains("Slogra"))
                    {
                        selectedBoss = "Slogra and Gaibon";
                    }
                }
            }
            
            tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "selectedboss", $"Currently selected boss: {selectedBoss}"));
            base.ModifyTooltips(tooltips);
        }

        public override bool? UseItem(Player player)
        {
            keys = new List<NPCDefinition>(tsorcRevampWorld.NewSlain.Keys);
            if(player.whoAmI != Main.myPlayer || Main.netMode == NetmodeID.Server)
            {
                return false;
            }

            RemoveBannedBosses(keys);
            if (tsorcRevampWorld.NewSlain == null || tsorcRevampWorld.NewSlain.Keys.Count == 0 || keys.Count == 0)
            {
                UsefulFunctions.BroadcastText("No rematchable bosses defeated!");
                return true;
            }

            if (player.altFunctionUse == 2)
            {                
                if (keys.Count > 1)
                {
                    if (index >= keys.Count - 1)
                    {
                        index = 0;
                    }
                    else
                    {
                        index++;
                    }
                }
                NPC temp = new NPC();
                temp.SetDefaults(keys[index].Type);

                string selectedBoss = temp.GivenOrTypeName;

                if (selectedBoss.Contains("Slogra"))
                {
                    selectedBoss = "Slogra and Gaibon";
                }
                if (keys[index].Type == ModContent.NPCType<NPCs.Bosses.Okiku.FirstForm.DarkShogunMask>())
                {
                    selectedBoss = "Attraidies First Phase";
                }
                if (keys[index].Type == ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>())
                {
                    selectedBoss = "Attraidies Final Form";
                }

                UsefulFunctions.BroadcastText("Boss selected: " + selectedBoss);
            }
            else
            {
                
                if (!tsorcRevampWorld.BossAlive)
                {
                    if (keys[index].Type == ModContent.NPCType<NPCs.Bosses.Slogra>())
                    {
                        if (Main.netMode == NetmodeID.SinglePlayer)
                        {
                            NPC.NewNPCDirect(Item.GetSource_FromThis(), player.Center + new Vector2(0, -300), ModContent.NPCType<NPCs.Bosses.Gaibon>());
                        }
                        else
                        {
                            ModPacket spawnNPCPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                            spawnNPCPacket.Write(tsorcPacketID.SpawnNPC);
                            spawnNPCPacket.Write(ModContent.NPCType<NPCs.Bosses.Gaibon>());
                            spawnNPCPacket.WriteVector2(player.Center + new Vector2(0, -300));
                            spawnNPCPacket.Send();
                        }
                    }
                    if (Main.netMode == NetmodeID.SinglePlayer)
                    {
                        NPC.NewNPCDirect(Item.GetSource_FromThis(), player.Center + new Vector2(0, -300), keys[index].Type);
                    }
                    else
                    {
                        ModPacket spawnNPCPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
                        spawnNPCPacket.Write(tsorcPacketID.SpawnNPC);
                        spawnNPCPacket.Write(keys[index].Type);
                        spawnNPCPacket.WriteVector2(player.Center + new Vector2(0, -300));
                        spawnNPCPacket.Send();
                    }
                }
                else
                {
                    UsefulFunctions.BroadcastText("Can not summon a second boss while one is alive!");
                }
            }
            return base.UseItem(player);
        }

        public void RemoveBannedBosses(List<NPCDefinition> keys)
        {
            //These are pieces or alternate phases of bosses which automatically get spawned by their 'parent' boss and should not be spawned on their own
            List<int> bannedBosses = new List<int>();
            bannedBosses.Add(ModContent.NPCType<NPCs.Bosses.Gaibon>());
            bannedBosses.Add(ModContent.NPCType<NPCs.Bosses.WyvernMage.MechaDragonHead>());
            bannedBosses.Add(ModContent.NPCType<NPCs.Bosses.Serris.SerrisX>());
            bannedBosses.Add(ModContent.NPCType<NPCs.Bosses.Fiends.LichKingDisciple>());
            bannedBosses.Add(ModContent.NPCType<NPCs.Bosses.Fiends.LichKingSerpentHead>());
            bannedBosses.Add(ModContent.NPCType<NPCs.Bosses.SuperHardMode.DarkCloudMirror>());
            bannedBosses.Add(ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonHead>());
            bannedBosses.Add(NPCID.EaterofWorldsBody);
            bannedBosses.Add(NPCID.EaterofWorldsTail);

            //These are bugged and need to be fixed
            bannedBosses.Add(ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernHead>()); //Head is detached from its body
            bannedBosses.Add(ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>()); //Head is detached from its body
            bannedBosses.Add(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>()); //Head is detached from its body
            bannedBosses.Add(ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>()); //Just spawns the head, nothing more
            bannedBosses.Add(ModContent.NPCType<NPCs.Enemies.RedKnight>()); //Bugged loot

            //Check if the keys list has any of the banned bosses, and if so remove them
            for (int i = 0; i < bannedBosses.Count; i++)
            {
                if (keys.Contains(new NPCDefinition(bannedBosses[i])))
                {
                    keys.Remove(new NPCDefinition(bannedBosses[i]));
                }
            }
        }

        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            return false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 25);
            recipe.AddIngredient(ItemID.Book, 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register(); 

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 25);
            recipe2.AddIngredient(ItemID.SpellTome, 1);
            recipe2.AddTile(TileID.DemonAltar);
            recipe2.Register();
        }
    }
}