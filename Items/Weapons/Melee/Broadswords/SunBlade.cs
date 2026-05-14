using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;
using tsorcRevamp.NPCs.Enemies.ParasyticWorm;

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
            Item.damage = 40;
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
            if (tsorcRevamp.UndeadNPCs.Contains(target.type))
            {
                modifiers.FinalDamage *= 3;
            }
        }
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile SunInferno = Projectile.NewProjectileDirect(Item.GetSource_FromThis(), target.Center, Vector2.Zero, ProjectileID.InfernoFriendlyBlast, (int)player.GetTotalDamage(DamageClass.Melee).ApplyTo(Item.damage), player.GetTotalKnockback(DamageClass.Melee).ApplyTo(Item.knockBack), Main.myPlayer);
            SunInferno.DamageType = DamageClass.Melee;
            SunInferno.CritChance = (int)player.GetTotalCritChance(DamageClass.Melee) + Item.crit;
            SunInferno.netUpdate = true;
        }
    }
}
