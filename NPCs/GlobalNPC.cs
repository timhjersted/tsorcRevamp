using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;

namespace tsorcRevamp.NPCs {
    class tsorcRevampGlobalNPC : GlobalNPC {


        float enemyValue;
        float multiplier = 1f;
        int DarkSoulQuantity;

        public override bool InstancePerEntity => true;
        public bool DarkInferno = false;
        public bool CrimsonBurn = false;

        public override void ResetEffects(NPC npc) {
            DarkInferno = false;
            CrimsonBurn = false;
        }


        #region SetDefaults - Vanilla NPC Changes

        public override void SetDefaults(NPC npc) { //initial re-balance of HP and damage == 2/3
            if (npc.type == NPCID.AngryBones) {
                npc.lifeMax = 145;
                npc.damage = 33;
                npc.knockBackResist = 0.2f;
            }

            if (npc.type == NPCID.Antlion) {
                npc.lifeMax = 50;
                npc.damage = 46;
            }

            if (npc.type == NPCID.ArmoredSkeleton) {
                npc.damage = 43;
                npc.knockBackResist = 0.2f;
                npc.defense = 36;
                npc.value = 450;
            }

            if (npc.type == NPCID.BaldZombie) {
                npc.knockBackResist = 0.8f;
            }

            if (npc.type == NPCID.BigBoned) {
                npc.lifeMax = 200;
                npc.damage = 46;
                npc.knockBackResist = 0.68f;
                npc.value = 500;
            }

            if (npc.type == NPCID.BigEater) {
                npc.scale = 1.15f;
            }

            if (npc.type == NPCID.BigStinger) {
                npc.scale = 1.2f;
                npc.value = 400;
            }

            if (npc.type == NPCID.BlazingWheel) {
                npc.scale = 1.2f;
                npc.damage = 53;
            }

            if (npc.type == NPCID.BlueJellyfish) {
                npc.value = 50;
            }

            if (npc.type == NPCID.BoneSerpentBody) {
                npc.lifeMax = 1450;
                npc.damage = 20;
                npc.value = 2750;
                npc.defense = 12;

            }

            if (npc.type == NPCID.BoneSerpentHead) {
                npc.lifeMax = 1450;
                npc.damage = 50;
                npc.value = 2750;
                npc.defense = 2;
            }

            if (npc.type == NPCID.BoneSerpentTail) {
                npc.lifeMax = 1450;
                npc.value = 2500;
                npc.defense = 25;
                npc.knockBackResist = 0.1f;
            }

            if (npc.type == NPCID.ChaosBall) {
                npc.damage = 26;
            }

            if (npc.type == NPCID.ChaosElemental) {
                npc.lifeMax = 396;
                npc.damage = 46;
                npc.value = 1500;
                npc.defense = 25;
                npc.knockBackResist = 0.0f;
            }

            if (npc.type == NPCID.Clinger) {
                npc.lifeMax = 410;
                npc.value = 800;
            }

            if (npc.type == NPCID.Clown) {
                npc.damage = 50;
                npc.lifeMax = 10;
                npc.value = 1000;
                npc.defense = 20;
            }

            if (npc.type == NPCID.CorruptBunny) {
                npc.damage = 53;
                npc.value = 80;
            }

            if (npc.type == NPCID.CorruptGoldfish) {
                npc.value = 90;
            }

            if (npc.type == NPCID.CorruptSlime) {
                npc.scale = 1.1f;
            }

            if (npc.type == NPCID.CursedSkull) {
                npc.lifeMax = 53;
                npc.damage = 51;
                npc.value = 350;
                npc.defense = 8;
                npc.knockBackResist = 0f;
            }

            if (npc.type == NPCID.DarkCaster) {
                npc.lifeMax = 100;
                npc.damage = 46;
                npc.value = 250;
                npc.defense = 5;
                npc.knockBackResist = 0.2f;
            }

            if (npc.type == NPCID.Demon) {
                npc.lifeMax = 140;
                npc.value = 630;
                npc.defense = 23;
                npc.knockBackResist = 0.4f;
            }

            if (npc.type == NPCID.DevourerBody) {
                npc.defense = 8;
            }

            if (npc.type == NPCID.DevourerHead) {
                npc.defense = 8;
            }

            if (npc.type == NPCID.DiggerBody) {
                npc.scale = .9f;
            }

            if (npc.type == NPCID.DiggerHead) {
                npc.scale = .9f;
            }

            if (npc.type == NPCID.DiggerTail) {
                npc.scale = .9f;
            }

            if (npc.type == NPCID.DungeonSlime) {
                npc.value = 250;
            }

            if (npc.type == NPCID.EaterofSouls) {
                npc.value = 100;
            }

            if (npc.type == NPCID.EaterofWorldsBody) {
                npc.lifeMax = 180;
                npc.damage = 14;
                npc.defense = 5;
                npc.value = 1000;
                npc.buffImmune[BuffID.Poisoned] = true;
                npc.buffImmune[BuffID.OnFire] = true;
                npc.buffImmune[BuffID.Frostburn] = true;
                npc.buffImmune[BuffID.CursedInferno] = true;
            }

            if (npc.type == NPCID.EaterofWorldsHead) {
                npc.lifeMax = 180;
                npc.damage = 30;
                npc.defense = 22;
                npc.value = 1000;
                npc.buffImmune[BuffID.Poisoned] = true;
                npc.buffImmune[BuffID.OnFire] = true;
                npc.buffImmune[BuffID.Frostburn] = true;
                npc.buffImmune[BuffID.CursedInferno] = true;
            }

            if (npc.type == NPCID.EaterofWorldsTail) {
                npc.lifeMax = 155;
                npc.defense = 8;
                npc.value = 1000;
                npc.buffImmune[BuffID.Poisoned] = true;
                npc.buffImmune[BuffID.OnFire] = true;
                npc.buffImmune[BuffID.Frostburn] = true;
                npc.buffImmune[BuffID.CursedInferno] = true;
            }

            if (npc.type == NPCID.EyeofCthulhu) {
                npc.damage = 24; // I get the feeling he's going to be pretty damn tough in Expert mode
                npc.value = 250;
                npc.defense = 5;
                npc.knockBackResist = 0.2f;

                if ( Main.player[Main.myPlayer].ZoneJungle) {
                    if (Main.expertMode) {
                        npc.lifeMax = 3077; // Which is actually 4k hp in expert mode
                    }
                    npc.scale = 1.1f;
                }
            }

            if (npc.type == NPCID.FireImp) {
                npc.lifeMax = 112;
                npc.value = 300;
                npc.defense = 18;
            }

            if (npc.type == NPCID.GiantBat) {
                npc.lifeMax = 105;
                npc.damage = 49;
                npc.value = 250;
                npc.defense = 20;
                npc.knockBackResist = 0.2f;
            }

            if (npc.type == NPCID.GiantWormHead) {
                npc.damage = 13;
                npc.value = 90;
            }

            if (npc.type == NPCID.GoblinSorcerer) {
                npc.lifeMax = 100;
                npc.damage = 40;
                npc.value = 550;
                npc.defense = 10;
                npc.knockBackResist = 0.1f;
            }

            if (npc.type == NPCID.GoblinWarrior) {
                npc.damage = 36;
                npc.value = 350;
                npc.scale = 1.1f;
            }

            if (npc.type == NPCID.HeavySkeleton) {
                npc.value = 600;
                npc.defense = 41;
                npc.scale = 1.15f;
            }

            if (npc.type == NPCID.FireImp) {
                npc.lifeMax = 100;
                npc.damage = 46;
                npc.value = 250;
                npc.defense = 5;
                npc.knockBackResist = 0.2f;
            }

            if (npc.type == NPCID.Hellbat) {
                npc.damage = 46;
                npc.scale = 1.1f;
            }

            if (npc.type == NPCID.Hornet) {
                npc.lavaImmune = true;
                npc.value = 260;
                npc.buffImmune[BuffID.OnFire] = true;
            }

            if (npc.type == NPCID.Hornet || npc.type == NPCID.HornetFatty || npc.type == NPCID.HornetHoney || npc.type == NPCID.HornetLeafy || npc.type == NPCID.HornetSpikey ||
                npc.type == NPCID.HornetStingy || npc.type == NPCID.BigHornetFatty || npc.type == NPCID.BigHornetHoney || npc.type == NPCID.BigHornetLeafy || npc.type == NPCID.BigHornetSpikey ||
                npc.type == NPCID.BigHornetStingy || npc.type == NPCID.BigMossHornet || npc.type == NPCID.GiantMossHornet || npc.type == NPCID.LittleHornetFatty || npc.type == NPCID.LittleHornetHoney ||
                npc.type == NPCID.LittleHornetLeafy || npc.type == NPCID.LittleHornetSpikey || npc.type == NPCID.LittleHornetStingy || npc.type == NPCID.LittleMossHornet ||
                npc.type == NPCID.MossHornet || npc.type == NPCID.TinyMossHornet) {
                npc.lavaImmune = true;
                npc.buffImmune[BuffID.OnFire] = true;
            }

            if (npc.type == NPCID.IlluminantBat) {
                npc.value = 650;
                npc.defense = 27;
                npc.knockBackResist = 0.6f;
            }

            if (npc.type == NPCID.IlluminantSlime)
            {
                npc.value = 450;
                npc.scale = 1.05f;
            }

            if (npc.type == NPCID.KingSlime)
            {
                npc.damage = 33;
                npc.defense = 15;
                npc.scale = 1.25f;
            }


        }

