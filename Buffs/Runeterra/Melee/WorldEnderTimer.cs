
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Runeterra;

namespace tsorcRevamp.Buffs.Runeterra.Melee
{
    public class WorldEnderTimer : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            if (player.buffTime[buffIndex] == 1)
            {
                SoundEngine.PlaySound(new SoundStyle(WorldEnderItem.SoundPath + "TimeOut") with { Volume = WorldEnderItem.SoundVolume });
                switch (player.GetModPlayer<tsorcRevampPlayer>().WorldEnderSwing)
                {
                    case 2:
                        {
                            player.AddBuff(ModContent.BuffType<WorldEnderCooldown>(), (int)Math.Max(1, (WorldEnderItem.FirstSwingCooldown - WorldEnderItem.TimeWindow) / player.GetTotalAttackSpeed(DamageClass.Melee) * 60f));
                            player.GetModPlayer<tsorcRevampPlayer>().WorldEnderSwing = 1;
                            break;
                        }
                    case 3:
                        {
                            player.AddBuff(ModContent.BuffType<WorldEnderCooldown>(), (int)Math.Max(1, (WorldEnderItem.SecondSwingCooldown - WorldEnderItem.TimeWindow) / player.GetTotalAttackSpeed(DamageClass.Melee) * 60f));
                            player.GetModPlayer<tsorcRevampPlayer>().WorldEnderSwing = 1;
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
        }
    }
}
