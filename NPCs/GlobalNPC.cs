using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Weapons.Ranged;

namespace tsorcRevamp.NPCs {
    class tsorcRevampGlobalNPC : GlobalNPC {


        float enemyValue;
        float multiplier = 1f;
        int DarkSoulQuantity;

        public override bool InstancePerEntity => true;
        public bool DarkInferno = false;
        public bool CrimsonBurn = false;
        public bool ToxicCatDrain = false;
        public bool ResetToxicCatBlobs = false;
        public bool ElectrocutedEffect = false;


        public override void ResetEffects(NPC npc) {
            DarkInferno = false;
            CrimsonBurn = false;
            ToxicCatDrain = false;
            ResetToxicCatBlobs = false;
            ElectrocutedEffect = false;
        }

        //vanilla npc changes moved to separate file

        public override void NPCLoot(NPC npc) {

            #region Loot Changes

            if (npc.type == NPCID.BigStinger) {
                Item.NewItem(npc.getRect(), mod.ItemType("BloodredMossClump"));
            }

            if (((npc.type == NPCID.EaterofWorldsHead) || (npc.type == NPCID.EaterofWorldsBody) || (npc.type == NPCID.EaterofWorldsTail)) && !Main.expertMode) {
                Item.NewItem(npc.getRect(), mod.ItemType("DarkSoul"), 10);
                Item.NewItem(npc.getRect(), ItemID.DemoniteOre, 4);
                Item.NewItem(npc.getRect(), ItemID.ShadowScale, 4);
            }

            if ((npc.type >= NPCID.BigHornetStingy && npc.type <= NPCID.LittleHornetFatty) ||
                                (npc.type >= NPCID.GiantMossHornet && npc.type <= NPCID.LittleStinger) ||
                                npc.type == NPCID.Hornet || npc.type == NPCID.ManEater ||
                                npc.type == NPCID.MossHornet ||
                                (npc.type >= NPCID.HornetFatty && npc.type <= NPCID.HornetStingy)) {
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

            if (npc.type == NPCID.TheDestroyer && !Main.expertMode) {
                Item.NewItem(npc.getRect(), ModContent.ItemType<CrestOfCorruption>(), 2);
                Item.NewItem(npc.getRect(), ModContent.ItemType<RTQ2>());
            }
            if (npc.type == NPCID.SkeletronPrime && !Main.expertMode)
            {
                Item.NewItem(npc.getRect(), ModContent.ItemType<Miakoda>(), 2);
            }
            if (npc.type == NPCID.SkeletronPrime && !Main.expertMode) {
                Item.NewItem(npc.getRect(), ModContent.ItemType<CrestOfSteel>(), 2);
                Item.NewItem(npc.getRect(), ItemID.AngelWings, 2);
            }
            if ((npc.type == NPCID.Retinazer || npc.type == NPCID.Spazmatism) && !Main.expertMode) {
                Item.NewItem(npc.getRect(), ModContent.ItemType<CrestOfSky>(), 2);
            }

            if (npc.netID == NPCID.GreenSlime && !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                Item.NewItem(npc.getRect(), mod.ItemType("DarkSoul"));
            }

            if ((npc.type == NPCID.Mimic || npc.type == NPCID.BigMimicCorruption || npc.type == NPCID.BigMimicCrimson || npc.type == NPCID.BigMimicHallow) && !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                if (Main.rand.Next(10) == 0) {
                    Item.NewItem(npc.getRect(), mod.ItemType("SymbolOfAvarice"));
                }
            }

            if (npc.type == NPCID.EyeofCthulhu && !Main.expertMode) {
                Item.NewItem(npc.getRect(), ItemID.HerosHat);
                Item.NewItem(npc.getRect(), ItemID.HerosShirt);
                Item.NewItem(npc.getRect(), ItemID.HerosPants);
                Item.NewItem(npc.getRect(), ItemID.HermesBoots);
            }

            if (npc.type == NPCID.PossessedArmor && Main.rand.Next(50) == 0 && !Main.expertMode)
            {
                Item.NewItem(npc.getRect(), mod.ItemType("WallTome"));
            }

            if ((npc.type == NPCID.PossessedArmor || npc.type == NPCID.Wraith) && Main.rand.Next(25) == 0 && Main.expertMode)
            {
                Item.NewItem(npc.getRect(), mod.ItemType("WallTome"));
            }

            if (npc.type == NPCID.Shark && Main.rand.Next(20) == 0)
            {
                Item.NewItem(npc.getRect(), mod.ItemType("CoralSword"));
            }

            if (Main.rand.Next(25) == 0 && ((npc.type >= NPCID.BigPantlessSkeleton && npc.type <= NPCID.SmallSkeleton) ||
                                (npc.type >= NPCID.HeadacheSkeleton && npc.type <= NPCID.PantlessSkeleton) ||
                                (npc.type >= NPCID.SkeletonTopHat && npc.type <= NPCID.SkeletonAlien) ||
                                (npc.type >= NPCID.BoneThrowingSkeleton && npc.type <= NPCID.BoneThrowingSkeleton4) ||
                                npc.type == NPCID.HeavySkeleton ||
                                npc.type == NPCID.Skeleton ||
                                npc.type == NPCID.ArmoredSkeleton ||
                                npc.type == NPCID.SkeletonArcher))

            {
                Item.NewItem(npc.getRect(), mod.ItemType("DeadChicken"));
            }

            if (npc.type == NPCID.Vulture && Main.rand.Next(10) == 0)
            {
                Item.NewItem(npc.getRect(), mod.ItemType("DeadChicken"));
            }

            if (npc.type == NPCID.Wraith)
            {
                Item.NewItem(npc.getRect(), ItemID.Heart, 7);
            }

            if (Main.rand.Next(25) == 0 && ((npc.type >= NPCID.BigFemaleZombie && npc.type <= NPCID.SmallFemaleZombie) ||
                                (npc.type >= NPCID.BigTwiggyZombie && npc.type <= NPCID.SmallZombie) ||
                                (npc.type >= NPCID.ZombieDoctor && npc.type <= NPCID.ZombiePixie) ||
                                (npc.type >= NPCID.ZombieXmas && npc.type <= NPCID.ZombieSweater) ||
                                (npc.type >= NPCID.ArmedZombie && npc.type <= NPCID.ArmedZombieCenx) ||
                                npc.type == NPCID.Zombie ||
                                npc.type == NPCID.BaldZombie ||
                                npc.type == NPCID.ZombieEskimo ||
                                npc.type == NPCID.FemaleZombie ||
                                (npc.type >= NPCID.PincushionZombie && npc.type <= NPCID.TwiggyZombie)))
            {
                Item.NewItem(npc.getRect(), mod.ItemType("DeadChicken"));
            }

            #endregion

            #region Dark Souls & Consumable Souls Drops


            if (npc.lifeMax > 5 && npc.value >= 10f || npc.boss) { //stop zero-value souls from dropping (the 'or boss' is for expert mode support)

                if (npc.netID != NPCID.JungleSlime) {
                    if (Main.expertMode) { //npc.value is the amount of coins they drop
                        enemyValue = (int)npc.value / 25; //all enemies drop more money in expert mode, so the divisor is larger to compensate
                    }
                    else {
                        enemyValue = (int)npc.value / 10;
                    }
                }

                if (npc.netID == NPCID.JungleSlime) //jungle slimes drop 10 souls
                {
                    if (Main.expertMode) {
                        enemyValue = (int)npc.value / 125;
                    }
                    else {
                        enemyValue = (int)npc.value / 50;
                    }
                }


                if (Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SilverSerpentRing) {
                    multiplier += 0.25f;
                }
                if (Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SoulSiphon) {
                    multiplier += 0.15f;
                }
                if (Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SOADrain) {
                    multiplier += 0.4f;
                }

                DarkSoulQuantity = (int)(multiplier * enemyValue);

                #region Bosses drop souls once
                if (npc.boss) {
                    if (tsorcRevampWorld.Slain.ContainsKey(npc.type)) {
                        DarkSoulQuantity = 0;
                        return;
                    }
                    else {
                        if (Main.netMode == NetmodeID.SinglePlayer) {
                            Main.NewText("The souls of " + npc.GivenOrTypeName + " have been released!", 175, 255, 75);
                        }
                        else if (Main.netMode == NetmodeID.Server) {
                            NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("The souls of " + npc.GivenOrTypeName + " have been released!"), new Color(175, 255, 75));
                        }
                        tsorcRevampWorld.Slain.Add(npc.type, 0);
                        if (Main.expertMode) {
                            DarkSoulQuantity = 0;
                        }
                    }
                }
                #endregion
                if (DarkSoulQuantity > 0) {
                    Item.NewItem(npc.getRect(), ModContent.ItemType<DarkSoul>(), DarkSoulQuantity);
                }


                // Consumable Soul drops ahead - Current numbers give aprox. +20% souls

                float chance = 0.01f;

                if (((npc.type == NPCID.EaterofWorldsHead) || (npc.type == NPCID.EaterofWorldsBody) || (npc.type == NPCID.EaterofWorldsTail)) == false && !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                    if (Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SoulSiphon) {
                        chance = 0.015f;
                    }

                    if ((enemyValue >= 1) && (enemyValue <= 200) && (Main.rand.NextFloat() < chance)) // 1% chance of all enemies between enemyValue 1 and 200 dropping FadingSoul aka 1/75
                    {
                        Item.NewItem(npc.getRect(), ModContent.ItemType<FadingSoul>(), 1); // Zombies and eyes are 6 and 7 enemyValue, so will only drop FadingSoul
                    }

                    if ((enemyValue >= 15) && (enemyValue <= 2000) && (Main.rand.NextFloat() < chance)) // 1% chance of all enemies between enemyValue 10 and 2000 dropping LostUndeadSoul aka 1/75
                    {
                        Item.NewItem(npc.getRect(), ModContent.ItemType<LostUndeadSoul>(), 1); // Most pre-HM enemies fall into this category
                    }

                    if ((enemyValue >= 55) && (enemyValue <= 10000) && (Main.rand.NextFloat() < chance)) // 1% chance of all enemies between enemyValue 50 and 10000 dropping NamelessSoldierSoul aka 1/75
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

            if (ToxicCatDrain)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                int toxiccatshotCount = 0;
                for (int i = 0; i < 1000; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<Projectiles.ToxicCatShot>() && p.ai[0] == 1f && p.ai[1] == npc.whoAmI)
                    {
                        toxiccatshotCount++;
                    }
                }
                npc.lifeRegen -= toxiccatshotCount * 1 * 1; //Use 1st N for damage, second N can be used to make it tick faster.
                if (damage < toxiccatshotCount * 1)
                {
                    damage = toxiccatshotCount * 1;
                }
            }

            if (ElectrocutedEffect)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                npc.lifeRegen -= 22;
                if (damage < 2)
                {
                    damage = 2;
                }
            }
        }

