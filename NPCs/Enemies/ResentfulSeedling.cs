using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.NPCs.Enemies
{
    public class ResentfulSeedling : ModNPC // Renewable source of wood
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Resentful Seedling");
            Main.npcFrameCount[npc.type] = 7;
        }
    
        public override void SetDefaults()
        {
            npc.CloneDefaults(NPCID.CorruptBunny);
            aiType = NPCID.CorruptBunny;
            npc.width = 10;
            npc.height = 16;
            npc.HitSound = SoundID.NPCHit33;
            npc.DeathSound = SoundID.NPCDeath29;
            npc.knockBackResist = .75f;
            npc.damage = 10;
            npc.lifeMax = 14;
            npc.defense = 6;
            animationType = NPCID.CorruptBunny;
            npc.value = 0;
            banner = npc.type;
            bannerItem = ModContent.ItemType<Banners.ResentfulSeedlingBanner>();
        }

        public int resindropped = 0;

        public override void ModifyHitByItem(Player player, Item item, ref int damage, ref float knockback, ref bool crit)
        {
            if ((item.type == ItemID.CopperAxe) || (item.type == ItemID.TinAxe) || (item.type == ItemID.IronAxe) || (item.type == ItemID.LeadAxe) || (item.type == ItemID.LeadAxe) || (item.type == ItemID.SilverAxe) || (item.type == ItemID.TungstenAxe) || (item.type == ItemID.GoldAxe) || (item.type == ItemID.PlatinumAxe)
                /*continued*/|| (item.type == ItemID.WarAxeoftheNight) || (item.type == ItemID.BloodLustCluster) || (item.type == ItemID.MeteorHamaxe) || (item.type == ItemID.MoltenHamaxe) || (item.type == ItemID.CobaltWaraxe) || (item.type == ItemID.CobaltChainsaw) || (item.type == ItemID.PalladiumWaraxe) || (item.type == ItemID.PalladiumChainsaw)
                /*half way ugh*/|| (item.type == ItemID.MythrilWaraxe) || (item.type == ItemID.MythrilChainsaw) || item.type == ItemID.OrichalcumWaraxe || (item.type == ItemID.OrichalcumChainsaw) || (item.type == ItemID.AdamantiteWaraxe) || (item.type == ItemID.AdamantiteChainsaw) || (item.type == ItemID.TitaniumWaraxe)
                /*regret*/|| (item.type == ItemID.TitaniumChainsaw) || (item.type == ItemID.PickaxeAxe) || (item.type == ItemID.SawtoothShark) || (item.type == ItemID.Drax) || (item.type == ItemID.ChlorophyteGreataxe) || (item.type == ItemID.ChlorophyteChainsaw) || (item.type == ItemID.ButchersChainsaw)
                /*Do ittttttttt! Kill meeeee! Aghhh agh aghh!*/|| (item.type == ItemID.TheAxe) || (item.type == ItemID.Picksaw) || (item.type == ItemID.ShroomiteDiggingClaw) || (item.type == ItemID.SpectreHamaxe) || (item.type == ItemID.SolarFlareAxe) || (item.type == ItemID.NebulaAxe) || (item.type == ItemID.StardustAxe)
                 || (item.type == ItemID.VortexAxe) || item.type == mod.ItemType("AdamantitePoleWarAxe") || item.type == mod.ItemType("AdamantiteWarAxe") || item.type == mod.ItemType("AncientFireAxe") || item.type == mod.ItemType("CobaltPoleWarAxe") || item.type == mod.ItemType("CobaltWarAxe")
                /*top 10 biggest mistakes of my life*/|| item.type == mod.ItemType("DunlendingAxe") || item.type == mod.ItemType("EphemeralThrowingAxe") || item.type == mod.ItemType("FieryPoleWarAxe") || item.type == mod.ItemType("FieryWarAxe") || item.type == mod.ItemType("HallowedGreatPoleAxe")
                /*spent more time making this list than the NPC iteself*/|| item.type == mod.ItemType("MythrilPoleWarAxe") || item.type == mod.ItemType("MythrilWarAxe") || item.type == mod.ItemType("OldAxe") || item.type == mod.ItemType("OldDoubleAxe") || item.type == mod.ItemType("OldHalberd")
                || item.type == mod.ItemType("ReforgedOldAxe") || item.type == mod.ItemType("ReforgedOldDoubleAxe") || (item.type == mod.ItemType("ReforgedOldHalberd")) || (item.type == mod.ItemType("ForgottenAxe")) || item.type == mod.ItemType("ForgottenGreatAxe") || item.type == mod.ItemType("CobaltHalberd")
                || item.type == mod.ItemType("ForgottenPoisonAxe") || item.type == mod.ItemType("ForgottenRuneAxe") || item.type == mod.ItemType("GigantAxe"))

            {
                CombatText.NewText(new Rectangle((int)npc.Center.X, (int)npc.Bottom.Y, 10, 10), Color.Crimson, "Weakness!", false, false);
                damage *= 2; //I never want to see or hear the word "axe" again in my life
                if (damage < 10)
                {
                    damage = 10;
                }

            }
            //fire melee
            if (player.HasBuff(BuffID.WeaponImbueFire) || item.type == mod.ItemType("AncientFireSword") || item.type == mod.ItemType("AncientFireAxe") || item.type == mod.ItemType("FieryFalchion") || item.type == mod.ItemType("FieryGreatWarhammer") || item.type == mod.ItemType("FieryMace") || item.type == mod.ItemType("FieryNinjato") || item.type == mod.ItemType("FieryNodachi")
                 || item.type == mod.ItemType("FieryPoleWarAxe") || item.type == mod.ItemType("FierySickle") || item.type == mod.ItemType("FieryWarAxe") || item.type == mod.ItemType("FieryZweihander") || item.type == mod.ItemType("ForgottenRisingSun") || item.type == mod.ItemType("MagmaTooth")
                 || item.type == ItemID.FieryGreatsword || item.type == ItemID.MoltenHamaxe || item.type == ItemID.MoltenPickaxe || item.type == mod.ItemType("SunBlade"))
            {
                CombatText.NewText(new Rectangle((int)npc.Center.X, (int)npc.Bottom.Y, 10, 10), Color.Crimson, "Weakness!", false, false);
                damage *= 2;
                if (damage < 10)
                {
                    damage = 10; //damage before defence
                }
                if (Main.rand.Next(20) == 0 && resindropped < 1)
                {
                    Item.NewItem(npc.Bottom, mod.ItemType("CharcoalPineResin"));
                    resindropped++;
                }
            }
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            Player player = Main.LocalPlayer;

            //fire projectiles bonus damage
            if (projectile.type == ProjectileID.FireArrow || projectile.type == ProjectileID.BallofFire || projectile.type == ProjectileID.Flamarang || projectile.type == ProjectileID.Flamelash || projectile.type == ProjectileID.Sunfury || projectile.type == ProjectileID.HellfireArrow ||
                projectile.type == ProjectileID.Flames || projectile.type == ProjectileID.CursedFlameFriendly || projectile.type == ProjectileID.CursedArrow || projectile.type == ProjectileID.CursedBullet || projectile.type == ProjectileID.Flare || projectile.type == ProjectileID.BlueFlare ||
                projectile.type == ProjectileID.FlamesTrap || projectile.type == ProjectileID.FlamethrowerTrap || projectile.type == ProjectileID.ImpFireball || projectile.type == ProjectileID.MolotovCocktail || projectile.type == ProjectileID.MolotovFire || projectile.type == ProjectileID.MolotovFire2 ||
                projectile.type == ProjectileID.MolotovFire3 || projectile.type == ProjectileID.Meteor1 || projectile.type == ProjectileID.Meteor2 || projectile.type == ProjectileID.Meteor3 || projectile.type == ProjectileID.SolarFlareChainsaw || projectile.type == ProjectileID.SolarFlareDrill ||
                projectile.type == ProjectileID.CursedDart || projectile.type == ProjectileID.CursedDartFlame || projectile.type == ProjectileID.Hellwing || projectile.type == ProjectileID.ShadowFlameArrow || projectile.type == ProjectileID.ShadowFlame || projectile.type == ProjectileID.ShadowFlameKnife ||
                projectile.type == ProjectileID.Spark || projectile.type == ProjectileID.Cascade || projectile.type == ProjectileID.HelFire || projectile.type == ProjectileID.DesertDjinnCurse || projectile.type == ProjectileID.SolarWhipSword || projectile.type == ProjectileID.SolarWhipSwordExplosion ||
                projectile.type == ProjectileID.Daybreak || projectile.type == ProjectileID.SpiritFlame || projectile.type == ProjectileID.DD2FlameBurstTowerT1Shot || projectile.type == ProjectileID.DD2FlameBurstTowerT2Shot || projectile.type == ProjectileID.DD2FlameBurstTowerT3Shot || projectile.type == ProjectileID.DD2PhoenixBowShot ||
                projectile.type == mod.ProjectileType("BlackFire") || projectile.type == mod.ProjectileType("BlackFirelet") || projectile.type == mod.ProjectileType("CursedFlames") || projectile.type == mod.ProjectileType("CursedFlamelash") || projectile.type == mod.ProjectileType("DevilSickle") ||
                projectile.type == mod.ProjectileType("FireBall") || projectile.type == mod.ProjectileType("Explosion") || projectile.type == mod.ProjectileType("ExplosionBall") || projectile.type == mod.ProjectileType("Firebomb") || projectile.type == mod.ProjectileType("FireBombBall") ||
                projectile.type == mod.ProjectileType("FireField") || projectile.type == mod.ProjectileType("FireFieldBall") || projectile.type == mod.ProjectileType("FireSpirit2") || projectile.type == mod.ProjectileType("FlameStrike") || projectile.type == mod.ProjectileType("GreatFireball") ||
                projectile.type == mod.ProjectileType("GreatFireballBall") || projectile.type == mod.ProjectileType("GreatFireStrike") || projectile.type == mod.ProjectileType("Meteor") || projectile.type == mod.ProjectileType("MeteorShower") || projectile.type == mod.ProjectileType("RedLaserBeam") ||
                projectile.type == mod.ProjectileType("BlackFire") || projectile.type == mod.ProjectileType("BlackFirelet") || projectile.type == mod.ProjectileType("CursedFlames") || projectile.type == mod.ProjectileType("CursedFlamelash") || projectile.type == mod.ProjectileType("DevilSickle") ||
                (projectile.melee && player.meleeEnchant == 3))
            {
                damage *= 2;
                CombatText.NewText(new Rectangle((int)npc.Center.X, (int)npc.Bottom.Y, 10, 10), Color.Crimson, "Weakness!", false, false);
                if (Main.rand.Next(30) == 0 && resindropped < 1)
                {
                    Item.NewItem(npc.Bottom, mod.ItemType("CharcoalPineResin"));
                    resindropped++;
                }
            }
        }
        public override void HitEffect(int hitDirection, double damage)
        {
            for (int i = 0; i < 5; i++)
            {
                int dustType = 7;
                int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, dustType);
                Dust dust = Main.dust[dustIndex];
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.velocity.Y = Main.rand.Next(-3, 0);
                dust.noGravity = false;
            }
            if (npc.life <= 0)
            {
                for (int i = 0; i < 15; i++)
                {
                    Dust.NewDust(npc.position, npc.width, npc.height, 7, Main.rand.Next(0, 2), Main.rand.Next(-2, 0), 0, default(Color), 1f);
                }
            }
        }
        public override void NPCLoot()
        {
            Item.NewItem(npc.getRect(), mod.ItemType("DarkSoul"));
            Item.NewItem(npc.getRect(), ItemID.Wood);

            if (Main.rand.Next(3) == 0) //sometimes drop 2 wood
            {
                Item.NewItem(npc.getRect(), ItemID.Wood);
            }

        }
    }
}
