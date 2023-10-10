using Microsoft.Xna.Framework;
using System.Security.Cryptography.X509Certificates;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class SunBlade : ModItem
    {

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.LightRed;
            Item.damage = 44;
            Item.width = 36;
            Item.height = 36;
            Item.knockBack = 9;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 40;
            Item.useTime = 40;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.LightRed_4;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.Orange;
        }

        public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers)
        {
            //todo add mod NPCs to this list
            if (NPCID.Sets.Skeletons[target.type]
                    || target.type == NPCID.Zombie
                    || target.type == NPCID.BaldZombie
                    || target.type == NPCID.AngryBones
                    || target.type == NPCID.DarkCaster
                    || target.type == NPCID.CursedSkull
                    || target.type == NPCID.UndeadMiner
                    || target.type == NPCID.Tim
                    || target.type == NPCID.DoctorBones
                    || target.type == NPCID.ArmoredSkeleton
                    || target.type == NPCID.Mummy
                    || target.type == NPCID.DarkMummy
                    || target.type == NPCID.LightMummy
                    || target.type == NPCID.Wraith
                    || target.type == NPCID.SkeletonArcher
                    || target.type == NPCID.PossessedArmor
                    || target.type == NPCID.TheGroom
                    || target.type == NPCID.SkeletronHand
                    || target.type == NPCID.SkeletronHead
                    || target.type == ModContent.NPCType<LichKingSerpentHead>()
                    || target.type == ModContent.NPCType<LichKingSerpentBody>()
                    || target.type == ModContent.NPCType<LichKingSerpentTail>()
                    || target.type == ModContent.NPCType<NPCs.Enemies.DemonSpirit>()
                    || target.type == ModContent.NPCType<NPCs.Enemies.CrazedDemonSpirit>()
                    || target.type == ModContent.NPCType<NPCs.Enemies.ParasyticWormHead>()
                    || target.type == ModContent.NPCType<NPCs.Enemies.ParasyticWormBody>()
                    || target.type == ModContent.NPCType<NPCs.Enemies.ParasyticWormTail>()
                    )
            {
                modifiers.FinalDamage *= 4;
            }
        }
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile SunInferno = Projectile.NewProjectileDirect(Projectile.GetSource_None(), target.Center, Vector2.Zero, ProjectileID.InfernoFriendlyBlast, (int)player.GetTotalDamage(DamageClass.Melee).ApplyTo(Item.damage), player.GetTotalKnockback(DamageClass.Melee).ApplyTo(Item.knockBack), Main.myPlayer);
            SunInferno.DamageType = DamageClass.Melee;
        }
    }
}
