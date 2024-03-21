using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    class SeveringDusk : ModItem
    {
        public const float DashBonusDmg = 50f; //player gains this as bonus melee dmg while dashing
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Yellow;
            Item.damage = 170;
            Item.width = 78;
            Item.height = 78;
            Item.knockBack = 5;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 12;
            Item.useTime = 12;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Blue_1;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.DarkMagenta;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
                tsorcRevampStaminaPlayer playerStamina = player.GetModPlayer<tsorcRevampStaminaPlayer>();
                if (playerStamina.staminaResourceCurrent > 30 && modPlayer.SeveringDuskDashTime < 1)
                {
                    playerStamina.staminaResourceCurrent -= 30;
                    player.velocity = UsefulFunctions.Aim(player.Center, Main.MouseWorld, 30);
                    player.immuneTime = 30;
                    modPlayer.SeveringDuskDashTime = 20;
                    if (Main.netMode != NetmodeID.SinglePlayer)
                    {
                        NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, player.whoAmI, 0f, 0f, 0f, 0);
                    }
                    modPlayer.effectRadius = 350;
                }
            }
            return base.Shoot(player, source, position, velocity, type, damage, knockback);
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        public override void HoldItem(Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.SetAuraState(tsorcAuraState.Darkness);
            if (modPlayer.SeveringDuskDashTime > 0)
            {
                player.ResetMeleeHitCooldowns();
                player.immune = true;
                modPlayer.SeveringDuskDashTime--;
                if (modPlayer.SeveringDuskDashTime == 0)
                {
                    player.velocity *= 0.1f;
                    if (Main.netMode != NetmodeID.SinglePlayer)
                    {
                        NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, player.whoAmI, 0f, 0f, 0f, 0);
                    }
                }
                /*for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && !npc.friendly && npc.Distance(player.Center) < 70)
                    {
                        npc.StrikeNPC(npc.CalculateHitInfo((int)player.GetTotalDamage(DamageClass.Melee).ApplyTo(Item.damage * 4), 0, true, 0, DamageClass.Melee, true), false, true);
                    }
                }*/
            }
            base.HoldItem(player);
        }

        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
    }
}
