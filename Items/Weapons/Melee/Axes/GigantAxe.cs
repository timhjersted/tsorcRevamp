﻿using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Axes
{
    class GigantAxe : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("An axe used to kill humans.");
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Cyan;
            Item.damage = 330;
            Item.height = 80;
            Item.knockBack = 9;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 21;
            Item.useTime = 21;
            Item.scale = 2f;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Cyan_9;
            Item.width = 84;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.Gray;
        }
        public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers)
        {
            //todo add mod NPCs to this list
            if (target.type == ModContent.NPCType<NPCs.Bosses.HeroofLumelia>()
                || target.type == ModContent.NPCType<NPCs.Enemies.Warlock>()
                || target.type == ModContent.NPCType<NPCs.Enemies.TibianAmazon>()
                || target.type == ModContent.NPCType<NPCs.Enemies.TibianValkyrie>()
                || target.type == ModContent.NPCType<NPCs.Enemies.ManHunter>()
                || target.type == ModContent.NPCType<NPCs.Enemies.Necromancer>()
                || target.type == ModContent.NPCType<NPCs.Enemies.RedCloudHunter>()
                || target.type == ModContent.NPCType<NPCs.Enemies.Assassin>()
                || target.type == ModContent.NPCType<NPCs.Enemies.BlackKnight>()
                || target.type == ModContent.NPCType<NPCs.Enemies.Dunlending>())
            {
                modifiers.FinalDamage *= 2;
            }
        }
    }
}
