# Translation updates needed

Entries whose **English text in `en-US.hjson` changed after the Russian / Chinese translation was written**. The old translations are kept in `ru-RU.hjson` / `zh-Hans.hjson`; each one here needs to be brought up to date with the new English.

For each key: the English diff (old English the translation was based on → current English), the words that changed, and the current translation(s) to edit. Once a translation is updated, its entry can be deleted from this file.

- Russian (`ru-RU`): **193** entries
- Chinese (`zh-Hans`): **262** entries
- ⚠️ = the `{0}`-style placeholders in the translation no longer match the English, so numbers in that tooltip are missing or wrong in-game.

## Contents

- [CommonItemTooltip](#commonitemtooltip) (5)
- [DeathText](#deathtext) (10)
- [UI](#ui) (3)
- [DropRules](#droprules) (2)
- [BossChecklist](#bosschecklist) (11)
- [Items](#items) (162)
- [Buffs](#buffs) (7)
- [Projectiles](#projectiles) (59)
- [NPCs](#npcs) (27)
- [Keybinds](#keybinds) (1)
- [Achievements](#achievements) (3)

## CommonItemTooltip

### `Mods.tsorcRevamp.CommonItemTooltip.ShunpoKeybind1`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-07, `b33f4cb9f` Typos

```diff
-Press 
+ or press 
```

Changed:

- `Press` → `or press`

**ru-RU** (current):

```text
Нажмите 
```

**zh-Hans** (current):

```text
按 
```

### `Mods.tsorcRevamp.CommonItemTooltip.ShunpoKeybind2`

Needs update in: **zh-Hans** · English last changed: 2026-08-07, `b33f4cb9f` Typos

```diff
- while hovering your mouse over an enemy to blink to it and briefly become immune
+ while hovering your mouse over an enemy to blink to it and briefly become invincible
```

Changed:

- `immune` → `invincible`

**zh-Hans** (current):

```text
 冲向鼠标悬停处所指的敌人并获得短暂的无敌帧
```

### `Mods.tsorcRevamp.CommonItemTooltip.LifeRegen`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-22, `ca690a574` Updated Life Regen tooltip to show rate per second given, buffed Band of Mana Regeneration upgrades

```diff
-Increases life regeneration by {0}
+Increases life regeneration by {0} per second
```

Changed:

- added `per second`

**ru-RU** (current):

```text
Увеличивает регенерацию здоровья на {0} ед.
```

**zh-Hans** (current):

```text
增加 {0} 点生命再生
```

### `Mods.tsorcRevamp.CommonItemTooltip.BadLifeRegen`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-22, `ca690a574` Updated Life Regen tooltip to show rate per second given, buffed Band of Mana Regeneration upgrades

```diff
-Decreases life regeneration by {0}
+Decreases life regeneration by {0} per second
```

Changed:

- added `per second`

**ru-RU** (current):

```text
Уменьшает регенерацию здоровья на {0} ед.
```

**zh-Hans** (current):

```text
减少 {0} 点生命再生
```

### `Mods.tsorcRevamp.CommonItemTooltip.PermanentPotion.CanScale`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-05, `127ed5ec3` Adjusted Runeterra Orbs

```diff
 [c/77ff77:Grants a weaker {0} effect which increases]
 [c/77ff77:in strength with each non-permanent source of]
 [c/77ff77:{0} consumed. Total consumed:] {1}
 [c/77ff77:Effect potency is ]{2}%
+[c/77ff77:Levels even before being crafted.]
```

Changed:

- added `⏎ [c/77ff77:Levels even before being crafted.]`

**ru-RU** (current):

```text
[c/77ff77:Даёт более слабый эффект от зелья {0}, который увеличивается]
[c/77ff77:по силе с каждым выпитыми зельем]
[c/77ff77:{0}. Всего выпито:] {1}
[c/77ff77:текущая сила эффекта ]{2}%
```

**zh-Hans** (current):

```text
[c/77ff77:提供{0}药水的增益效果]
[c/77ff77:在永久药水未激活时，使用对应的普通药水以增加其效率]
[c/77ff77:当前总使用次数：]{1}
[c/77ff77:当前药水效率：]{2}%
```

## DeathText

### `Mods.tsorcRevamp.DeathText.02`

Needs update in: **zh-Hans** · English last changed: 2026-06-17, `f0b018cc5` New behavior system: Kiting, Ported more enemies to SF4 and stagger system ++

```diff
-Glowing green skulls often hold important secrets...Right Click to open them.
+Glowing green skulls hold important secrets...Right Click to open them
```

Changed:

- removed `often`
- `them.` → `them`

**zh-Hans** (current):

```text
发光的绿色头骨通常隐藏着重要的秘密...右键以打开
```

### `Mods.tsorcRevamp.DeathText.03`

Needs update in: **zh-Hans** · English last changed: 2026-06-20, `61b5339ea` New Class Selection Menu added during New Character Creation ++

```diff
-The dodgeroll keybind can be changed in Controls. Try LeftShift for dodge and LeftAlt for Auto Select
+The dodgeroll keybind can be changed in Controls. Recommended Controls can be turned on via Mod Config: Controls.
```

Changed:

- `Try LeftShift for dodge and LeftAlt for Auto Select` → `Recommended Controls can be turned on via Mod Config: Controls.`

**zh-Hans** (current):

```text
可以在“控制”中更改绑定的翻滚快捷键，尝试左Shift绑定翻滚、左Alt绑定智能光标
```

### `Mods.tsorcRevamp.DeathText.05`

Needs update in: **zh-Hans** · English last changed: 2026-06-17, `f0b018cc5` New behavior system: Kiting, Ported more enemies to SF4 and stagger system ++

```diff
-Losing souls on death can be disabled in the config. Try it for more of a Legend of Zelda experience
+Losing souls on death can be disabled in the config.
```

Changed:

- removed `Try it for more of a Legend of Zelda experience`

**zh-Hans** (current):

```text
死亡时失去灵魂可以在模组配置中禁用，试试吧就像塞尔达传说那样
```

### `Mods.tsorcRevamp.DeathText.11`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-06-17, `f0b018cc5` New behavior system: Kiting, Ported more enemies to SF4 and stagger system ++

```diff
-Get a bad modifier on a weapon? Talk to Jade, the Emerald Herald. She can remove it with her Blessing
+Get a bad modifier on a weapon? Talk to Jade, the Emerald Herald, outside Ashenpeak Village. She can remove it with her Blessing
```

Changed:

- `Herald.` → `Herald, outside Ashenpeak Village.`

**ru-RU** (current):

```text
У оружия плохой модификатор? Поговорите с Джейд, Изумрудным Вестником. Она может снять его с помощью своего благословения
```

**zh-Hans** (current):

```text
在武器上有一个负面的修饰词缀？与绿衣朝圣者Jade交谈，她可以用祝福来移除它
```

### `Mods.tsorcRevamp.DeathText.14`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-06-17, `f0b018cc5` New behavior system: Kiting, Ported more enemies to SF4 and stagger system ++

```diff
-Play with the Recipe Browser mod to easily see what your loot can be transformed into
+Use the Guide Slot in your inventory to easily see what your loot can be transformed into
```

Changed:

- `Play with` → `Use`
- `Recipe Browser mod` → `Guide Slot in your inventory`

**ru-RU** (current):

```text
Играйте с модом Recipe Browser, чтобы легко увидеть, во что можно превратить вашу добычу
```

**zh-Hans** (current):

```text
推荐使用 RecipeBrowser(合成表) 辅助模组，轻松查看新获得的物品有哪些合成配方
```

### `Mods.tsorcRevamp.DeathText.15`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-17, `9c2c80882` Modded minion changes in preparation of 1.4.5

```diff
-Hitting enemies with the tip of a whip will always result in a crit
+Hitting enemies with the tip of a whip will deal extra damage
```

Changed:

- `always result in a crit` → `deal extra damage`

**ru-RU** (current):

```text
Удар по врагам кончиком хлыста всегда приводит к критическому урону
```

**zh-Hans** (current):

```text
用鞭子尖端攻击敌人必定造成暴击
```

### `Mods.tsorcRevamp.DeathText.16`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-06-17, `f0b018cc5` New behavior system: Kiting, Ported more enemies to SF4 and stagger system ++

```diff
-I am so fucking mad
+Lao Tzu: "Let your workings remain a mystery. Just show people the results."
```

Changed:

- `I am so fucking mad` → `Lao Tzu: "Let your workings remain a mystery. Just show people the results."`

**ru-RU** (current):

```text
Я чертовски зол
```

**zh-Hans** (current):

```text
我真是**的疯了
```

### `Mods.tsorcRevamp.DeathText.25`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-18, `ef2231d5d` Introducing Unkindled Mode (PT1), Enhanced Soapstone System (PT2) + more

```diff
-Progression clues can be found by hovering over entries in the Boss Rematch Tome, or some unique items dropped by bosses
+Progression clues can be found by hovering over entries in the Boss Hunting Tome, or some unique items dropped by bosses
```

Changed:

- `Rematch` → `Hunting`

**ru-RU** (current):

```text
Подсказки к прохождению можно найти, наведя курсор на записи в книге "Матч-реванш с боссом" или на некоторые уникальные предметы, выпавшие из боссов
```

**zh-Hans** (current):

```text
可以在Boss重召唤书或Boss掉落的独特物品上找到下一步的线索
```

### `Mods.tsorcRevamp.DeathText.27`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-12, `685ea832c` Overcrit typo

```diff
-If you go above 100% crit chance you have a chance to multicrit, allowing you to deal 4 or more times the damage
+If you go above 100% crit chance you have a chance to multicrit, allowing you to deal even more damage
```

Changed:

- `4 or` → `even`
- removed `times the`

**ru-RU** (current):

```text
Если шанс крита превысит 100%, вы получите шанс мультикрита, что позволит вам наносить в 4 или более раз больше урона.
```

**zh-Hans** (current):

```text
如果你的暴击几率超过100%，你就有机会造成多重暴击，从而造成4倍或更多的伤害
```

### `Mods.tsorcRevamp.DeathText.BossDeath2`

Needs update in: **zh-Hans** · English last changed: 2026-06-17, `f0b018cc5` New behavior system: Kiting, Ported more enemies to SF4 and stagger system ++

```diff
-Certain bosses can be fought earlier than necessary. If you're struggling, try waiting until you're more powerful.
+Certain bosses can be fought earlier than necessary. If you're struggling, try coming back when you're more powerful
```

Changed:

- `waiting until` → `coming back when`
- `powerful.` → `powerful`

**zh-Hans** (current):

```text
可能会提前碰到不是当前时期的某些Boss。如果你打不过，试着提升装备后再来挑战。
```

## UI

### `Mods.tsorcRevamp.UI.StaminaUse`

Needs update in: **zh-Hans** · English last changed: 2023-09-14, `47680e24f` Stamine

```diff
-Stamine Use: 
+Stamina Use: 
```

Changed:

- `Stamine` → `Stamina`

**zh-Hans** (current):

```text
耐力消耗: 
```

### `Mods.tsorcRevamp.UI.MinionInfoDisplay`

Needs update in: **zh-Hans** · English last changed: 2024-01-19, `4a2169a87` Added Samurai Beetle and a few fixes

```diff
-{0}/{1} Minion Slot
+{0} / {1} Minion Slots
```

Changed:

- `{0}/{1}` → `{0} / {1}`
- `Slot` → `Slots`

**zh-Hans** (current):

```text
{0}/{1} 召唤栏
```

### `Mods.tsorcRevamp.UI.SentryInfoDisplay`

Needs update in: **zh-Hans** · English last changed: 2024-01-19, `4a2169a87` Added Samurai Beetle and a few fixes

```diff
-{0}/{1} Sentry Slot
+{0} / {1} Sentry Slots
```

Changed:

- `{0}/{1}` → `{0} / {1}`
- `Slot` → `Slots`

**zh-Hans** (current):

```text
{0}/{1} 哨兵栏
```

## DropRules

### `Mods.tsorcRevamp.DropRules.BotCOnly`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-18, `ef2231d5d` Introducing Unkindled Mode (PT1), Enhanced Soapstone System (PT2) + more

```diff
-[c/ff9999: Only drops when the player is a Bearer of the Curse]
+[c/ff9999: Only drops when the player is Unkindled or a Bearer of the Curse]
```

Changed:

- added `Unkindled or`

**ru-RU** (current):

```text
[c/ff9999: Выпадает только в том случае, если игрок является носителем Проклятия]
```

**zh-Hans** (current):

```text
[c/ff9999: 只有玩家是被诅咒者时掉落]
```

### `Mods.tsorcRevamp.DropRules.FirstBagBotCOnly`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-18, `ef2231d5d` Introducing Unkindled Mode (PT1), Enhanced Soapstone System (PT2) + more

```diff
-[c/ff9999: Only drops from the first opened specific Bag while the player is a Bearer of the Curse]
+[c/ff9999: Only drops from the first opened specific Bag while the player is Unkindled or a Bearer of the Curse]
```

Changed:

- added `Unkindled or`

**ru-RU** (current):

```text
[c/ff9999: Выпадает только из первого открытого особого мешка босса, пока игрок является Носителем проклятия]
```

**zh-Hans** (current):

```text
[c/ff9999: 当玩家是被诅咒者时，只能从第一个打开的特定袋子中掉落]
```

## BossChecklist

### `Mods.tsorcRevamp.BossChecklist.LeonhardDesc`

Needs update in: **zh-Hans** · English last changed: 2024-05-20, `60c430ebe` not finished BossRematchTome guide text currently text only shows after Boss Downed, so how to solve it? update Bosschecklist spawninfo for EarthFrined

```diff
-Explore.
+Explore the suspension bridge between snow-topped mountains.
```

Changed:

- `Explore.` → `Explore the suspension bridge between snow-topped mountains.`

**zh-Hans** (current):

```text
探索双峰之间的吊桥...
```

### `Mods.tsorcRevamp.BossChecklist.PinwheelDesc`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-02-01, `3220fbc41` Big commit, added/removed some weapons

```diff
-Seek Catacombs of the Drowned under the village entrance.
+Seek the Path of the Drowned under the village entrance.
```

Changed:

- `Catacombs` → `the Path`

**ru-RU** (current):

```text
Найдите Катакомбы Утопленников под входом в деревню.
```

**zh-Hans** (current):

```text
深入村庄入口附近的溺亡者地下墓穴...
```

### `Mods.tsorcRevamp.BossChecklist.HellkiteDragonDesc`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-04-14, `bd5f2da23` A lot of changes and content!!

English diff for ru-RU:

```diff
-flies on a large magmatic sky island, above the western sand dunes.
+Often sighted roaming the skies above the Village, searching for its next feast.
```

Changed:

- `flies on a large magmatic sky island,` → `Often sighted roaming the skies`
- `western sand dunes.` → `Village, searching for its next feast.`

English diff for zh-Hans:

```diff
 Often sighted roaming the skies above the Village, searching for its next feast.
-Remix Map: Flies on a large magmatic sky island, above the western sand dunes.
```

Changed:

- removed `⏎ Remix Map: Flies on a large magmatic sky island, above the western sand dunes.`

**ru-RU** (current):

```text
Летает на гигантском магматическом небесном острове, расположенным над западными дюнами
```

**zh-Hans** (current):

```text
经常在村庄上方的天空中游荡，寻找它的下一顿餐食...
重构版地图: 据说它盘踞于西部沙漠上空的巨型熔岩空岛...
```

### `Mods.tsorcRevamp.BossChecklist.WaterFiendKrakenDesc`

Needs update in: **ru-RU** · English last changed: 2025-10-12, `d0270a130` Ready for 0.16.2

```diff
 Seek out the lihzahrd gate in the Great Chasm.
+Remix Map: Seek out the lihzahrd gate in the Mycelium Forest.
```

Changed:

- added `⏎ Remix Map: Seek out the lihzahrd gate in the Mycelium Forest.`

**ru-RU** (current):

```text
Ищите врата яхщеров в Великой пропасти.
```

### `Mods.tsorcRevamp.BossChecklist.EarthFiendLichDesc`

Needs update in: **ru-RU** · English last changed: 2025-10-12, `d0270a130` Ready for 0.16.2

```diff
 Find the ancient Temple west side of the Ashen Cave.
+Remix Map: Seek out the lihzahrd gate in the Great Chasm.
```

Changed:

- added `⏎ Remix Map: Seek out the lihzahrd gate in the Great Chasm.`

**ru-RU** (current):

```text
Ищите древний храм к западу от Пепельной пещеры.
```

### `Mods.tsorcRevamp.BossChecklist.WitchkingDesc`

Needs update in: **ru-RU** · English last changed: 2025-10-12, `d0270a130` Ready for 0.16.2

```diff
-Find the ancient ship hidden in the Fire Temple.
+Found in his lair deep underground, shrouded in extreme darkness.
+Remix Map: Find the ancient ship hidden in the Shadow Temple.
```

Changed:

- added `Found in his lair deep underground, shrouded in extreme darkness. ⏎ Remix Map:`
- `Fire` → `Shadow`

**ru-RU** (current):

```text
Ищите древний корабль, спрятанный в храме огня
```

### `Mods.tsorcRevamp.BossChecklist.ArtoriasDesc`

Needs update in: **ru-RU** · English last changed: 2026-02-01, `3220fbc41` Big commit, added/removed some weapons

```diff
 The Abyss pervades the tunnels east of the Forgotten City.
+Remix Map: Find the forgotten path to Artorias in the Great Foundry somewhere in the Jungle.
```

Changed:

- added `⏎ Remix Map: Find the forgotten path to Artorias in the Great Foundry somewhere in the Jungle.`

**ru-RU** (current):

```text
Бездна пронизывает туннели к востоку от Забытого города.
```

### `Mods.tsorcRevamp.BossChecklist.AbysmalOolacileSorcererDesc`

Needs update in: **ru-RU** · English last changed: 2025-10-12, `d0270a130` Ready for 0.16.2

```diff
 Seek out the lihzahrd gate below the Lihzahrd Temple.
+Remix Map: Seek out the lihzahrd gate below the Eastern Ocean.
```

Changed:

- added `⏎ Remix Map: Seek out the lihzahrd gate below the Eastern Ocean.`

**ru-RU** (current):

```text
Ищите врата яхщеров под храмом яхщеров.
```

### `Mods.tsorcRevamp.BossChecklist.FireFiendMarilithDesc`

Needs update in: **ru-RU** · English last changed: 2025-10-12, `d0270a130` Ready for 0.16.2

```diff
 Seek out the lihzahrd gate in the Western Desert.
+Remix Map: Find the ancient ship hidden in the Fire Temple.
```

Changed:

- added `⏎ Remix Map: Find the ancient ship hidden in the Fire Temple.`

**ru-RU** (current):

```text
Ищите врата яхщеров в Западной пустыне.
```

### `Mods.tsorcRevamp.BossChecklist.ChaosDesc`

Needs update in: **ru-RU** · English last changed: 2025-10-12, `d0270a130` Ready for 0.16.2

```diff
 Seek the Moon Temple Gate, to the East of the Great Pryamid.
+Remix Map: Seek the Hallow once more, in the palladium caves.
```

Changed:

- added `⏎ Remix Map: Seek the Hallow once more, in the palladium caves.`

**ru-RU** (current):

```text
Ищите ворота Лунного храма, к востоку от Великой пирамиды.
```

### `Mods.tsorcRevamp.BossChecklist.DarkCloudDesc`

Needs update in: **ru-RU** · English last changed: 2025-10-12, `d0270a130` Ready for 0.16.2

```diff
 The ancient pyramid grows dark once more.
+Remix Map: Use the Dark Mirror.
```

Changed:

- added `⏎ Remix Map: Use the Dark Mirror.`

**ru-RU** (current):

```text
Древняя пирамида снова погружается во тьму.
```

## Items

### `Mods.tsorcRevamp.Items.VanillaItems.AmmoBox`

Needs update in: **zh-Hans** · English last changed: 2024-10-11, `7e043f34d` Worked on whips

```diff
-Increases ammo critical strike chance by the ammo's base damage
+Increases critical strike chance by your ammo's base damage
```

Changed:

- removed `ammo`
- `the` → `your`

**zh-Hans** (current):

```text
以弹药基础伤害值的一半增加弹药暴击几率
```

### `Mods.tsorcRevamp.Items.VanillaItems.ManaRegenerationBand`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-22, `ca690a574` Updated Life Regen tooltip to show rate per second given, buffed Band of Mana Regeneration upgrades

```diff
-Increases mana regen by 25 and decreases mana regen delay by 100%
+Increases mana regen by 30 and decreases mana regen delay by 100%
```

Changed:

- `25` → `30`

**ru-RU** (current):

```text
Увеличивает регенерацию маны на 25 и уменьшает задержку восстановления маны на 100%
```

**zh-Hans** (current):

```text
增加 25 点魔力再生同时减少 100% 魔力再生延迟
```

### `Mods.tsorcRevamp.Items.VanillaItems.CelestialMagnetBotC`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-09-07, `da015c2f2` Few melee wepaons fixes

```diff
-Mana Star pickups only restore {0}% of the [c/6d8827:Bearer of the Curse's] maximum mana
+Mana Star pickups restore {0}% of the [c/6d8827:Bearer of the Curse's] maximum mana
```

Changed:

- removed `only`

**ru-RU** (current):

```text
Подбор звёзд маны восстанавливает только {0}% от максимального количества маны [c/6d8827:носителя проклятия]
```

**zh-Hans** (current):

```text
魔力星只固定恢复[c/6d8827:被诅咒者]最大魔力值的 {0}%
```

### `Mods.tsorcRevamp.Items.VanillaItems.MagicCuffsBotC`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-09-07, `da015c2f2` Few melee wepaons fixes

```diff
-Only restores {0}% of pre-mitigation damage taken by the [c/6d8827:Bearer of the Curse] as mana
+Restores {0}% of pre-mitigation damage taken by the [c/6d8827:Bearer of the Curse] as mana
```

Changed:

- `Only restores` → `Restores`

**ru-RU** (current):

```text
Восстанавливает только {0}% урона, полученного [c/6d8827:носителем проклятия] до смягчения, в виде маны
```

**zh-Hans** (current):

```text
只恢复[c/6d8827:被诅咒者]受到伤害 {0}% 的魔力
```

### `Mods.tsorcRevamp.Items.AbysswalkerBanner.DisplayName` ⚠️

Needs update in: **ru-RU** · English last changed: 2026-06-16, `48660f706` Reorganization of some enemy files, improved Soapstone interactions and added new attacks for DemonSpirit

```diff
-{$NPCs.Abysswalker.DisplayName}{$CommonItemTooltip.Banner}
+{$NPCs.DworcAbysswalker.DisplayName}{$CommonItemTooltip.Banner}
```

Changed:

- `{$NPCs.Abysswalker.DisplayName}{$CommonItemTooltip.Banner}` → `{$NPCs.DworcAbysswalker.DisplayName}{$CommonItemTooltip.Banner}`

**ru-RU** (current):

```text
Знамя дворка-странника бездны
```

### `Mods.tsorcRevamp.Items.DworcVoodoomasterBanner.DisplayName` ⚠️

Needs update in: **ru-RU** · English last changed: 2026-06-16, `48660f706` Reorganization of some enemy files, improved Soapstone interactions and added new attacks for DemonSpirit

```diff
-{$NPCs.DworcVoodoomaster.DisplayName}{$CommonItemTooltip.Banner}
+{$NPCs.DworcAlchemist.DisplayName}{$CommonItemTooltip.Banner}
```

Changed:

- `{$NPCs.DworcVoodoomaster.DisplayName}{$CommonItemTooltip.Banner}` → `{$NPCs.DworcAlchemist.DisplayName}{$CommonItemTooltip.Banner}`

**ru-RU** (current):

```text
Знамя дворка алхимика
```

### `Mods.tsorcRevamp.Items.AuraOfDecay.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-14, `d78f685c2` Lots of fixes and new Miasma Debuff given to Plantera

```diff
-You periodically release a damaging wave of poison
+Every {1} seconds you release a wave of poison that deals {0} damage
```

Changed:

- `You periodically` → `Every {1} seconds you`
- removed `damaging`
- added `that deals {0} damage`

**ru-RU** (current):

```text
Вы периодически выпускаете разрушительную волну яда
```

**zh-Hans** (current):

```text
你会定期释放造成伤害的毒环
```

### `Mods.tsorcRevamp.Items.ConditionOverload.Tooltip`

Needs update in: **ru-RU** · English last changed: 2026-02-01, `3220fbc41` Big commit, added/removed some weapons

```diff
-Enemies take bonus damage for every debuff affecting them.
+Enemies take 7% bonus damage for every debuff affecting them.
 "H Deimos CO farm 3/4 LF despoil"
 Debuffs inflicted by whips excluded.
```

Changed:

- added `7%`

**ru-RU** (current):

```text
Враги получают дополнительный урон за каждый действующий на них дебафф.
"3/4 часть фермы компании Деймоса была ограблена"
Дебаффы, наносимые хлыстом, исключены.
```

### `Mods.tsorcRevamp.Items.BlueTearstoneRing.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-20, `53f31ef0e` Reworked Blue Tearstone Ring

```diff
 The rare gem called tearstone has the uncanny ability to sense imminent death.
-{$CommonItemTooltip.LifeThreshold}{$CommonItemTooltip.IncreasesDefenseBy@1} and
-{$CommonItemTooltip.DRStat@2}
+Increases the damage dealt by ranged projectiles based on how long they travel for.
+Caps at {0}% and takes {1} seconds to reach the maximum.
```

Changed:

- `{$CommonItemTooltip.LifeThreshold}{$CommonItemTooltip.IncreasesDefenseBy@1}` → `Increases the damage dealt by ranged projectiles based on how long they travel for. ⏎ Caps at {0}%`
- `⏎ {$CommonItemTooltip.DRStat@2}` → `takes {1} seconds to reach the maximum.`

**ru-RU** (current):

```text
Редкий драгоценный слёзный камень обладает сверхъестественной способностью чувствовать неминуемую смерть.
{$CommonItemTooltip.LifeThreshold}{$CommonItemTooltip.IncreasesDefenseBy@1} и
{$CommonItemTooltip.DRStat@2}
```

**zh-Hans** (current):

```text
这种珍贵的宝石不可思议的具备察觉正在迫近的死亡的能力
{$CommonItemTooltip.LifeThreshold}{$CommonItemTooltip.IncreasesDefenseBy@1}
{$CommonItemTooltip.DRStat@2}
```

### `Mods.tsorcRevamp.Items.GreyWolfRing.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-21, `d223b9c33` Basic setup for proper Max Health claclulator system

```diff
 A great ring, resonating with the abyss, which gives you a will of steel.
-Inherits {$Items.RingOfClarity.DisplayName} and {$Items.WolfRing.DisplayName} immunities
+Inherits {$Items.RingOfClarity.DisplayName} and {$Items.WolfRingItem.DisplayName} immunities
 Press Wolf Ring to massively increase life regen and damage taken temporarily
 Removes the life regen if hit during the effect and puts it on a long cooldown
 {$CommonItemTooltip.IncreasesDefenseBy} within the Abyss
 {$CommonItemTooltip.DRStat@1}
 {$CommonItemTooltip.LifeRegen@2}
 {$CommonItemTooltip.IncreasesMaxLifeByPercent@3}
```

Changed:

- `{$Items.WolfRing.DisplayName}` → `{$Items.WolfRingItem.DisplayName}`

**ru-RU** (current):

```text
Великое, резонирующее с бездной кольцо, придающее вам стальную волю
наследует иммунитеты предметов: {$Items.RingOfClarity.DisplayName} и {$Items.WolfRing.DisplayName}
Нажмите назначенную кнопку, чтобы временно усилить регенерацию здоровья и увеличить получаемый урон
При попадании в цель во время действия эффекта вы восстанавливаете здоровье, этот эффект имеет длительную перезарядку
{$CommonItemTooltip.IncreasesDefenseBy} в Бездне
{$CommonItemTooltip.DRStat@1}
{$CommonItemTooltip.LifeRegen@2}
{$CommonItemTooltip.IncreasesMaxLifeByPercent@3}
```

**zh-Hans** (current):

```text
一枚伟大的戒指，与深渊共鸣，给你钢铁般的意志
继承{$Items.RingOfClarity.DisplayName}和{$Items.WolfRing.DisplayName}的免疫效果
按下绑定的苍狼戒指快捷键以短暂提高生命恢复和受到的伤害
效果生效期间受到伤害会移除生命恢复效果加成，并进入长时间的冷却
在深渊中时{$CommonItemTooltip.IncreasesDefenseBy}
{$CommonItemTooltip.DRStat@1}
{$CommonItemTooltip.LifeRegen@2}
{$CommonItemTooltip.IncreasesMaxLifeByPercent@3}
```

### `Mods.tsorcRevamp.Items.MythrilGlove.Tooltip`

Needs update in: **ru-RU** · English last changed: 2025-11-01, `842282575` BIG COMMIT FOR 0.16.4

```diff
 Grants bonus defense and resistance based on health:
-Increase defense by 5 and resistance by 8 at all times
-Increase defense by 10 and resistance by 16 below 66% health
-Increase defense by 16 and resistance by 32 below 33% health
+Increase defense by 6 and resistance by 10 at all times
+Increase defense by 11 and resistance by 18 below 66% health
+Increase defense by 18 and resistance by 34 below 33% health
```

Changed:

- `5` → `6`
- `8` → `10`
- `10` → `11`
- `16` → `18`
- `16` → `18`
- `32` → `34`

**ru-RU** (current):

```text
Дает дополнительную защиту и сопротивление, зависящие от здоровья:
Увеличивает защиту на 5 и сопротивление на 8 в любое время.
Увеличивает защиту на 10 и сопротивление на 16 при уровне здоровья ниже 66%.
Увеличивает защиту на 16 и сопротивление на 32 при уровне здоровья ниже 33%.
```

### `Mods.tsorcRevamp.Items.BurningAura.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-14, `d78f685c2` Lots of fixes and new Miasma Debuff given to Plantera

```diff
 An array of meteorite shards, floating in a slow orbit
 Passively launches homing fireballs at nearby enemies
-Fireballs scale in power with each boss you kill
+Fireballs deal {1} damage, scaling with each boss you kill
 Burning foes take {0}% more damage from other attacks
```

Changed:

- `scale in power` → `deal {1} damage, scaling`

**ru-RU** (current):

```text
Массив осколков метеорита, медленно движущихся по орбите
Пассивно запускает самонаводящиеся огненные шары в ближайших врагов
Сила огненных шаров увеличивается с каждым убитым вами боссом
Горящие противники получают на {0}% больше урона от других атак
```

**zh-Hans** (current):

```text
一排缓慢的在轨道上漂浮的陨石碎片
向附近的敌人一连串发射追踪火球
每击杀一个Boss，火球的威力就会增加
燃烧的敌人受到其他攻击的伤害增加 {0}%
```

### `Mods.tsorcRevamp.Items.ChloranthyRing.DisplayName`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-09-06, `6e6f59b6e` Nerfed Chloranthy Ring out of "S infinity+" tier to S/A+ tier, reset default Estus drink timer

```diff
-Chloranthy Ring I
+Chloranthy Ring
```

Changed:

- removed `I`

**ru-RU** (current):

```text
Хлорантиевое кольцо I
```

**zh-Hans** (current):

```text
绿花戒指 I
```

### `Mods.tsorcRevamp.Items.ChloranthyRing.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-09-06, `6e6f59b6e` Nerfed Chloranthy Ring out of "S infinity+" tier to S/A+ tier, reset default Estus drink timer

English diff for ru-RU:

```diff
+[c/ffbf00:Enhances your agility and evasiveness when dodge rolling]
+Stamina begins recovering {1}% sooner after you spend it
+Increases Stamina Droplet pickup range.
 {$CommonItemTooltip.StaminaRecovery}
-[c/ffbf00:Enhances your agility and evasiveness when dodge rolling]
-[c/ffbf00:Reduces the movement penalty for holding a shield by 2%]
-[c/ca1e00:When drinking your Estus Flask, your movement is no longer hindered, at the cost of being inflicted with Ichor (-15 defense)]
-Increases Stamina Droplet pickup range.
 This old ring is named for its decorative green blossom,
 but its luster is long since faded
```

Changed:

- removed `{$CommonItemTooltip.StaminaRecovery} ⏎`
- `[c/ffbf00:Reduces the movement penalty for holding a shield by 2%] ⏎ [c/ca1e00:When drinking your Estus Flask, your movement is no longer hindered, at the cost of being inflicted with Ichor (-15 defense)]` → `Stamina begins recovering {1}% sooner after you spend it`
- added `⏎ {$CommonItemTooltip.StaminaRecovery}`

English diff for zh-Hans:

```diff
+[c/ffbf00:Enhances your agility and evasiveness when dodge rolling]
+Stamina begins recovering {1}% sooner after you spend it
+Increases Stamina Droplet pickup range.
 {$CommonItemTooltip.StaminaRecovery}
-[c/ffbf00:Enhances your agility and evasiveness when dodge rolling]
-BotCOnly: "[c/ca1e00:When drinking your Cerulean Flask, your movement is no longer hindered at the cost of temporary vulnerability.]"
-Increases Stamina Droplet pickup range.
 This old ring is named for its decorative green blossom,
 but its luster is long since faded
```

Changed:

- removed `{$CommonItemTooltip.StaminaRecovery} ⏎`
- `BotCOnly: "[c/ca1e00:When drinking your Cerulean Flask, your movement is no longer hindered at the cost of temporary vulnerability.]"` → `Stamina begins recovering {1}% sooner after you spend it`
- added `⏎ {$CommonItemTooltip.StaminaRecovery}`

**ru-RU** (current):

```text
{$CommonItemTooltip.StaminaRecovery}
[c/ffbf00:Повышает вашу ловкость и уклончивость при уклонении от ударов]
Режим носителя проклятия: "[c/ca1e00:Когда вы пьете из лазурной фляги, ваши движения больше не стеснены ценой временной уязвимости.]"
Увеличивает дальность подбора капель выносливости.
Это старое кольцо получило свое название из-за своего декоративного зеленого цветка,
но его блеск давно померк.
```

**zh-Hans** (current):

```text
{$CommonItemTooltip.StaminaRecovery}
[c/ffbf00:加强了翻滚时的灵巧度与闪避能力]
[c/ca1e00:被诅咒者: 使用蓝露滴圣杯瓶时不再限制移动能力]
增加耐力液滴的拾取范围
这枚古老的戒指因为其装饰性的绿色花朵而得名
但光泽却早已流逝
```

### `Mods.tsorcRevamp.Items.ChloranthyRing2.DisplayName`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-09-06, `6e6f59b6e` Nerfed Chloranthy Ring out of "S infinity+" tier to S/A+ tier, reset default Estus drink timer

```diff
-Chloranthy Ring II
+Enchanted Chloranthy Ring
```

Changed:

- added `Enchanted`
- removed `II`

**ru-RU** (current):

```text
Хлорантиевое кольцо II
```

**zh-Hans** (current):

```text
绿花戒指 II
```

### `Mods.tsorcRevamp.Items.ChloranthyRing2.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-09-06, `6e6f59b6e` Nerfed Chloranthy Ring out of "S infinity+" tier to S/A+ tier, reset default Estus drink timer

English diff for ru-RU:

```diff
+[c/ffbf00:Further enhances your agility and evasiveness when dodge rolling]
+[c/ffbf00:Grants superior mid-air dexterity]
+Stamina begins recovering {1}% sooner after you spend it
+Increases Stamina Droplet pickup range even further.
 {$CommonItemTooltip.StaminaRecovery}
-[c/ffbf00:Further enhances your agility and evasiveness when dodge rolling]
-[c/ffbf00:Reduces the movement penalty for holding a shield by 4%]
-[c/ffbf00:Grants the Black Belt's effect & superior mid-air dexterity]
-[c/ca1e00:When drinking your Estus Flask, your movement is no longer hindered, at the cost of being inflicted with Ichor (-15 defense)]
 This ring is named for its decorative green blossom.
 Its luster has now been fully restored.
```

Changed:

- removed `{$CommonItemTooltip.StaminaRecovery} ⏎`
- removed `[c/ffbf00:Reduces the movement penalty for holding a shield by 4%] ⏎`
- removed `the Black Belt's effect &`
- `[c/ca1e00:When drinking your Estus Flask, your movement is no longer hindered, at the cost of being inflicted with Ichor (-15 defense)]` → `Stamina begins recovering {1}% sooner after you spend it ⏎ Increases Stamina Droplet pickup range even further. ⏎ {$CommonItemTooltip.StaminaRecovery}`

English diff for zh-Hans:

```diff
+[c/ffbf00:Further enhances your agility and evasiveness when dodge rolling]
+[c/ffbf00:Grants superior mid-air dexterity]
+Stamina begins recovering {1}% sooner after you spend it
+Increases Stamina Droplet pickup range even further.
 {$CommonItemTooltip.StaminaRecovery}
-[c/ffbf00:Further enhances your agility and evasiveness when dodge rolling]
-[c/ffbf00:Grants the Black Belt's effect & superior mid-air dexterity]
-BotCOnly: "[c/ca1e00:When drinking your Cerulean Flask, your movement is no longer hindered at the cost of temporary vulnerability.]"
 This ring is named for its decorative green blossom.
 Its luster has now been fully restored.
```

Changed:

- removed `{$CommonItemTooltip.StaminaRecovery} ⏎`
- removed `the Black Belt's effect &`
- `BotCOnly: "[c/ca1e00:When drinking your Cerulean Flask, your movement is no longer hindered at the cost of temporary vulnerability.]"` → `Stamina begins recovering {1}% sooner after you spend it ⏎ Increases Stamina Droplet pickup range even further. ⏎ {$CommonItemTooltip.StaminaRecovery}`

**ru-RU** (current):

```text
{$CommonItemTooltip.StaminaRecovery}
[c/ffbf00:Повышает вашу ловкость и уклончивость при уклонении от ударов]
[c/ffbf00:Придаёт эффект чёрного пояса и превосходную ловкость в воздухе]
Режим носителя проклятия: "[c/ca1e00:Когда вы пьете из лазурной фляги, ваши движения больше не стеснены ценой временной уязвимости.]"
Это кольцо названо так из-за его декоративного зеленого цветка.
Теперь его блеск полностью восстановлен.
```

**zh-Hans** (current):

```text
{$CommonItemTooltip.StaminaRecovery}
[c/ffbf00:加强了翻滚时的灵巧度与闪避能力]
[c/ffbf00:提供黑腰带的效果和空中的高机动能力]
[c/ca1e00:被诅咒者: 使用蓝露滴圣杯瓶时不再限制移动能力]
这枚古老的戒指因为其装饰性的绿色花朵而得名
它的光泽现在已经完全恢复了
```

### `Mods.tsorcRevamp.Items.DragonCrestShield.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-06-02, `4a30cc1aa` 3 New Features: Active Shields, Parries & 2nd Slot

```diff
 Unique shield of a nameless knight
-[c/ffbf00:{$CommonItemTooltip.DRStat} when not attacking or {2} when attacking, but drains {3} stamina per hit]
-{$CommonItemTooltip.NoKnockback} and protects against {$CommonItemTooltip.OnFire} but reduces stamina regen by {4}%
+[c/ffbf00:{$CommonItemTooltip.DRStat} when not attacking or {2} when attacking, but drains {3} stamina per hit and reduces stamina regen by {4}%]
+{$CommonItemTooltip.NoKnockback} and protects against {$CommonItemTooltip.OnFire}
 {$CommonItemTooltip.DRStat@1} if you can't afford the stamina cost
```

Changed:

- `hit]` → `hit and reduces stamina regen by {4}%]`
- removed `but reduces stamina regen by {4}%`

**ru-RU** (current):

```text
Уникальный щит безымянного рыцаря
[c/ffbf00:{$CommonItemTooltip.DRStat} когда не атакует или {2} когда атакует, но отнимает {3} выносливости за удар]
{$CommonItemTooltip.NoKnockback} и защищает от {$CommonItemTooltip.OnFire} но уменьшает регенерацию выносливости на {4}%
{$CommonItemTooltip.DRStat@1} если вы не можете позволить себе затраты на выносливость
```

**zh-Hans** (current):

```text
属于一个无名骑士的独特盾牌
[c/ffbf00:不攻击时{$CommonItemTooltip.DRStat}，攻击时增加 {2} 抵抗力，每次抵挡伤害消耗{3}耐力]
{$CommonItemTooltip.NoKnockback}并免疫{$CommonItemTooltip.OnFire}，但会减少{4}%的耐力再生
在体力不足时{$CommonItemTooltip.DRStat@1}
```

### `Mods.tsorcRevamp.Items.DragonStone.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-20, `dd6f7d9a3` Revamped DoT debuff code for npcs and fixed Oiled stacking

```diff
 Increases the potency of most weak debuffs on enemies by {0}x
-Potency increase is 2x less effective with strong debuffs
+Potency increase is {1}x less effective with strong debuffs
```

Changed:

- `2x` → `{1}x`

**ru-RU** (current):

```text
{0}x Увеличние эффективности большинства слабых дебаффов на врагах
Повышение эффективности в 2 раза менее эффективно при сильных дебаффах
```

**zh-Hans** (current):

```text
增加 {0} 倍对敌人造成的减益伤害
较强力减益的伤害只受到增益的一半
```

### `Mods.tsorcRevamp.Items.GoldenHairpin.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-09-06, `226f5e580` Added shield benefits to Ring of Favor and Protection, buffing and fixing it

```diff
 Increases either arrow, bullet or specialist damage by {0}%
 Ammo type is decided by a timer that picks one of the three regularly
 Plays a sound to indicate the switch every {1} seconds
+Disabling accessory visibility mutes the sound
```

Changed:

- added `⏎ Disabling accessory visibility mutes the sound`

**ru-RU** (current):

```text
Увеличивает урон от стрел, пуль или спецсредств на {0}%
Тип боеприпасов определяется таймером, который регулярно выбирает один из трёх
Каждые {1} секунд воспроизводится звуковой сигнал, указывающий на переключение
```

**zh-Hans** (current):

```text
增加 {0}% 弓箭、子弹或特殊弹药的伤害
增伤的类型定期从三种弹药中选择一种
七秒一更换
```

### `Mods.tsorcRevamp.Items.EnchantedBeholderShield2.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-06-02, `4a30cc1aa` 3 New Features: Active Shields, Parries & 2nd Slot

```diff
-A legendary shield for melee warriors only
+{$CommonItemTooltip.Melee.Only}
 {$CommonItemTooltip.ObsidianShield} plus {$CommonItemTooltip.OnFire}, {$CommonItemTooltip.CursedInferno} and {$CommonItemTooltip.Ichor}
 {$CommonItemTooltip.DRStat}
 {$CommonItemTooltip.NonMeleeBadDmg@1}
 {$CommonItemTooltip.BadMoveSpeedMult@2}
```

Changed:

- `A legendary shield for melee warriors only` → `{$CommonItemTooltip.Melee.Only}`

**ru-RU** (current):

```text
Легендарный щит, предназначенный только для воинов ближнего боя
{$CommonItemTooltip.ObsidianShield} плюс {$CommonItemTooltip.OnFire}, {$CommonItemTooltip.CursedInferno} и {$CommonItemTooltip.Ichor}
{$CommonItemTooltip.DRStat}
{$CommonItemTooltip.NonMeleeBadDmg@1}
{$CommonItemTooltip.BadMoveSpeedMult@2}
```

**zh-Hans** (current):

```text
一面为战士打造的传奇盾牌
{$CommonItemTooltip.ObsidianShield}，同时免疫{$CommonItemTooltip.OnFire}、{$CommonItemTooltip.CursedInferno}、{$CommonItemTooltip.Ichor}
{$CommonItemTooltip.DRStat}
{$CommonItemTooltip.NonMeleeBadDmg@1}
{$CommonItemTooltip.BadMoveSpeedMult@2}
```

### `Mods.tsorcRevamp.Items.SupersonicWings.Tooltip`

Needs update in: **zh-Hans** · English last changed: 2024-05-16, `8ca4fdc2e` 0.15.8

```diff
-Moderate supersonic movement speed, flight, fire-walk skill,
+Awesome supersonic movement speed, flight, fire-walk skill,
 plus all the previous abilities of Supersonic Boots.
 {$CommonItemTooltip.DragoonGearCompatible}
 {$CommonItemTooltip.BootsDisableThis}
```

Changed:

- `Moderate` → `Awesome`

**zh-Hans** (current):

```text
提供中等超音速移动速度、飞行能力和火块行走能力
继承超音速之靴的所有能力
{$CommonItemTooltip.DragoonGearCompatible}
{$CommonItemTooltip.BootsDisableThis}
```

### `Mods.tsorcRevamp.Items.MonocleOfPrecision.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-17, `9c2c80882` Modded minion changes in preparation of 1.4.5

```diff
 {$CommonItemTooltip.PercentIncreasedCritChance}
-{$CommonItemTooltip.Summon.WhipCritHitbox@1}
+{$CommonItemTooltip.Summon.WhipTipHitbox@1}
```

Changed:

- `{$CommonItemTooltip.Summon.WhipCritHitbox@1}` → `{$CommonItemTooltip.Summon.WhipTipHitbox@1}`

**ru-RU** (current):

```text
{$CommonItemTooltip.PercentIncreasedCritChance}
{$CommonItemTooltip.Summon.WhipCritHitbox@1}
```

**zh-Hans** (current):

```text
{$CommonItemTooltip.PercentIncreasedCritChance}
{$CommonItemTooltip.Summon.WhipCritHitbox@1}
```

### `Mods.tsorcRevamp.Items.ChallengersGlove.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-17, `9c2c80882` Modded minion changes in preparation of 1.4.5

```diff
 {$CommonItemTooltip.PercentIncreasedSummonDamage@0}
 {$CommonItemTooltip.Summon.AtkSpeed@1}
 {$CommonItemTooltip.IncreasesWhipRangeByPercent@2}
-{$CommonItemTooltip.Summon.WhipCritHitbox@3}
+{$CommonItemTooltip.Summon.WhipTipHitbox@3}
 {$CommonItemTooltip.Summon.WhipKnockback}
 {$CommonItemTooltip.MeleeWhipAutoswing}
 {$CommonItemTooltip.AggroBoost}
```

Changed:

- `{$CommonItemTooltip.Summon.WhipCritHitbox@3}` → `{$CommonItemTooltip.Summon.WhipTipHitbox@3}`

**ru-RU** (current):

```text
{$CommonItemTooltip.PercentIncreasedSummonDamage@0}
{$CommonItemTooltip.Summon.AtkSpeed@1}
{$CommonItemTooltip.IncreasesWhipRangeByPercent@2}
{$CommonItemTooltip.Summon.WhipCritHitbox@3}
{$CommonItemTooltip.Summon.WhipKnockback}
{$CommonItemTooltip.MeleeWhipAutoswing}
{$CommonItemTooltip.AggroBoost}
```

**zh-Hans** (current):

```text
{$CommonItemTooltip.PercentIncreasedSummonDamage@0}
{$CommonItemTooltip.Summon.AtkSpeed@1}
{$CommonItemTooltip.IncreasesWhipRangeByPercent@2}
{$CommonItemTooltip.Summon.WhipCritHitbox@3}
{$CommonItemTooltip.Summon.WhipKnockback}
{$CommonItemTooltip.MeleeWhipAutoswing}
{$CommonItemTooltip.AggroBoost}
```

### `Mods.tsorcRevamp.Items.CrystallineShard.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-07-19, `1cc785f6c` Nerfed Morgul Blade and Crystalline Shard

```diff
 {$CommonItemTooltip.IncreasesMaxMinionsBy}
 For each minion slot you have,
-increase whip damage by {2}% and tag strength by {1}% but also decrease whip range by {1}%
+increase whip damage by {2}% and tag strength by {1}% but also decrease whip range by {3}%
 "A magical substance found only in the Crystalline Dimension"
```

Changed:

- `{1}%` → `{3}%`

**ru-RU** (current):

```text
{$CommonItemTooltip.IncreasesMaxMinionsBy}
За каждый слот для миньонов, который у вас есть,
увеличивает урон от хлыста на {2}% и силу метки призыва на {1}%, но также уменьшает дальность действия хлыста на {1}%
"Магическое вещество, встречающееся только в Кристаллическом Измерении"
```

**zh-Hans** (current):

```text
{$CommonItemTooltip.IncreasesMaxMinionsBy}
每拥有一个召唤栏
增加 {2}% 鞭子伤害和 {1}% 召唤标记强度，但减少 {1}% 鞭子攻击范围
"一种只在水晶维度中发现的神奇物质"
```

### `Mods.tsorcRevamp.Items.CrystallineShard.Tooltip0` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-07-19, `1cc785f6c` Nerfed Morgul Blade and Crystalline Shard

```diff
-Current Modifier: {0}% summon tag strength and negative whip range, {1}% whip damage
+Current Modifier: {0}% summon tag strength and -{2}% whip range, {1}% whip damage
```

Changed:

- `negative` → `-{2}%`

**ru-RU** (current):

```text
Текущий модификатор: {0}% силы метки призыва и негативной дальности хлыста, {1}% урона хлыста
```

**zh-Hans** (current):

```text
当前召唤属性改变量: 增加 {0}% 召唤标记强度和 {1}% 鞭子伤害，减少 {0}% 鞭子攻击范围
```

### `Mods.tsorcRevamp.Items.AncientMagicPlateArmor.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-09-06, `2db54b66b` Fixed some shenanigans with Magic Plating and Shunpo

```diff
 Fueled by a magical gem in the chest
 {$CommonItemTooltip.AtkSpeed}{$CommonItemTooltip.Melee.Bonus@1}
-{$CommonItemTooltip.SetBonus}Chance to gain stacks upon damaging anything
-Collect stacks to gain up to 28 resistance against the next hit
+{$CommonItemTooltip.SetBonus}1/{3} chance to gain stacks upon damaging anything
+Collect stacks to gain up to {2} resistance against the next hit
```

Changed:

- `{$CommonItemTooltip.SetBonus}Chance` → `{$CommonItemTooltip.SetBonus}1/{3} chance`
- `28` → `{2}`

**ru-RU** (current):

```text
Подпитываемый волшебным камнем в нагрудной части
{$CommonItemTooltip.AtkSpeed}{$CommonItemTooltip.Melee.Bonus@1}
{$CommonItemTooltip.SetBonus}Шанс получить стаки при нанесении урона кому-либо
при накоплении 28 стаков вы получите сопротивление к следующему удару.
```

**zh-Hans** (current):

```text
由一个宝箱里的魔法石提供燃料
{$CommonItemTooltip.AtkSpeed}{$CommonItemTooltip.Melee.Bonus@1}
{$CommonItemTooltip.SetBonus}对任何东西造成伤害时获得堆叠层数
每层堆叠增加 1% 抵抗力，最高到28%
```

### `Mods.tsorcRevamp.Items.ArtoriasArmor.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-14, `d6c5394a5` Last commit for 1.4.4, a lot of progress but a lot of wip stuffs too!

```diff
 Enchanted armor of Artorias of the Abyss.
 {$CommonItemTooltip.PercentIncreasedMeleeDamage}
 {$CommonItemTooltip.SetBonus}Allows you to use items while dodgerolling
 Increase additional {1}x melee damage while dodgerolling
+{$CommonItemTooltip.AggroBoost}
```

Changed:

- added `⏎ {$CommonItemTooltip.AggroBoost}`

**ru-RU** (current):

```text
Зачарованные доспехи Арториаса из Бездны.
{$CommonItemTooltip.PercentIncreasedMeleeDamage}
{$CommonItemTooltip.SetBonus}Позволяет вам использовать предметы во время переката
{1}x увеличение урона ближнего боя при перекате
```

**zh-Hans** (current):

```text
亚尔特留斯的附魔盔甲
{$CommonItemTooltip.PercentIncreasedMeleeDamage}
{$CommonItemTooltip.SetBonus}允许你在翻滚时使用武器
翻滚时额外增加 {1} 倍近战伤害
```

### `Mods.tsorcRevamp.Items.KrakenCarcass.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-21, `0e336de62` add armor pen tooltip to KrakenCarcass

```diff
 {$CommonItemTooltip.PercentChanceToSaveAmmo}
-{$CommonItemTooltip.SetBonus}{$Items.KrakenCarcass.Keybind1}KrakensCast{$Items.KrakenCarcass.Keybind2}
-The Tsunami will increase the damage of ranged projectiles that pass through it by {1}%
+{$CommonItemTooltip.Ranged.ArmorPenRanged@1}
+{$Items.KrakenCarcass.Keybind1}KrakensCast{$Items.KrakenCarcass.Keybind2}
+The Tsunami will increase the damage of ranged projectiles that pass through it by {2}%
```

Changed:

- `{$CommonItemTooltip.SetBonus}{$Items.KrakenCarcass.Keybind1}KrakensCast{$Items.KrakenCarcass.Keybind2}` → `{$CommonItemTooltip.Ranged.ArmorPenRanged@1} ⏎ {$Items.KrakenCarcass.Keybind1}KrakensCast{$Items.KrakenCarcass.Keybind2}`
- `{1}%` → `{2}%`

**ru-RU** (current):

```text
{$CommonItemTooltip.PercentChanceToSaveAmmo}
{$CommonItemTooltip.SetBonus}{$Items.KrakenCarcass.Keybind1}KrakensCast{$Items.KrakenCarcass.Keybind2}
Цунами увеличит урон от дальнобойных снарядов, которые пройдут сквозь него, на {1}%
```

**zh-Hans** (current):

```text
{$CommonItemTooltip.PercentChanceToSaveAmmo}
{$CommonItemTooltip.SetBonus}{$Items.KrakenCarcass.Keybind1}KrakensCast{$Items.KrakenCarcass.Keybind2}
提高 {1}% 穿过海妖旋风的远程射弹的伤害
```

### `Mods.tsorcRevamp.Items.KrakenCarcass.Keybind1`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-07, `ca156a456` Armor Set actives can now be activated via vanilla armor set bonus key

```diff
-{$CommonItemTooltip.SetBonus}Press 
+{$CommonItemTooltip.SetBonus}
```

Changed:

- `{$CommonItemTooltip.SetBonus}Press` → `{$CommonItemTooltip.SetBonus}`

**ru-RU** (current):

```text
{$CommonItemTooltip.SetBonus}Нажмите 
```

**zh-Hans** (current):

```text
{$CommonItemTooltip.SetBonus}按 
```

### `Mods.tsorcRevamp.Items.KrakenCarcass.Keybind2`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-07, `ca156a456` Armor Set actives can now be activated via vanilla armor set bonus key

```diff
- to cast a floating Tsunami at your cursor position
+ or press 
```

Changed:

- `to cast a floating Tsunami at your cursor position` → `or press`

**ru-RU** (current):

```text
 чтобы создать плавающее цунами в месте расположения вашего курсора
```

**zh-Hans** (current):

```text
 在光标位置生成一个浮空的海妖旋风
```

### `Mods.tsorcRevamp.Items.GenjiArmor.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-07, `ca156a456` Armor Set actives can now be activated via vanilla armor set bonus key

```diff
 Armor from the East, enchanted with magic forces
 {$CommonItemTooltip.PercentReducedManaCost}
 {$CommonItemTooltip.Magic.ManaRegen@1}
 {$CommonItemTooltip.SetBonus}{$CommonItemTooltip.TitaniumBonus}
-{$CommonItemTooltip.ShunpoKeybind1}Shunpo{$CommonItemTooltip.ShunpoKeybind2}
 {$CommonItemTooltip.ShunpoCD@2}
```

Changed:

- removed `{$CommonItemTooltip.ShunpoKeybind1}Shunpo{$CommonItemTooltip.ShunpoKeybind2} ⏎`

**ru-RU** (current):

```text
Доспехи с Востока, зачарованные магическими силами
{$CommonItemTooltip.PercentReducedManaCost}
{$CommonItemTooltip.Magic.ManaRegen@1}
{$CommonItemTooltip.SetBonus}{$CommonItemTooltip.TitaniumBonus}
{$CommonItemTooltip.ShunpoKeybind1}поступь{$CommonItemTooltip.ShunpoKeybind2}
{$CommonItemTooltip.ShunpoCD@2}
```

**zh-Hans** (current):

```text
来自东方武士的附魔盔甲
{$CommonItemTooltip.PercentReducedManaCost}
{$CommonItemTooltip.Magic.ManaRegen@1}
{$CommonItemTooltip.SetBonus}{$CommonItemTooltip.TitaniumBonus}
{$CommonItemTooltip.ShunpoKeybind1}绑定的瞬步快捷键{$CommonItemTooltip.ShunpoKeybind2}
{$CommonItemTooltip.ShunpoCD@2}
```

### `Mods.tsorcRevamp.Items.RTQ2Chestplate.Tooltip`

Needs update in: **zh-Hans** · English last changed: 2024-01-21, `7c85a6912` A bunch of fixes + Code Cleanup

```diff
 {$CommonItemTooltip.PercentIncreasedMagicCritChance}
 {$CommonItemTooltip.SetBonus}{$CommonItemTooltip.Magic.AtkSpeed@1}, meteorite set bonus
-{$CommonItemTooltip.LifeThreshold@2}Emergency Shield kicks:
+{$CommonItemTooltip.LifeThreshold@2}Emergency Shield kicks in:
 {$CommonItemTooltip.IncreasesDefenseBy@3}
```

Changed:

- `kicks:` → `kicks in:`

**zh-Hans** (current):

```text
{$CommonItemTooltip.PercentIncreasedMagicCritChance}
{$CommonItemTooltip.SetBonus}{$CommonItemTooltip.Magic.AtkSpeed@1}，提供陨石盔甲套装效果
{$CommonItemTooltip.LifeThreshold@2}护盾开始生效:
{$CommonItemTooltip.IncreasesDefenseBy@3}
```

### `Mods.tsorcRevamp.Items.MirkwoodElvenLeggings.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-06-27, `326312221` Added Cursed Dragon transformation accessory + 3 invaders + more

```diff
 Gifted with ranged combat. High defense not necessary.
-{$CommonItemTooltip.PercentIncreasedMovementSpeed} and Jump Boost
+{$CommonItemTooltip.PercentIncreasedMovementSpeed}
```

Changed:

- removed `and Jump Boost`

**ru-RU** (current):

```text
Обладают способностями к дальнему бою. Высокая защита не требуется.
{$CommonItemTooltip.PercentIncreasedMovementSpeed} и ускорение прыжка
```

**zh-Hans** (current):

```text
擅长远程战斗，不需要高防御
{$CommonItemTooltip.PercentIncreasedMovementSpeed}，同时跳跃能力得到提升
```

### `Mods.tsorcRevamp.Items.AncientDemonArmor.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-17, `9c2c80882` Modded minion changes in preparation of 1.4.5

```diff
 Forged by those who brave Annihilation
 {$CommonItemTooltip.Summon.WhipDmg}
 {$CommonItemTooltip.Summon.TagDuration@1}
 {$CommonItemTooltip.SetBonus}{$CommonItemTooltip.IncreasesWhipRangeByPercent@2}
 {$CommonItemTooltip.Summon.AtkSpeed@3}
-{$CommonItemTooltip.LifeThreshold@4}Whip Tip Crits trigger nasty explosions
+{$CommonItemTooltip.LifeThreshold@4}Whip Tip Hits trigger nasty explosions,
+{$Items.DarkSword.DisplayName} triggers these on-hit
```

Changed:

- `Crits` → `Hits`
- `explosions` → `explosions, ⏎ {$Items.DarkSword.DisplayName} triggers these on-hit`

**ru-RU** (current):

```text
Выкованный теми, кто отважно пережил уничтожение
{$CommonItemTooltip.Summon.WhipDmg}
{$CommonItemTooltip.Summon.TagDuration@1}
{$CommonItemTooltip.SetBonus}{$CommonItemTooltip.IncreasesWhipRangeByPercent@2}
{$CommonItemTooltip.Summon.AtkSpeed@3}
{$CommonItemTooltip.LifeThreshold@4}Крит. удары от кончика хлыста вызывают ужасные взрывы
```

**zh-Hans** (current):

```text
由直面毁灭的勇敢者铸造
{$CommonItemTooltip.Summon.WhipDmg}
{$CommonItemTooltip.Summon.TagDuration@1}
{$CommonItemTooltip.SetBonus}{$CommonItemTooltip.IncreasesWhipRangeByPercent@2}
{$CommonItemTooltip.Summon.AtkSpeed@3}
{$CommonItemTooltip.LifeThreshold@4}鞭子尖端造成暴击的同时引发爆炸
```

### `Mods.tsorcRevamp.Items.WitchkingRobe.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-14, `d6c5394a5` Last commit for 1.4.4, a lot of progress but a lot of wip stuffs too!

```diff
 {$CommonItemTooltip.PercentIncreasedSummonDamage}
 {$CommonItemTooltip.IncreasesMaxMinionsBy@1}
-{$CommonItemTooltip.SetBonus}{$Items.WitchkingRobe.Keybind1}WitchScream{$Items.WitchkingRobe.Keybind2}
+{$CommonItemTooltip.SetBonus}{$Items.WitchkingRobe.Keybind1}{$Items.WitchkingRobe.Keybind2}
 Affected enemies deal less damage, have reduced defense and speed, and take significant damage over time for a short moment
+{$CommonItemTooltip.AggroDecrease}
```

Changed:

- `{$CommonItemTooltip.SetBonus}{$Items.WitchkingRobe.Keybind1}WitchScream{$Items.WitchkingRobe.Keybind2}` → `{$CommonItemTooltip.SetBonus}{$Items.WitchkingRobe.Keybind1}{$Items.WitchkingRobe.Keybind2}`
- added `⏎ {$CommonItemTooltip.AggroDecrease}`

**ru-RU** (current):

```text
{$CommonItemTooltip.PercentIncreasedSummonDamage}
{$CommonItemTooltip.IncreasesMaxMinionsBy@1}
{$CommonItemTooltip.SetBonus}{$Items.WitchkingRobe.Keybind1}WitchScream{$Items.WitchkingRobe.Keybind2}
У поражённых врагов становится меньше урона, они имеют меньшую защиту, скорость, а также в течение короткого времени получают значительный урон
```

**zh-Hans** (current):

```text
{$CommonItemTooltip.PercentIncreasedSummonDamage}
{$CommonItemTooltip.IncreasesMaxMinionsBy@1}
{$CommonItemTooltip.SetBonus}{$Items.WitchkingRobe.Keybind1}WitchScream{$Items.WitchkingRobe.Keybind2}
降低受影响敌人的伤害、防御与速度，并在短时间内对其持续造成显著伤害
```

### `Mods.tsorcRevamp.Items.WitchkingRobe.Keybind1` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-07, `ca156a456` Armor Set actives can now be activated via vanilla armor set bonus key

```diff
-Press 
+{$CommonItemTooltip.SetBonus}
```

Changed:

- `Press` → `{$CommonItemTooltip.SetBonus}`

**ru-RU** (current):

```text
Нажмите 
```

**zh-Hans** (current):

```text
按 
```

### `Mods.tsorcRevamp.Items.WitchkingRobe.Keybind2`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-07, `ca156a456` Armor Set actives can now be activated via vanilla armor set bonus key

```diff
- to let out a terrifying scream that affects enemies in a radius
+ or press 
```

Changed:

- `to let out a terrifying scream that affects enemies in a radius` → `or press`

**ru-RU** (current):

```text
 , чтобы издать ужасающий крик, поражающий врагов в радиусе действия
```

**zh-Hans** (current):

```text
 释放巫师尖啸来削弱附近的敌人
```

### `Mods.tsorcRevamp.Items.BequeathedSoul.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-17, `c528a2301` Finished Covenant of Everlasting Love, couple of fixes

```diff
-Soul of the albino Seath the Scaleless.
+'The essence of the albino Seath the Scaleless'
 A fragment of a Lord Soul discovered at the dawn of the Age of Fire.
 Seath allied with Lord Gwyn and turned upon the dragons, and for this he was
 awarded dukedom, embraced by the royalty, and given a fragment of a great soul.
```

Changed:

- `Soul` → `'The essence`
- `Scaleless.` → `Scaleless'`

**ru-RU** (current):

```text
Душа альбиноса — Нагого Сита.
Фрагмент Души Лорда, обнаруженный на заре Эпохи Огня.
Сит объединился с Лордом Гвином и выступил против драконов, за что был
награждён герцогством, принят королевской властью и получил фрагмент великой души.
```

**zh-Hans** (current):

```text
无鳞冰龙希斯的灵魂
火焰时代黎明时发现的领主灵魂碎片
希斯与葛温结盟对抗龙群，因此他被授予公爵，
被皇室接受，并被赐予一个伟大灵魂的碎片
```

### `Mods.tsorcRevamp.Items.BloodredMossClump.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-16, `0b40fe302` Added Acid Venom Buildup, new Plantera attack, and lots of debuff adjustments

```diff
 [c/ffbf00:{$CommonItemTooltip.RestoresLife}, with only {1} seconds of potion sickness.]
-[c/ffbf00:Removes bleeding and poisoned.]
+[c/ffbf00:Removes bleeding and poisoned, and clears poison buildup.]
 A supply of these may be essential for exploring some areas.
 Can still be used to remove poison and bleeding while under the
 effect of potion sickness or being the [c/6d8827:Bearer of the Curse]. However, it will not heal any HP.
```

Changed:

- `poisoned.]` → `poisoned, and clears poison buildup.]`

**ru-RU** (current):

```text
[c/ffbf00:{$CommonItemTooltip.RestoresLife}, и накладывает всего {1} секунд послезельевой болезни.]
[c/ffbf00:Устраняет кровотечение и отравление.]
Их наличие может оказаться необходимым для исследования некоторых местностей.
Его все еще можно использовать для устранения яда и кровотечения, находясь под действием
послезельевой болезни или являясь [c/6d8827:Носителем проклятия]. Однако оно не восстанавливает здоровье.
```

**zh-Hans** (current):

```text
[c/ffbf00:{$CommonItemTooltip.RestoresLife}, 药水疾病仅 {1} 秒]
[c/ffbf00:去除流血和中毒减益]
在探索某些区域时最好带上它
在药水疾病的影响或在[c/6d8827:被诅咒者模式]下，
它仍然可以用来去除中毒和流血，但此时它不会回复任何生命值
```

### `Mods.tsorcRevamp.Items.BlueTitanite.Tooltip`

Needs update in: **ru-RU** · English last changed: 2025-11-01, `842282575` BIG COMMIT FOR 0.16.4

```diff
 A rare and valuable ore
 Ice cold to the touch, it glows with enchanted power
 More common in the abyss
+[c/00ffd4:To enter the Abyss, equip the Covenant of Artorias.]
```

Changed:

- added `⏎ [c/00ffd4:To enter the Abyss, equip the Covenant of Artorias.]`

**ru-RU** (current):

```text
Редкая и ценная руда
Холодный как лёд на ощупь, он излучает волшебную силу
Распространён в бездне
```

### `Mods.tsorcRevamp.Items.AaronsProtectionStone.Tooltip`

Needs update in: **zh-Hans** · English last changed: 2023-11-06, `b0c99c785` Typhoon, Pinwheel bag, resprites

```diff
-The volcanic stoned etched in Aaron's image
-Said to protect the one who carries it in times of despair
-[c/00ffd4:It's true use may reveal itself in time...or at a particular location...]
+A volcanic stone etched in Aaron's image
+Said to protect the one who carries it in times of despair.
+[c/00ffd4:It's true use may reveal itself in time...or at a particular location deep underground...]
```

Changed:

- `The` → `A`
- `stoned` → `stone`
- `despair` → `despair.`
- `location...]` → `location deep underground...]`

**zh-Hans** (current):

```text
被亚伦铭记在心的岩浆之石
据说能在绝望之时护主
[c/00ffd4:它的真正用途可能会在某个时间或某个特定位置显露出来...]
```

### `Mods.tsorcRevamp.Items.BlightStone.Tooltip`

Needs update in: **ru-RU** · English last changed: 2025-11-30, `e51bd7f27` Reworked Dwarven Contract, Progression on Thorium compatibility, Added Vault Of Endless Greed

```diff
-Summons The Blight, one of six guardians of The Abyss.
+Summons The Blight, one of the seven guardians of The Abyss.
 You must fight this battle on the surface.
 The Blight cannot be fought with the Covenant of Artorias ring equipped.
```

Changed:

- `six` → `the seven`

**ru-RU** (current):

```text
Призывает Мора, одного из шести стражей Бездны.
Эту битву вам предстоит вести на поверхности.
С Мором нельзя сражаться, экипировав кольцо Ковенанта Арториаса.
```

### `Mods.tsorcRevamp.Items.PottedPlanteraBulb.DisplayName`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-16, `64e96582e` Renamed Plantera the Verdurous Monarch (VUHR-juh-ruhs)

```diff
-Potted Plantera Bulb
+Potted Monarch Bulb
```

Changed:

- `Plantera` → `Monarch`

**ru-RU** (current):

```text
Луковица Плантеры в горшке
```

**zh-Hans** (current):

```text
瓶装世纪之花花苞
```

### `Mods.tsorcRevamp.Items.PottedPlanteraBulb.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-16, `64e96582e` Renamed Plantera the Verdurous Monarch (VUHR-juh-ruhs)

```diff
-Summons Plantera
+Summons the Verdurous Monarch
```

Changed:

- `Plantera` → `the Verdurous Monarch`

**ru-RU** (current):

```text
Призывает Плантеру
```

**zh-Hans** (current):

```text
召唤世纪之花（弱小可怜无助
```

### `Mods.tsorcRevamp.Items.BossRematchTome.DisplayName`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-18, `ef2231d5d` Introducing Unkindled Mode (PT1), Enhanced Soapstone System (PT2) + more

```diff
-Boss Rematch Tome
+Boss Hunting Tome
```

Changed:

- `Rematch` → `Hunting`

**ru-RU** (current):

```text
Книга "Матч-реванш с боссом"
```

**zh-Hans** (current):

```text
Boss重召唤书
```

### `Mods.tsorcRevamp.Items.BossRematchTome.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-18, `ef2231d5d` Introducing Unkindled Mode (PT1), Enhanced Soapstone System (PT2) + more

```diff
+Hover over any boss in the menu for a hint on where to find it
 Allows you to re-summon any boss you have defeated
 Left click to show Boss Select Menu, then click on the Boss icon to summon
 Use the arrows on both sides to switch between different periods
 Bosses summoned in this way will not drop dark souls, guardian souls or stamina vessels
 However, they will drop the rest of their normal loot
 [Developer note: Not all bosses can be summoned this way yet, but the rest will be added over time]
```

Changed:

- added `Hover over any boss in the menu for a hint on where to find it ⏎`

**ru-RU** (current):

```text
Позволяет повторно вызвать любого босса, которого вы победили
Нажмите ЛКМ, чтобы отобразить меню выбора босса, затем нажмите на значок босса, чтобы вызвать его
Используйте стрелки с обеих сторон, чтобы переключаться между различными периодами
Из боссов вызванных таким образом не выпадут тёмные души, души хранителя или сосуды выносливости.
Однако, из них выпадет часть их обычной добычи.
[Примечание разработчика: Пока еще не всех боссов можно вызвать таким образом, но остальные будут добавлены со временем]
```

**zh-Hans** (current):

```text
允许你重新召唤已经击败的Boss
手持该物品时左键点击显示Boss选择光圈，再点击光圈上的Boss图标以召唤
通过光圈两侧的箭头切换不同时期的Boss
以这种方式召唤的Boss不会掉落黑暗之魂、守护之魂或耐力容器
然而，他们仍会掉落剩下的正常战利品
[c/ff0000:开发者注：有些Boss还不能通过这种方式被正常召唤]
```

### `Mods.tsorcRevamp.Items.BossRematchTome.DeathCountText`

Needs update in: **zh-Hans** · English last changed: 2024-06-22, `c620a773d` First wave of fixes

```diff
-Death Times: {0}
+Death Count: {0}
```

Changed:

- `Times:` → `Count:`

**zh-Hans** (current):

```text
死亡次数: {0}
```

### `Mods.tsorcRevamp.Items.BossRematchTome.2`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-19, `245987141` Boss Hunting Tome Upgrades / Location Names added to Map!

```diff
-What is under Leonhard, or at end of the Path of Ambition?
+What is under the mountain cave where Leonhard dwells, or at end of the Path of Ambition?
```

Changed:

- `Leonhard,` → `the mountain cave where Leonhard dwells,`

**ru-RU** (current):

```text
Что находится под руководством Леонарда, или в конце пути амбиций?
```

**zh-Hans** (current):

```text
李奥纳德出现之地的下方有什么，野心之路的终点又有什么？
```

### `Mods.tsorcRevamp.Items.BossRematchTome.5`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-19, `245987141` Boss Hunting Tome Upgrades / Location Names added to Map!

English diff for zh-Hans:

```diff
-Deep inside the Corruption Temple, also rumours about crimson on world surface.
+Delve deep inside the Corruption Temple, or investigate rumours about crimson on the surface, to the East.
```

Changed:

- `Deep` → `Delve deep`
- `also` → `or investigate`
- `world surface.` → `the surface, to the East.`

English diff for ru-RU:

```diff
-Delve deep inside the Corruption Temple, or investigate rumours about crimson on the surface.
+Delve deep inside the Corruption Temple, or investigate rumours about crimson on the surface, to the East.
```

Changed:

- `surface.` → `surface, to the East.`

**ru-RU** (current):

```text
Загляните поглубже в Храм Порчи или исследуйте слухи о багровом цвете на поверхности.
```

**zh-Hans** (current):

```text
探索腐化神殿，还有传言说猩红正在地表蔓延...
```

### `Mods.tsorcRevamp.Items.BossRematchTome.6`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-19, `245987141` Boss Hunting Tome Upgrades / Location Names added to Map!

```diff
-Somewhere east of the Arazium Mountain Chasm.
+Seek the Corruption Temple, East of the Arazium Mountain Chasm.
```

Changed:

- `Somewhere east` → `Seek the Corruption Temple, East`

**ru-RU** (current):

```text
Где-то к востоку от пропасти горы Аразиум.
```

**zh-Hans** (current):

```text
阿拉齐姆双峰裂谷的东边...
```

### `Mods.tsorcRevamp.Items.BossRematchTome.8`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-19, `245987141` Boss Hunting Tome Upgrades / Location Names added to Map!

English diff for zh-Hans:

```diff
-Explore Rainforest of the Orid Tribe, east of your burned house.
+Explore the Rainforest of the Orid Tribe, East of your burned house. Gold temple ruins hide a secret.
```

Changed:

- added `the`
- `east` → `East`
- added `Gold temple ruins hide a secret.`

English diff for ru-RU:

```diff
-Explore the Rainforest of the Orid Tribe, east of your burned house.
+Explore the Rainforest of the Orid Tribe, East of your burned house. Gold temple ruins hide a secret.
```

Changed:

- `east` → `East`
- added `Gold temple ruins hide a secret.`

**ru-RU** (current):

```text
Исследуйте тропический лес племени Орид, к востоку от вашего сгоревшего дома.
```

**zh-Hans** (current):

```text
探索被烧毁家园东边的奥里德部落雨林...
```

### `Mods.tsorcRevamp.Items.BossRematchTome.10`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-19, `245987141` Boss Hunting Tome Upgrades / Location Names added to Map!

```diff
-From the Jungle Ruins to Forgotten City, Larvas are everywhere.
+From the Underground Jungle Ruins to the Forgotten City, Larvas are everywhere.
```

Changed:

- added `Underground`
- added `the`

**ru-RU** (current):

```text
Личинки есть повсюду - от руин в джунглях до Забытых городов.
```

**zh-Hans** (current):

```text
自丛林遗迹到失落之城，蜂蛹随处可见...
```

### `Mods.tsorcRevamp.Items.BossRematchTome.11`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-19, `245987141` Boss Hunting Tome Upgrades / Location Names added to Map!

English diff for zh-Hans:

```diff
-Found Lake of Flowered Paths in underground jungle, open the shut gate.
+Found past the Lake of Flowered Paths, deep in the underground jungle. Open the shut gate.
```

Changed:

- added `past the`
- `Paths` → `Paths, deep`
- added `the`
- `jungle, open` → `jungle. Open`

English diff for ru-RU:

```diff
-Found past the Lake of Flowered Paths in underground jungle, open the shut gate.
+Found past the Lake of Flowered Paths, deep in the underground jungle. Open the shut gate.
```

Changed:

- `Paths` → `Paths, deep`
- added `the`
- `jungle, open` → `jungle. Open`

**ru-RU** (current):

```text
Найдя за озером Цветущие тропинки в подземных джунглях, открой закрытые ворота.
```

**zh-Hans** (current):

```text
找到丛林地下的花间湖，打开尘封的大门...
```

### `Mods.tsorcRevamp.Items.BossRematchTome.12`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-19, `245987141` Boss Hunting Tome Upgrades / Location Names added to Map!

English diff for ru-RU:

```diff
-Check the basement of Wizard's house, to an icy land.
+Check the basement of the Wizard's house.
+Remix Map: Seek far to the East, at the entrance to the Frozen Ocean past the Hallow.
```

Changed:

- added `the`
- `house,` → `house. ⏎ Remix Map: Seek far`
- `an icy land.` → `the East, at the entrance to the Frozen Ocean past the Hallow.`

English diff for zh-Hans:

```diff
-Check the basement of Wizard's house, to an icy land.
-Remix Map: Seek far to the east, at the entrance to the Frozen Ocean past the Hallow.
+Check the basement of the Wizard's house.
+Remix Map: Seek far to the East, at the entrance to the Frozen Ocean past the Hallow.
```

Changed:

- added `the`
- `house, to an icy land.` → `house.`
- `east,` → `East,`

**ru-RU** (current):

```text
Проверьте подвал дома Волшебника, в ледяной стране.
```

**zh-Hans** (current):

```text
雪山上巫师住所的地下室，通往一处冰封之地...
重构版地图: 继续向东方前进，穿过神圣前往冻洋的入口...
```

### `Mods.tsorcRevamp.Items.BossRematchTome.15`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-19, `245987141` Boss Hunting Tome Upgrades / Location Names added to Map!

English diff for ru-RU:

```diff
-Pass the four gate west of the Fire Temple, will you find an answer on the altar?
+Past the four gates West of the Fire Temple. Will you find an answer on the altar?
```

Changed:

- `Pass` → `Past`
- `gate west` → `gates West`
- `Temple, will` → `Temple. Will`

English diff for zh-Hans:

```diff
-Pass the four gate west of the Fire Temple, will you find answer on the altar?
+Past the four gates West of the Fire Temple. Will you find an answer on the altar?
```

Changed:

- `Pass` → `Past`
- `gate west` → `gates West`
- `Temple, will` → `Temple. Will`
- added `an`

**ru-RU** (current):

```text
Пройди четверо ворот к западу от Храма Огня, найдёшь ли ты ответ на алтаре?
```

**zh-Hans** (current):

```text
穿过火焰神庙西侧的四道大门，你会在祭坛上找到答案吗？
```

### `Mods.tsorcRevamp.Items.BossRematchTome.16`

Needs update in: **zh-Hans** · English last changed: 2026-05-19, `245987141` Boss Hunting Tome Upgrades / Location Names added to Map!

```diff
-Search the Hallowed Caverns which been sealed by Cobalt.
+Search the Hallowed Caverns, which have been sealed by Cobalt.
```

Changed:

- `Caverns` → `Caverns,`
- added `have`

**zh-Hans** (current):

```text
探索东边被钴大门封锁的神圣洞穴...
```

### `Mods.tsorcRevamp.Items.BossRematchTome.23`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-19, `245987141` Boss Hunting Tome Upgrades / Location Names added to Map!

```diff
-It awaits at the end of Shadow Temple.
+It awaits at the end of the Shadow Temple, buried under the Western desert.
```

Changed:

- added `the`
- `Temple.` → `Temple, buried under the Western desert.`

**ru-RU** (current):

```text
Он ждет вас в конце Храма Теней.
```

**zh-Hans** (current):

```text
它在暗影神庙的尽头等待着你...
```

### `Mods.tsorcRevamp.Items.BossRematchTome.24`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-19, `245987141` Boss Hunting Tome Upgrades / Location Names added to Map!

```diff
-Below a Broken, familiar Bridge, you will find the Flooded Machine Temple.
+Below a broken, familiar bridge, you will find a temple flooded to protect villagers from what was sealed inside.
```

Changed:

- `Broken,` → `broken,`
- `Bridge,` → `bridge,`
- `the Flooded Machine Temple.` → `a temple flooded to protect villagers from what was sealed inside.`

**ru-RU** (current):

```text
Под сломанным, знакомым мостом вы найдете Затопленный Храм Механизмов.
```

**zh-Hans** (current):

```text
在一座断裂的、熟悉的桥下，你会找到被淹没的机器神殿，那里有什么？
```

### `Mods.tsorcRevamp.Items.BossRematchTome.25`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-19, `245987141` Boss Hunting Tome Upgrades / Location Names added to Map!

English diff for zh-Hans:

```diff
 Why do the Great Pyramid and Hunter's arena have the same pink flowers?
-Remix Map: Search in the Jungle Ruins the entrance to its lush and forgotten depths.
+Remix Map: Search in the Jungle Ruins for the entrance to its lush and forgotten depths.
```

Changed:

- added `for`

English diff for ru-RU:

```diff
 Why do the Great Pyramid and Hunter's arena have the same pink flowers?
+Remix Map: Search in the Jungle Ruins for the entrance to its lush and forgotten depths.
```

Changed:

- added `⏎ Remix Map: Search in the Jungle Ruins for the entrance to its lush and forgotten depths.`

**ru-RU** (current):

```text
Почему у Великой пирамиды и Охотничьей арены одинаковые розовые цветы?
```

**zh-Hans** (current):

```text
大金字塔里为什么会有和猎手场地一样的粉色花苞？
重构版地图: 在丛林废墟中寻找其郁郁葱葱、被遗忘的深处的入口...
```

### `Mods.tsorcRevamp.Items.BossRematchTome.27`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-19, `245987141` Boss Hunting Tome Upgrades / Location Names added to Map!

```diff
-Golem controls the forcefield of the floating island above the village.
+Golem controls the forcefield of the floating island high above Ashen Peak village.
```

Changed:

- added `high`
- `the` → `Ashen Peak`

**ru-RU** (current):

```text
Голем контролирует силовое поле парящего острова над деревней.
```

**zh-Hans** (current):

```text
石巨人控制着村庄上方空岛的力场...
```

### `Mods.tsorcRevamp.Items.BossRematchTome.28`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-19, `245987141` Boss Hunting Tome Upgrades / Location Names added to Map!

```diff
-Atop the Twin Peaks of Arazium, they gaze at Sky Above and the Great Chasm Below.
+Atop the Twin Peaks of Arazium, find the temple mouth that gazes at the sky above and the Great Chasm Below.
```

Changed:

- `they gaze` → `find the temple mouth that gazes`
- `Sky Above` → `the sky above`

**ru-RU** (current):

```text
На вершинах двух пиков Аразиума они смотрят на небо над головой и Великую пропасть внизу.
```

**zh-Hans** (current):

```text
在阿拉齐姆双峰之上，他们凝视上天，下观沟壑。
```

### `Mods.tsorcRevamp.Items.BossRematchTome.29`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-19, `245987141` Boss Hunting Tome Upgrades / Location Names added to Map!

English diff for ru-RU:

```diff
-Craft the 8 Crests with shards found at the top of Attraidies' Tower.
+Craft the 8 Crests with shards found at the top of Attraidies' Dark Tower.
```

Changed:

- added `Dark`

English diff for zh-Hans:

```diff
-Craft the 8 Crests with shards found at the top of Attraidies Tower.
+Craft the 8 Crests with shards found at the top of Attraidies' Dark Tower.
```

Changed:

- `Attraidies` → `Attraidies' Dark`

**ru-RU** (current):

```text
Создайте 8 гребней из осколков, найденных на вершине башни Аттраидис.
```

**zh-Hans** (current):

```text
阿卓迪斯之塔顶端的光明碎块和暗黑碎块补全了合成所需的最后之物...
```

### `Mods.tsorcRevamp.Items.BossRematchTome.33`

Needs update in: **ru-RU** · English last changed: 2026-05-19, `245987141` Boss Hunting Tome Upgrades / Location Names added to Map!

```diff
-Solve the puzzle of Three stone pillars on the west Side of the Ashen Cave.
+Solve the puzzle of Three stone pillars on the West side of the Ashen Cave.
+Remix Map: Confront the four celestial pillars and search for an ancient alien structure in the sky.
```

Changed:

- `west Side` → `West side`
- added `⏎ Remix Map: Confront the four celestial pillars and search for an ancient alien structure in the sky.`

**ru-RU** (current):

```text
Разгадайте загадку Трех каменных столбов на западной стороне Пепельной пещеры.
```

### `Mods.tsorcRevamp.Items.CrestOfEarth.Tooltip`

Needs update in: **ru-RU** · English last changed: 2025-10-12, `d0270a130` Ready for 0.16.2

```diff
-A shard of pure minerals
-One of eight. [c/00ffd4:On its side there's a clue:]
-"[c/ffbf00:Two paths lead to my entrance. Below the Oasis in the Desert, Serris unlocks the Way.]
-"[c/ffbf00:In a small sandy Cave atop a hill, a Chlorophyte Gate guards the other.]"
+
```

Changed:

- removed `A shard of pure minerals ⏎ One of eight. [c/00ffd4:On its side there's a clue:] ⏎ "[c/ffbf00:Two paths lead to my entrance. Below the Oasis in the Desert, Serris unlocks the Way.] ⏎ "[c/ffbf00:In a small sandy Cave atop a hill, a Chlorophyte Gate guards the other.]"`

**ru-RU** (current):

```text
Осколок чистого минерала
Один из восьми. [c/00ffd4:На его стороне есть подсказка:]
"[c/ffbf00:К моему входу ведут две тропинки. Под оазисом в пустыне Серрис открывает Путь.]
"[c/ffbf00:В маленькой песчаной пещере на вершине холма одни ворота из хлорофита охраняют другие.]"
```

### `Mods.tsorcRevamp.Items.CrestOfFire.Tooltip`

Needs update in: **zh-Hans** · English last changed: 2025-05-20, `ad0563cbf` Improved Miakoda Logic + additional dialogue

```diff
 A shard of blazing fury
 You don't know what it's for, but you're sure it's important.
 [c/00ffd4:After defeating The Rage, Miakoda tells you about the Wyvern Mage that lives high atop the mountain above these caverns] -
-[c/00ffd4:an ally of Attraidies.] [c/ffbf00:"If he is helping the Mindflayer King, he could know where Asha is."]
+[c/00ffd4:an ally of Attraidies.] [c/ffbf00:"If he is helping the Mindflayer King, he could know where your wife Asha is."]
```

Changed:

- added `your wife`

**zh-Hans** (current):

```text
炽热的怒火碎片
你不知道它的用途，但你确信它很重要
[c/00ffd4:在击败愤怒之后，Miakoda告诉你关于住在神圣洞穴上方山顶高处的飞龙法师的一些事]
[c/ffbf00:"他是阿卓迪斯的盟友，如果他在帮助夺心魔王，他应该知道Asha在哪里。"]
```

### `Mods.tsorcRevamp.Items.CrestOfSteel.Tooltip`

Needs update in: **ru-RU** · English last changed: 2025-10-12, `d0270a130` Ready for 0.16.2

```diff
-A shard of burning hot steel
-One of eight.
-[c/00ffd4:A riddle is etched into the metal:]
-"[c/ffbf00:Above the Machine's Throne, Below the Dryad's Cove: A Locked Door guards an Ancient Pyramid, long ago forgotten.]"
+
```

Changed:

- removed `A shard of burning hot steel ⏎ One of eight. ⏎ [c/00ffd4:A riddle is etched into the metal:] ⏎ "[c/ffbf00:Above the Machine's Throne, Below the Dryad's Cove: A Locked Door guards an Ancient Pyramid, long ago forgotten.]"`

**ru-RU** (current):

```text
Осколок раскалённой стали
Один из восьми.
[c/00ffd4:На металле выгравирована загадка:]
"[c/ffbf00:Над Троном Механизма, под Пещерой Дриад: Запертая дверь охраняет Древнюю пирамиду, давно забытую.]"
```

### `Mods.tsorcRevamp.Items.CursedSoul.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-17, `c528a2301` Finished Covenant of Everlasting Love, couple of fixes

```diff
-Soul of a cursed being.
+'The essence of cursed beings from the abyss'
```

Changed:

- `Soul` → `'The essence`
- removed `a`
- `being.` → `beings from the abyss'`

**ru-RU** (current):

```text
Душа проклятого существа.
```

**zh-Hans** (current):

```text
被诅咒生物的灵魂
```

### `Mods.tsorcRevamp.Items.DodgerollMemo.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-07-27, `7ff66746f` Stamina & Shield Balancing + Weapon Logs + BotC Magic Mechanic + More

```diff
 It looks like a diary entry. It reads as follows:
 "There have been rumours of bandits in the forest lately,
 so father has been apprehensive about letting me go into the
 forest alone to pick mushrooms... Pfft, what does he know?
 I may be small, but I'm nimble and have mastered [c/00ffd4:the technique]
 [c/00ffd4:of the dodgeroll!] I'll be fine as long as I don't run out of stamina..."
 Judging by how bandits were in possession of this note, I guess it
 didn't go too well for them. Perhaps I can make good use of this technique?
-[c/ffbf00:Press Left Alt to roll. Try rebinding it to Left Shift, R, or something new.]
```

Changed:

- removed `⏎ [c/ffbf00:Press Left Alt to roll. Try rebinding it to Left Shift, R, or something new.]`

**ru-RU** (current):

```text
Выглядит как запись в дневнике. Она гласит следующее:
"В последнее время ходили слухи о бандитах в лесу
поэтому отец опасался отпускать меня одного
в лес за грибами... Пфф, что он знает?
Может, я и маленький, но я ловкий и в совершенстве овладел [c/00ffd4:техникой]
[c/00ffd4:переката!] Со мной всё будет в порядке, пока у меня не закончится выносливость..."
Судя по тому, как бандиты завладели этой запиской, думаю,
у них все прошло не слишком хорошо. Возможно, я смогу с пользой использовать эту технику?
[c/ffbf00:Нажмите левый Alt для переката. Попробуйте переключить ее на левый Shift, R или что-нибудь другое.]
```

**zh-Hans** (current):

```text
这看起来像一个日记片段：
"最近流传着森林里有土匪的说法，
所以父亲总是担心让我一个人去森林里采蘑菇......
笑死，他知道什么？
我可能个头小，但我身手敏捷，掌握了[c/00ffd4:躲闪术]！
只要不耗尽体力就可以了......"
从这些土匪的表现来看，我想他们掌握的还不够熟练
或许我应该在战斗中多加练习，避免重蹈覆辙？
[c/ffbf00:按 左Alt 翻滚，或在控制中将其绑定到你用着顺手的键位]
```

### `Mods.tsorcRevamp.Items.DwarvenContract.Tooltip`

Needs update in: **ru-RU** · English last changed: 2025-11-30, `e51bd7f27` Reworked Dwarven Contract, Progression on Thorium compatibility, Added Vault Of Endless Greed

```diff
 A contract for a dwarf guard.
-Will summon a dwarf to guard a piece of property.
+[c/00ffd4:The dwarf is very interested in this, and may give you a gift in return.]
```

Changed:

- `Will summon` → `[c/00ffd4:The dwarf is very interested in this, and may give you`
- `dwarf to guard a piece of property.` → `gift in return.]`

**ru-RU** (current):

```text
Контракт для дворфа-охранника
Призовет дворфа для охраны собственности.
```

### `Mods.tsorcRevamp.Items.EphemeralDust.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-04-14, `bd5f2da23` A lot of changes and content!!

```diff
-
+'As tangible as ghostly'
```

Changed:

- added `'As tangible as ghostly'`

**ru-RU** (current):

```text

```

**zh-Hans** (current):

```text

```

### `Mods.tsorcRevamp.Items.EstusFlaskShard.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2024-07-21, `a31a4f802` 0.15.17 hotfix and some other changes

```diff
-Restoration Flask Shard
+Estus Flask Shard
```

Changed:

- `Restoration` → `Estus`

**zh-Hans** (current):

```text
元素瓶碎片
```

### `Mods.tsorcRevamp.Items.FlameOfTheAbyss.Tooltip`

Needs update in: **ru-RU** · English last changed: 2025-11-01, `842282575` BIG COMMIT FOR 0.16.4

```diff
 A persistent flame that can cause incineration from the abyss
 [c/ffbf00:Only dropped by fallen souls in the abyss]
+[c/00ffd4:To enter the Abyss, equip the Covenant of Artorias.]
```

Changed:

- added `⏎ [c/00ffd4:To enter the Abyss, equip the Covenant of Artorias.]`

**ru-RU** (current):

```text
Стойкое пламя, которое может испепелить бездну
[c/ffbf00:Выпадает только из падших душ в бездне]
```

### `Mods.tsorcRevamp.Items.ForgottenIceBowScroll.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-14, `d6c5394a5` Last commit for 1.4.4, a lot of progress but a lot of wip stuffs too!

```diff
-Used to make a great magic weapon
+Used to make a great magic bow
 Sold by a powerful sorcerer
```

Changed:

- `weapon` → `bow`

**ru-RU** (current):

```text
Используется для изготовления великого магического оружия
Продан могущественным колдуном
```

**zh-Hans** (current):

```text
用来制作一把强大的魔法武器
由提比亚法师出售
```

### `Mods.tsorcRevamp.Items.ForgottenThunderBowScroll.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-14, `d6c5394a5` Last commit for 1.4.4, a lot of progress but a lot of wip stuffs too!

```diff
-Used to make a great magic weapon
+Used to make a great magic bow
 Sold by a powerful sorcerer
```

Changed:

- `weapon` → `bow`

**ru-RU** (current):

```text
Использовался для изготовления великого магического оружия
Продан могущественным колдуном
```

**zh-Hans** (current):

```text
用来制作一把强大的魔法武器
由提比亚法师出售
```

### `Mods.tsorcRevamp.Items.GhostWyvernSoul.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-17, `c528a2301` Finished Covenant of Everlasting Love, couple of fixes

```diff
-The essence of the Ghost Wyvern.
+'The essence of the Wyvern Mage Shadow and his disciple'
```

Changed:

- `The` → `'The`
- `Ghost Wyvern.` → `Wyvern Mage Shadow and his disciple'`

**ru-RU** (current):

```text
Сущность Призрачной виверны.
```

**zh-Hans** (current):

```text
灵鬼虬龙的精华
```

### `Mods.tsorcRevamp.Items.GreatMagicMirror.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-30, `783888c02` SoulsMode Mobility Limit, Abyss VFX revamp, Great Magic Mirror revamp + more

```diff
 Equip this in an accessory slot anywhere to create a new warp point.
-Use the mirror to return to that warp point.
+Use the mirror within {1} tiles of that point to return to it.
 Reduces defense to 0 and slows movement while equipped and setting your warp point.
 Teleport takes {0} seconds and you cannot move during the channel.
```

Changed:

- added `within {1} tiles of that point`
- `that warp point.` → `it.`

**ru-RU** (current):

```text
Наденьте его в любой слот для акссесуаров, чтобы создать новую точку телепортации
Используйте зеркало чтобы вернуть в точку телепортации.
во время установки точки телепортации защита снижается до 0 а передвижение замедляется.
Телепортация занимает {0} секунд, и вы не можете двигаться во время прохождения канала.
```

**zh-Hans** (current):

```text
便携的全图传送器—制作组の仁慈，把它放在饰品栏会记录你所在位置并创建传送点
再次使用就会传送到该点，传送需要 {0} 秒时间，期间不能移动
记录传送点时防御降低到 0 并减慢移动速度
你只需要把它在饰品栏里放一下就好了，不要一直放着
```

### `Mods.tsorcRevamp.Items.GuardianSoul.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-17, `c528a2301` Finished Covenant of Everlasting Love, couple of fixes

```diff
-Soul of an ancient guardian.
+'The essence of an ancient guardian'
 Can be used at Demon Altars to forge powerful weapons and items.
```

Changed:

- `Soul` → `'The essence`
- `guardian.` → `guardian'`

**ru-RU** (current):

```text
Душа древнего хранителя.
Можно использовать на Алтарях Демонов для создания мощного оружия и предметов.
```

**zh-Hans** (current):

```text
远古守护者的灵魂
可以在恶魔祭坛上用来锻造强大的武器和物品
```

### `Mods.tsorcRevamp.Items.MastersScroll.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-09-19, `28ab6f457` Reorganized some code related to Dark Souls and some accessories

```diff
 Turns on Master Mode or turns it off
-Receive 20% more souls in master mode
+Receive {0}% more souls in master mode
```

Changed:

- `20%` → `{0}%`

**ru-RU** (current):

```text
Включает или выключает режим мастера
В режиме мастера вы получаете на 20% больше душ
```

**zh-Hans** (current):

```text
用于开关大师模式
大师模式下敌怪大幅加强，玩家获得额外20%黑暗之魂
[c/ca1e00:第一次游玩时 【不】 推荐开启]
```

### `Mods.tsorcRevamp.Items.MiakodaNew.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-06-27, `326312221` Added Cursed Dragon transformation accessory + 3 invaders + more

```diff
 Miakoda - an ancient being of light over 100 years old,
 who has vowed to help you find your wife and defeat Attraidies.
 Miakoda is an indigenous name that means "Power of the Moon".
 New Moon Form - Imbues weapons with Midas when you hit a crit,
 boosting enemy coin drops ({$CommonItemTooltip.SecondDuration@3}, {$CommonItemTooltip.Cooldown@4})
-{$CommonItemTooltip.PercentIncreasedMovementSpeed}, stacking with the boost
+{$CommonItemTooltip.PercentIncreasedMovementSpeed}
 While boost is active, gain the following bonuses:
 {$CommonItemTooltip.PercentIncreasedMovementSpeed@1}
 {$CommonItemTooltip.NoKnockback}
 {$CommonItemTooltip.DRStat@2}
 Can switch between forms at an altar
```

Changed:

- `{$CommonItemTooltip.PercentIncreasedMovementSpeed}, stacking with the boost` → `{$CommonItemTooltip.PercentIncreasedMovementSpeed}`

**ru-RU** (current):

```text
Миакода - древнее существо из света, которому более 100 лет,
оно поклялось помочь вам найти вашу жену и победить Аттрайдиса.
Миакода - это местное имя, которое означает "Сила Луны".
Форма Новолуния - придает оружию силу Мидаса при крит. ударе,
увеличивая количество выпадающих из врагов монет ({$CommonItemTooltip.SecondDuration@3}, {$CommonItemTooltip.Cooldown@4})
{$CommonItemTooltip.PercentIncreasedMovementSpeed}, накапливается с усилением
Пока активно усиление, вы можете получить следующие бонусы:
{$CommonItemTooltip.PercentIncreasedMovementSpeed@1}
{$CommonItemTooltip.NoKnockback}
{$CommonItemTooltip.DRStat@2}
Можно переключаться между формами на алтаре.
```

**zh-Hans** (current):

```text
Miakoda，一个存世百年之久的光之造物
它发誓要帮助你找到你的妻子并击败阿卓迪斯
Miakoda 是一个当地方言的名字，意思是“月亮的力量”
【新月形态】，{$CommonItemTooltip.PercentIncreasedMovementSpeed}
当你暴击时用迈达斯灌注武器使敌人掉落更多钱币
{$CommonItemTooltip.PercentIncreasedMovementSpeed@1}，{$CommonItemTooltip.DRStat@2}，{$CommonItemTooltip.NoKnockback}
{$CommonItemTooltip.SecondDuration@3}, {$CommonItemTooltip.Cooldown@4}
可以在恶魔祭坛上切换形态
```

### `Mods.tsorcRevamp.Items.DemonDrugPotion.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-26, `363240256` Nerfing/Fixing some of the 4 "banger" potions that we add being way too op and reducing free defense handouts

```diff
-{$CommonItemTooltip.PercentIncreasedDamage}
+{$CommonItemTooltip.DmgMult}
 {$CommonItemTooltip.BadDefense@1}
 Does not stack with Armor Drug, Strength, or Battlefront Potions.
```

Changed:

- `{$CommonItemTooltip.PercentIncreasedDamage}` → `{$CommonItemTooltip.DmgMult}`

**ru-RU** (current):

```text
{$CommonItemTooltip.PercentIncreasedDamage}
{$CommonItemTooltip.BadDefense@1}
Не сочетается с препаратами брони, а также с зельями силы и боевого фронта.
```

**zh-Hans** (current):

```text
{$CommonItemTooltip.PercentIncreasedDamage}
{$CommonItemTooltip.BadDefense@1}
不与护甲药水、力量药水或前线药水叠加
```

### `Mods.tsorcRevamp.Items.PermanentPlentySatisfied.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-09-29, `27522d445` 0.18.16

```diff
-
+There's tea in that soup!
```

Changed:

- added `There's tea in that soup!`

**ru-RU** (current):

```text

```

**zh-Hans** (current):

```text

```

### `Mods.tsorcRevamp.Items.PermanentExquisitelyStuffed.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-09-29, `27522d445` 0.18.16

```diff
-
+The soup has been injected into it as a sort of juice...
```

Changed:

- added `The soup has been injected into it as a sort of juice...`

**ru-RU** (current):

```text

```

**zh-Hans** (current):

```text

```

### `Mods.tsorcRevamp.Items.PowerWithin.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-07-27, `7ff66746f` Stamina & Shield Balancing + Weapon Logs + BotC Magic Mechanic + More

```diff
 Harness the power of flame to actualize the inner-self
 {$CommonItemTooltip.DmgMult}
 {$CommonItemTooltip.StaminaRegen@1}
 Excessive power eats away the life-force of its caster
 Lasts {2} seconds, can't be cancelled
+Uses 20 mana
 Effect potency doubled{$CommonItemTooltip.ForBotCOnly}
```

Changed:

- added `Uses 20 mana ⏎`

**ru-RU** (current):

```text
Используй силу пламени для раскрытия своего внутреннего "я"
{$CommonItemTooltip.DmgMult}
{$CommonItemTooltip.StaminaRegen@1}
Чрезмерная сила отнимает жизненные силы у того, кто ее применяет
Длится {2} секунд, не может быть отменено
Эффективность эффекта удваивается{$CommonItemTooltip.ForBotCOnly}
```

**zh-Hans** (current):

```text
运用火焰之力探索自我的真谛
{$CommonItemTooltip.DmgMult}
{$CommonItemTooltip.StaminaRegen@1}
过度的力量吞噬施法者的生命力
持续 {2} 秒且无法取消
{$CommonItemTooltip.ForBotCOnly}此物品的效果加倍
```

### `Mods.tsorcRevamp.Items.RedTitanite.Tooltip`

Needs update in: **ru-RU** · English last changed: 2025-11-01, `842282575` BIG COMMIT FOR 0.16.4

```diff
 A rare and valuable ore
 Radiating heat, it shimmers with forbidden magic
 More common in the abyss
+[c/00ffd4:To enter the Abyss, equip the Covenant of Artorias.]
```

Changed:

- added `⏎ [c/00ffd4:To enter the Abyss, equip the Covenant of Artorias.]`

**ru-RU** (current):

```text
Редкая и ценная руда
Излучая тепло, она излучает запретную магию
Распространён в бездне
```

### `Mods.tsorcRevamp.Items.SoulOfChaos.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-17, `c528a2301` Finished Covenant of Everlasting Love, couple of fixes

```diff
-The essence of chaos.
+'The essence of pure discord'
```

Changed:

- `The` → `'The`
- `chaos.` → `pure discord'`

**ru-RU** (current):

```text
Эссенция Хаоса.
```

**zh-Hans** (current):

```text
混沌的本质
```

### `Mods.tsorcRevamp.Items.DivineBoomCannon.DisplayName`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-06-14, `c8cac6bb1` Abyssal Inferno expansion, debuffs stuffs, other stuffs

```diff
-Divine Boom Cannon
+Ultimate Boom Cannon
```

Changed:

- `Divine` → `Ultimate`

**ru-RU** (current):

```text
Божественная взрывная пушка
```

**zh-Hans** (current):

```text
神爆炮
```

### `Mods.tsorcRevamp.Items.ExplosionRune.Tooltip`

Needs update in: **zh-Hans** · English last changed: 2024-06-22, `c620a773d` First wave of fixes

```diff
-Projectile that explodes with 5 small fireballs at the point of impact.
+Homing projectile that explodes with 5 fireballs at the point of impact.
```

Changed:

- `Projectile` → `Homing projectile`
- removed `small`

**zh-Hans** (current):

```text
在碰撞时射出 5 个能爆炸的小火球
```

### `Mods.tsorcRevamp.Items.FlamesOfTheNight.Tooltip`

Needs update in: **zh-Hans** · English last changed: 2024-06-26, `25d12bd14`  Update on dust from old projectiles

```diff
-A full sprectral array of green flame will light up the skies
+A full sprectral array of purple flame will light up the skies
```

Changed:

- `green` → `purple`

**zh-Hans** (current):

```text
一排绿色的火焰光谱阵列将照亮天空
```

### `Mods.tsorcRevamp.Items.ForgottenIceBow.Tooltip`

Needs update in: **ru-RU** · English last changed: 2025-11-01, `842282575` BIG COMMIT FOR 0.16.4

```diff
-Casts magic shards of ice from your bow.
-Attuned with the greatest powers when wielded by mages.
-Each shot can be channeled with the powers of your mind once in the air.
-Channeling is useful for directing the shot directly above your enemies for maximum damage
+Shoots 2 arrows and a strong bolt of ice at a time
```

Changed:

- `Casts magic shards` → `Shoots 2 arrows and a strong bolt`
- `from your bow. ⏎ Attuned with the greatest powers when wielded by mages. ⏎ Each shot can be channeled with the powers of your mind once in the air. ⏎ Channeling is useful for directing the shot directly above your enemies for maximum damage` → `at a time`

**ru-RU** (current):

```text
Стреляет магическими осколками льда из вашего лука.
При использовании магами он обладает наибольшей силой.
Каждый выстрел можно направить силой своего разума, когда он находится в воздухе.
Концентрация полезна для направления выстрела прямо над врагами для максимального урона.
```

### `Mods.tsorcRevamp.Items.ForgottenThunderBow.Tooltip`

Needs update in: **ru-RU** · English last changed: 2025-11-01, `842282575` BIG COMMIT FOR 0.16.4

```diff
-Casts a bolt of lightning from your bow, doing massive damage over time. 
+Shoots 2 arrows and a powerful lightning bolt at a time
```

Changed:

- `Casts` → `Shoots 2 arrows and`
- added `powerful lightning`
- `of lightning from your bow, doing massive damage over time.` → `at a time`

**ru-RU** (current):

```text
Выпускает молнию из вашего лука, нанося огромный урон с течением времени. 
```

### `Mods.tsorcRevamp.Items.FreezeBolt.DisplayName`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-02-28, `6e3324379` Big patch, Reworked Chaos for real this time (thanks to Boatsoon, still wip though)

```diff
-Freeze Bolt
+Frozen Tear
```

Changed:

- `Freeze Bolt` → `Frozen Tear`

**ru-RU** (current):

```text
Замораживающий снаряд
```

**zh-Hans** (current):

```text
霜与电 低阶
```

### `Mods.tsorcRevamp.Items.FreezeBolt2.DisplayName`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-02-28, `6e3324379` Big patch, Reworked Chaos for real this time (thanks to Boatsoon, still wip though)

```diff
-Freeze Bolt II
+Frozen Tear II
```

Changed:

- `Freeze Bolt` → `Frozen Tear`

**ru-RU** (current):

```text
Замораживающий снаряд II
```

**zh-Hans** (current):

```text
霜与电 中阶
```

### `Mods.tsorcRevamp.Items.FreezeBolt2.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-02-28, `6e3324379` Big patch, Reworked Chaos for real this time (thanks to Boatsoon, still wip though)

```diff
-Casts a fast-moving bolt of ice
+Casts a super fast-moving bolt of ice
```

Changed:

- added `super`

**ru-RU** (current):

```text
Создаёт быстро движущийся ледяной снаряд
```

**zh-Hans** (current):

```text
抛出快速移动的冰锥
```

### `Mods.tsorcRevamp.Items.FreezeBolt3.DisplayName`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-02-28, `6e3324379` Big patch, Reworked Chaos for real this time (thanks to Boatsoon, still wip though)

```diff
-Freeze Bolt III
+Frozen Tear III
```

Changed:

- `Freeze Bolt` → `Frozen Tear`

**ru-RU** (current):

```text
Замораживающий снаряд III
```

**zh-Hans** (current):

```text
霜与电 高阶
```

### `Mods.tsorcRevamp.Items.FreezeBolt3.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-02-28, `6e3324379` Big patch, Reworked Chaos for real this time (thanks to Boatsoon, still wip though)

```diff
-Casts a super fast-moving bolt of ice
+Casts a strong, super fast-moving bolt of ice
```

Changed:

- added `strong,`

**ru-RU** (current):

```text
Создаёт сверхбыстрый ледяной разряд
```

**zh-Hans** (current):

```text
抛出超级快速的冰锥
```

### `Mods.tsorcRevamp.Items.Muramassa.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-13, `eaf2f12ac` BotC mage is now an arcane sorcerer I guess

```diff
 A sword crafted for magic users
-Deals +1 damage for every {1} max mana the user has over {0}
```

Changed:

- removed `⏎ Deals +1 damage for every {1} max mana the user has over {0}`

**ru-RU** (current):

```text
Меч, созданный для пользователей магии
Наносит +1 урон за каждые {1} максимальной маны, вплоть до {0} максимальной маны
```

**zh-Hans** (current):

```text
一把为魔法使用者制作的剑
使用者的魔力值超过 {0} 后，每 {1} 点魔力增加 1 点伤害
```

### `Mods.tsorcRevamp.Items.Murassame.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-13, `eaf2f12ac` BotC mage is now an arcane sorcerer I guess

```diff
 A sword crafted for magic users
-Deals +1 damage for every {1} max mana the user has over {0}
```

Changed:

- removed `⏎ Deals +1 damage for every {1} max mana the user has over {0}`

**ru-RU** (current):

```text
Меч, созданный для пользователей магии
Наносит +1 урон за каждые {1} максимальной маны, вплоть до {0} максимальной маны
```

**zh-Hans** (current):

```text
一把为魔法使用者制作的剑
使用者的魔力值超过 {0} 后，每 {1} 点魔力增加 1 点伤害
```

### `Mods.tsorcRevamp.Items.Masamune.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-13, `6834109ff` Disabled Accuracy, Attunement, reorganized some files

```diff
 The blade almost cannot be seen it cuts so fast, ripping enemies to shreds in seconds
 Has the power to shoot a magical water flame from its blade
-Deals +1 damage for every {1} max mana the user has over {0}
```

Changed:

- removed `⏎ Deals +1 damage for every {1} max mana the user has over {0}`

**ru-RU** (current):

```text
Лезвие почти не видно, оно режет очень быстро, разрывая врагов в клочья за считанные секунды
Обладает способностью выбрасывать из своего лезвия магическое водяное пламя
Наносит +1 урон за каждые {1} максимальной маны, вплоть до {0} максимальной маны
```

**zh-Hans** (current):

```text
它切割得如此之快，刀刃几乎看不见，在几秒钟内将敌人撕成碎片
拥有从刀刃上射出魔法水焰的力量
使用者的魔力值超过 {0} 后，每 {1} 点魔力增加 1 点伤害
```

### `Mods.tsorcRevamp.Items.AncientFireAxe.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-09-25, `b7452cadd` Owl Father has joined the hunt...

English diff for ru-RU:

```diff
 The blade hits with a powerful magic flame.
-Spawns a bouncy fireball on-hit, which explodes for each enemy hit and when bouncing.
 Knocks back foes with a force that also sets them ablaze, doing damage over time.
+Hold attack on the ground for {0} second(s), then release for a heavy swing that sends a fire wave {4} tiles along the ground
+Underhand held swings also lift you {6} tiles into the air
+Hold attack in the air: an overhand swing plunges into a slam that erupts the fire wave on landing, an underhand swing carries you upward
+Held attacks cost {1} mana and deal {2}% more damage
+Without enough mana they still deal {3}% more damage, but release no fire wave
+The fire wave sets foes ablaze for {5} seconds
+Right click to switch to the short axe
```

Changed:

- removed `⏎ Spawns a bouncy fireball on-hit, which explodes for each enemy hit and when bouncing.`
- added `⏎ Hold attack on the ground for {0} second(s), then release for a heavy swing that sends a fire wave {4} tiles along the ground ⏎ Underhand held swings also lift you {6} tiles into the air ⏎ Hold attack in the air: an overhand swing plunges into a slam that erupts the fire wave on landing, an underhand swing carries you upward ⏎ Held attacks cost {1} mana and deal {2}% more damage ⏎ Without enough mana they still deal {3}% more damage, but release no fire wave ⏎ The fire wave sets foes ablaze for {5} seconds ⏎ Right click to switch to the short axe`

English diff for zh-Hans:

```diff
 The blade hits with a powerful magic flame.
-Spawns a fireball on-hit.
 Knocks back foes with a force that also sets them ablaze, doing damage over time.
+Hold attack on the ground for {0} second(s), then release for a heavy swing that sends a fire wave {4} tiles along the ground
+Underhand held swings also lift you {6} tiles into the air
+Hold attack in the air: an overhand swing plunges into a slam that erupts the fire wave on landing, an underhand swing carries you upward
+Held attacks cost {1} mana and deal {2}% more damage
+Without enough mana they still deal {3}% more damage, but release no fire wave
+The fire wave sets foes ablaze for {5} seconds
+Right click to switch to the short axe
```

Changed:

- removed `⏎ Spawns a fireball on-hit.`
- added `⏎ Hold attack on the ground for {0} second(s), then release for a heavy swing that sends a fire wave {4} tiles along the ground ⏎ Underhand held swings also lift you {6} tiles into the air ⏎ Hold attack in the air: an overhand swing plunges into a slam that erupts the fire wave on landing, an underhand swing carries you upward ⏎ Held attacks cost {1} mana and deal {2}% more damage ⏎ Without enough mana they still deal {3}% more damage, but release no fire wave ⏎ The fire wave sets foes ablaze for {5} seconds ⏎ Right click to switch to the short axe`

**ru-RU** (current):

```text
Лезвие ударяет мощным магическим пламенем.
При попадании в цель образуется упругий огненный шар, который взрывается при каждом попадании по врагу и при отскоке.
Отбрасывает врагов с силой, которая также поджигает их, нанося урон с течением времени.
```

**zh-Hans** (current):

```text
斧刃散发出强大的魔法火焰
击退并点燃敌人，随着时间的推移造成伤害
命中时释放火球
```

### `Mods.tsorcRevamp.Items.AncientFireSword.Tooltip`

Needs update in: **zh-Hans** · English last changed: 2024-03-20, `b2d355acd` Ancient Fire Sword and Axe reworks and summoner nerfs/compensations

```diff
 The blade is a magic flame, slicing quickly.
-Spawns a gust of fire on-hit.
+Casts a small fiery slash on each swing.
 Will set enemies ablaze and do damage over time.
```

Changed:

- `Spawns` → `Casts`
- `gust of fire on-hit.` → `small fiery slash on each swing.`

**zh-Hans** (current):

```text
刀萦魔焰，快速斩切
点燃敌人，越烤越脆
命中时喷发火焰
```

### `Mods.tsorcRevamp.Items.CoralSword.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-17, `c528a2301` Finished Covenant of Everlasting Love, couple of fixes

```diff
-Edged to slay those of the sea. Deals 4x damage to water enemies.
+Edged to slay those of the sea. Deals 3x damage to water enemies.
```

Changed:

- `4x` → `3x`

**ru-RU** (current):

```text
Острый, чтобы убивать морских обитателей. Наносит 4-кратный урон водным врагам.
```

**zh-Hans** (current):

```text
用于斩杀来自大海的生物，对与水有关的敌人造成 4 倍伤害
```

### `Mods.tsorcRevamp.Items.ForgottenGaiaSword.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-14, `d6c5394a5` Last commit for 1.4.4, a lot of progress but a lot of wip stuffs too!

```diff
-An earthbound blade, capable of breaking some magical protections
-[c/ffbf00:Does 2x damage to the Witchking and dispels the defensive shield of the Witchking and Artorias]
+An earthbound blade, breaking the earth and capable of breaking some magical protections
+Deals 25% more damage and reduces the enemy's defense while on the ground, 50% more damage against Witchking
+[c/ffbf00:Dispels the defensive shield of the Witchking and Artorias]
```

Changed:

- added `breaking the earth and`
- `[c/ffbf00:Does 2x` → `Deals 25% more`
- `to` → `and reduces`
- added `enemy's defense while on the ground, 50% more damage against`
- `and dispels` → `⏎ [c/ffbf00:Dispels`

**ru-RU** (current):

```text
Связанный с землёй клинок, способный разрушать некоторые магические защиты
[c/ffbf00:Наносит 2-кратный урон королю-чародею и рассеивает защитный щит короля-чародея и Арториаса]
```

**zh-Hans** (current):

```text
一柄被遗忘的接壤尘世的神兵，用来破除一些魔法护障
[c/ffbf00:对巫师王造成 2 倍伤害并击碎巫师王和亚尔特留斯的防御护盾]
```

### `Mods.tsorcRevamp.Items.Jawblade.Tooltip`

Needs update in: **zh-Hans** · English last changed: 2024-10-19, `589a4a00e` Some melee weapons change, Hellkite dragon HP buff and Crests tooltip change

```diff
 'A blade of bone and fangs'
-Shoots out a homing skull upon hitting enemies with the blade
+Shoots out a homing skull, even more upon hitting enemies with the blade
```

Changed:

- `skull` → `skull, even more`

**zh-Hans** (current):

```text
“一把由骨头与牙齿做成的刀”
用刀击中敌人时射出一个追踪骷髅头
```

### `Mods.tsorcRevamp.Items.MoonlightGreatsword.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-09-22, `0f8023f1e` Fix Moonlight Greatsword dmg type bs

```diff
 [c/ffbf00:The Moonlight Greatsword, the sword of legend...]
 Glows and becomes stronger at night
-Scales with your magic or melee stats, whichever is higher
+Scales with total melee damage + bonus magic damage
 Launches glimmering waves of moonlight
```

Changed:

- `your` → `total melee damage + bonus`
- `or melee stats, whichever is higher` → `damage`

**ru-RU** (current):

```text
[c/ffbf00:Двуручный меч Лунного света, легендарный меч...]
Сияет и становится сильнее при свете луны
Усиливается в зависимости от ваших магических характеристик или характеристик ближнего боя, в зависимости от того, что выше
Испускает мерцающие волны лунного света
```

**zh-Hans** (current):

```text
[c/ffbf00:月光大剑，传奇之剑...]
在夜间发光并变得更强
该武器可以获得近战、法师职业的增益，取较高的为准
发射月光波
```

### `Mods.tsorcRevamp.Items.SeveringDusk.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-30, `348b93b16` Adjusted Severing Dusk and Golden Hairpin

```diff
 A blade honed sharp by the magic of the coming night.
-Right click to dash, consuming stamina, increasing damage dealt and making you immune for a moment
+Tooltip0
+Dashing will block stamina regen and grant the sword a {0}% damage buff
```

Changed:

- `Right click to dash, consuming stamina, increasing` → `Tooltip0 ⏎ Dashing will block stamina regen and grant the sword a {0}%`
- `dealt and making you immune for a moment` → `buff`

**ru-RU** (current):

```text
Клинок, отточенный магией наступающей ночи.
Нажмите ПКМ, чтобы нанести удар, потребляющий выносливость, увеличивающий наносимый урон и на мгновение делающий вас неуязвимым
```

**zh-Hans** (current):

```text
一把被残夜魔力锻造过的锋利的剑
<right>冲刺，消耗耐力让你暂时免疫攻击并增加伤害
```

### `Mods.tsorcRevamp.Items.YianBlade.Tooltip2` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-14, `d6c5394a5` Last commit for 1.4.4, a lot of progress but a lot of wip stuffs too!

```diff
-Right click using all mana to cast a homing blaze, need at least {0} mana
-Blaze deals {1}x the swords damage
+Shoots Yin blazes and confuses enemies on true melee hits
+Right click using all mana to cast three Yang homing blazes, need at least {0} mana
```

Changed:

- added `Shoots Yin blazes and confuses enemies on true melee hits ⏎`
- `a` → `three Yang`
- `blaze,` → `blazes,`
- removed `⏎ Blaze deals {1}x the swords damage`

**ru-RU** (current):

```text
Нажмите ПКМ, используя всю ману, чтобы использовать самонаводящийся клинок. Требуется не менее {0} маны.
Клинок наносит {1}x больше урона от мечей
```

**zh-Hans** (current):

```text
右键消耗全部魔力释放追踪火魇，最少需要 {0} 魔力
火魇造成 {1} 倍伤害
```

### `Mods.tsorcRevamp.Items.EphemeralThrowingAxe.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-14, `d6c5394a5` Last commit for 1.4.4, a lot of progress but a lot of wip stuffs too!

```diff
-Passes through solid walls
+Passes through solid walls and does 20% more damage against mages
```

Changed:

- added `and does 20% more damage against mages`

**ru-RU** (current):

```text
Проходит сквозь твёрдые стены
```

**zh-Hans** (current):

```text
能穿过坚固的墙壁
```

### `Mods.tsorcRevamp.Items.HeavensTear.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-14, `d6c5394a5` Last commit for 1.4.4, a lot of progress but a lot of wip stuffs too!

```diff
 Heaven splits with each swing
-Deals double damage to mages and ghosts
+Deals 40% more damage to mages and ghosts
```

Changed:

- `double` → `40% more`

**ru-RU** (current):

```text
Небеса раскалываются при каждом ударе
Наносит двойной урон магам и призракам
```

**zh-Hans** (current):

```text
每次的挥击都让天堂分崩离析
对法师、巫师和幽灵造成双倍伤害
```

### `Mods.tsorcRevamp.Items.SunderingLight.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-14, `d6c5394a5` Last commit for 1.4.4, a lot of progress but a lot of wip stuffs too!

```diff
 Rips apart the border of life and death
-Deals double damage to mages and ghosts
+Deals 50% more damage to mages and ghosts
```

Changed:

- `double` → `50% more`

**ru-RU** (current):

```text
Разрывает границу между жизнью и смертью
Наносит двойной урон магам и призракам
```

**zh-Hans** (current):

```text
撕裂生与死的边界
对法师、巫师和幽灵造成双倍伤害
```

### `Mods.tsorcRevamp.Items.AncientWarhammer.DisplayName`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-30, `b8814ab80` Resprited Ancient Warhammer and Barrow Blade with a couple of tweaks

```diff
-Ancient Warhammer
+Dwarf Warhammer
```

Changed:

- `Ancient` → `Dwarf`

**ru-RU** (current):

```text
Древний боевой молот
```

**zh-Hans** (current):

```text
古代战锤
```

### `Mods.tsorcRevamp.Items.AncientWarhammer.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-30, `b8814ab80` Resprited Ancient Warhammer and Barrow Blade with a couple of tweaks

```diff
-An old choice for advancing druids
+An old choice for dwarf soldiers
 Breaks the enemy's defense on hit
```

Changed:

- `advancing druids` → `dwarf soldiers`

**ru-RU** (current):

```text
Старый выбор для продвинутых друидов
Пробивает защиту противника при попадании
```

**zh-Hans** (current):

```text
德鲁伊的旧选择
粉碎敌人的防御
```

### `Mods.tsorcRevamp.Items.CelestialLance.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-04-14, `bd5f2da23` A lot of changes and content!!

```diff
-Celestial lance fabled to hold sway over the world.
-Rains fallen stars on enemy hits
-{0}% increased damage while falling and has a chance to heal {1} HP on hit.
+Ruler of stars
+Rains fallen stars on direct hits
```

Changed:

- `Celestial lance fabled to hold sway over the world.` → `Ruler of stars`
- `enemy` → `direct`
- removed `⏎ {0}% increased damage while falling and has a chance to heal {1} HP on hit.`

**ru-RU** (current):

```text
Считается, что Небесное копье правит миром.
При попадании по врагу вызывает звёздный дождь.
Урон при падении увеличивается на {0}%, а при попадании появляется шанс восстановить {1} здоровья
```

**zh-Hans** (current):

```text
传说中的天界之矛掌管着世界
命中时召唤坠星
坠落时增加 {0}% 伤害，每次攻击有机会回复 {1} 生命值
```

### `Mods.tsorcRevamp.Items.PilgrimSpontoon.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-09-07, `da015c2f2` Few melee wepaons fixes

```diff
-Inherits Tridents water effect and pierces {0} armor penetration
-Right-click to catalyze the spear with magicat a cost of 60 mana and increase its abilities for a short time
-Increases weapon speed, allows projectiles to pierce and inflicts a debuff during the buff
-Hitting enemies with the spear speeds up the cooldown reset
-18 seconds cooldown
+
```

Changed:

- removed `Inherits Tridents water effect and pierces {0} armor penetration ⏎ Right-click to catalyze the spear with magicat a cost of 60 mana and increase its abilities for a short time ⏎ Increases weapon speed, allows projectiles to pierce and inflicts a debuff during the buff ⏎ Hitting enemies with the spear speeds up the cooldown reset ⏎ 18 seconds cooldown`

**ru-RU** (current):

```text
Наследует водяной эффект трезубца и пробивает {0} ед. брони
Нажмите ПКМ, чтобы активировать магические способности копья, ценой в 60 ед. маны, усиливая его способности на короткое время
Увеличивает скорость оружия, позволяет метательным снарядам пробивать врагов насквозь и накладывает дебафф во время усиления
Удары копьём по врагам ускоряют перезарядку
Перезарядка 18 секунд
```

**zh-Hans** (current):

```text
继承三叉戟的水中移动效果，{0}点护甲穿透
右键消耗60点魔力催化三叉戟，短时间内提升性能
提升攻击速度，射弹获得穿透效果，攻击施加减益
18 秒冷却，命中敌人加速冷却
```

### `Mods.tsorcRevamp.Items.GaeBolg.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-07, `99bb4a555` 0.19.7 (beta updated): 3 spears fixed and improved

```diff
 Pierce reality
+Click to stab, or hold to charge a throw
```

Changed:

- added `⏎ Click to stab, or hold to charge a throw`

**ru-RU** (current):

```text
Пронзает реальность
```

**zh-Hans** (current):

```text
刺穿现实
```

### `Mods.tsorcRevamp.Items.HiRyuuSpear.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-04-14, `bd5f2da23` A lot of changes and content!!

```diff
 Ruler of sky
-Deal {0}% extra damage to airborne enemies
+{0}% increased damage while falling and has a chance to heal {1} HP on hit
+Deal {2}% extra damage to airborne enemies
```

Changed:

- added `{0}% increased damage while falling and has a chance to heal {1} HP on hit ⏎`
- `{0}%` → `{2}%`

**ru-RU** (current):

```text
Повелитель неба
Наносит {0}% дополнительного урона врагам, находящимся в воздухе
```

**zh-Hans** (current):

```text
空域统治者
对空中的敌人造成 {0}% 额外伤害
```

### `Mods.tsorcRevamp.Items.Longinus.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-07, `99bb4a555` 0.19.7 (beta updated): 3 spears fixed and improved

```diff
 Legendary spear fabled to hold sway over the world.
-Summons red energy from the sky on enemy hits
-Increases attack damage by {0}% when falling and has a chance to heal {1} HP on hit.
+Click to stab, or hold to charge a throw
+Fully charged spears create large powerful explosions
```

Changed:

- `Summons red energy from the sky on enemy hits` → `Click to stab, or hold to charge a throw`
- `Increases attack damage by {0}% when falling and has a chance to heal {1} HP on hit.` → `Fully charged spears create large powerful explosions`

**ru-RU** (current):

```text
Легендарное копье, которое, по слухам, правит миром.
При попадании во врага вызывает с неба красную энергию.
Увеличивает урон от атаки на {0}% при падении и даёт шанс восстановить {1} здоровья при попадании.
```

**zh-Hans** (current):

```text
称霸世界的传说长矛
命中时召唤星火
坠落时增加 {0}% 伤害，每次攻击有机会回复 {1} 生命值
```

### `Mods.tsorcRevamp.Items.DarkTrident.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-07, `99bb4a555` 0.19.7 (beta updated): 3 spears fixed and improved

```diff
 Pierces enemies with dark magic
-Hold left click to charge a throw, right click to stab
+Click to stab, or hold to charge a throw
 Changes its damage type between ranged or melee depending your highest stats
 Fully charged tridents ignite, setting hit enemies ablaze
```

Changed:

- `Hold left click` → `Click to stab, or hold`
- `throw, right click to stab` → `throw`

**ru-RU** (current):

```text
Пронзает врагов тёмной магией
Удерживайте нажатым ЛКМ, чтобы зарядить бросок, а при нажатии ПКМ наносит колющий удар
В зависимости от ваших самых высоких характеристик, тип урона меняется между дальним и ближним боями
При полностью заряженном броске трезумец воспламеняется, поджигая поражённых врагов
```

**zh-Hans** (current):

```text
用黑魔法刺穿敌人
左键像长矛一样戳刺，右键挥舞
根据你的最高属性在远程或近战之间改变其伤害类型
满充能的武器附加燃烧效果，使被击中的敌人燃烧
```

### `Mods.tsorcRevamp.Items.SpiritBell.DisplayName`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-06-05, `3cf3c0aab` AI / Navigation overhaul (FighterAI), Added New Resource Bar UI, Added Archer Spirit Bell ++

```diff
-Spirit Bell
+Knight Spirit Bell
```

Changed:

- added `Knight`

**ru-RU** (current):

```text
Колокольчик Духа
```

**zh-Hans** (current):

```text
招魂铃
```

### `Mods.tsorcRevamp.Items.DetonationSignal.Tooltip` ⚠️

Needs update in: **zh-Hans** · English last changed: 2024-10-10, `4fb90aea1` A bunch of whip reworks

```diff
 Enemies struck by this whip will explode once on minion hit
 Explosion increases the damage dealt by {0}%
-Exploded enemies will also deal {1}% the contact damage to you for 4 seconds
+Exploded enemies will also deal {1}% increased contact damage to you for {2} seconds
 {$CommonItemTooltip.Whips}
```

Changed:

- `the` → `increased`
- `4` → `{2}`

**zh-Hans** (current):

```text
被该鞭子击中的敌人会在受到仆从攻击时产生爆炸
爆炸额外造成 {0}% 的伤害
爆炸的敌人4秒内会对你造成 {1}% 接触伤害
{$CommonItemTooltip.Whips}
```

### `Mods.tsorcRevamp.Items.DragoonLash.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-17, `9c2c80882` Modded minion changes in preparation of 1.4.5

```diff
-Breathe fire at enemies struck by this whip and a minion on a cooldown
-Refresh the cooldown by whipping an enemy
+Breathe fire at enemies struck by this whip or a minion on a cooldown
+Lower the cooldown by whipping an enemy
+Whip tip hits instantly breathe empowered flames at the enemy
 {$CommonItemTooltip.Whips}
```

Changed:

- `and` → `or`
- `Refresh` → `Lower`
- added `Whip tip hits instantly breathe empowered flames at the enemy ⏎`

**ru-RU** (current):

```text
Изрыгает огонь на врагов, пораженных этим хлыстом или миньоном во время перезарядки
Увеличьте время перезарядки, ударив врага
{$CommonItemTooltip.Whips}
```

**zh-Hans** (current):

```text
击中敌人时释放火焰吐息
击中敌人刷新冷却
{$CommonItemTooltip.Whips}
```

### `Mods.tsorcRevamp.Items.Urumi.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-17, `9c2c80882` Modded minion changes in preparation of 1.4.5

```diff
 This whip ignores {1} defense
-{$CommonItemTooltip.Summon.WhipCritDmg}
+{$CommonItemTooltip.Summon.WhipTipDmg}
 {$CommonItemTooltip.Whips}
```

Changed:

- `{$CommonItemTooltip.Summon.WhipCritDmg}` → `{$CommonItemTooltip.Summon.WhipTipDmg}`

**ru-RU** (current):

```text
Этот хлыст игнорирует {1} ед. защиты
{$CommonItemTooltip.Summon.WhipCritDmg}
{$CommonItemTooltip.Whips}
```

**zh-Hans** (current):

```text
{1} 点护甲穿透
{$CommonItemTooltip.Summon.WhipCritDmg}
{$CommonItemTooltip.Whips}
```

### `Mods.tsorcRevamp.Items.WhiteTitanite.Tooltip`

Needs update in: **ru-RU** · English last changed: 2025-11-01, `842282575` BIG COMMIT FOR 0.16.4

```diff
 A rare and valuable ore
 A solid white surface, it casts an eerie purple shadow
 More common in the abyss
+[c/00ffd4:To enter the Abyss, equip the Covenant of Artorias.]
```

Changed:

- added `⏎ [c/00ffd4:To enter the Abyss, equip the Covenant of Artorias.]`

**ru-RU** (current):

```text
Редкая и ценная руда
Благодаря твёрдой белой поверхности, он отбрасывает жутковатую фиолетовую тень
Распространён в бездне
```

### `Mods.tsorcRevamp.Items.WorldRune.Tooltip`

Needs update in: **zh-Hans** · English last changed: 2024-07-20, `ca028e51b` Clue tweak

```diff
 An otherworldy artifact of great power
 It seems to react to dark souls...
-[c/ffbf00:Rumours about red knight below the Eye and torch god in the depths of the corrupted temple guarding the powerful runes]
+[c/ffbf00:Rumours suggest a Red Knight below the Eye, beyond the Path of Ambition, guards the powerful rune]
```

Changed:

- `about red knight` → `suggest a Red Knight`
- `Eye and torch god in` → `Eye, beyond`
- `depths` → `Path`
- `the corrupted temple guarding` → `Ambition, guards`
- `runes]` → `rune]`

**zh-Hans** (current):

```text
一件具有巨大力量的异世界的神器
它似乎对黑暗之魂有反应...
[c/ffbf00:传说克苏鲁之眼正下方的红骑士和腐化神殿深处的火把神守护着强大的符文]
```

### `Mods.tsorcRevamp.Items.BonfirePlaceable.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-14, `811ff6a2c` 0.17.8: Added 5th "Deprived" Class to Character Creation Menu + Sandbox improvements

```diff
 Right-click to light
-You probably shouldn't have this
-Can only be placed by devs
+Costs many Dark Souls to craft...
+but many can be found naturally in the world.
```

Changed:

- `You probably shouldn't have this` → `Costs many Dark Souls to craft...`
- `Can only` → `but many can`
- `placed by devs` → `found naturally in the world.`

**ru-RU** (current):

```text
Нажмите ПКМ, чтобы зажечь его
Возможно, вам не стоит его использовать
Может быть размещён только разработчиками
```

**zh-Hans** (current):

```text
右键点亮
你不应有这个
只能由开发人员放置
```

### `Mods.tsorcRevamp.Items.SteelTempest.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-09, `4b8642f08` Code reorganization for Runeterra items

```diff
-<right> to [c/708090:thrust]
+Right-click to [c/708090:thrust]
 Gain a stack of [c/708090:Steel Tempest] upon [c/708090:thrusting] an enemy
-Upon reaching 2 stacks, the next <right> will release a [c/708090:tornado]
+Upon reaching 2 stacks, the next Right-click will release a [c/708090:tornado]
 [c/708090:'Death is like the wind, always by my side']
```

Changed:

- `<right>` → `Right-click`
- `<right>` → `Right-click`

**ru-RU** (current):

```text
<right> чтобы совершить [c/708090:выпад]
Вы получаете стак [c/708090:Стального шквала] после попадания [c/708090:выпадом] по врагу
при накоплении 2 стаков следующий <right> вызовет [c/708090:торнадо]
[c/708090:"Смерть подобна ветру, она всегда рядом со мной"]
```

**zh-Hans** (current):

```text
<right>释放[c/708090:突刺]
[c/708090:突刺]敌人时获得一层[c/708090:钢铁风暴堆叠]
获得两层堆叠后，下次<right>将会释放一阵[c/708090:旋风]
[c/708090:“死亡如风，常伴吾身”]
```

### `Mods.tsorcRevamp.Items.PlasmaWhirlwind.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-09, `4b8642f08` Code reorganization for Runeterra items

```diff
-<right> to [c/708090:thrust]
+Right-click to [c/708090:thrust]
 Gain a stack of [c/708090:Steel Tempest] upon [c/708090:thrusting] an enemy
-Upon reaching 2 stacks, the next <right> will release a [c/708090:tornado]
+Upon reaching 2 stacks, the next Right-click will release a [c/708090:tornado]
 {$Items.PlasmaWhirlwind.Keybind1}Special Ability{$Items.PlasmaWhirlwind.Keybind2}
-<right>ing while [c/00ffff:dashing] will release a [c/00ffff:Spinning Slash]
+Right-clicking while [c/00ffff:dashing] will release a [c/00ffff:Spinning Slash]
 [c/00ffff:'A companion on this Odyssey']
```

Changed:

- `<right>` → `Right-click`
- `<right>` → `Right-click`
- `<right>ing` → `Right-clicking`

**ru-RU** (current):

```text
<right> чтобы совершить [c/708090:выпад]
Вы получаете стак [c/708090:Стального шквала] после попадания [c/708090:выпадом] по врагу
При накоплении двух стаков следующий <right> вызовет [c/708090:торнадо]
{$Items.PlasmaWhirlwind.Keybind1}Особая способность{$Items.PlasmaWhirlwind.Keybind2}
<right> во время [c/00ffff:рывка] приведёт к появлению [c/00ffff:Вращающегося разреза]
[c/00ffff:"Спутник в этой одиссее"
```

**zh-Hans** (current):

```text
<right>释放[c/708090:突刺]
[c/708090:突刺]敌人时获得一层[c/708090:钢铁风暴堆叠]
获得两层堆叠后，下次<right>将会释放一阵[c/708090:旋风]
{$Items.PlasmaWhirlwind.Keybind1}特殊能力键{$Items.PlasmaWhirlwind.Keybind2}
当[c/00ffff:冲撞]时长按<right>会释放一道[c/00ffff:剑光]
[c/00ffff:“奥德赛的同伴”]
```

### `Mods.tsorcRevamp.Items.Nightbringer.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-09, `4b8642f08` Code reorganization for Runeterra items

```diff
-<right> to [c/708090:thrust]
+Right-click to [c/708090:thrust]
 Gain a stack of [c/708090:Steel Tempest] upon [c/708090:thrusting] an enemy
-Upon reaching 2 stacks, the next <right> will release a [c/708090:tornado]
+Upon reaching 2 stacks, the next Right-click will release a [c/708090:tornado]
 {$Items.PlasmaWhirlwind.Keybind1}Special Ability{$Items.PlasmaWhirlwind.Keybind2}
-<right>ing while [c/00ffff:dashing] will release a [c/00ffff:Spinning Slash]
+Right-clicking while [c/00ffff:dashing] will release a [c/00ffff:Spinning Slash]
 {$Items.Nightbringer.Keybind1}Special Ability{$Items.Nightbringer.Keybind2}
 [c/ff4500:'Harmony is a lie told to force Obedience']
```

Changed:

- `<right>` → `Right-click`
- `<right>` → `Right-click`
- `<right>ing` → `Right-clicking`

**ru-RU** (current):

```text
<right> чтобы совершить [c/708090:выпад]
Вы получаете стак [c/708090:Стального шквала] после попадания [c/708090:выпадом] по врагу
При накоплении 2 стаков, следующий <right> вызовет [c/708090:торнадо]
{$Items.PlasmaWhirlwind.Keybind1}Особая способность{$Items.PlasmaWhirlwind.Keybind2}
<right> во время [c/00ffff:рывка] приведёт к повялению [c/00ffff:Вращающегося разреза]
{$Items.Nightbringer.Keybind1}Особая способность{$Items.Nightbringer.Keybind2}
[c/ff4500:'Гармония - это ложь, сказанная, чтобы заставить повиноваться']
```

**zh-Hans** (current):

```text
<right>释放[c/708090:突刺]
[c/708090:突刺]敌人时获得一层[c/708090:钢铁风暴堆叠]
获得两层堆叠后，下次<right>将会释放一阵[c/708090:旋风]
{$Items.PlasmaWhirlwind.Keybind1}特殊能力键{$Items.PlasmaWhirlwind.Keybind2}
当[c/00ffff:冲撞]时长按<right>会释放一道[c/00ffff:剑光]
{$Items.Nightbringer.Keybind1}特殊能力键{$Items.Nightbringer.Keybind2}
[c/ff4500:“和谐是一个用来迫使顺从的谎言”]
```

### `Mods.tsorcRevamp.Items.Nightbringer.Keybind1`

Needs update in: **zh-Hans** · English last changed: 2023-09-05, `1505518c8` A lot of work on the Runeterra minions

```diff
-Press A + D + 
+Press Left/Right Alt + 
```

Changed:

- `A` → `Left/Right Alt`
- removed `D +`

**zh-Hans** (current):

```text
按 A+D键和
```

### `Mods.tsorcRevamp.Items.ToxicShot.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-22, `0f805c799` Small bug fixes

```diff
 Converts seeds into [c/ba55d3:Toxic Shots] and allows you to gather Seeds from grass
 [c/ba55d3:Toxic Shots] apply a short burst of [c/ba55d3:Venom] and home into enemies
-Also uses all darts as ammo, but Poison Darts deal {0}x damage
+Also uses all darts as ammo, but Poison Darts deal {0}x damage and gain {1} pierce
 [c/ba55d3:Inhibits player movement conditionally] whilst being used
 [c/ba55d3:May grant a movement speed and stamina regen boost] whilst being held
 {$Items.ToxicShot.Keybind1}Special Ability{$Items.ToxicShot.Keybind2}
 [c/ba55d3:'That's gotta sting']
```

Changed:

- added `and gain {1} pierce`

**ru-RU** (current):

```text
Превращает семена в [c/ba55d3:токсичные выстрелы] и позволяет собирать семена с травы
[c/ba55d3:Ядовитые выстрелы] наносят короткий заряд [c/ba55d3:яда] и наводятся на врагов
Также все дротики используются в качестве боеприпасов, но ядовитые дротики наносят {0}x урона
[c/ba55d3:При использовании дротики условно блокируют передвижение игрока]
[c/ba55d3:Может увеличивать скорость передвижения и восстанавливать выносливость] при удержании
{$Items.ToxicShot.Keybind1}Особая способность{$Items.ToxicShot.Keybind2}
[c/ba55d3:'Это должно быть больно']
```

**zh-Hans** (current):

```text
将种子转化为[c/ba55d3:恶毒飞镖]，并允许你从草中收集种子
[c/ba55d3:恶毒飞镖]对敌人造成[c/ba55d3:酸性毒液]减益并自动追踪附近敌人
也可以使用其它飞镖作为弹药，但使用毒镖造成 {0} 倍伤害
在使用时[c/ba55d3:有条件的限制玩家的移动]
手持该物品时，获得[c/ba55d3:移动速度和耐力回复提升]
{$Items.ToxicShot.Keybind1}Special Ability{$Items.ToxicShot.Keybind2}
[c/ba55d3:“那一定很刺痛”]
```

### `Mods.tsorcRevamp.Items.ToxicShot.Details`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-23, `cbf8f187f` Buffed and fixed Runeterra Darts

```diff
 [c/ba55d3:Venom] applied by [c/ba55d3:Toxic Shots] scales with ranged damage and critical strike chance
-Grants [c/ba55d3:Scouts Boost] passively while being held:{$CommonItemTooltip.MoveSpeedMult} and,
+Grants [c/ba55d3:Scouts Boost] passively while being held:
+{$CommonItemTooltip.MoveSpeedMult} and,
 multiplicatively increases stamina regeneration by {1}%
 [c/ba55d3:Scouts Boost] is removed upon taking damage and put on a {$CommonItemTooltip.Cooldown@2}
 Using the weapon with [c/ba55d3:Scouts Boost] active passively will slow you down
 Using it when [c/ba55d3:Scouts Boost] is on cooldown will root you briefly
-[c/ba55d3:Activated Ability] removes movement penalties unconditionally for {$CommonItemTooltip.SecondDuration@3}
+[c/ba55d3:Activated Ability] removes movement penalties unconditionally for a {$CommonItemTooltip.SecondDuration@3}
 [c/ba55d3:Activated Ability] doubles the movement speed and stamina regeneration boosts
```

Changed:

- `held:{$CommonItemTooltip.MoveSpeedMult}` → `held: ⏎ {$CommonItemTooltip.MoveSpeedMult}`
- added `a`

**ru-RU** (current):

```text
[c/ba55d3:Яд], наносимый [c/ba55d3:ядовитыми выстрелами], усиливается от урона дальнего боя и шанса крит. удара
Дарует пассивное [c/ba55d3:ускорение разведчиков] при удержании оружия: {$CommonItemTooltip.MoveSpeedMult} и
мультипликативно увеличивает регенерацию выносливости на {1}%
[c/ba55d3:Ускорение разведчиков] Снимается при получении урона и получает {$CommonItemTooltip.Cooldown@2}
Использование оружия с активным [c/ba55d3:ускорение разведчиков] пассивно замедляет вас
Использование, когда [c/ba55d3:ускорение разведчиков] на перезарядке, ненадолго оцепенит вас
[c/ba55d3:Активированная способность] снимает все штрафы к передвижению {$CommonItemTooltip.SecondDuration@3}
[c/ba55d3:Активированная способность]  удваивает скорость передвижения и увеличивает регенерацию выносливости.
```

**zh-Hans** (current):

```text
[c/ba55d3:恶毒飞镖]造成的[c/ba55d3:酸性毒液]减益受到远程伤害和暴击率加成
手持该武器时获得[c/ba55d3:侦察兵激励]增益：
{$CommonItemTooltip.MoveSpeedMult}，同时乘算增加 {1}% 耐力再生
当受到伤害时[c/ba55d3:侦察兵激励]被移除，同时获得 {$CommonItemTooltip.Cooldown@2}
当[c/ba55d3:侦察兵激励]激活时使用武器会减缓你的速度
当[c/ba55d3:侦察兵激励]冷却时使用该物品，会让你短暂的不能移动
[c/ba55d3:侦察兵激励II]移除移动速度惩罚，有 {$CommonItemTooltip.SecondDuration@3}
[c/ba55d3:侦察兵激励II]加倍移动速度和耐力再生增益
```

### `Mods.tsorcRevamp.Items.AlienGun.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-09, `4b8642f08` Code reorganization for Runeterra items

```diff
 Converts seeds into [c/ba55d3:Alien Lasers] and allows you to gather Seeds from grass
 [c/ba55d3:Alien Lasers] apply a short burst of [c/ba55d3:Electrified] and home into enemies
-Also uses all darts as ammo, but Poison Darts deal {0}x damage
+Also uses all darts as ammo, but Poison Darts deal {0}x damage and gain {1} pierce
 [c/ba55d3:Inhibits player movement conditionally] whilst being used
 [c/ba55d3:May grant a movement speed and stamina regen boost] whilst being held
 {$Items.ToxicShot.Keybind1}Special Ability{$Items.ToxicShot.Keybind2}
-<right> to shoot a homing [c/00ffff:Blinding Laser] using Mana which inflicts confusion
+Right-click to shoot a homing [c/00ffff:Blinding Laser] which inflicts confusion
 [c/00ffff:'Armed and ready']
```

Changed:

- added `and gain {1} pierce`
- `<right>` → `Right-click`
- removed `using Mana`

**ru-RU** (current):

```text
Превращает семена в [c/ba55d3:инопланетные лазеры] и позволяет собирать семена с травы
[c/ba55d3:Инопланетные лазеры] наносят дебафф [c/ba55d3:наэлектризованный] и наводятся на врагов
Также все дротики используются в качестве боеприпасов, но ядовитые дротики наносят {0}x урона
[c/ba55d3:При использовании дротики условно блокируют передвижение игрока]
[c/ba55d3:Может увеличивать скорость передвижения и восстанавливать выносливость] при удержании
{$Items.ToxicShot.Keybind1}Специальная способность{$Items.ToxicShot.Keybind2}
<right> чтобы выстрелить самонаводящимся [c/00ffff:ослепляющим лазером], использующим ману, который накладывает замешательство
[c/00ffff:'Вооружён и готов']
```

**zh-Hans** (current):

```text
将种子转化为[c/ba55d3:外星激光]，并允许你从草中收集种子
[c/ba55d3:外星激光]对敌人造成短暂的[c/ba55d3:触电]减益并自动追踪附近敌人
也可以使用其它飞镖作为弹药，但使用毒镖造成 {0} 倍伤害
在使用时[c/ba55d3:有条件的限制玩家的移动]
手持该物品时，获得[c/ba55d3:移动速度和耐力回复提升]
{$Items.ToxicShot.Keybind1}Special Ability{$Items.ToxicShot.Keybind2}
<right>消耗魔力射出一道追踪的[c/00ffff:致盲激光]造成困惑减益
[c/00ffff:“全副武装，随时战斗”]
```

### `Mods.tsorcRevamp.Items.AlienGun.Details`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-23, `cbf8f187f` Buffed and fixed Runeterra Darts

```diff
 [c/00ffff:Blinding Laser] deals {4}x damage, multiplied by {5} again while using Seeds as ammunition({$CommonItemTooltip.Cooldown@6})
 [c/00ffff:Blinding Laser] gains {7}% bonus critical strike chance and {8}% max health damage on-hit, up to {9}
 [c/ba55d3:Electrification] applied by [c/ba55d3:Alien Lasers] scales with ranged damage and critical strike chance
-Grants [c/ba55d3:Scouts Boost] passively while being held:{$CommonItemTooltip.MoveSpeedMult} and,
+Grants [c/ba55d3:Scouts Boost] passively while being held:
+{$CommonItemTooltip.MoveSpeedMult} and,
 multiplicatively increases stamina regeneration by {1}%
 [c/ba55d3:Scouts Boost] is removed upon taking damage and put on a {$CommonItemTooltip.Cooldown@2}
 Using the weapon with [c/ba55d3:Scouts Boost] active passively will slow you down
 Using it when [c/ba55d3:Scouts Boost] is on cooldown will root you briefly
-[c/ba55d3:Activated Ability] removes movement penalties unconditionally for {$CommonItemTooltip.SecondDuration@3}
+[c/ba55d3:Activated Ability] removes movement penalties unconditionally for a {$CommonItemTooltip.SecondDuration@3}
 [c/ba55d3:Activated Ability] doubles the movement speed and stamina regeneration boosts
```

Changed:

- `held:{$CommonItemTooltip.MoveSpeedMult}` → `held: ⏎ {$CommonItemTooltip.MoveSpeedMult}`
- added `a`

**ru-RU** (current):

```text
[c/00ffff:Ослепляющий лазер] наносит {4}x урона, умножающегося на {5} при использовании семян в качестве боеприпасов({$CommonItemTooltip.Cooldown@6})
[c/00ffff:Ослепляющий лазер] получает {7}% бонусного шанса крит. удара и {8}% бонусного урона от максимального здоровья цели, вплоть до {9}
[c/ba55d3:Наэлектризованность], накладываемая [c/ba55d3:инопланетными лазерами] усиливается от урона дальнего боя и шанса крит. удара
Дарует пассивное [c/ba55d3:ускорение разведчиков] при удержании оружия: {$CommonItemTooltip.MoveSpeedMult} и
мультипликативно увеличивает регенерацию выносливости на {1}%
[c/ba55d3:Ускорение разведчиков] Снимается при получении урона и получает {$CommonItemTooltip.Cooldown@2}
Использование оружия с активным [c/ba55d3:ускорение разведчиков] пассивно замедляет вас
Использование, когда [c/ba55d3:ускорение разведчиков] на перезарядке, ненадолго оцепенит вас
[c/ba55d3:Активированная способность] снимает все штрафы к передвижению {$CommonItemTooltip.SecondDuration@3}
[c/ba55d3:Активированная способность]  удваивает скорость передвижения и увеличивает регенерацию выносливости.
```

**zh-Hans** (current):

```text
[c/00ffff:致盲激光]造成 {4} 伤害，使用种子作为弹药时伤害再次乘 {5} ({$CommonItemTooltip.Cooldown@6})
[c/00ffff:致盲激光]获得 {7}% 暴击几率加成和 {8}% 每击最大生命值伤害，最大到 {9}
[c/ba55d3:外星激光]造成的 [c/ba55d3:触电]减益受到远程伤害和暴击率加成
手持该武器时获得[c/ba55d3:侦察兵激励]增益：
{$CommonItemTooltip.MoveSpeedMult}，同时乘算增加 {1}% 耐力再生
当受到伤害时[c/ba55d3:侦察兵激励]被移除，同时获得 {$CommonItemTooltip.Cooldown@2}
当[c/ba55d3:侦察兵激励]激活时使用武器会减缓你的速度
当[c/ba55d3:侦察兵激励]冷却时使用该物品，会让你短暂的不能移动
[c/ba55d3:侦察兵激励II]移除移动速度惩罚，有 {$CommonItemTooltip.SecondDuration@3}
[c/ba55d3:侦察兵激励II]加倍移动速度和耐力再生增益
```

### `Mods.tsorcRevamp.Items.OmegaSquadRifle.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-09, `4b8642f08` Code reorganization for Runeterra items

```diff
 Converts seeds into [c/ba55d3:Radioactive Darts] and allows you to gather Seeds from grass
 [c/ba55d3:Radioactive Darts] apply a short burst of [c/ba55d3:Radiation] and home into enemies
-Also uses all darts as ammo, but Poison Darts deal {0}x damage
+Also uses all darts as ammo, but Poison Darts deal {0}x damage and gain {1} pierce
 [c/ba55d3:Inhibits player movement conditionally] whilst being used
 [c/ba55d3:May grant a movement speed and stamina regen boost] whilst being held
 {$Items.ToxicShot.Keybind1}Special Ability{$Items.ToxicShot.Keybind2}
-<right> to shoot a homing [c/00ffff:Blinding Laser] using Mana which inflicts confusion
+Right-click to shoot a homing [c/00ffff:Blinding Laser] which inflicts confusion
 {$Items.OmegaSquadRifle.Keybind1}Special Ability{$Items.OmegaSquadRifle.Keybind2}
 [c/adff2f:'There's a mushroom out there with your name on it']
```

Changed:

- added `and gain {1} pierce`
- `<right>` → `Right-click`
- removed `using Mana`

**ru-RU** (current):

```text
Превращает семена в [c/ba55d3:радиоактивные дротики] и позволяет собирать семена с травы
[c/ba55d3:Радиоактивные дротики] наносят дебафф [c/ba55d3:радиации] и наводятся на врагов
Также все дротики используются в качестве боеприпасов, но ядовитые дротики наносят {0}x урона
[c/ba55d3:При использовании дротики условно блокируют передвижение игрока]
[c/ba55d3:Может увеличивать скорость передвижения и восстанавливать выносливость] при удержании
{$Items.ToxicShot.Keybind1}Специальная способность{$Items.ToxicShot.Keybind2}
<right>, чтобы выстрелить самонаводящимся [c/00ffff:Ослепляющим лазером], использующим ману, который накладывает замешательство
{$Items.OmegaSquadRifle.Keybind1}Специальная способность{$Items.OmegaSquadRifle.Keybind2}
[c/adff2f:'Там есть гриб с твоим именем']
```

**zh-Hans** (current):

```text
将种子转化为[c/ba55d3:放射性飞镖]，并允许你从草中收集种子
[c/ba55d3:放射性飞镖]对敌人造成短暂的[c/ba55d3:辐射]减益并自动追踪附近敌人
也可以使用其它飞镖作为弹药，但使用毒镖造成 {0} 倍伤害
在使用时[c/ba55d3:有条件的限制玩家的移动]
手持该物品时，获得[c/ba55d3:移动速度和耐力回复提升]
{$Items.ToxicShot.Keybind1}Special Ability{$Items.ToxicShot.Keybind2}
<right>消耗魔力射出一道追踪的[c/00ffff:致盲激光]造成困惑减益
{$Items.OmegaSquadRifle.Keybind1}特殊能力键{$Items.OmegaSquadRifle.Keybind2}
[c/adff2f:“外面有一个蘑菇，上面有你的名字”]
```

### `Mods.tsorcRevamp.Items.OmegaSquadRifle.Keybind1`

Needs update in: **zh-Hans** · English last changed: 2023-09-05, `1505518c8` A lot of work on the Runeterra minions

```diff
-Press A + D + 
+Press Left/Right Alt + 
```

Changed:

- `A` → `Left/Right Alt`
- removed `D +`

**zh-Hans** (current):

```text
按 A+D键和
```

### `Mods.tsorcRevamp.Items.OmegaSquadRifle.Keybind2`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-23, `cbf8f187f` Buffed and fixed Runeterra Darts

```diff
- to drop a [c/adff2f:Nuclear Mushroom] using Mana
+ to drop a [c/adff2f:Nuclear Mushroom]
```

Changed:

- removed `using Mana`

**ru-RU** (current):

```text
 чтобы сбросить [c/adff2f:ядерный гриб] используя ману
```

**zh-Hans** (current):

```text
键 消耗魔力在光标所指位置生成一个 [c/adff2f:蘑菇弹]
```

### `Mods.tsorcRevamp.Items.OmegaSquadRifle.Details`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-23, `cbf8f187f` Buffed and fixed Runeterra Darts

```diff
 [c/adff2f:Nuclear Mushroom] needs {10} seconds to arm itself after being dropped({$CommonItemTooltip.Cooldown@11})
 The explosion resulting from the [c/adff2f:Nuclear Mushroom] deals massive damage and applies an even stronger [c/adff2f:Radiation]
 [c/adff2f:Radiation] applied from the [c/adff2f:Nuclear Mushroom's] explosion stacks with that of [c/ba55d3:Radioactive Darts]
 [c/00ffff:Blinding Laser] deals {4}x damage, multiplied by {5} again while using Seeds as ammunition({$CommonItemTooltip.Cooldown@6})
 [c/00ffff:Blinding Laser] gains {7}% bonus critical strike chance and {8}% max health damage on-hit, up to {9}
 [c/ba55d3:Radiation] applied by [c/ba55d3:Radioactive Darts] scales with ranged damage and critical strike chance
-Grants [c/ba55d3:Scouts Boost] passively while being held:{$CommonItemTooltip.MoveSpeedMult} and,
+Grants [c/ba55d3:Scouts Boost] passively while being held:
+{$CommonItemTooltip.MoveSpeedMult} and,
 multiplicatively increases stamina regeneration by {1}%
 [c/ba55d3:Scouts Boost] is removed upon taking damage and put on a {$CommonItemTooltip.Cooldown@2}
 Using the weapon with [c/ba55d3:Scouts Boost] active passively will slow you down
 Using it when [c/ba55d3:Scouts Boost] is on cooldown will root you briefly
-[c/ba55d3:Activated Ability] removes movement penalties unconditionally for {$CommonItemTooltip.SecondDuration@3}
+[c/ba55d3:Activated Ability] removes movement penalties unconditionally for a {$CommonItemTooltip.SecondDuration@3}
 [c/ba55d3:Activated Ability] doubles the movement speed and stamina regeneration boosts
```

Changed:

- `held:{$CommonItemTooltip.MoveSpeedMult}` → `held: ⏎ {$CommonItemTooltip.MoveSpeedMult}`
- added `a`

**ru-RU** (current):

```text
[c/adff2f:Ядерному грибу] требуется {10} секунд, чтобы привести себя в действие после падения({$CommonItemTooltip.Cooldown@11})
Взрыв, вызванный [c/adff2f:ядерным грибом], наносит огромный урон и вызывает еще более сильное [c/adff2f:излучение]
[c/adff2f:Излучение], вызванное взрывом [c/adff2f:ядерного гриба] суммируется с излучением [c/ba55d3:радиоактивных дротиков]
[c/00ffff:Ослепляющий лазер] наносит {4}x урона, умножающегося на {5} при использовании семян в качестве боеприпасов({$CommonItemTooltip.Cooldown@6})
[c/00ffff:Ослепляющий лазер] получает {7}% бонусного шанса крит. удара и {8}% бонусного урона от максимального здоровья цели, вплоть до {9}
[c/ba55d3:Излучение], накладываемое [c/ba55d3:радиоактивными дротиками] усиливается от урона дальнего боя и шанса крит. удара
Дарует пассивное [c/ba55d3:ускорение разведчиков] при удержании оружия: {$CommonItemTooltip.MoveSpeedMult} и
мультипликативно увеличивает регенерацию выносливости на {1}%
[c/ba55d3:Ускорение разведчиков] Снимается при получении урона и получает {$CommonItemTooltip.Cooldown@2}
Использование оружия с активным [c/ba55d3:ускорение разведчиков] пассивно замедляет вас
Использование, когда [c/ba55d3:ускорение разведчиков] на перезарядке, ненадолго оцепенит вас
[c/ba55d3:Активированная способность] снимает все штрафы к передвижению {$CommonItemTooltip.SecondDuration@3}
[c/ba55d3:Активированная способность]  удваивает скорость передвижения и увеличивает регенерацию выносливости.
```

**zh-Hans** (current):

```text
[c/adff2f:蘑菇弹]在发射后需要 {10} 秒来引爆，({$CommonItemTooltip.Cooldown@11})
[c/adff2f:蘑菇弹]的爆炸造成巨量的伤害并施加更强的[c/adff2f:辐射]减益
[c/adff2f:蘑菇弹]爆炸和[c/ba55d3:放射性飞镖]造成的[c/adff2f:辐射]减益相互叠加
[c/00ffff:致盲激光]造成 {4} 倍伤害, 使用种子作为弹药时伤害再次乘 {5} ({$CommonItemTooltip.Cooldown@6})
[c/00ffff:致盲激光]获得 {7}% 暴击几率加成和 {8}% 每击最大生命值伤害，最大到 {9}
[c/ba55d3:放射性飞镖]造成的[c/ba55d3:辐射]减益受到远程伤害和暴击率加成
手持该武器时获得[c/ba55d3:侦察兵激励]增益：
{$CommonItemTooltip.MoveSpeedMult}，同时乘算增加 {1}% 耐力再生
当受到伤害时[c/ba55d3:侦察兵激励]被移除，同时获得 {$CommonItemTooltip.Cooldown@2}
当[c/ba55d3:侦察兵激励]激活时使用武器会减缓你的速度
当[c/ba55d3:侦察兵激励]冷却时使用该物品，会让你短暂的不能移动
[c/ba55d3:侦察兵激励II]移除移动速度惩罚，有 {$CommonItemTooltip.SecondDuration@3}
[c/ba55d3:侦察兵激励II]加倍移动速度和耐力再生增益
```

### `Mods.tsorcRevamp.Items.OrbOfDeception.Details` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-05, `127ed5ec3` Adjusted Runeterra Orbs

```diff
 The [c/87cefa:Orb] deals {0}% more damage and {1}% on the way back
-[c/87cefa:Homing flames restore half of their mana cost on-hit]
+[c/87cefa:Homing flames cast mana cost is reduced by {9}%]
 Only the first [c/87cefa:Orb] hit and the first hit once it starts returning grant [c/87cefa:Essence Thief] stacks
 Gain 2 [c/87cefa:Essence Thief] stacks if those crit
 Also has a {2}% chance to gain an [c/87cefa:Essence Thief] stack on enemy kills
 A full [c/87cefa:Orb] will deal {3}% bonus damage and its heal scales with maximum mana and magic damage
```

Changed:

- `restore half of their` → `cast`
- `on-hit]` → `is reduced by {9}%]`

**ru-RU** (current):

```text
[c/87cefa:Сфера] наносит на {0}% больше урона и на {1}% больше урона на обратном пути
[c/87cefa:Самонаводящееся пламя восстанавливает половину затраченной маны при попадании]
Лишь первое попадание [c/87cefa:сферы] и первое попадание на обратном пути дарует стаки [c/87cefa:эссенции похитителя]
Даёт 2 стака [c/87cefa:Эссенции похитителя] при крит. ударе
Также есть шанс {2}% получить стак [c/87cefa:Эссенции похитиеля] при убийстве врага
Полная [c/87cefa:Сфера] наносит {3}% бонусного урона, а также её исцеление усиливается от максимума маны и магического урона
```

**zh-Hans** (current):

```text
[c/87cefa:魔法珠]额外造成 {0}% 的伤害，返回玩家时额外造成 {1}%
[c/87cefa:追踪火焰命中敌人时回复其消耗的一半魔力]
只有[c/87cefa:魔法珠]投出和返回时造成的第一次伤害可以获得[c/87cefa:精华窃取]堆叠
如果暴击则获得两层[c/87cefa:精华窃取]堆叠
击杀敌人时有 {2}% 的几率获得一层[c/87cefa:精华窃取]堆叠
满堆叠层数的 [c/87cefa:魔法珠]对敌人额外造成 {3}% 的伤害，同时其治疗量由玩家最大魔力值和魔法伤害决定
```

### `Mods.tsorcRevamp.Items.OrbOfFlame.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-09-28, `2ccf7ef9b` Fixed some bs that somehow broke on runeterra magic weapons

```diff
 Throws a flaming [c/87cefa:Orb] which will return to you after a certain distance
 Casts [c/87cefa:homing flames] while the [c/87cefa:Orb] is sent out when used
 Gathers stacks of [c/87cefa:Essence Thief] on [c/87cefa:Orb] hits and kills
 Upon reaching 9 stacks, the next [c/87cefa:Orb] cast will consume 9 stacks, [c/87cefa:heal you and deal massive damage]
-<right> to cast a [c/ff7f50:Fireball which sunders enemies, increasing their vulnerability to magic damage]
+Right-click to cast a [c/ff7f50:Fireball which sunders enemies, increasing their vulnerability to magic damage]
 [c/ff7f50:'There's beauty in even the most painful memories']
```

Changed:

- `<right>` → `Right-click`

**ru-RU** (current):

```text
Бросает [c/87cefa:пылающую сферу], которая вернётся к вам, пролетев определённое расстояние
Создаёт [c/87cefa:самонаводящееся пламя], пока [c/87cefa:сфера] находится в полёте
Накапливает стаки [c/87cefa:эссенции похитителя] при попадании [c/87cefa:сферой] и при убийствах
При накоплении 9 стаков, следующая [c/87cefa:сфера] поглотит стаки, [c/87cefa:исцелит вас и нанесёт огромный урон]
<right> чтобы бросить [c/ff7f50:Огненный шар, который разделят врагов на части, повышая их уязвимость к магическому урону]
[c/ff7f50:'Даже в самых болезненных воспоминаниях есть красота']
```

**zh-Hans** (current):

```text
投出一个附有火焰的[c/87cefa:魔法珠]，它会在一定距离后返回你身边
当[c/87cefa:珠子]投出时攻击会生成[c/87cefa:追踪火焰]
当[c/87cefa:珠子]命中和击杀敌人时，获得[c/87cefa:精华窃取]堆叠
当获得九层堆叠时，下一次投出的[c/87cefa:魔法珠]将会消耗所有层数，[c/87cefa:治疗玩家并对敌人造成大量伤害]
<right>施放一个[c/ff7f50:割裂敌人的魔法火球并增加他们受到的魔法伤害]
[c/ff7f50:“苦痛之中，亦见光辉”]
```

### `Mods.tsorcRevamp.Items.OrbOfFlame.Details` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-05, `127ed5ec3` Adjusted Runeterra Orbs

```diff
 [c/ff7f50:Fireball] deals {4}x damage({$CommonItemTooltip.Cooldown@5}) and {6}% max health damage on-hit, up to {7}
 [c/ff7f50:Sunder] increases magic damage taken by {8}%({$CommonItemTooltip.SecondDuration@12})
 [c/87cefa:Homing flames] will prioritize [c/ff7f50:sundered] targets
 The [c/87cefa:Orb] deals {0}% more damage and {1}% on the way back
-[c/87cefa:Homing flames restore half of their mana cost on-hit]
+[c/87cefa:Homing flames cast mana cost is reduced by {9}%]
 Only the first [c/87cefa:Orb] hit and the first hit once it starts returning grant [c/87cefa:Essence Thief] stacks
 Gain 2 [c/87cefa:Essence Thief] stacks if those crit
 Also has a {2}% chance to gain an [c/87cefa:Essence Thief] stack on enemy kills
 A full [c/87cefa:Orb] will deal {3}% bonus damage and its heal scales with maximum mana and magic damage
```

Changed:

- `restore half of their` → `cast`
- `on-hit]` → `is reduced by {9}%]`

**ru-RU** (current):

```text
[c/ff7f50:Огненный шар] наносит {4}x урона({$CommonItemTooltip.Cooldown@5}) и {6}% урона от максимального здоровья цели, вплоть до {7}
[c/ff7f50:Разделение] увеличивает магический урон на {8}%({$CommonItemTooltip.SecondDuration@12})
[c/87cefa:Самонаводящееся пламя] берёт в приоритет [c/ff7f50:разделённые] цели
[c/87cefa:Сфера] наносит на {0}% больше урона и на {1}% больше урона на обратном пути
[c/87cefa:Самонаводящееся пламя восстанавливает половину затраченной маны при попадании]
Лишь первое попадание [c/87cefa:сферы] и первое попадание на обратном пути дарует стаки [c/87cefa:эссенции похитителя]
Даёт 2 стака [c/87cefa:Эссенции похитителя] при крит. ударе
Также есть шанс {2}% получить стак [c/87cefa:Эссенции похитиеля] при убийстве врага
Полная [c/87cefa:Сфера] наносит {3}% бонусного урона, а также её исцеление усиливается от максимума маны и магического урона
```

**zh-Hans** (current):

```text
[c/ff7f50:魔法火球]每次攻击造成 {4} 倍伤害({$CommonItemTooltip.Cooldown@5}) 和 {6}% 最高生命值伤害，最高到 {7}）
[c/ff7f50:割裂]的敌人受到的魔法伤害增加 {8}% ({$CommonItemTooltip.SecondDuration@12})
[c/87cefa:追踪火焰]优先攻击[c/ff7f50:割裂]的敌人
[c/87cefa:魔法珠]额外造成 {0}% 的伤害，返回玩家时额外造成 {1}%
[c/87cefa:追踪火焰命中敌人时回复其消耗的一半魔力]
只有[c/87cefa:魔法珠]投出和返回时造成的第一次伤害可以获得[c/87cefa:精华窃取]堆叠
如果暴击则获得两层[c/87cefa:精华窃取]堆叠
击杀敌人时有 {2}% 的几率获得一层[c/87cefa:精华窃取]堆叠
满堆叠层数的[c/87cefa:魔法珠]对敌人额外造成 {3}% 的伤害，同时其治疗量由玩家最大魔力值和魔法伤害决定
```

### `Mods.tsorcRevamp.Items.OrbOfSpirituality.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-09-28, `2ccf7ef9b` Fixed some bs that somehow broke on runeterra magic weapons

```diff
 Throws a spiritual [c/87cefa:Orb] which will return to you after a certain distance
 Casts [c/87cefa:homing flames] while the [c/87cefa:Orb] is sent out when used
 Gathers stacks of [c/87cefa:Essence Thief] on [c/87cefa:Orb] hits and kills
 Upon reaching 9 stacks, the next [c/87cefa:Orb] cast will consume 9 stacks, [c/87cefa:heal you and deal massive damage]
-<right> to cast a [c/ff7f50:Charm which sunders enemies, increasing their vulnerability to magic damage]
-{$Items.OrbOfSpirituality.Keybind1}Special Ability{$Items.OrbOfSpirituality.Keybind2}
+Right-click to cast a [c/ff7f50:Charm which sunders enemies, increasing their vulnerability to magic damage]
 [c/ff00ff:'Everything finds its way to the Spirit Realm eventually,]
 [c/ff00ff:our opponents seem eager to get there quickly']
```

Changed:

- `<right>` → `Right-click`
- removed `⏎ {$Items.OrbOfSpirituality.Keybind1}Special Ability{$Items.OrbOfSpirituality.Keybind2}`

**ru-RU** (current):

```text
Бросает [c/87cefa:сферу духовности], которая вернётся к вам, пролетев определённое расстояние
Создаёт [c/87cefa:самонаводящееся пламя], пока [c/87cefa:сфера] находится в полёте
Накапливает стаки [c/87cefa:эссенции похитителя] при попадании [c/87cefa:сферой] и при убийствах
При накоплении 9 стаков, следующая [c/87cefa:сфера] поглотит стаки, [c/87cefa:исцелит вас и нанесёт огромный урон]
<right> чтобы бросить [c/ff7f50:фрагмент очарования, который разделят врагов на части, повышая их уязвимость к магическому урону]
{$Items.OrbOfSpirituality.Keybind1}Специальная способность{$Items.OrbOfSpirituality.Keybind2}
[c/ff00ff:'Рано или поздно все находит свой путь в Царство Духов,]
[c/ff00ff:и наши противники, похоже, стремятся попасть туда побыстрее']
```

**zh-Hans** (current):

```text
投出一个灵性的[c/87cefa:魔法珠]，它会在一定距离后返回你身边
当[c/87cefa:珠子]投出时攻击会生成[c/87cefa:追踪火焰]
当[c/87cefa:珠子]命中和击杀敌人时，获得[c/87cefa:精华窃取]堆叠
当获得九层堆叠时，下一次投出的[c/87cefa:魔法珠]将会消耗所有层数，[c/87cefa:治疗玩家并对敌人造成大量伤害]
<right>施放一个[c/ff7f50:割裂敌人的爱心并增加他们受到的魔法伤害]
{$Items.OrbOfSpirituality.Keybind1}特殊能力键{$Items.OrbOfSpirituality.Keybind2}
[c/ff00ff:万物有灵，终有归宿]
[c/ff00ff:敌速投胎，物理超度]
```

### `Mods.tsorcRevamp.Items.OrbOfSpirituality.Keybind2`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-09-28, `2ccf7ef9b` Fixed some bs that somehow broke on runeterra magic weapons

```diff
- to [c/ff00ff:dash towards the cursor], casting [c/87cefa:homing flames] along the way
+ to [c/ff00ff:dash towards the cursor], casting an [c/87cefa:empowered homing flame] along the way
```

Changed:

- `[c/87cefa:homing flames]` → `an [c/87cefa:empowered homing flame]`

**ru-RU** (current):

```text
 чтобы [c/ff00ff:совершить рывок в сторону курсора], испуская по пути [c/87cefa:самонаводящееся пламя]
```

**zh-Hans** (current):

```text
 以 [c/ff00ff:冲向光标所在的位置] 沿途发射[c/87cefa:追踪火焰]
```

### `Mods.tsorcRevamp.Items.OrbOfSpirituality.Details` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-05, `127ed5ec3` Adjusted Runeterra Orbs

```diff
 Using [c/ff00ff:Special Ability] grants you a buff({$CommonItemTooltip.SecondDuration@9}),
 which will allow you to [c/ff00ff:dash up to 3 times]({$CommonItemTooltip.Cooldown@10})
-[c/87cefa:Homing flames] [c/ff00ff:spawned by dashing do not restore mana, Special Ability costs {11} mana to cast]
+[c/87cefa:Homing flames] [c/ff00ff:spawned by dashing have infinite pierce, Special Ability costs {11} mana to cast]
 [c/ff7f50:Charm] deals {4}x damage({$CommonItemTooltip.Cooldown@5}) and {6}% max health damage on-hit, up to {7}
 [c/ff7f50:Sunder] increases magic damage taken by {8}%({$CommonItemTooltip.SecondDuration@12})
 [c/87cefa:Homing flames] will prioritize [c/ff7f50:sundered] targets
 The [c/87cefa:Orb] deals {0}% more damage and {1}% on the way back
-[c/87cefa:Homing flames restore half of their mana cost on-hit]
+[c/87cefa:Homing flames cast mana cost is reduced by {9}%]
 Only the first [c/87cefa:Orb] hit and the first hit once it starts returning grant [c/87cefa:Essence Thief] stacks
 Gain 2 [c/87cefa:Essence Thief] stacks if those crit
 Also has a {2}% chance to gain an [c/87cefa:Essence Thief] stack on enemy kills
 A filled [c/87cefa:Orb] will deal {3}% bonus damage and its heal scales with maximum mana and magic damage
```

Changed:

- `do not restore mana,` → `have infinite pierce,`
- `restore half of their` → `cast`
- `on-hit]` → `is reduced by {9}%]`

**ru-RU** (current):

```text
Используя [c/ff00ff:Специальную способность] дарует усиление({$CommonItemTooltip.SecondDuration@9}),
который позволит вам [c/ff00ff:совершить рывок до 3 раз]({$CommonItemTooltip.Cooldown@10})
[c/87cefa:Самонаводящиеся огни], [c/ff00ff:порождённые рывком, не восстанавливают ману, Использование специальной способности требует {11} маны]
[c/ff7f50:Фрагмент очарования] наносит {4}x урона({$CommonItemTooltip.Cooldown@5}) и {6}% урона от максимального здоровья цели, вплоть до {7}
[c/ff7f50:Разделение] увеличивает магический урон на {8}%({$CommonItemTooltip.SecondDuration@12})
[c/87cefa:Самонаводящееся пламя] берёт в приоритет [c/ff7f50:разделённые] цели
[c/87cefa:Сфера] наносит на {0}% больше урона и на {1}% больше урона на обратном пути
[c/87cefa:Самонаводящееся пламя восстанавливает половину затраченной маны при попадании]
Лишь первое попадание [c/87cefa:сферы] и первое попадание на обратном пути дарует стаки [c/87cefa:эссенции похитителя]
Даёт 2 стака [c/87cefa:Эссенции похитителя] при крит. ударе
Также есть шанс {2}% получить стак [c/87cefa:Эссенции похитиеля] при убийстве врага
Полная [c/87cefa:Сфера] наносит {3}% бонусного урона, а также её исцеление усиливается от максимума маны и магического урона
```

**zh-Hans** (current):

```text
按[c/ff00ff:特殊能力键]获得增益，允许你[c/ff00ff:冲刺三次]
增益{$CommonItemTooltip.SecondDuration@9}，{$CommonItemTooltip.Cooldown@10}
[c/ff00ff:冲刺产生的][c/87cefa:追踪火焰][c/ff00ff:命中敌人时不恢复魔力，激活特殊能力消耗 {11} 魔力]
[c/ff7f50:爱心]每次攻击造成 {4} 倍伤害({$CommonItemTooltip.Cooldown@5}) 和 {6}% 最高生命值伤害，最高到 {7}
[c/ff7f50:割裂]的敌人受到的魔法伤害增加 {8}% ({$CommonItemTooltip.SecondDuration@12})
[c/87cefa:追踪火焰]优先攻击[c/ff7f50:割裂]的敌人
[c/87cefa:魔法珠]额外造成 {0}% 的伤害，返回玩家时额外造成 {1}%
[c/87cefa:追踪火焰命中敌人时回复其消耗的一半魔力]
只有[c/87cefa:魔法珠]投出和返回时造成的第一次伤害可以获得[c/87cefa:精华窃取]堆叠
如果暴击则获得两层[c/87cefa:精华窃取]堆叠
击杀敌人时有 {2}% 的几率获得一层[c/87cefa:精华窃取]堆叠
满堆叠层数的[c/87cefa:魔法珠]对敌人额外造成 {3}% 的伤害，同时其治疗量由玩家最大魔力值和魔法伤害决定
```

### `Mods.tsorcRevamp.Items.ScorchingPoint.Details` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-06-02, `892493e76` Adjusted Runeterra Gauntlets(Scorching Point and its upgrades)

English diff for ru-RU:

```diff
 The [c/b22222:ashen dragon] itself uses half a minion slot and scales with the amount of [c/b22222:fireballs] summoned
 [c/b22222:Dragon's hits apply the Scorched debuff and can mark enemies]
 Each hit has a {0}% [c/b22222:chance to add a mark], 6 Marks can be [c/b22222:detonated by hitting the enemy with a fireball]
 [c/b22222:Detonating marks] will increase the debuff's strength for {1} seconds
-The empowered debuff [c/b22222:increases fireball damage] and damage over time
+The empowered debuff [c/b22222:greatly increases fireball damage] and damage over time(+{6}%)
```

Changed:

- `[c/b22222:increases` → `[c/b22222:greatly increases`
- `time` → `time(+{6}%)`

English diff for zh-Hans:

```diff
 The [c/b22222:ashen dragon] itself uses half a minion slot and scales with the amount of [c/b22222:fireballs] summoned
 [c/b22222:Dragon's hits apply the Scorched debuff and can mark enemies]
 Each hit has a {0}% [c/b22222:chance to add a mark], 6 Marks can be [c/b22222:detonated by hitting the enemy with a fireball]
 [c/b22222:Detonating marks] will increase the debuff's strength for {1} seconds
-The empowered debuff [c/b22222:increases fireball damage], damage over time and [c/b22222:applies {2}% summon tag critical strike chance]
+The empowered debuff [c/b22222:greatly increases fireball damage] and damage over time(+{6}%)
```

Changed:

- `[c/b22222:increases` → `[c/b22222:greatly increases`
- `damage],` → `damage] and`
- `time and [c/b22222:applies {2}% summon tag critical strike chance]` → `time(+{6}%)`

**ru-RU** (current):

```text
Сам [c/b22222:пепельный дракон] использует половину слота для миньонов и усиливается от количества попаданий призванных [c/b22222:огненных шаров]
[c/b22222:Удары дракона могут наложить дебафф "выженный", и может пометить врагов]
каждый удар с вероятностью {0}% [c/b22222:добавляет метку], 6 меток можно [c/b22222:сдетонировать, ударив по врагу огненным шаром]
[c/b22222:Детонирующие метки увеличат силу дебаффов на {1} секунд
Усиленный дебафф [c/b22222:увеличивает урон огненных шаров] и урон, наносимый со временем
```

**zh-Hans** (current):

```text
[c/b22222:灰烬领主]本身消耗半个召唤栏，并根据召唤的[c/b22222:火球]数量进行缩放
[c/b22222:灰烬领主对敌人施加炽热减益，并且可以标记敌人]
每次攻击有 {0}% [c/b22222:几率施加标记]，标记 6 次后的敌人[c/b22222:被火球攻击可引发爆炸]
[c/b22222:引爆后的标记]会增强炽热减益的效果，持续 {1} 秒
炽热减益[c/b22222:增加火球的伤害]，[c/b22222:施加 {2}% 的召唤标记暴击率]，并对敌人造成持续伤害
```

### `Mods.tsorcRevamp.Items.InterstellarVesselGauntlet.Keybind2`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-17, `9c2c80882` Modded minion changes in preparation of 1.4.5

```diff
- to activate [c/7fffd4:Turboboost], overclocking the [c/b22222:vessel] and it's [c/b22222:spaceships]
+ to activate/deactivate [c/7fffd4:Turboboost], overclocking the [c/b22222:vessel] and it's [c/b22222:spaceships]
```

Changed:

- `activate` → `activate/deactivate`

**ru-RU** (current):

```text
 чтобы активировать [c/7fffd4:турбо усиление], разгоняя [c/b22222:судно] и [c/b22222:космические корабли]
```

**zh-Hans** (current):

```text
 启用[c/7fffd4:涡轮增压]，超载[c/b22222:霸天龙神]和[c/b22222:飞船]
```

### `Mods.tsorcRevamp.Items.InterstellarVesselGauntlet.Details` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-17, `9c2c80882` Modded minion changes in preparation of 1.4.5

English diff for ru-RU:

```diff
 [c/7fffd4:Turboboost increases spaceships movement speed and increases the vessel's attack speed]
 [c/7fffd4:Turboboost adds {2}% max health damage on-hit, up to {3}]
-[c/7fffd4:Turboboost will actively drain your mana and will be cancelled by Mana Sickness]
+[c/7fffd4:Turboboost({$CommonItemTooltip.Cooldown@7}) gets refunded if it is deactivated early]
 The [c/b22222:vessel] itself uses half a minion slot and scales with the amount of [c/b22222:spaceships] summoned
 [c/b22222:Vessel's hits apply the Shocked debuff and can mark enemies]
 Each hit has a {0}% [c/b22222:chance to add a mark], 6 Marks can be [c/b22222:detonated by hitting the enemy with a spaceship]
 [c/b22222:Detonating marks] will increase the debuff's strength for {1} seconds
-The empowered debuff [c/b22222:increases spaceship damage] and damage over time
+The empowered debuff [c/b22222:greatly increases spaceship damage] and damage over time(+{6}%)
```

Changed:

- `[c/7fffd4:Turboboost will actively drain your mana and will be cancelled by Mana Sickness]` → `[c/7fffd4:Turboboost({$CommonItemTooltip.Cooldown@7}) gets refunded if it is deactivated early]`
- `[c/b22222:increases` → `[c/b22222:greatly increases`
- `time` → `time(+{6}%)`

English diff for zh-Hans:

```diff
 [c/7fffd4:Turboboost increases spaceships movement speed and increases the vessel's attack speed]
-[c/7fffd4:Turboboost adds {3}% max health damage on-hit, up to {4}]
-[c/7fffd4:Turboboost will actively drain your mana and will be cancelled by Mana Sickness]
+[c/7fffd4:Turboboost adds {2}% max health damage on-hit, up to {3}]
+[c/7fffd4:Turboboost({$CommonItemTooltip.Cooldown@7}) gets refunded if it is deactivated early]
 The [c/b22222:vessel] itself uses half a minion slot and scales with the amount of [c/b22222:spaceships] summoned
 [c/b22222:Vessel's hits apply the Shocked debuff and can mark enemies]
 Each hit has a {0}% [c/b22222:chance to add a mark], 6 Marks can be [c/b22222:detonated by hitting the enemy with a spaceship]
 [c/b22222:Detonating marks] will increase the debuff's strength for {1} seconds
-The empowered debuff [c/b22222:increases spaceship damage], damage over time and [c/b22222:applies {2}% summon tag critical strike chance]
+The empowered debuff [c/b22222:greatly increases spaceship damage] and damage over time(+{6}%)
```

Changed:

- `{3}%` → `{2}%`
- `{4}]` → `{3}]`
- `[c/7fffd4:Turboboost will actively drain your mana and will be cancelled by Mana Sickness]` → `[c/7fffd4:Turboboost({$CommonItemTooltip.Cooldown@7}) gets refunded if it is deactivated early]`
- `[c/b22222:increases` → `[c/b22222:greatly increases`
- `damage],` → `damage] and`
- `time and [c/b22222:applies {2}% summon tag critical strike chance]` → `time(+{6}%)`

**ru-RU** (current):

```text
[c/7fffd4:Турбо усиление увеличивает скорость передвижения космических кораблей и увеличивает скорость атаки судна]
[c/7fffd4:Турбо усиление добавляет бонус урона в {2}% от максимального здоровья, вплоть до {3}]
[c/7fffd4:Турбо усиление будет активно истощать вашу ману и будет отменено из-за мановой болезни]
Само [c/b22222:судно] использует половину слота для миньонов и усиливается от количества попаданий призванных [c/b22222:космических кораблей]
[c/b22222:Удары судна наносят дебафф "Шокирован" и могут пометить врагов]
каждый удар с вероятностью {0}% [c/b22222:добавляет метку], 6 меток можно [c/b22222:сдетонировать, ударив по врагу космическим кораблём]
[c/b22222:Детонирующие метки увеличат силу дебаффов на {1} секунд
Усиленный дебафф [c/b22222:увеличивает урон космических кораблей] и урон, наносимый со временем
```

**zh-Hans** (current):

```text
[c/7fffd4:涡轮增压增加飞船的移动速度和霸天龙神的攻击速度]
[c/7fffd4:涡轮增压额外造成相当于目标最大生命值 {3}% 的伤害，最多 {4}]
[c/7fffd4:涡轮增压会快速消耗你的魔力，并在触发魔力病时结束]
[c/b22222:霸天龙神]本身消耗半个召唤栏，并根据召唤的[c/b22222:飞船]数量进行缩放
[c/b22222:霸天龙神对敌人施加出其不意减益，并且可以标记敌人]
每次攻击有 {0}% [c/b22222:几率施加标记]，标记 6 次后的敌人[c/b22222:被飞船攻击可引发爆炸]
[c/b22222:引爆后的标记]会增强出其不意减益的效果，持续 {1} 秒
出其不意减益[c/b22222:增加飞船的伤害]，[c/b22222:施加 {2}% 的召唤标记暴击率]，并对敌人造成持续伤害
```

### `Mods.tsorcRevamp.Items.CenterOfTheUniverse.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-17, `9c2c80882` Modded minion changes in preparation of 1.4.5

```diff
 Summons the [c/b22222:Star Forger] which will chase after your cursor and [c/b22222:breathe fire] at nearby enemies
 Summons [c/b22222:stars] which rotate around you and damage enemies in their way
 {$Items.ScorchingPoint.Keybind1}Special Ability{$Items.ScorchingPoint.Keybind2}
 The [c/b22222:stars] scale with {0}% Tag Damage, but the [c/b22222:Star Forger] only with {1}% Tag Damage
 {$Items.CenterOfTheUniverse.Keybind1}Special Ability{$Items.CenterOfTheUniverse.Keybind2}
-Gain [c/00008b:Stardust] every time you detonate a [c/b22222:mark]
-<right> to summon a [c/00008b:Grand Comet] at your cursor's location for [c/00008b:10 Stardust]
-[c/00008b:'Leave them in awe at the sight of the Star Forger']
+Gain [c/493dff:Stardust] every time you detonate a [c/b22222:mark]
+<right> to summon a [c/493dff:Grand Comet] at your cursor's location for [c/493dff:10 Stardust]
+[c/493dff:'Leave them in awe at the sight of the Star Forger']
```

Changed:

- `[c/00008b:Stardust]` → `[c/493dff:Stardust]`
- `[c/00008b:Grand` → `[c/493dff:Grand`
- `[c/00008b:10` → `[c/493dff:10`
- `[c/00008b:'Leave` → `[c/493dff:'Leave`

**ru-RU** (current):

```text
Призывает [c/b22222:кузнеца звёзд], который будет преследовать ваш курсор и [c/b22222:изрыгать огонь] на ближайших врагов
Призывает [c/b22222:звёзды], которые вращаются вокруг вас и наносят урон врагам на своем пути
{$Items.ScorchingPoint.Keybind1}Специальная способность{$Items.ScorchingPoint.Keybind2}
[c/b22222:Звёзды] получают {0}% урона метки, но [c/b22222:кузнец звёзд] только {1}% урона метки
{$Items.CenterOfTheUniverse.Keybind1}Специальная способность{$Items.CenterOfTheUniverse.Keybind2}
Даёт [c/00008b:звёздную пыль] каждый раз, когда вы взрываете [c/b22222:меченого врага]
<right> чтобы вызвать [c/00008b:великую комету] в позиции вашего курсора, за [c/00008b:10 звёздной пыли]
[c/00008b:'Заставьте их трепетать при виде кузница звёзд']
```

**zh-Hans** (current):

```text
召唤一条[c/b22222:铸星龙王]，它会向你的光标移动并向附近的敌人[c/b22222:喷火]
召唤围绕你旋转的[c/b22222:星芒]，并伤害经过的敌人
{$Items.ScorchingPoint.Keybind1}Special Ability{$Items.ScorchingPoint.Keybind2}
[c/b22222:星芒]受到 {0}% 标记伤害加成，[c/b22222:铸星龙王]只受到 {1}% 加成
{$Items.CenterOfTheUniverse.Keybind1}Special Ability{$Items.CenterOfTheUniverse.Keybind2}
每次[c/b22222:引爆标记]会获得[c/00008b:星尘]
<right>消耗 10 [c/00008b:星尘]在光标位置召唤一颗[c/00008b:彗星]
[c/00008b:“瑟缩，崇拜，惊叹。人之常态，概莫能外。”]
```

### `Mods.tsorcRevamp.Items.CenterOfTheUniverse.Keybind2`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-17, `9c2c80882` Modded minion changes in preparation of 1.4.5

```diff
- to activate [c/7fffd4:Turboboost], accelerating the [c/b22222:Star Forger] and it's [c/b22222:stars]
+ to activate/deactivate [c/7fffd4:Turboboost], accelerating the [c/b22222:Star Forger] and it's [c/b22222:stars]
```

Changed:

- `activate` → `activate/deactivate`

**ru-RU** (current):

```text
 чтобы активировать [c/7fffd4:турбо усиление], ускоряя [c/b22222:кузнеца звёзд] и его [c/b22222:звёзды]
```

**zh-Hans** (current):

```text
 启用[c/7fffd4:涡轮增压]，超载[c/b22222:铸星龙王]和[c/b22222:星芒]
```

### `Mods.tsorcRevamp.Items.CenterOfTheUniverse.Details` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-17, `9c2c80882` Modded minion changes in preparation of 1.4.5

English diff for ru-RU:

```diff
-[c/00008b:Grand Comet deals massive damage in a huge area and leaves behind a shockwave]
-[c/00008b:Grand Comet applies the Awestruck debuff]({$CommonItemTooltip.SecondDuration@4})
-This further increases star damage and deals massive damage over time
-Star damage increase is {5}%
+[c/493dff:Grand Comet deals massive damage in a huge area and leaves behind a shockwave]
+[c/493dff:Grand Comet applies the Awestruck debuff]({$CommonItemTooltip.SecondDuration@4})
+This increases star damage even further beyond and deals massive damage over time(+{5}%)
 [c/7fffd4:Turboboost increases star movement speed and increases the Star Forger's attack speed]
 [c/7fffd4:Turboboost adds {2}% max health damage on-hit, up to {3}]
-[c/7fffd4:Turboboost will actively drain your mana and will be cancelled by Mana Sickness]
+[c/7fffd4:Turboboost({$CommonItemTooltip.Cooldown@7}) gets refunded if it is deactivated early]
 The [c/b22222:Star Forger] itself uses half a minion slot and scales with the amount of [c/b22222:stars] summoned
 [c/b22222:Star Forger's hits apply the Sunburnt debuff and can mark enemies]
 Each hit has a {0}% [c/b22222:chance to add a mark], 6 Marks can be [c/b22222:detonated by hitting the enemy with a star]
 [c/b22222:Detonating marks] will increase the debuff's strength for {1} seconds
-The empowered debuff [c/b22222:increases star damage] and damage over time
+The empowered debuff [c/b22222:greatly increases star damage] and damage over time(+{6}%)
```

Changed:

- `[c/00008b:Grand` → `[c/493dff:Grand`
- `[c/00008b:Grand` → `[c/493dff:Grand`
- removed `further`
- added `even further beyond`
- `time ⏎ Star damage increase is {5}%` → `time(+{5}%)`
- `[c/7fffd4:Turboboost will actively drain your mana and will be cancelled by Mana Sickness]` → `[c/7fffd4:Turboboost({$CommonItemTooltip.Cooldown@7}) gets refunded if it is deactivated early]`
- `[c/b22222:increases` → `[c/b22222:greatly increases`
- `time` → `time(+{6}%)`

English diff for zh-Hans:

```diff
-[c/00008b:Grand Comet deals massive damage in a huge area and leaves behind a shockwave]
-[c/00008b:Grand Comet applies the Awestruck debuff]({$CommonItemTooltip.SecondDuration@5})
-This further increases star damage and deals massive damage over time
-Star damage increase is {6}%
+[c/493dff:Grand Comet deals massive damage in a huge area and leaves behind a shockwave]
+[c/493dff:Grand Comet applies the Awestruck debuff]({$CommonItemTooltip.SecondDuration@4})
+This increases star damage even further beyond and deals massive damage over time(+{5}%)
 [c/7fffd4:Turboboost increases star movement speed and increases the Star Forger's attack speed]
-[c/7fffd4:Turboboost adds {3}% max health damage on-hit, up to {4}]
-[c/7fffd4:Turboboost will actively drain your mana and will be cancelled by Mana Sickness]
+[c/7fffd4:Turboboost adds {2}% max health damage on-hit, up to {3}]
+[c/7fffd4:Turboboost({$CommonItemTooltip.Cooldown@7}) gets refunded if it is deactivated early]
 The [c/b22222:Star Forger] itself uses half a minion slot and scales with the amount of [c/b22222:stars] summoned
 [c/b22222:Star Forger's hits apply the Sunburnt debuff and can mark enemies]
 Each hit has a {0}% [c/b22222:chance to add a mark], 6 Marks can be [c/b22222:detonated by hitting the enemy with a star]
 [c/b22222:Detonating marks] will increase the debuff's strength for {1} seconds
-The empowered debuff [c/b22222:increases star damage], damage over time and [c/b22222:applies {2}% summon tag critical strike chance]
+The empowered debuff [c/b22222:greatly increases star damage] and damage over time(+{6}%)
```

Changed:

- `[c/00008b:Grand` → `[c/493dff:Grand`
- `[c/00008b:Grand` → `[c/493dff:Grand`
- `debuff]({$CommonItemTooltip.SecondDuration@5})` → `debuff]({$CommonItemTooltip.SecondDuration@4})`
- removed `further`
- added `even further beyond`
- `time ⏎ Star damage increase is {6}%` → `time(+{5}%)`
- `{3}%` → `{2}%`
- `{4}]` → `{3}]`
- `[c/7fffd4:Turboboost will actively drain your mana and will be cancelled by Mana Sickness]` → `[c/7fffd4:Turboboost({$CommonItemTooltip.Cooldown@7}) gets refunded if it is deactivated early]`
- `[c/b22222:increases` → `[c/b22222:greatly increases`
- `damage],` → `damage] and`
- `time and [c/b22222:applies {2}% summon tag critical strike chance]` → `time(+{6}%)`

**ru-RU** (current):

```text
[c/00008b:Великая комета наносит огромный урон на огромной площади и оставляет после себя ударную волну]
[c/00008b:Великая комета накладывает дебафф "Благоговейный трепет"]({$CommonItemTooltip.SecondDuration@4})
Это ещё больше увеличивает урон от звёзд и со временем наносит огромный урон
Урон звёзд увеличивается на {5}%
[c/7fffd4:Турбо усиление увеличивает скорость передвижения звёзд и увеличивает скорость кузнеца звёзд]
[c/7fffd4:Турбо усиление добавляет бонус урона в {2}% от максимального здоровья, вплоть до {3}]
[c/7fffd4:Турбо усиление будет активно истощать вашу ману и будет отменено из-за мановой болезни]
Сам [c/b22222:кузнец звёзд] использует половину слота для миньонов и усиливается от количества попаданий призванных [c/b22222:звёзд]
[c/b22222:Удары Звездного фальсификатора накладывают дебафф "солнечный ожог" и могут отмечать врагов]
каждый удар с вероятностью {0}% [c/b22222:добавляет метку], 6 меток можно [c/b22222:сдетонировать, ударив по врагу Кузнецом звёзд]
[c/b22222:Детонирующие метки увеличат силу дебаффов на {1} секунд
Усиленный дебафф [c/b22222:увеличивает урон звёзд] и урон, наносимый со временем
```

**zh-Hans** (current):

```text
[c/00008b:彗星在大范围内造成巨大破坏并释放冲击波]
[c/00008b:彗星施加惶恐减益]({$CommonItemTooltip.SecondDuration@5})
惶恐减益会增加星芒伤害，并造成高额持续伤害
增加 {6}% 星芒伤害
[c/7fffd4:涡轮增压增加星芒的移动速度和铸星龙王的攻击速度]
[c/7fffd4:涡轮增压额外造成相当于目标最大生命值 {3}% 的伤害，最多 {4}]
[c/7fffd4:涡轮增压会快速消耗你的魔力，并在触发魔力病时结束]
[c/b22222:铸星龙王]本身消耗半个召唤栏，并根据召唤的[c/b22222:星芒]数量进行缩放
[c/b22222:铸星龙王对敌人施加日照减益，并且可以标记敌人]
每次攻击有 {0}% [c/b22222:几率施加标记]，标记 6 次后的敌人[c/b22222:被星芒攻击可引发爆炸]
[c/b22222:引爆后的标记]会增强日照减益的效果，持续 {1} 秒
日照减益[c/b22222:增加星芒的伤害]，[c/b22222:施加 {2}% 的召唤标记暴击率]，并对敌人造成持续伤害
```

### `Mods.tsorcRevamp.Items.AdventurersCard.Summon`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-17, `9c2c80882` Modded minion changes in preparation of 1.4.5

```diff
 [c/6efaff:Summon Stats:]
 Total Damage Multiplier: {0}%
 Critical Strike Chance: {1}%
 Attack Speed: {2}%
 Armor Penetration: {3}
 Total Whip Damage Multiplier: {4}%
 Minion Slots: {5}
 Turret Slots: {6}
 Whip Range: {7}%
-Tips: Hitting enemies with the tip of a whip always results in a crit
-Whip tip critical strike damage is {8}%
-Minions can crit based on your summon critical strike chance
+Tips: Hitting enemies with the tip of a whip increases damage dealt by {8}%
+All summon damage respects summon critical strike chance
```

Changed:

- `always results in a crit ⏎ Whip tip critical strike` → `increases`
- `is` → `dealt by`
- `Minions can crit based on your` → `All summon damage respects`

**ru-RU** (current):

```text
[c/6efaff:Характеристики призывателя:]
Множитель урона: {0}%
Шанс крит. удара: {1}%
Скорость атаки: {2}%
Пробивание брони: {3}
Множитель урона хлыста: {4}%
Слоты призываемых существ: {5}
Слотов для часовых: {6}
Дальность хлыста: {7}%
Советы: Удар по врагам кончиком хлыста всегда будет критичиеским
Крит. урон от удара наконечником хлыста составляет {8}%
Миньоны могут наносить крит. урон в зависимости от вашего шанса крит. удара
```

**zh-Hans** (current):

```text
[c/6efaff:<召唤>]
总伤害倍率: {0}%
暴击率: {1}%
攻击速度: {2}%
护甲穿透: {3}
鞭子伤害倍率: {4}%
召唤栏: {5}
哨兵栏: {6}
鞭子范围: {7}%
用鞭子的尖端击中敌人总是会造成暴击
鞭子尖端的暴击伤害为 {8}%
召唤物受到玩家的召唤暴击率加成
```

### `Mods.tsorcRevamp.Items.AncestralSpiritBag.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-12-09, `d7b0d1545` make Sundial, Moondial placeable

```diff
-Ancestral Spirit Bag
+Treasure Bag (Ancestral Spirit)
```

Changed:

- `Ancestral Spirit` → `Treasure`
- added `(Ancestral Spirit)`

**zh-Hans** (current):

```text
宝藏袋（先祖之灵）
```

### `Mods.tsorcRevamp.Items.LichBone.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-04-14, `bd5f2da23` A lot of changes and content!!

```diff
-
+'The hard bones of a lich infused with necromantic power and a stink smell of ichor'
```

Changed:

- added `'The hard bones of a lich infused with necromantic power and a stink smell of ichor'`

**ru-RU** (current):

```text

```

**zh-Hans** (current):

```text

```

### `Mods.tsorcRevamp.Items.PinwheelBag.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-12-09, `d7b0d1545` make Sundial, Moondial placeable

```diff
-Pinwheel Bag
+Treasure Bag (Pinwheel Bag)
```

Changed:

- `Pinwheel` → `Treasure`
- added `(Pinwheel Bag)`

**zh-Hans** (current):

```text
宝藏袋（三焱家族）
```

### `Mods.tsorcRevamp.Items.KrakenFlesh.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-04-14, `bd5f2da23` A lot of changes and content!!

```diff
-
+'A moist flesh of a kraken that is both resistant and elastic'
```

Changed:

- added `'A moist flesh of a kraken that is both resistant and elastic'`

**ru-RU** (current):

```text

```

**zh-Hans** (current):

```text

```

### `Mods.tsorcRevamp.Items.EstusRing.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-09-06, `226f5e580` Added shield benefits to Ring of Favor and Protection, buffing and fixing it

```diff
 Increases the Estus Flask's healing by {0} flat and {1}% of your max life
+{$CommonItemTooltip.EstusDrinkTimeReduction@2}
```

Changed:

- added `⏎ {$CommonItemTooltip.EstusDrinkTimeReduction@2}`

**ru-RU** (current):

```text
Увеличивает количество здоровья, восстанавливаемого флягой эстуса на {0} и на {1}% от вашего максимального здоровья
```

**zh-Hans** (current):

```text
元素瓶的生命恢复效果固定增加 {0} 点，并额外恢复最大生命值的 {1}%
```

### `Mods.tsorcRevamp.Items.RingOfFavorAndProtection.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-09-06, `226f5e580` Added shield benefits to Ring of Favor and Protection, buffing and fixing it

```diff
-Increases HP by 40 and stamina by 15 while worn
+{$CommonItemTooltip.IncreasesMaxLifeBy} & {$CommonItemTooltip.MaxStaminaFlat@1}
+[c/ffbf00:Reduces the movement penalty for holding a shield by {2}%]
+[c/ffbf00:Increases stamina regeneration while your shield is raised by {3}% of its normal rate]
 It seems as though Pinwheel held this to prevent
 the necklace it wore from releasing its full power
```

Changed:

- `Increases HP` → `{$CommonItemTooltip.IncreasesMaxLifeBy} & {$CommonItemTooltip.MaxStaminaFlat@1} ⏎ [c/ffbf00:Reduces the movement penalty for holding a shield`
- `40 and` → `{2}%] ⏎ [c/ffbf00:Increases`
- added `regeneration while your shield is raised`
- `15 while worn` → `{3}% of its normal rate]`

**ru-RU** (current):

```text
Увеличивает здоровье на 40 и выносливость на 15 при ношении
Похоже, что Вертопрах носил его, чтобы
ожерелье, которое он носил, не высвободило всю свою силу
```

**zh-Hans** (current):

```text
装备时增加 40 点生命值和 15 点耐力
看起来三焱家族戴着这个
是为了防止它释放出全部的力量
```

### `Mods.tsorcRevamp.Items.FieryScale.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-04-14, `bd5f2da23` A lot of changes and content!!

```diff
-
+'Thick, incandescent scale of a half-serpent that burns on contact'
```

Changed:

- added `'Thick, incandescent scale of a half-serpent that burns on contact'`

**ru-RU** (current):

```text

```

**zh-Hans** (current):

```text

```

### `Mods.tsorcRevamp.Items.SupremeDragoonLash.Tooltip0`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-17, `9c2c80882` Modded minion changes in preparation of 1.4.5

```diff
 {$CommonItemTooltip.PercentSummonTagCritChance}
 Reforged to its full potential
-Breathe fire at enemies struck by this whip and a minion on a cooldown
-Refresh the cooldown by whipping an enemy
+Breathe rainbow flames at enemies struck by this whip or a minion on a cooldown
+Lower the cooldown by whipping an enemy
+Whip tip hits instantly breathe empowered rainbow flames at the enemy
 {$CommonItemTooltip.Whips}
```

Changed:

- `fire` → `rainbow flames`
- `and` → `or`
- `Refresh` → `Lower`
- added `Whip tip hits instantly breathe empowered rainbow flames at the enemy ⏎`

**ru-RU** (current):

```text
{$CommonItemTooltip.PercentSummonTagCritChance}
Перекована на полную мощность
Изрыгает огонь на врагов, пораженных этой плетью, а также миньонами во время перезарядки
Обновляет время перезарядки, нанося удары по врагу
{$CommonItemTooltip.Whips}
```

**zh-Hans** (current):

```text
{$CommonItemTooltip.PercentSummonTagCritChance}
展现其真正的力量
击中敌人时释放火焰吐息和仆从
击中敌人刷新冷却
{$CommonItemTooltip.Whips}
```

### `Mods.tsorcRevamp.Items.WitchkingMace.Tooltip` ⚠️

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-14, `d6c5394a5` Last commit for 1.4.4, a lot of progress but a lot of wip stuffs too!

```diff
+{$CommonItemTooltip.Whips}
 Inflicts 1 random whip debuff each hit
```

Changed:

- added `{$CommonItemTooltip.Whips} ⏎`

**ru-RU** (current):

```text
Накладывает 1 случайный дебафф хлыстов при ударе
```

**zh-Hans** (current):

```text
每次攻击随机施加1个鞭子减益
```

### `Mods.tsorcRevamp.Items.TaintedBow.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-04-14, `bd5f2da23` A lot of changes and content!!

```diff
-The arrows fired are poisoned and conver wooden arrows into tainted arrows
+The arrows fired are poisoned and converts wooden arrows into tainted arrows
 Tainted arrows deal more damage and inflict shadowflame on nature enemies
```

Changed:

- `conver` → `converts`

**ru-RU** (current):

```text
Отравляет выпускаемые стрелы, а также превращает деревянный стрелы в испорченные
Испорченные стрелы наносят больше урона и накладывают теневое пламя на природных врагов
```

**zh-Hans** (current):

```text
施加中毒，并将木箭转化为污染箭
污染箭造成额外伤害，并对自然生物类的敌人附加暗影焰
```

### `Mods.tsorcRevamp.Items.SpearOfMage.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-04-14, `bd5f2da23` A lot of changes and content!!

```diff
-
+A magic spear that passes through solid walls
+Inflicts 'dissolving', which stacks to increase enemy damage taken by up to 10%
```

Changed:

- added `A magic spear that passes through solid walls ⏎ Inflicts 'dissolving', which stacks to increase enemy damage taken by up to 10%`

**ru-RU** (current):

```text

```

**zh-Hans** (current):

```text

```

### `Mods.tsorcRevamp.Items.DiamondCrusher.Tooltip`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-05-18, `1f9aac2f6` Added DiamondCrusherShockwave & miscellanous changes

```diff
-
+Creates an shockwave upon hitting solid blocks that damages nearby enemies
```

Changed:

- added `Creates an shockwave upon hitting solid blocks that damages nearby enemies`

**ru-RU** (current):

```text

```

**zh-Hans** (current):

```text

```

### `Mods.tsorcRevamp.Items.SunderedMoon.Tooltip`

Needs update in: **zh-Hans** · English last changed: 2026-04-14, `bd5f2da23` A lot of changes and content!!

```diff
 A legendary tome drawing on lunar power.
 Casts powerful lunar energy streaks
-Can summon stars on hit
```

Changed:

- removed `⏎ Can summon stars on hit`

**zh-Hans** (current):

```text
借月华之力写就的传奇典籍
释放强效月能光束
命中时召唤星辰坠落
```

## Buffs

### `Mods.tsorcRevamp.Buffs.VanillaBuffs.ManaSickness`

Needs update in: **zh-Hans** · English last changed: 2024-05-30, `5948ae6e4` make soapstone more readable

```diff
-SolarShield: "{$CommonItemTooltip.DRStat}, repel enemies when taking damage"
+Reduces damage dealt by{0}%
+Reduces melee damage dealt by {1}%
```

Changed:

- `SolarShield: "{$CommonItemTooltip.DRStat}, repel enemies when taking damage"` → `Reduces damage dealt by{0}% ⏎ Reduces melee damage dealt by {1}%`

**zh-Hans** (current):

```text
减少 {0}% 魔法、召唤、远程伤害
减少 {1}% 近战伤害
```

### `Mods.tsorcRevamp.Buffs.LethalTempo.Description`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-09-07, `cf790ad46` Lethat Tempo revamp

```diff
-Your melee speed is increased
+
```

Changed:

- removed `Your melee speed is increased`

**ru-RU** (current):

```text
Ваша скорость в ближнем бою увеличена
```

**zh-Hans** (current):

```text
提高近战攻速
```

### `Mods.tsorcRevamp.Buffs.AbyssInferno.DisplayName`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-06-14, `c8cac6bb1` Abyssal Inferno expansion, debuffs stuffs, other stuffs

```diff
-Abyss Inferno
+Abyssal Inferno
```

Changed:

- `Abyss` → `Abyssal`

**ru-RU** (current):

```text
Инферно бездны
```

**zh-Hans** (current):

```text
孽火
```

### `Mods.tsorcRevamp.Buffs.AbyssInferno.Description`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-06-14, `c8cac6bb1` Abyssal Inferno expansion, debuffs stuffs, other stuffs

```diff
-Mods.tsorcRevamp.Buffs.AbyssInferno.Description
+Greatly losing life!
```

Changed:

- `Mods.tsorcRevamp.Buffs.AbyssInferno.Description` → `Greatly losing life!`

**ru-RU** (current):

```text
Mods.tsorcRevamp.Buffs.AbyssInferno.Description
```

**zh-Hans** (current):

```text

```

### `Mods.tsorcRevamp.Buffs.TrinityBuff.DisplayName`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-20, `8c5b403e6` Reworked Trinity

```diff
-Natural Defenses
+Spellblade
```

Changed:

- `Natural Defenses` → `Spellblade`

**ru-RU** (current):

```text
Природная защита
```

**zh-Hans** (current):

```text
自然防御
```

### `Mods.tsorcRevamp.Buffs.TrinityBuff.Description`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-20, `8c5b403e6` Reworked Trinity

```diff
-Your natural defenses have been activated!
-Grants +4 life regeneration, 12 defense, 15% damage and 20% movement speed
+Your crit chance is greatly increased
```

Changed:

- `natural defenses have been activated! ⏎ Grants +4 life regeneration, 12 defense, 15% damage and 20% movement speed` → `crit chance is greatly increased`

**ru-RU** (current):

```text
Ваша природная защита активирована!
Она даёт +4 к регенерации здоровья, 12 ед. защиты, 15% урона и 20% скорости передвижения
```

**zh-Hans** (current):

```text
你的自然防御本能已激活！
增加 4 点生命再生、12 点防御、15% 伤害和 20% 移动速度
```

### `Mods.tsorcRevamp.Buffs.AbyssalSinking.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2026-02-28, `6e3324379` Big patch, Reworked Chaos for real this time (thanks to Boatsoon, still wip though)

```diff
-Abyssal Sinking
+Sinking
```

Changed:

- removed `Abyssal`

**zh-Hans** (current):

```text
深渊沉溺
```

## Projectiles

### `Mods.tsorcRevamp.Projectiles.CMSCrescent.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2024-01-21, `7c85a6912` A bunch of fixes + Code Cleanup

```diff
-C M S Crescent
+Crescent Moon Sword
```

Changed:

- removed `C M S`
- added `Moon Sword`

**zh-Hans** (current):

```text
C M S月牙
```

### `Mods.tsorcRevamp.Projectiles.EphemeralThrowingAxeProj.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Ephemeral Throwing Axe Proj
+Ephemeral Throwing Axe
```

Changed:

- removed `Proj`

**zh-Hans** (current):

```text
夺命投斧
```

### `Mods.tsorcRevamp.Projectiles.EphemeralThrowingAxeProj2.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Ephemeral Throwing Axe Proj2
+Ephemeral Throwing Axe 2
```

Changed:

- `Proj2` → `2`

**zh-Hans** (current):

```text
附魔夺命投斧
```

### `Mods.tsorcRevamp.Projectiles.FirebombProj.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Firebomb Proj
+Firebomb
```

Changed:

- removed `Proj`

**zh-Hans** (current):

```text
火焰炸弹
```

### `Mods.tsorcRevamp.Projectiles.ForgottenRisingSunProj.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Forgotten Rising Sun Proj
+Forgotten Rising Sun
```

Changed:

- removed `Proj`

**zh-Hans** (current):

```text
被遗忘的旭日
```

### `Mods.tsorcRevamp.Projectiles.BarbarousThornBladeProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Barbarous Thorn Blade Projectile
+Barbarous Thorn
```

Changed:

- removed `Blade Projectile`

**zh-Hans** (current):

```text
野蛮刺刃
```

### `Mods.tsorcRevamp.Projectiles.BarrowBladeProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Barrow Blade Projectile
+Barrow Blade
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
巴罗之刃
```

### `Mods.tsorcRevamp.Projectiles.ClaiomhSolaisProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Claiomh Solais Projectile
+Claiomh Solais
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
克拉乌·索拉斯
```

### `Mods.tsorcRevamp.Projectiles.LaevateinnProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Laevateinn Projectile
+Laevateinn
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
莱瓦汀
```

### `Mods.tsorcRevamp.Projectiles.WyrmkillerProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Wyrmkiller Projectile
+Wyrmkiller
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
屠龙者
```

### `Mods.tsorcRevamp.Projectiles.YellowTailProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Yellow Tail Projectile
+Yellow Tail
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
黄尾
```

### `Mods.tsorcRevamp.Projectiles.AncientBloodLanceProj.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Ancient Blood Lance Proj
+Ancient Blood Lance
```

Changed:

- removed `Proj`

**zh-Hans** (current):

```text
远古血矛
```

### `Mods.tsorcRevamp.Projectiles.AncientDragonLanceProj.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Ancient Dragon Lance Proj
+Ancient Dragon Lance
```

Changed:

- removed `Proj`

**zh-Hans** (current):

```text
远古血矛
```

### `Mods.tsorcRevamp.Projectiles.AncientHolyLanceProj.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Ancient Holy Lance Proj
+Ancient Holy Lance
```

Changed:

- removed `Proj`

**zh-Hans** (current):

```text
古代圣枪
```

### `Mods.tsorcRevamp.Projectiles.CelestialLanceProj.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Celestial Lance Proj
+Celestial Lance
```

Changed:

- removed `Proj`

**zh-Hans** (current):

```text
天国长枪
```

### `Mods.tsorcRevamp.Projectiles.CobaltHalberdProj.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Cobalt Halberd Proj
+Cobalt Halberd
```

Changed:

- removed `Proj`

**zh-Hans** (current):

```text
钴蓝长戟
```

### `Mods.tsorcRevamp.Projectiles.DragoonLanceProj.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Dragoon Lance Proj
+Dragoon Lance
```

Changed:

- removed `Proj`

**zh-Hans** (current):

```text
龙骑枪
```

### `Mods.tsorcRevamp.Projectiles.ForgottenPearlSpearProj.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Forgotten Pearl Spear Proj
+Forgotten Pearl Spear
```

Changed:

- removed `Proj`

**zh-Hans** (current):

```text
被遗忘的珍珠矛
```

### `Mods.tsorcRevamp.Projectiles.ForgottenRadiantLanceProj.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Forgotten Radiant Lance Proj
+Forgotten Radiant Lance
```

Changed:

- removed `Proj`

**zh-Hans** (current):

```text
被遗忘的光辉之枪
```

### `Mods.tsorcRevamp.Projectiles.GaeBolgProj.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Gae Bolg Proj
+Gae Bolg
```

Changed:

- removed `Proj`

**zh-Hans** (current):

```text
盖博尔格
```

### `Mods.tsorcRevamp.Projectiles.LonginusProj.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Longinus Proj
+Longinus
```

Changed:

- removed `Proj`

**zh-Hans** (current):

```text
朗基努斯
```

### `Mods.tsorcRevamp.Projectiles.StarfallProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Starfall Projectile
+Starfall
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
流星雨
```

### `Mods.tsorcRevamp.Projectiles.ShatteredReflectionProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Shattered Reflection Projectile
+Shattered Reflection
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
碎镜反射
```

### `Mods.tsorcRevamp.Projectiles.CrystalNunchakuProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Crystal Nunchaku Projectile
+Crystal Nunchaku
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
水晶双截棍
```

### `Mods.tsorcRevamp.Projectiles.DetonationSignalProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Detonation Signal Projectile
+Detonation Signal
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
引爆信号
```

### `Mods.tsorcRevamp.Projectiles.DominatrixProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Dominatrix Projectile
+Dominatrix
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
主宰
```

### `Mods.tsorcRevamp.Projectiles.DragoonLashProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Dragoon Lash Projectile
+Dragoon Lash
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
龙纹鞭
```

### `Mods.tsorcRevamp.Projectiles.EnchantedWhipProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Enchanted Whip Projectile
+Enchanted Whip
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
附魔之鞭
```

### `Mods.tsorcRevamp.Projectiles.NightsCrackerProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Nights Cracker Projectile
+Nights Cracker
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
碎夜
```

### `Mods.tsorcRevamp.Projectiles.PolarisLeashProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Polaris Leash Projectile
+Polaris Leash
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
北极星之鞭
```

### `Mods.tsorcRevamp.Projectiles.PyrosulfateProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Pyrosulfate Projectile
+Pyrosulfate
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
热硫酸
```

### `Mods.tsorcRevamp.Projectiles.SearingLashProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Searing Lash Projectile
+Searing Lash
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
灼热之鞭
```

### `Mods.tsorcRevamp.Projectiles.TerraFallProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Terra Fall Projectile
+Terra Fall
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
泰拉之殇
```

### `Mods.tsorcRevamp.Projectiles.UrumiProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Urumi Projectile
+Urumi
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
软剑鞭
```

### `Mods.tsorcRevamp.Projectiles.PortlyPlateRollHitbox.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2024-01-21, `7c85a6912` A bunch of fixes + Code Cleanup

```diff
-Portly Plate Roll Hitbox
+Portly Plate Roll
```

Changed:

- removed `Hitbox`

**zh-Hans** (current):

```text
臃肿板甲翻滚
```

### `Mods.tsorcRevamp.Projectiles.PlasmaWhirlwindDashHitbox.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2024-01-21, `7c85a6912` A bunch of fixes + Code Cleanup

```diff
-Plasma Whirlwind Dash Hitbox
+Plasma Whirlwind
```

Changed:

- removed `Dash Hitbox`

**zh-Hans** (current):

```text
等离子旋风
```

### `Mods.tsorcRevamp.Projectiles.NightbringerDashHitbox.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2024-01-21, `7c85a6912` A bunch of fixes + Code Cleanup

```diff
-Nightbringer Dash Hitbox
+Nightbringer
```

Changed:

- removed `Dash Hitbox`

**zh-Hans** (current):

```text
夜之使者
```

### `Mods.tsorcRevamp.Projectiles.PhotonicDownpourProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Photonic Downpour Projectile
+Photonic Downpour
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
光子陷阵战机
```

### `Mods.tsorcRevamp.Projectiles.OilDroplet1.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2024-01-21, `7c85a6912` A bunch of fixes + Code Cleanup

```diff
-Oil Droplet1
+Oil Droplet
```

Changed:

- `Droplet1` → `Droplet`

**zh-Hans** (current):

```text
油滴
```

### `Mods.tsorcRevamp.Projectiles.OilDroplet2.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2024-01-21, `7c85a6912` A bunch of fixes + Code Cleanup

```diff
-Oil Droplet2
+Oil Droplet
```

Changed:

- `Droplet2` → `Droplet`

**zh-Hans** (current):

```text
油滴
```

### `Mods.tsorcRevamp.Projectiles.OilPotProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Oil Pot Projectile
+Oil Pot
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
油壶
```

### `Mods.tsorcRevamp.Projectiles.OilPotShard1.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2024-01-21, `7c85a6912` A bunch of fixes + Code Cleanup

```diff
-Oil Pot Shard1
+Oil Pot Shard
```

Changed:

- `Shard1` → `Shard`

**zh-Hans** (current):

```text
油壶碎片
```

### `Mods.tsorcRevamp.Projectiles.OilPotShard2.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2024-01-21, `7c85a6912` A bunch of fixes + Code Cleanup

```diff
-Oil Pot Shard2
+Oil Pot Shard
```

Changed:

- `Shard2` → `Shard`

**zh-Hans** (current):

```text
油壶碎片
```

### `Mods.tsorcRevamp.Projectiles.OilPotShard3.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2024-01-21, `7c85a6912` A bunch of fixes + Code Cleanup

```diff
-Oil Pot Shard3
+Oil Pot Shard
```

Changed:

- `Shard3` → `Shard`

**zh-Hans** (current):

```text
油壶碎片
```

### `Mods.tsorcRevamp.Projectiles.OilPotShard4.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2024-01-21, `7c85a6912` A bunch of fixes + Code Cleanup

```diff
-Oil Pot Shard4
+Oil Pot Shard
```

Changed:

- `Shard4` → `Shard`

**zh-Hans** (current):

```text
油壶碎片
```

### `Mods.tsorcRevamp.Projectiles.EnemyForgottenPearlSpearProj.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2023-07-21, `fe2a6851e` More fixes

```diff
-Enemy Forgotten Pearl Spear Proj
+Enemy Forgotten Pearl Spear
```

Changed:

- removed `Proj`

**zh-Hans** (current):

```text
被遗忘的珍珠矛
```

### `Mods.tsorcRevamp.Projectiles.CursedTormentorProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2024-01-21, `7c85a6912` A bunch of fixes + Code Cleanup

```diff
-Cursed Tormentor Projectile
+Cursed Tormentor
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
咒火刑罚
```

### `Mods.tsorcRevamp.Projectiles.GaleforceProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2024-01-21, `7c85a6912` A bunch of fixes + Code Cleanup

```diff
-Galeforce Projectile
+Galeforce
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
极风之弦
```

### `Mods.tsorcRevamp.Projectiles.PyromethaneProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2024-01-21, `7c85a6912` A bunch of fixes + Code Cleanup

```diff
-Pyromethane Projectile
+Pyromethane
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
燠沼气
```

### `Mods.tsorcRevamp.Projectiles.TyphoonProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2024-01-21, `7c85a6912` A bunch of fixes + Code Cleanup

```diff
-Typhoon Projectile
+Typhoon
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
炽风之弦
```

### `Mods.tsorcRevamp.Projectiles.CenterOfTheUniverseCometZone.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2024-01-21, `7c85a6912` A bunch of fixes + Code Cleanup

```diff
-Center Of The Universe Comet Zone
+Grand Comet
```

Changed:

- `Center Of The Universe` → `Grand`
- removed `Zone`

**zh-Hans** (current):

```text
宇宙重心彗星冲击波
```

### `Mods.tsorcRevamp.Projectiles.CenterOfTheUniverseFallingComet.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2024-01-21, `7c85a6912` A bunch of fixes + Code Cleanup

```diff
-Center Of The Universe Falling Comet
+Grand Comet
```

Changed:

- `Center Of The Universe Falling` → `Grand`

**zh-Hans** (current):

```text
宇宙重心坠落彗星
```

### `Mods.tsorcRevamp.Projectiles.CenterOfTheUniverseShockwave.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2024-06-02, `32657f238` Word Capitalisation Issue Color.Purple don't look well

```diff
-Center Of The Universe Shockwave
+Center of the Universe
```

Changed:

- `Of The` → `of the`
- removed `Shockwave`

**zh-Hans** (current):

```text
宇宙重心彗星冲击波
```

### `Mods.tsorcRevamp.Projectiles.GrandCometProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2024-01-21, `7c85a6912` A bunch of fixes + Code Cleanup

```diff
-Grand Comet Projectile
+Grand Comet
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
宇宙重心彗星
```

### `Mods.tsorcRevamp.Projectiles.GrandCometShockwaveTrail.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2024-01-21, `7c85a6912` A bunch of fixes + Code Cleanup

```diff
-Grand Comet Shockwave Trail
+Grand Comet Shockwave
```

Changed:

- removed `Trail`

**zh-Hans** (current):

```text
宇宙重心彗星冲击波
```

### `Mods.tsorcRevamp.Projectiles.GrandCometZone.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2024-01-21, `7c85a6912` A bunch of fixes + Code Cleanup

```diff
-Grand Comet Zone
+Grand Comet
```

Changed:

- removed `Zone`

**zh-Hans** (current):

```text
宇宙重心彗星冲击波
```

### `Mods.tsorcRevamp.Projectiles.SamuraiBeetleTrail.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2024-01-21, `7c85a6912` A bunch of fixes + Code Cleanup

```diff
-Samurai Beetle Trail
+Samurai Beetle
```

Changed:

- removed `Trail`

**zh-Hans** (current):

```text
圣甲虫武士
```

### `Mods.tsorcRevamp.Projectiles.SamuraiBeetleProjectile.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2024-01-21, `7c85a6912` A bunch of fixes + Code Cleanup

```diff
-Samurai Beetle Projectile
+Samurai Beetle
```

Changed:

- removed `Projectile`

**zh-Hans** (current):

```text
圣甲虫武士
```

### `Mods.tsorcRevamp.Projectiles.SamuraiBeetleCloud.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2024-01-21, `7c85a6912` A bunch of fixes + Code Cleanup

```diff
-Samurai Beetle Cloud
+Samurai Beetle
```

Changed:

- removed `Cloud`

**zh-Hans** (current):

```text
圣甲虫武士
```

## NPCs

### `Mods.tsorcRevamp.NPCs.Plantera.DespawnHandler`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-16, `64e96582e` Renamed Plantera the Verdurous Monarch (VUHR-juh-ruhs)

```diff
-Plantera has avenged its child...
+The Verdurous Monarch has avenged its child...
```

Changed:

- `Plantera` → `The Verdurous Monarch`

**ru-RU** (current):

```text
Плантера отомстила за своё дитя...
```

**zh-Hans** (current):

```text
世纪之花为它的子民报了仇...
```

### `Mods.tsorcRevamp.NPCs.HeroofLumelia.Archers2`

Needs update in: **zh-Hans** · English last changed: 2023-11-05, `8093ed7f5` Archers in the distance!

```diff
-Archers in the trees!
+Archers in the distance!
```

Changed:

- `trees!` → `distance!`

**zh-Hans** (current):

```text
弓箭从天而降！
```

### `Mods.tsorcRevamp.NPCs.Gwyn.Protected`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-01, `dbfcce830` -Removed night condition or merged with day for enemy spawns underground

```diff
-Gwyn is protected by the Soul of Cinder: You're too far away!
+Gwyn is protected by Marik: You're too far away!
```

Changed:

- `the Soul of Cinder:` → `Marik:`

**ru-RU** (current):

```text
Гвин защищён Душой пепла: Ты слишком далеко!
```

**zh-Hans** (current):

```text
葛温受到燃灰之魂的保护:你离得太远了！
```

### `Mods.tsorcRevamp.NPCs.Gwyn.Coward`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-01, `dbfcce830` -Removed night condition or merged with day for enemy spawns underground

```diff
-The Soul of Cinder burns the flesh of cowards: You're too far away!
+Marik burns the flesh of cowards: You're too far away!
```

Changed:

- `The Soul of Cinder` → `Marik`

**ru-RU** (current):

```text
Душа из пепла сжигает плоть труса: Ты слишком далеко!
```

**zh-Hans** (current):

```text
燃灰之魂将焚尽懦夫的肉体:你离得太远了！
```

### `Mods.tsorcRevamp.NPCs.AncientDemonOfTheAbyss.DisplayName`

Needs update in: **zh-Hans** · English last changed: 2025-04-12, `def7d1c67` 0.15.24.1 update

```diff
-Ancient Demon of the Abyss
+Great Demon of the Abyss
```

Changed:

- `Ancient` → `Great`

**zh-Hans** (current):

```text
渊古恶魔
```

### `Mods.tsorcRevamp.NPCs.CorruptedElemental.DisplayName`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2025-11-30, `e51bd7f27` Reworked Dwarven Contract, Progression on Thorium compatibility, Added Vault Of Endless Greed

```diff
-Corrupted Elemental
+Abyss Elemental
```

Changed:

- `Corrupted` → `Abyss`

**ru-RU** (current):

```text
Элементаль порчи
```

**zh-Hans** (current):

```text
堕落元素
```

### `Mods.tsorcRevamp.NPCs.CorruptedHornet.DisplayName`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2025-11-30, `e51bd7f27` Reworked Dwarven Contract, Progression on Thorium compatibility, Added Vault Of Endless Greed

```diff
-Corrupted Hornet
+Defiled Hornet
```

Changed:

- `Corrupted` → `Defiled`

**ru-RU** (current):

```text
Шершень порчи
```

**zh-Hans** (current):

```text
堕落黄蜂
```

### `Mods.tsorcRevamp.NPCs.DarkBloodKnight.DisplayName`

Needs update in: **ru-RU** · English last changed: 2025-11-06, `b29e078f6` Buffed the HP values of most SHM enemies

```diff
-Dark Blood Knight
+Demonic Blood Knight
```

Changed:

- `Dark` → `Demonic`

**ru-RU** (current):

```text
Рыцарь тёмной крови
```

### `Mods.tsorcRevamp.NPCs.IceSkeleton.DisplayName`

Needs update in: **ru-RU** · English last changed: 2025-11-30, `e51bd7f27` Reworked Dwarven Contract, Progression on Thorium compatibility, Added Vault Of Endless Greed

```diff
-Ice Skeleton
+Adept of Seath
```

Changed:

- `Ice Skeleton` → `Adept of Seath`

**ru-RU** (current):

```text
Ледяной скелет
```

### `Mods.tsorcRevamp.NPCs.Dwarf.Quote1`

Needs update in: **ru-RU** · English last changed: 2025-11-30, `e51bd7f27` Reworked Dwarven Contract, Progression on Thorium compatibility, Added Vault Of Endless Greed

```diff
-I've got some contracts for sale.
+Hello my friend, how are you?
```

Changed:

- `I've got some contracts for sale.` → `Hello my friend, how are you?`

**ru-RU** (current):

```text
У меня есть несколько контрактов на продажу.
```

### `Mods.tsorcRevamp.NPCs.Dwarf.Quote2`

Needs update in: **ru-RU** · English last changed: 2025-11-30, `e51bd7f27` Reworked Dwarven Contract, Progression on Thorium compatibility, Added Vault Of Endless Greed

```diff
-How are you?
+I'm thirsty... would you like to have a drink with me?
```

Changed:

- `How are you?` → `I'm thirsty... would you like to have a drink with me?`

**ru-RU** (current):

```text
Как ты?
```

### `Mods.tsorcRevamp.NPCs.Dwarf.Quote3`

Needs update in: **ru-RU** · English last changed: 2025-11-30, `e51bd7f27` Reworked Dwarven Contract, Progression on Thorium compatibility, Added Vault Of Endless Greed

```diff
-I'm thirsty...
+We dwarves are very warm and helpful; if you have a problem, you can always come to us without any trouble!
```

Changed:

- `I'm thirsty...` → `We dwarves are very warm and helpful; if you have a problem, you can always come to us without any trouble!`

**ru-RU** (current):

```text
Я хочу пить...
```

### `Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip1`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-07-27, `7ff66746f` Stamina & Shield Balancing + Weapon Logs + BotC Magic Mechanic + More

```diff
-Forge your souls in the flames of sacred altars and make their power your own.
+Forge your souls in the flames of sacred bonfires and make their power your own.
```

Changed:

- `altars` → `bonfires`

**ru-RU** (current):

```text
Закаляйте свои души в пламени священных алтарей и овладевайте их силой.
```

**zh-Hans** (current):

```text
让神圣祭坛的火焰锻造你的灵魂，让它们的力量成为你自己的力量。
```

### `Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip2`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-07-27, `7ff66746f` Stamina & Shield Balancing + Weapon Logs + BotC Magic Mechanic + More

```diff
-If you ever chance upon a weapon befallen the terrible curse of poor craftsmanship, bring it to me and I shall bless it.
+If you ever chance upon a weapon cursed by poor craftsmanship, bring it to me and I shall bless it.
```

Changed:

- `befallen the terrible curse of` → `cursed by`

**ru-RU** (current):

```text
Если вы когда-нибудь случайно наткнётесь на оружие, на которое обрушилось ужасное проклятие плохого ремесла, принесите его мне, и я благословлю его.
```

**zh-Hans** (current):

```text
如果你偶然发现一件武器遭受了低劣词缀的可怕诅咒，把它带给我，我会祝福它。
```

### `Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip4`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-07-27, `7ff66746f` Stamina & Shield Balancing + Weapon Logs + BotC Magic Mechanic + More

```diff
-The near-constant use of potions will prove vital on your journey, especially when attempting to retrieve lost souls.
+The frequent use of potions will prove vital on your journey, especially when attempting to retrieve lost souls.
```

Changed:

- `near-constant` → `frequent`

**ru-RU** (current):

```text
Почти постоянное употребление зелий окажется жизненно важным в вашем путешествии, особенно при попытке вернуть потерянные души.
```

**zh-Hans** (current):

```text
在你的旅程中，尤其是在试图找回丢失的黑暗之魂时，持续使用药水是至关重要的。
```

### `Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip5`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-07-27, `7ff66746f` Stamina & Shield Balancing + Weapon Logs + BotC Magic Mechanic + More

```diff
-A pickaxe or sticky bomb can be used to break blocks that are 1 width wide. Stone gates need switches to open. If in doubt, give it a whack.
+A pickaxe or sticky bomb can be used to break blocks that are 1 width wide, but there are some exceptions. Stone gates need switches to open. If in doubt...
```

Changed:

- `wide.` → `wide, but there are some exceptions.`
- `doubt, give it a whack.` → `doubt...`

**ru-RU** (current):

```text
Киркой или липкой бомбой можно разбивать блоки шириной в 1 блок. Для открытия каменных ворот нужны переключатели. Если сомневаетесь, попробуйте.
```

**zh-Hans** (current):

```text
可以使用镐子或粘性炸弹来打破宽度为 1 格的墙壁。石制门需要找到开关来开启。如果不确定你面对的是哪种门，先给它来一镐子再说。
```

### `Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip7`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-07-27, `7ff66746f` Stamina & Shield Balancing + Weapon Logs + BotC Magic Mechanic + More

```diff
-On your journey you will encounter unlit bonfires, light them and enjoy the peaceful respite they provide. You can nuse them as checkpoints and storage.
+On your journey you will encounter unlit bonfires. Light them and enjoy the peaceful respite they provide. You can use them as checkpoints and for crafting with souls.
```

Changed:

- `bonfires, light` → `bonfires. Light`
- `nuse` → `use`
- `storage.` → `for crafting with souls.`

**ru-RU** (current):

```text
На своем пути вы встретите незажженные костры, разожгите их и наслаждайтесь мирной передышкой, которую они дают. Вы можете использовать их в качестве контрольных точек и хранилища.
```

**zh-Hans** (current):

```text
在你的旅途中，你会遇到未点燃的篝火，点燃它们并享受它们提供的宁静。你可以将它们用作检查点和存储库。
```

### `Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip8`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-07-27, `7ff66746f` Stamina & Shield Balancing + Weapon Logs + BotC Magic Mechanic + More

```diff
-You can stash excess ammo, weapons and armor in the safes and piggy banks found at save points and bonfires.
+You can stash excess items in the bottomless Storage Slot to the right of your inventory. Press T to open. Shift-click to transfer items.
```

Changed:

- `ammo, weapons and armor` → `items`
- `safes and piggy banks found at save points and bonfires.` → `bottomless Storage Slot to the right of your inventory. Press T to open. Shift-click to transfer items.`

**ru-RU** (current):

```text
Вы можете спрятать лишние боеприпасы, оружие и броню в сейфах и копилках, которые можно найти в точках сохранения и у костров.
```

**zh-Hans** (current):

```text
你可以将多余的弹药、武器和盔甲存放在保存点和篝火处的保险箱和猪猪存钱罐中。
```

### `Mods.tsorcRevamp.NPCs.MiakodaNPC.HunterElengadRuinHint`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-04-14, `bd5f2da23` A lot of changes and content!!

```diff
-The Orspeak built many traps underneath their tribe as a way to defend against the first coming of the Abyss.
+The Orespeak built many traps underneath their tribe as a way to defend against the first coming of the Abyss.
 They were a tribe of Fire Walkers - they had the magical ability to walk on molten rock yet remain unharmed.
 If you don't have a way to cross lava, delving deep into their tunnels would be a fool's errand.
```

Changed:

- `Orspeak` → `Orespeak`

**ru-RU** (current):

```text
Орспик построили множество ловушек под своим племенем, чтобы защититься от первого пришествия бездны.
Они были племенем идущих в огне — они обладали магической способностью ходить по расплавленным камням, оставаясь при этом невредимыми.
Если у тебя нет способа пересечь лаву, углубляться в их туннели было бы глупой затеей.
```

**zh-Hans** (current):

```text
奥斯匹克人于部落地下设置诸多陷阱，以御深渊初次侵袭
他们是烈焰行者，掌握踏熔岩而无伤的本领
如果没有横渡熔岩的方法，贸然深入其隧纯属莽行
```

### `Mods.tsorcRevamp.NPCs.MiakodaNPC.DestroyerFromSerris`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2025-10-12, `d0270a130` Ready for 0.16.2

```diff
 It is said that earning the recognition of Serris will grant one access to the depths of the Labyrinth.
-The path opened by defeating Serris-X should be somewhere to the east of the Serris ruins, underneath a pool of water.
+The path opened by defeating Serris-X should be somewhere under the Desert Surface, near the ruins.
```

Changed:

- `to` → `under`
- `east of` → `Desert Surface, near`
- `Serris ruins, underneath a pool of water.` → `ruins.`

**ru-RU** (current):

```text
Говорят, что, заслужив признание Серриса, можно проникнуть в глубины Лабиринта.
Путь, открывшийся после победы над Серрис-Икс, должен находиться где-то к востоку от её руин, под бассейном с водой.
```

**zh-Hans** (current):

```text
欲入暗影神庙，需获赛里斯认可
击败赛里斯后所启之径，位于赛里斯废墟以东，一汪水潭之下
```

### `Mods.tsorcRevamp.NPCs.MiakodaNPC.PlanteraLorePart1`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-16, `64e96582e` Renamed Plantera the Verdurous Monarch (VUHR-juh-ruhs)

```diff
-Do you wish to know more about The Awakened? I will now tell you about Plantera, the Infection that plagues the Jungle.
+Do you wish to know more about The Awakened? I will now tell you about the Verdurous Monarch, the Infection that plagues the Jungle.
 The Jungle is the heart of Mother Nature. When it was tunneled through by the Destroyer, the corruption it left behind could not consume the Jungle. However, its presence had other effects within.
```

Changed:

- `Plantera,` → `the Verdurous Monarch,`

**ru-RU** (current):

```text
Хочешь узнать больше о Пробуждённых? Сейчас я расскажу о Плантере, заразе, которая поражает джунгли.
Джунгли — это сердце Матери-природы. Когда Разрушитель проложил в них туннель, оставленное им разложение не смогло уничтожить Джунгли. Однако его присутствие имело и другие последствия внутри.
```

**zh-Hans** (current):

```text
想了解更多关于恶魔守护者的真相吗？且听我讲述丛林瘟疫—世纪之花的起源
丛林本是自然之母的心脏，当毁灭者贯穿其躯体时，残留的腐化虽未吞噬丛林，却引发了更深层的异变
```

### `Mods.tsorcRevamp.NPCs.MiakodaNPC.PlanteraLorePart2`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-16, `64e96582e` Renamed Plantera the Verdurous Monarch (VUHR-juh-ruhs)

```diff
 It became a madness, a disease. Mother Nature could only contain the infection and keep it at bay.
-But Plantera, the incarnation of death and madness, will truly be freed one day. Its taint continues to spread throughout the Jungle whispering to those foolish enough to draw near.
+But the Verdurous Monarch, the incarnation of death and madness, will truly be freed one day. Its taint continues to spread throughout the Jungle whispering to those foolish enough to draw near.
```

Changed:

- `Plantera,` → `the Verdurous Monarch,`

**ru-RU** (current):

```text
Это стало безумием, болезнью. Мать-природа могла только сдерживать инфекцию.
Но Плантера, воплощение смерти и безумия, однажды будет по-настоящему освобождена. Её зараза продолжает распространяться по джунглям, нашёптывая тем, кто достаточно глуп, чтобы приблизиться.
```

**zh-Hans** (current):

```text
它化作疯狂与疫病，自然之母只能将其勉强遏制
但作为死亡化身的世纪之花终将破封，它的低语在丛林中蔓延，引诱着每个靠近的愚者...
```

### `Mods.tsorcRevamp.NPCs.MiakodaNPC.GolemLorePart2`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-10-16, `64e96582e` Renamed Plantera the Verdurous Monarch (VUHR-juh-ruhs)

```diff
 His last act while still sane was to seal himself away deep within his Temple.
 The sealing was said to be done in two parts. The lock was entrusted to the Moon Tribe, who resided in the present day Flooded Machine Temple.
-The key was linked to the very infection that drove them mad: Plantera itself. If the source of the madness could be destroyed, the key to their freedom would reveal itself.
+The key was linked to the very infection that drove them mad: the Verdurous Monarch itself. If the source of the madness could be destroyed, the key to their freedom would reveal itself.
```

Changed:

- `Plantera` → `the Verdurous Monarch`

**ru-RU** (current):

```text
Последним его поступком, когда он был ещё в здравом уме, было запечатать себя глубоко в своём храме.
Говорят, что ритуал запечатывания состоял из двух частей. Замок был доверен племени Луны, которое проживало в современном затопленном Храме Машин.
Ключ был связан с той самой инфекцией, которая свела их с ума: с самой Плантерой. Если бы источник безумия можно было уничтожить, ключ к их свободе открылся бы сам собой.
```

**zh-Hans** (current):

```text
理智尚存时，他最后的举动是将自我封印在神庙深处
传说封印分为两部：月之部族掌管锁印—他们居于现今被淹没的机械神殿
而钥匙竟与疯狂本源相连：若能摧毁世纪之花，自由之钥自现
```

### `Mods.tsorcRevamp.NPCs.MiakodaNPC.RemixEarthFiendLichHint1`

Needs update in: **zh-Hans** · English last changed: 2026-02-01, `3220fbc41` Big commit, added/removed some weapons

```diff
-We must dive into the Great Chasm and find the Lihzahrd Gate that blocks the path to the Gray Catacombs.
+We must dive into the Great Chasm and find the Lihzahrd Gate that blocks the path to the Catacombs.
 Despite the horror of this place, we have no choice but to venture there and face what is there.
 The Earth Fiend Lich is here, deep in the Catacombs.
```

Changed:

- removed `Gray`

**zh-Hans** (current):

```text
我们必须潜入大裂谷，找到阻隔灰色陵寝通路的丛林蜥蜴砖大门
即使此地诡谲可怖，我们亦当深入幽邃，直面蛰伏之物
大地恶魔巫妖王便栖身于此，盘踞于苍灰陵寝的幽暗深处
```

### `Mods.tsorcRevamp.NPCs.MiakodaNPC.RemixChaosHint`

Needs update in: **zh-Hans** · English last changed: 2026-02-28, `6e3324379` Big patch, Reworked Chaos for real this time (thanks to Boatsoon, still wip though)

```diff
 That Amethyst that we got from Artorias; it is a seal that locks away the throne of the King of Hell, Chaos.
 Like the Calamities that came before it, this being's power and magic were too great for the world.
-He was sealed by the luminous power of the Hallow. If you wish to face it in combat, return to the Hallow, which is invaded by shadows.
+He was sealed by the luminous power of the Hallow. If you wish to face it in combat, return at the entrance of the Hallowed Caves, now invaded by shadows.
```

Changed:

- `to` → `at`
- `Hallow, which is` → `entrance of the Hallowed Caves, now`

**zh-Hans** (current):

```text
从阿尔特留斯处获得的大紫晶，实为封印地狱之王混沌的钥匙
与此前的恶魔类似，此存在的力量过于强大而被封印
神圣的伟力将其禁锢，若欲挑战，请返回神圣洞穴，那里正在被黑暗入侵
```

### `Mods.tsorcRevamp.NPCs.MiakodaNPC.BlightHint`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-07-15, `b9ae9ea96` Main Menu Music Added, Made Blight and Plantera optional fights, Improved Boss Hunting Tome with Boss Tiers + More

```diff
-We should journey East again {0}, to the surface of the frozen ocean.
+Should you decide to journey East again {0}, seek the far reaches of the frozen ocean.
 There is a manifestation there, an infinitesimally small part of a higher being that seeks to widen the crack and fully enter our world..
 It is a Blight on the world. If it one day makes it through I fe our realm will be shattered.
```

Changed:

- `We should` → `Should you decide to`
- `to` → `seek`
- `surface` → `far reaches`

**ru-RU** (current):

```text
We should journey East again {0}, to the surface of the frozen ocean.
There is a manifestation there, an infinitesimally small part of a higher being that seeks to widen the crack and fully enter our world..
It is a Blight on the world. If it one day makes it through I fe our realm will be shattered.
```

**zh-Hans** (current):

```text
我们该再次东行，{0}，前往冻洋的表面
那里有高等存在的微小具现，正试图撕裂世界裂缝完全降临
它是世界的灾疽，若其得逞，我们的位面必将崩毁
```

### `Mods.tsorcRevamp.NPCs.ShamanElder.RemixAdvModeSHMQuote3`

Needs update in: **zh-Hans** · English last changed: 2025-07-09, `f4fc87a68` Remix soapstones, buffed Dragoon Boots and removed compact frame

```diff
 Have you seen the [c/ffbf00:Lihzahrd Gates] scattered across this world? They will lead you towards your goal.
-I would start with the one at the very west of the Mycellium Forest, which leads to the Dreamlike Caverns.
+I would start with the one at the very west of the Mycelium Forest, which leads to the Dreamlike Caverns.
 There is another, in a strange ship in the underworld. The rest, I'm sure you will find. The ancestors will help guide you.
```

Changed:

- `Mycellium` → `Mycelium`

**zh-Hans** (current):

```text
你见过散布在世界各地的[c/ffbf00:丛林蜥蜴神庙门]吗？它们会指引你走向你的目标。
建议从菌丝林园西边的那个开始，它通向梦幻洞穴
还有另一个，在地狱岩浆中的奇怪飞船。剩下的，我相信你会找到的。祖先将帮助引导你。
```

## Keybinds

### `Mods.tsorcRevamp.Keybinds.Special Ability.DisplayName`

Needs update in: **ru-RU, zh-Hans** · English last changed: 2026-08-17, `9c2c80882` Modded minion changes in preparation of 1.4.5

```diff
-Special Ability
+Weapon Ability
```

Changed:

- `Special` → `Weapon`

**ru-RU** (current):

```text
Специальная способность
```

**zh-Hans** (current):

```text
特殊能力键
```

## Achievements

### `Mods.tsorcRevamp.Achievements.CollectorCosmicWatch.Description`

Needs update in: **zh-Hans** · English last changed: 2026-04-14, `bd5f2da23` A lot of changes and content!!

```diff
-Day by Night
+Try to instantly advance time with a cosmic watch.
```

Changed:

- `Day by Night` → `Try to instantly advance time with a cosmic watch.`

**zh-Hans** (current):

```text
夜以继日。
```

### `Mods.tsorcRevamp.Achievements.SlayerLeonhard.FriendlyName`

Needs update in: **zh-Hans** · English last changed: 2026-04-14, `bd5f2da23` A lot of changes and content!!

```diff
-Defeat Leonhard
+A Mysterious Encounter
```

Changed:

- `Defeat Leonhard` → `A Mysterious Encounter`

**zh-Hans** (current):

```text
击败李奥纳德
```

### `Mods.tsorcRevamp.Achievements.SlayerLeonhard.Description` ⚠️

Needs update in: **zh-Hans** · English last changed: 2026-04-14, `bd5f2da23` A lot of changes and content!!

```diff
-{$BossChecklist.LeonhardDesc}
+Defeat Leonhard, a mysterious man keeping the high bridge between the mountains.
```

Changed:

- `{$BossChecklist.LeonhardDesc}` → `Defeat Leonhard, a mysterious man keeping the high bridge between the mountains.`

**zh-Hans** (current):

```text
{$BossChecklist.LeonhardDesc}
```
