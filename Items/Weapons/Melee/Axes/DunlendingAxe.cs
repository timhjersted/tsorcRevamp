using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Axes
{
    class DunlendingAxe : ModItem
    {

        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("A cruel hill-man's axe fashioned to kill men" +
                 "\nDeals massive damage to humans"); */
        }
        public override void SetDefaults()
        {
            Item.damage = 8;
            Item.width = 30;
            Item.height = 28;
            Item.knockBack = 5;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = 3500;
            Item.axe = 10; // same as iron axe
            Item.rare = ItemRarityID.White;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.DarkGray;
        }

        public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers)
        {

            if (tsorcRevamp.HumanNPCs.Contains(target.type))
            {
                modifiers.FinalDamage *= 3;
            }
            if (target.type == ModContent.NPCType<NPCs.Enemies.BlackKnight>())
            {
                modifiers.FinalDamage *= 6;
            }
        }

    }
}