        #endregion


        public override void NPCLoot(NPC npc) {

            #region Bosses drop souls once

            #endregion

            #region Loot Changes

            if (npc.type == NPCID.BigStinger) {
                Item.NewItem(npc.getRect(), mod.ItemType("BloodredMossClump"));
            }

            if (((npc.type == NPCID.EaterofWorldsHead) || (npc.type == NPCID.EaterofWorldsBody) || (npc.type == NPCID.EaterofWorldsTail)) && !Main.expertMode) {
                Item.NewItem(npc.getRect(), mod.ItemType("DarkSoul"), 10);
                Item.NewItem(npc.getRect(), ItemID.DemoniteOre, 4);
                Item.NewItem(npc.getRect(), ItemID.ShadowScale, 4);
            }

            if (npc.type == NPCID.Hornet || npc.type == NPCID.HornetFatty || npc.type == NPCID.HornetHoney || npc.type == NPCID.HornetLeafy || npc.type == NPCID.HornetSpikey ||
                npc.type == NPCID.HornetStingy || npc.type == NPCID.BigHornetFatty || npc.type == NPCID.BigHornetHoney || npc.type == NPCID.BigHornetLeafy || npc.type == NPCID.BigHornetSpikey ||
                npc.type == NPCID.BigHornetStingy || npc.type == NPCID.BigMossHornet || npc.type == NPCID.GiantMossHornet || npc.type == NPCID.LittleHornetFatty || npc.type == NPCID.LittleHornetHoney ||
                npc.type == NPCID.BigStinger || npc.type == NPCID.LittleHornetSpikey || npc.type == NPCID.LittleHornetStingy || npc.type == NPCID.LittleMossHornet ||
                npc.type == NPCID.MossHornet || npc.type == NPCID.TinyMossHornet) {
                if (Main.rand.NextFloat() >= .33f) { // 66% chance
                    Item.NewItem(npc.getRect(), mod.ItemType("BloodredMossClump"));
                }
            }

            if (npc.type == NPCID.KingSlime) {
                Item.NewItem(npc.getRect(), mod.ItemType("DarkSoul"), 500);
                if (!Main.expertMode) {
                    Item.NewItem(npc.getRect(), ItemID.GoldCoin, 10); //obtained from boss bag in Expert mode (see tsorcGlobalItem for boss bag edits)
                }
            }

            if (npc.netID == NPCID.GreenSlime && !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                Item.NewItem(npc.getRect(), mod.ItemType("DarkSoul"));
            }

            if ((npc.type == NPCID.Mimic || npc.type == NPCID.BigMimicCorruption || npc.type == NPCID.BigMimicCrimson || npc.type == NPCID.BigMimicHallow) && !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                if (Main.rand.Next(10) == 0) {
                    Item.NewItem(npc.getRect(), mod.ItemType("SymbolOfAvarice"));
                }
            }


            #endregion

            #region Dark Souls & Consumable Souls Drops


            if (npc.lifeMax > 5 && npc.value >= 10f)
            { //stop zero-value souls from dropping

                if (npc.netID != NPCID.JungleSlime)
                {
                    if (Main.expertMode)
                    { //npc.value is the amount of coins they drop
                        enemyValue = (int)npc.value / 25; //all enemies drop more money in expert mode, so the divisor is larger to compensate
                    }
                    else
                    {
                        enemyValue = (int)npc.value / 10;
                    }
                }

                if (npc.netID == NPCID.JungleSlime) //jungle slimes drop 10 souls
                {
                    if (Main.expertMode)
                    {
                        enemyValue = (int)npc.value / 125;
                    }
                    else
                    {
                        enemyValue = (int)npc.value / 50;
                    }
                }


                if (Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SilverSerpentRing)
                {
                    multiplier += 0.25f;
                }
                if (Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SoulSiphon)
                {
                    multiplier += 0.15f;
                }
                if (Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SOADrain)
                {
                    multiplier += 0.4f;
                }

                DarkSoulQuantity = (int)(multiplier * enemyValue);

                Item.NewItem(npc.getRect(), ModContent.ItemType<DarkSoul>(), DarkSoulQuantity);



                // Consumable Soul drops ahead - Current numbers give aprox. +20% souls

                float chance = 0.01f;

                if (((npc.type == NPCID.EaterofWorldsHead) || (npc.type == NPCID.EaterofWorldsBody) || (npc.type == NPCID.EaterofWorldsTail)) == false && !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode)
                {
                    if (Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SoulSiphon)
                    {
                        chance = 0.015f;
                    }

                    if ((enemyValue >= 1) && (enemyValue <= 200) && (Main.rand.NextFloat() < chance)) // 1% chance of all enemies between enemyValue 1 and 200 dropping FadingSoul aka 1/75
                    {
                        Item.NewItem(npc.getRect(), ModContent.ItemType<FadingSoul>(), 1); // Zombies and eyes are 6 and 7 enemyValue, so will only drop FadingSoul
                    }

                    if ((enemyValue >= 10) && (enemyValue <= 2000) && (Main.rand.NextFloat() < chance)) // 1% chance of all enemies between enemyValue 10 and 2000 dropping LostUndeadSoul aka 1/75
                    {
                        Item.NewItem(npc.getRect(), ModContent.ItemType<LostUndeadSoul>(), 1); // Most pre-HM enemies fall into this category
                    }

                    if ((enemyValue >= 50) && (enemyValue <= 10000) && (Main.rand.NextFloat() < chance)) // 1% chance of all enemies between enemyValue 50 and 10000 dropping NamelessSoldierSoul aka 1/75
                    {
                        Item.NewItem(npc.getRect(), ModContent.ItemType<NamelessSoldierSoul>(), 1); // Most HM enemies fall into this category
                    }

                    if ((enemyValue >= 150) && (enemyValue <= 10000) && (Main.rand.NextFloat() < chance) && Main.hardMode) // 1% chance of all enemies between enemyValue 150 and 10000 dropping ProudKnightSoul aka 1/75
                    {
                        Item.NewItem(npc.getRect(), ModContent.ItemType<ProudKnightSoul>(), 1);
                    }
                }
                //End consumable souls drops
            }
            #endregion

        }

