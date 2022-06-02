using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

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
            banner = NPC.type;
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
                || (item.type == ItemID.VortexAxe) || item.type == Mod.Find<ModItem>("AncientFireAxe").Type || item.type == Mod.Find<ModItem>("CobaltHalberd").Type
                /*top 10 biggest mistakes of my life*/|| item.type == Mod.Find<ModItem>("DunlendingAxe").Type || item.type == Mod.Find<ModItem>("EphemeralThrowingAxe").Type
                /*spent more time making this list than the NPC iteself*/|| item.type == Mod.Find<ModItem>("OldAxe").Type || item.type == Mod.Find<ModItem>("OldDoubleAxe").Type || item.type == Mod.Find<ModItem>("OldHalberd").Type
                || item.type == Mod.Find<ModItem>("ReforgedOldAxe").Type || item.type == Mod.Find<ModItem>("ReforgedOldDoubleAxe").Type || (item.type == Mod.Find<ModItem>("ReforgedOldHalberd").Type) || (item.type == Mod.Find<ModItem>("ForgottenAxe").Type) || item.type == Mod.Find<ModItem>("ForgottenGreatAxe").Type
                || item.type == Mod.Find<ModItem>("ForgottenPoisonAxe").Type || item.type == Mod.Find<ModItem>("ForgottenRuneAxe").Type || item.type == Mod.Find<ModItem>("GigantAxe").Type)

            {
                CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, "Weakness!", false, false);
                damage *= 2; //I never want to see or hear the word "axe" again in my life
                if (damage < 10)
                {
                    damage = 10;
                }

            }
            //fire melee
            if (player.HasBuff(BuffID.WeaponImbueFire) || item.type == Mod.Find<ModItem>("AncientFireSword").Type || item.type == Mod.Find<ModItem>("AncientFireAxe").Type || item.type == Mod.Find<ModItem>("FieryFalchion").Type || item.type == Mod.Find<ModItem>("FieryGreatWarhammer").Type || item.type == Mod.Find<ModItem>("FieryMace").Type || item.type == Mod.Find<ModItem>("FieryNinjato").Type || item.type == Mod.Find<ModItem>("FieryNodachi").Type
                 || item.type == Mod.Find<ModItem>("FieryPoleWarAxe").Type || item.type == Mod.Find<ModItem>("FierySickle").Type || item.type == Mod.Find<ModItem>("FieryWarAxe").Type || item.type == Mod.Find<ModItem>("FieryZweihander").Type || item.type == Mod.Find<ModItem>("ForgottenRisingSun").Type || item.type == Mod.Find<ModItem>("MagmaTooth").Type
                 || item.type == ItemID.FieryGreatsword || item.type == ItemID.MoltenHamaxe || item.type == ItemID.MoltenPickaxe || item.type == Mod.Find<ModItem>("SunBlade").Type)
            {
                CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, "Weakness!", false, false);
                damage *= 2;
                if (damage < 10)
                {
                    damage = 10; //damage before defence
                }
                if (Main.rand.Next(20) == 0 && resindropped < 1)
                {
                    Item.NewItem(NPC.GetSource_Loot(), NPC.Bottom, Mod.Find<ModItem>("CharcoalPineResin").Type);
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
                projectile.type == Mod.Find<ModProjectile>("BlackFire").Type || projectile.type == Mod.Find<ModProjectile>("BlackFirelet").Type || projectile.type == Mod.Find<ModProjectile>("CursedFlames").Type || projectile.type == Mod.Find<ModProjectile>("CursedFlamelash").Type || projectile.type == Mod.Find<ModProjectile>("DevilSickle").Type ||
                projectile.type == Mod.Find<ModProjectile>("FireBall").Type || projectile.type == Mod.Find<ModProjectile>("Explosion").Type || projectile.type == Mod.Find<ModProjectile>("ExplosionBall").Type || projectile.type == Mod.Find<ModProjectile>("Firebomb").Type || projectile.type == Mod.Find<ModProjectile>("FireBombBall").Type ||
                projectile.type == Mod.Find<ModProjectile>("FireField").Type || projectile.type == Mod.Find<ModProjectile>("FireFieldBall").Type || projectile.type == Mod.Find<ModProjectile>("FireSpirit2").Type || projectile.type == Mod.Find<ModProjectile>("FlameStrike").Type || projectile.type == Mod.Find<ModProjectile>("GreatFireball").Type ||
                projectile.type == Mod.Find<ModProjectile>("GreatFireballBall").Type || projectile.type == Mod.Find<ModProjectile>("GreatFireStrike").Type || projectile.type == Mod.Find<ModProjectile>("Meteor").Type || projectile.type == Mod.Find<ModProjectile>("MeteorShower").Type || projectile.type == Mod.Find<ModProjectile>("RedLaserBeam").Type ||
                projectile.type == Mod.Find<ModProjectile>("BlackFire").Type || projectile.type == Mod.Find<ModProjectile>("BlackFirelet").Type || projectile.type == Mod.Find<ModProjectile>("CursedFlames").Type || projectile.type == Mod.Find<ModProjectile>("CursedFlamelash").Type || projectile.type == Mod.Find<ModProjectile>("DevilSickle").Type ||
                (projectile.DamageType == DamageClass.Melee && player.meleeEnchant == 3))
            {
                damage *= 2;
                CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, "Weakness!", false, false);
                if (Main.rand.Next(30) == 0 && resindropped < 1)
                {
                    Item.NewItem(NPC.GetSource_Loot(), NPC.Bottom, Mod.Find<ModItem>("CharcoalPineResin").Type);
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
        public override void OnKill()
        {
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), Mod.Find<ModItem>("DarkSoul").Type);
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Wood);

            if (Main.rand.Next(3) == 0) //sometimes drop 2 wood
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Wood);
            }

            if (Main.rand.Next(8) == 0) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.GreenBlossom>());

        }
    }
}
