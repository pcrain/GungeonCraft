# Changelog

## 1.1.1 (2023-11-05)
- New custom projectile and ammo clip sprites for several guns thanks to Lynceus :D
- Added full support for the [ItemTips](https://enter-the-gungeon.thunderstore.io/package/Glorfindel/ItemTips/?utm_source=discord) mod for all GungeonCraft items
- Updated to ModTheGungeon API 1.7.0 for faster load times (thanks SpecialAPI!)
- Fixed an issue where a few console commands from other mods would be disabled when GungeonCraft is loaded
- Fixed Tranquilizer being able to stun enemies immune to stun
- Fixed Thunderstore preview icon image link in Credits.md

## 1.1.0 (2023-11-03)

#### Features and Polish:
- Added Aimu Hakurei, Seltzer Pelter, Missiletoe, Bubble Wand, Insurance Policy, and Ice Cream
- Items can now be found in specialty shops (subshops), including support for modded shops (Planetside of Gunymede & Once More Into the Breach)
- Added a few new sprites for Bullet Kin (they love ice cream :>)
- All of the mod's guns now spawn with their idle animations playing by default (before, they would be stuck on the first frame)
- Use a new, faster exponent approximation in a few places to reduce lag

#### Balance Changes:
- Ticonderogun's damage now scales inversely with the number of enemies encircled rather than inversely with circle size, resulting in higher damage on average
- Dead ringer now has a short grace period after activating to make it less likely you will immediately lose your cloak
- Halved max ammo of Schrodinger's Gat
- [REDACTED]'s active item effect can now be triggered by either the active item button or the fire button

#### Bugfixes:
- Fixed Alyx magically replenishing all of its ammo when dropped and picked up again
- Fixed an oversight where the contents of all chests could be replaced with a singular specific item rather than drawing from their normal loot table
- Fixed incorrect impact effects playing on some projectiles
- Updated to Alexandria 0.3.24 to fix co-op blank issue (thanks SomeBunny!)

## 1.0.4 (2023-10-21)

- Fix custom dodge rolls not properly putting out fires
- Fix bosses killed with Quarter Pounder not spawning reward pedestals
- Make gold statues created with Quarter Pounder pushable 
- Fix Z-axis rendering issues with statues created with Quarter Pounder
- Possibly fix issue with gold sprite from Kill Pillars killed with Quarter Pounder appearing randomly on the next floor
- Add cooldown to Soul Kaliber vfx / sfx so per-frame damage doesn't cause lag and noise
- Adjust audio levels for Soul Kaliber
- Clean up outline on Dead Ringer sprite
- Correct typo in Alyx's description

## 1.0.3 (2023-10-21)

- Fix custom floor failing to load after returning to the Breach (thanks SomeBunny!)

## 1.0.2 (2023-10-20)

- Remove unnecessary dependency on Kyle's Custom Rooms

## 1.0.1 (2023-10-20)

- Fix Thunderstore build manifest
- Fix Dead Ringer crash
- Fix null dereference in Update() of several items

## 1.0.0 (2023-10-20)
	
- Initial Release! :D
