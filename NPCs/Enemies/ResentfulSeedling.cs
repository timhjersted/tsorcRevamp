using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.Items.Weapons.Melee;
using tsorcRevamp.Items;
using tsorcRevamp.Projectiles;

namespace tsorcRevamp.NPCs.Enemies
{
    public class ResentfulSeedling : ModNPC // Renewable source of wood
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Resentful Seedling");
            Main.npcFrameCount[NPC.type] = 7;
        }

        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.CorruptBunny);
            AIType = NPCID.CorruptBunny;
            NPC.width = 10;
            NPC.height = 16;
            NPC.HitSound = SoundID.NPCHit33;
            NPC.DeathSound = SoundID.NPCDeath29;
            NPC.knockBackResist = .75f;
            NPC.damage = 10;
            NPC.lifeMax = 14;
            NPC.defense = 6;
            AnimationType = NPCID.CorruptBunny;
            NPC.value = 0;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.ResentfulSeedlingBanner>();
        }

        public int resindropped = 0;

        public override void ModifyHitByItem(Player player, Item item, ref int damage, ref float knockback, ref bool crit)
        {
            if (item.Name.Contains("Axe") || item.Name.Contains("axe") || item.Name.Contains("saw") || (item.type == ItemID.BloodLustCluster) || (item.type == ItemID.SawtoothShark) || (item.type == ItemID.Drax)
                || (item.type == ItemID.ShroomiteDiggingClaw) || item.ModItem.Name.Contains("Axe") || item.ModItem.Name.Contains("Halberd") && !item.ModItem.Name.Contains("Pick") && !item.Name.Contains("Pick"))
            {
                CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, "Weakness!", false, false);
                damage *= 2; //I never want to see or hear the word "axe" again in my life
                if (damage < 10)
                {
                    damage = 10;
                }

            }
            //fire melee
            if (player.HasBuff(BuffID.WeaponImbueFire) || item.type == ModContent.ItemType<AncientFireSword>() || item.type == ModContent.ItemType<AncientFireAxe>()
                 || item.type == ModContent.ItemType<ForgottenRisingSun>() || item.type == ModContent.ItemType<MagmaTooth>()
                 || item.type == ItemID.FieryGreatsword || item.type == ItemID.MoltenHamaxe || item.type == ItemID.MoltenPickaxe || item.type == ModContent.ItemType<SunBlade>())
            {
                CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, "Weakness!", false, false);
                damage *= 2;
                if (damage < 10)
                {
                    damage = 10; //damage before defence
                }
                if (Main.rand.NextBool(20) && resindropped < 1)
                {
                    Item.NewItem(NPC.GetSource_Loot(), NPC.Bottom, ModContent.ItemType<CharcoalPineResin>());
                    resindropped++;
                }
            }
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            Player player = Main.LocalPlayer;

            //fire projectiles bonus damage
            //However... If it is a fire projectile...
            if (projectile.Name.Contains("Fire") || projectile.Name.Contains("fire") || projectile.Name.Contains("Flame") || projectile.Name.Contains("flame") || projectile.Name.Contains("Curse") ||
                projectile.Name.Contains("Flare") || projectile.Name.Contains("Molotov") || projectile.Name.Contains("Meteor") || projectile.type == ProjectileID.Hellwing ||
                projectile.type == ProjectileID.Spark || projectile.type == ProjectileID.Cascade || projectile.type == ProjectileID.SolarWhipSword || projectile.type == ProjectileID.SolarWhipSwordExplosion ||
                projectile.type == ProjectileID.Daybreak || projectile.type == ProjectileID.DD2PhoenixBowShot ||
                projectile.ModProjectile.Name.Contains("Fire") || projectile.ModProjectile.Name.Contains("Flame") || projectile.ModProjectile.Name.Contains("Explosion") || projectile.ModProjectile.Name.Contains("Meteor") ||
                projectile.type == ModContent.ProjectileType<DevilSickle>() || projectile.type == ModContent.ProjectileType<RedLaserBeam>() ||
                (projectile.DamageType == DamageClass.Melee && player.meleeEnchant == 3))
            {
                damage *= 2;
                CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, "Weakness!", false, false);
                if (Main.rand.NextBool(30) && resindropped < 1)
                {
                    Item.NewItem(NPC.GetSource_Loot(), NPC.Bottom, ModContent.ItemType<CharcoalPineResin>());
                    resindropped++;
                }
            }
        }
        public override void HitEffect(int hitDirection, double damage)
        {
            for (int i = 0; i < 5; i++)
            {
                int DustType = 7;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.velocity.Y = Main.rand.Next(-3, 0);
                dust.noGravity = false;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 15; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 7, Main.rand.Next(0, 2), Main.rand.Next(-2, 0), 0, default(Color), 1f);
                }
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) {
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<DarkSoul>()));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ItemID.Wood));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ItemID.Wood, 3));
            npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ModContent.ItemType<Items.Potions.GreenBlossom>(), 8));
        }
    }
}
