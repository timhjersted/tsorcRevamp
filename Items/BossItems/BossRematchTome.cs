using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;

namespace tsorcRevamp.Items.BossItems
{
    public class BossRematchTome : ModItem
    {
        public override void SetStaticDefaults()
        {
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

        

        public static List<int> PreHardmodeBossIDs = new List<int>
        {
            ModContent.NPCType<NPCs.Special.LeonhardPhase1>(),
            ModContent.NPCType<NPCs.Enemies.RedKnight>(),
            NPCID.EyeofCthulhu,
            NPCID.BrainofCthulhu,
            NPCID.EaterofWorldsHead,
            ModContent.NPCType<NPCs.Bosses.AncientOolacileDemon>(),
            NPCID.KingSlime,
            ModContent.NPCType<NPCs.Bosses.Slogra>(),
            NPCID.QueenBee,
            NPCID.Skeleton,
            NPCID.Deerclops,
            ModContent.NPCType<NPCs.Bosses.JungleWyvern.JungleWyvernHead>(),
            ModContent.NPCType<NPCs.Bosses.AncientDemon>(),
            NPCID.WallofFlesh
        };
                
        public static List<int> HardmodeBossIDs = new List<int>
        {
            NPCID.QueenSlimeBoss,
            ModContent.NPCType<NPCs.Bosses.TheRage>(),
            ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>(),
            ModContent.NPCType<NPCs.Bosses.TheSorrow>(),
            ModContent.NPCType<NPCs.Bosses.Serris.SerrisX>(),
            ModContent.NPCType<NPCs.Bosses.Death>(),
            ModContent.NPCType<NPCs.Bosses.TheHunter>(),
            NPCID.TheDestroyer,
            ModContent.NPCType<NPCs.Bosses.PrimeV2.TheMachine>(),
            ModContent.NPCType<NPCs.Bosses.Cataluminance>(),
            NPCID.Plantera,
            NPCID.Golem,
            NPCID.HallowBoss,
            ModContent.NPCType<NPCs.Bosses.Okiku.FirstForm.DarkShogunMask>(),
            ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>()
        };

        
        public static List<int> SHMBossIDs = new List<int>
        {
            ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>(),
            ModContent.NPCType<NPCs.Bosses.SuperHardMode.Witchking>(),
            NPCID.MoonLordCore,
            ModContent.NPCType<NPCs.Bosses.SuperHardMode.Fiends.WaterFiendKraken>(),
            ModContent.NPCType<NPCs.Bosses.SuperHardMode.Fiends.EarthFiendLich>(),
            ModContent.NPCType<NPCs.Bosses.SuperHardMode.Fiends.FireFiendMarilith>(),
            ModContent.NPCType<NPCs.Bosses.SuperHardMode.Blight>(),
            ModContent.NPCType<NPCs.Bosses.SuperHardMode.AbysmalOolacileSorcerer>(),
            ModContent.NPCType<NPCs.Bosses.SuperHardMode.Artorias>(),
            ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>(),
            ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>(),
            ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>(),
            ModContent.NPCType<NPCs.Bosses.SuperHardMode.DarkCloud>(),
            ModContent.NPCType<NPCs.Bosses.SuperHardMode.Gwyn>(),
        };

        //Calls PhaseBossIDs for each phase, and glues the lists together with a tuple.
        public static Tuple<List<NPCDefinition>, List<NPCDefinition>, List<NPCDefinition>> GetDownedBossIDs()
        {
            return new Tuple<List<NPCDefinition>, List<NPCDefinition>, List<NPCDefinition>>(PhaseBossIDs(PreHardmodeBossIDs), PhaseBossIDs(HardmodeBossIDs), PhaseBossIDs(SHMBossIDs));
        }

        //Returns a list of all bosses downed during one phase of the game
        public static List<NPCDefinition> PhaseBossIDs(List<int> idList)
        {
            List<NPCDefinition> result = new List<NPCDefinition>();
            foreach (int id in idList)
            {
                if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(id)))
                {
                    result.Add(new NPCDefinition(id));
                }
                else
                {
                    result.Add(new NPCDefinition(NPCID.Bunny));
                }
            }

            return result;
        }


        public override bool CanUseItem(Player player)
        {
            return !UsefulFunctions.AnyProjectile(ModContent.ProjectileType<Projectiles.VFX.BossSelectVisuals>());
        }

        //TODO: Make it work like the Grand Design
        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer || Main.netMode == NetmodeID.Server)
            {
                return false;
            }
            if (tsorcRevampWorld.NewSlain == null || tsorcRevampWorld.NewSlain.Keys.Count == 0)
            {
                UsefulFunctions.BroadcastText(Language.GetTextValue("Mods.tsorcRevamp.Items.BossRematchTome.None"));
                return false;
            }
            if (tsorcRevampWorld.BossAlive)
            {
                UsefulFunctions.BroadcastText(Language.GetTextValue("Mods.tsorcRevamp.Items.BossRematchTome.Forbidden"));
                return false;
            }

            Projectile.NewProjectile(Item.GetSource_FromThis(), player.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.BossSelectVisuals>(), 0, 0, player.whoAmI);
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