        public override void AI(NPC npc) {
            #region block certain NPCs from spawning
            if (npc.type == NPCID.BigRainZombie
                || npc.type == NPCID.SmallRainZombie
                || npc.type == NPCID.Clown
                || npc.type == NPCID.UmbrellaSlime) {

                npc.active = false;
            }
            #endregion
            if ((npc.friendly) && (npc.lifeMax == 250)) { //town NPCs are immortal
                npc.dontTakeDamage = true;
                npc.dontTakeDamageFromHostiles = true;
            }
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection) {
            if (Main.player[projectile.owner].GetModPlayer<tsorcRevampPlayer>().ConditionOverload) {
                int buffIndex = 0;
                foreach (int buffType in npc.buffType) {

                    if (Main.debuff[buffType]) {
                        damage = (int)(damage * 1.3f);
                    }
                    buffIndex++;
                }
            }
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref int damage, ref float knockback, ref bool crit) {
            if (player.GetModPlayer<tsorcRevampPlayer>().ConditionOverload) {
                int buffIndex = 0;
                foreach (int buffType in npc.buffType) {

                    if (Main.debuff[buffType]) {
                        damage = (int)(damage * 1.2f);
                    }
                    buffIndex++;
                }
            }
        }

        public override bool PreNPCLoot(NPC npc) {
            if (npc.type == NPCID.ChaosElemental) {
                NPCLoader.blockLoot.Add(ItemID.RodofDiscord); //we dont want any sequence breaks, do we
            }
            if (npc.netID == NPCID.JungleSlime)

            NPCLoader.blockLoot.Add(ItemID.SlimeHook);
            return base.PreNPCLoot(npc);
        }

        public override void UpdateLifeRegen(NPC npc, ref int damage) {
            if (DarkInferno) {

                if (npc.lifeRegen > 0) {
                    npc.lifeRegen = 0;
                }
                npc.lifeRegen -= 16;
                if (damage < 2) {
                    damage = 2;
                }

                var N = npc;
                for (int j = 0; j < 6; j++) {
                    int dust = Dust.NewDust(N.position, N.width / 2, N.height / 2, 54, (N.velocity.X * 0.2f), N.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust].noGravity = true;

                    int dust2 = Dust.NewDust(N.position, N.width / 2, N.height / 2, 58, (N.velocity.X * 0.2f), N.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust2].noGravity = true;
                }
            }

            if (CrimsonBurn) {
                if (npc.lifeRegen > 0) {
                    npc.lifeRegen = 0;
                }
                npc.lifeRegen -= 16;
                if (damage < 2) {
                    damage = 2;
                }

            }
        }

        public override void SetupShop(int type, Chest shop, ref int nextSlot) {
            if (type == NPCID.Merchant && !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                shop.item[nextSlot].SetDefaults(ItemID.Bottle); //despite being able to find the archeologist right after (who sells bottled water), it's nice to have
                nextSlot++;
            }
        }
    }
}