        public override void SetupShop(int type, Chest shop, ref int nextSlot) {
            if (type == NPCID.Merchant && !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                shop.item[nextSlot].SetDefaults(ItemID.Bottle); //despite being able to find the archeologist right after (who sells bottled water), it's nice to have
                nextSlot++;
            }
            if (type == NPCID.SkeletonMerchant && !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode && Main.rand.Next(2) == 0) {
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<EternalCrystal>());
                shop.item[nextSlot].shopCustomPrice = 5000;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
            }
            if (type == NPCID.GoblinTinkerer && !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode)
            {
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Pulsar>());
                shop.item[nextSlot].shopCustomPrice = 5000;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;

                shop.item[nextSlot].SetDefaults(ModContent.ItemType<ToxicCatalyzer>());
                shop.item[nextSlot].shopCustomPrice = 5000;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
            }
        }
        public override void OnHitByProjectile(NPC npc, Projectile projectile, int damage, float knockback, bool crit)
        {
            if (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ToxicCatDrain && (projectile.type == ModContent.ProjectileType<Projectiles.ToxicCatDetonator>() || projectile.type == ModContent.ProjectileType<Projectiles.ToxicCatExplosion>()))
            {
                Main.PlaySound(SoundID.Item74.WithPitchVariance(.3f), projectile.position);
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ResetToxicCatBlobs = true;
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ToxicCatDrain = false;
                for (int i = 0; i < 1000; i++) {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<Projectiles.ToxicCatShot>() && p.ai[0] == 1f && p.ai[1] == npc.whoAmI) {
                        p.active = false;
                        Projectile.NewProjectile(p.Center, npc.velocity, ModContent.ProjectileType<Projectiles.ToxicCatExplosion>(), projectile.damage, projectile.knockBack, projectile.owner, 0, 1);

                    }
                }
            }

        }

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (ElectrocutedEffect)
            {
                int dust = Dust.NewDust(npc.position, npc.width, npc.height, 226, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .4f);
                Main.dust[dust].noGravity = true;
            }
        }

    }
}
