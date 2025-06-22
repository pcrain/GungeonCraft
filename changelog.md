# Changelog

## 1.26.3 (TBD)

- Prevented Pizza Peel event gun from randomly spawning in green chests
- Fixed null derefs caused by [REDACTED] checking for floor / enemy data in between runs where that data is invalid
- Prevented some of GungeonCraft's special event items from spawning in certain modded loot pools
- Cleaned up outlines on Blank Checks sprite
- Cleaned up outlines on all of Bubblebeam's sprites
- Fixed not being able to aim Ki Blast's mastery projectile when on controller
- Damage from Ki Blast's mastery projectile now scales with the current floor's enemy health modifier
- Ignizol now has a delay between teleporting and igniting the ground under it to prevent burning the player when entering a new room
- Fixed Domino breaking in several silly ways for characters that can't switch guns for whatever reason

## 1.26.2 (2025-06-06)

- Updated visuals for Pincushion's projectiles and reduced lag from creating them a bit
- Reduced knockback on Seltzer Pelter's soda streams from 100 to 20
- Added placeholder Robot sprites to Rogo's punchout fight so the game doesn't softlock
- Fixed null deref when charging vanilla Railgun caused by faulty patch
- Fixed null deref caused by Platinum Star trying to set its animation before its animator was set up

## 1.26.1 (2025-06-01)

#### Balance Changes and Polish:

- Increased Deadline's ammo from 64 to 200
- Bubblebeam's projectiles no longer collide with other projectiles belonging to the player until they have enbubbled something
- Allowed Wayfarer to have multiple projectiles under its control at the same time (e.g., if firing with Scattershot or Helix Bullets)

#### Bugfixes and Misc:

- Fixed (for real this time) B. B. Gun sometimes not giving player ammo back when the player catches the projectile
- Fixed Zag's wall-following code being completely broken since 1.21.1 how did this go unnoticed D:
- Fixed null deref caused by Suncaster projectiles not spawned by Suncaster trying to refract to their non-existent gun's prisms
- Fixed null deref caused by Racket Launcher projectiles trying to deregister themselves from their parent gun after it was destroyed
- Fixed null deref caused by Alligator's projectiles trying to destroy their cables / sparks after already destroying them
- Fixed null deref caused by damage handler not being properly deregistered when dropping Sunderbuss
- Fixed pitch distortion on several audio clips 
- Fixed sounds from Banana playing when GungeonCraft guns are fired as part of a Duct Tape combo
- Fixed noisy audio glitch caused by repeatedly taking 0 damage hits while under Sunderbuss or Macheening's curse
- Fixed Stereoscope's projectiles throwing an exception every frame when not fired from Stereoscope directly
- Fixed Mastered Macheening's projectiles colliding with and destroying other player-owned projectiles
- Fixed Empath's projectiles colliding with and destroying other player-owned projectiles
- Fixed Helix Bullets rendering Stereoscope, Sunderbuss, and Sextant unusable
- Fixed Sextant firing every single frame when reload time is reduced to 0 (e.g., via Cormorant)
- Substantially reduced frequency of Forkbomb projectiles getting stuck inside walls
- Blacklisted Flakseed projectiles from being affected by Orbital Bullets (projectiles should just sprout on impact)
- Made Wayfarer projectiles created without a controlling Wayfarer (e.g., via Chance Bullets) die on impact
- Removed placeholder in Outbreak's ammonomicon description

## 1.26.0 (2025-05-29)

#### Features:

- Added Display Pedestal, Glass Ammo Box, Trading Guide, Goop Stabilizer, Forkbomb, Zealot, and Combat Leotard
	- Display Pedestal, Trading Guide, Goop Stabilizer, and Forkbomb sprites thanks to Dallan!
- Added masteries for Forkbomb and Zealot
- Added two new puzzle rooms
	- Floor puzzle tile sprites thanks to Some Bunny!

#### Balance Changes and Polish:

- Decreased Gunflower's ammo usage from 10 per second to 3 per second
- Touched up Yggdrashell's Earth Armor heart sprites and added a proper half heart sprite
- Added missing outlines to ammo clip sprites for Bouncer, Bubblebeam, Derail Gun, and Plasmarble

#### Bugfixes and Misc:

- Fixed (hopefully) null deref caused by enemy gun replacement code not checking for the presence of hand sprites or gun sprites
- Fixed X-elsior's reticle persisting when changing or dropping guns while firing at an enemy
- Fixed B. B. Gun sometimes not giving player ammo back when the player catches the projectile
- Made Barter Shop sign that explains how bartering works indestructible
- Lowered volume on Nycterian's projectile and alert sounds
- Updated Pogo Stick's description to mention granting an extra active item slot

## 1.25.5 (2025-05-21)

#### Balance Changes and Polish:

- Ki Blast now passively recharges Ki even when it is not the active gun
- Pogo Stick item now grants an additional active item slot
- Added custom ammo clip sprite for Oddjob
- Added "Revved" indicator for when Natascha will stay spun up when not firing
- Increased R.C. Launcher's ammo from 240 to 320

#### Bugfixes and Misc:

- Fixed Starmageddon's mastery being completely nonfunctional since 1.22.1
- Fixed Oddjob poofing whenever it is attached and detached from the player's head (broken since 1.23.0)
- Fixed hats sometimes improperly rendering below the player when riding Pogo Stick
- Fixed lag when several of Blackjack's cards are lying on the ground
- Removed indicator incorrectly suggesting that Pogo Stick Gun can be mastered
- Reduced volume on Pogo Stick Gun, R. C. Launcher, and Paintball Cannon projectile sounds
- Optimized code for checking when interact is pressed with no interactible nearby (used by Scalding Jelly)
- Optimized code for drawing mastery labels for guns
- Optimized some code checking whether screen shaders should be applied

## 1.25.4 (2025-05-08)

- Separated vanilla bugfixes and optimizations into its own mod for the greater good: [Gungeon Go Vroom](https://thunderstore.io/c/enter-the-gungeon/p/CaptainPretzel/Gungeon_Go_Vroom/)
- Updated required Gunfig version to 1.1.6 for configuration bugfixes

## 1.25.3 (2025-05-07)

#### Balance Changes and Polish:

- Made Subtractor Beam's health labels look significantly nicer
- Added new sounds and UI heart indicators for when the player is under the effects of Sunderbuss' or Macheening's double damage penalty
- Added better impact sounds and VFX for Wavefront's projectiles
- Added lighting effects to Macheening's charge animation
- Added Armor Piercing Rounds, 4D Bullets, and Echo Chamber to [Once More Into the Breach](https://thunderstore.io/c/enter-the-gungeon/p/Nevernamed/Once_More_Into_The_Breach/)'s Doug shop
- Added Cubud from Once More Into the Breach to the companion shop
- Rebuffed Paintball Cannon's damage from 4.5 to 5.5

#### Bugfixes and Misc:

- Fixed broken cross-mod compatibility with Once More Into the Breach's Shops
- Fixed earsplitting audio glitch when cancelling mastered Ki Blast's charge attack at the wrong time
- Fixed secret floor's flag for returning to the previous floor not being reset when dying, causing weird floor skips and repeats
- Fixed null deref caused by trying to flip sprites towards non-existent players
- Fixed Stereoscope's VFX not rendering above Bullet King and probably some other enemies
- Added version checks for modding library dependencies to prevent errors resulting from launching using out of date libraries 

## 1.25.2 (2025-05-06)

#### Balance Changes and Polish:

- Rebalanced Paintball Cannon:
	- Reduced damage from 9 to 4.5
	- Changed gun class from PISTOL to RIFLE
	- Slightly increased projectile size
- Replaced Voodoo Doll item sprite with a new one by Dallan (thanks Dallan!)
- Improved muzzle lighting for Natascha, Alyx, and Platinum Star
- Improved lighting on projectiles from Ki Blast, Suncaster, Plasmarble, Macheening, Wavefront, and Lightwing
- Improved lighting on Yggdrashell's leaves

#### Bugfixes and Misc:

- Fixed onslaught of null derefs caused by Sextant trying to load stale UI elements when loading a new floor
- Fixed premature charging sound effect playing when firing normal projectiles with Ki Blast's mastery active
- Fixed Ki Blast idle sprite appearing for a single frame under certain circumstances where it should be invisible
- Updated required Alexandria version to 0.4.22 for UI bugfixes

## 1.25.1 (2025-05-04)

- Fixed enemy killed by [REDACTED] not being remembered when reloading a midgame save
- Fixed not being able to swing Racket Launcher at extant tennis balls when out of ammo
- Fixed charging phase of Pogo Stick bounce counting as airborne, allowing the player to hover over pits
- Fixed collision jankiness when charging and landing Pogo Stick's super bounce immediately after taking damage
- Lowered volume on some of Rogo's sounds

## 1.25.0 (2025-05-03)

#### Features:

- Added new custom character: Rogo from [Pogo Rogue](https://store.steampowered.com/app/2870280/Pogo_Rogue/)!
- Added Pogo Stick

#### Bugfixes and Misc:

- Fixed Sextant being able to target companions
- Fixed potential null deref when starting a run with Gyroscope or Drifter's Headgear (e.g., with Paradox)
- Updated required Alexandria version to 0.4.21 for new Dodge Roll API features

## 1.24.0 (2025-04-27)

#### Features:

- Added Sextant, Wayfarer, and Leafblower
- Added masteries for Sextant, Wayfarer, and Leafblower
- Added 13 new synergies

#### Balance Changes and Polish:

- Reworked B. B. Gun:
	- Reduced max ammo from 3 to 1
	- Changed clip size from 3 to unlimited (i.e., clip size now matches max ammo)
	- Increased projectile friction at high speeds, making them slow down faster
	- Projectiles can now be caught while they're in motion to restore ammo
	- Projectiles now knock the player back when caught
	- Ammo is now fully restored upon room clear
- Polished Uppskeruvel:
	- Enemies with high HP now drop large soul fragments worth 10 normal soul fragments each when killed by Uppskeruvel
	- Reduced frequency of particles spawned by soul fragments created by enemies killed with Uppskeruvel
	- Uppskeruvel's Aimless Souls now render above most objects
- Lowered volume and reverb of impact sounds for Ki Blast's projectiles

#### Bugfixes and Misc:

- Fixed null deref caused by Silver Bullets attempting to tint Seltzer Pelter's soda streams before they were set up
- Fixed (hopefully) null deref caused by Borrowed Time attempting to activate after being destroyed or while the player was not in a valid room
- Fixed (hopefully) null deref caused by trying to reflect Ki Blast's projectiles towards non-existent enemies
- Fixed Uppskeruvel's lost souls not spawning when entering a new floor while Uppskeruvel is not the active gun
- Fixed empty ammo clip sprites for Outbreak, Schrodinger's Gat, Soul Kaliber, and Stereoscope being a pixel offset from their full ammo clip sprites
- Fixed Prismatic Scope not recognizing several beam weapons as such
- Fixed transparency of Vacuum Cleaner's particles being framerate dependent
- Fixed homing behavior of Uppskeruvel's Aimless Souls being framerate dependent
- Fixed certain VFX not playing correctly on Blue and Red Shotgun Kin
- Fixed pixel gaps when rendering certain sprite shaders
- Added missing music credits to credits page

## 1.23.1 (2025-04-02)

#### Features:

- Added 9 new synergies

#### Balance Changes and Polish:

- Projectile modification items are now properly applied to projectiles created by Flakseed's flowers, Maestro's reflections, enemies infected by Outbreak, Pistol Whip's whip and pistol, Seltzer Pelter's soda cans, Sunderbuss' ground slam, and Widowmaker's spider turrets
- Increased Vacuum Cleaner's per-debris chance to restore ammo from 1% to 5%
	- Increased Scavengest synergy's per-debris chance to restore ammo from 4% to 20%
- Increased Starmageddon's projectile damage from 8 to 11
- Prevented Starmageddon from targeting Gunreapers and other invulnerable enemies
- Capped Ki Blast's reflection damage multiplier at 8 times the projectile's original damage
- Made X-elsior always starts with one pistol in its arsenal
- Tweaked several gun and item descriptions for clarity, consistency, and grammar

## 1.23.0 (2025-03-31)

#### Features:

- Added X-elsior, Empath, and Domino
- Added masteries for X-elsior and Empath

#### Balance Changes and Polish:

- Added dynamic reload animation speeds to several guns (i.e., their animations reflect changes to the player's reload speed stat)
	- (Bouncer, Bubblebeam, Carpet Bomber, Derail Gun, Empath, Flakseed, Hand Cannon, Hatchling Gun, Lightwing, Nycterian, Platinum Star, Sub Machine Gun, Subtractor Beam, Tranquilizer, Uppskeruvel, Wavefront, and Widowmaker)
- Improved Gun destruction VFX for mastery ritual
- Made the Bubble Blasters given to enemies by Bubble Wand deal no damage to the player
- Made [REDACTED] no longer damage enemies while charging, preventing it from being useful on certain enemies

#### Bugfixes and Misc:

- Fixed null deref caused by looking for non-existent guns on the ground after completing mastery ritual
- Potentially fixed null deref caused by certain enemies trying to fire Bubble Blasters before initialization

## 1.22.3 (2025-03-21)

#### Features:

- Added masteries for Chroma, Flakseed, R.C. Launcher, B. B. Gun, Nycterian, Seltzer Pelter, Suncaster, Bouncer, Sub Machine Gun, Stereoscope, Groundhog, Glockarina, Macheening, and Plasmarble

#### Balance Changes and Polish:

- Rebalanced Nycterian to accentuate distraction mechanics:
	- Reduced fire rate from 0.19 seconds to 0.3 seconds
	- Reduced speed from 27 to 18
	- Reduced ammo from 425 to 325
	- Bats now have unlimited piercing
	- Bats now bounce off walls once
- Rebalanced Plasmarble to be more suitable for its assigned C quality:
	- Reduced number of bolts dischared by Plasmarble's orb from 2 to 1
	- Increased number of times Plasmarble's orb can bounce before breaking from 4 to 6 
- Adjusted shader lighting on Flakseed's flowers
	
#### Bugfixes and Misc:

- Fixed issue with certain non-looping VFX animations preventing other unrelated animations from playing properly
- Fixed Groundhog's charge animation not matching it's charge rate after the player's stats are changed
- Clarified in R.C. Launcher's description that reload time scales with number of shots fired from clip (this was always true, but never mentioned)
- Fixed a typo in Plasmarble's description

## 1.22.2 (2025-03-17)

#### Features and Polish:

- Added masteries for Overflow, Missiletoe, and Macchi Auto
- Changed player's animation speed to match their movement speed while under the effects of Macchi Auto's coffee goop
- Made player smoothly decelerate while charging Gyroscope's dodge roll

#### Bugfixes and Misc:

- Fixed janky sprite offsets when using Vacuum Cleaner or Magunet to attract large objects
- Fixed mastered Iron Maid sometimes only firing a single projectile when switching to another gun and back
- Removed a bunch of debug code from M.M.: Aiming causing a null deref when picked up

## 1.22.1 (2025-03-16)

#### Balance Changes:

- Tightened max orbit speed of Wavefront projectiles to prevent them from drifting too far away from the player
- Blamethrower now grants the scapegoat status to the enemy that actually damaged the player when possible (rather than a random enemy)
- Sub Machine Gun can now be eaten when out of ammo to restore 1.5 hearts

#### Polish:

- Added custom ammo clip sprites for Stereoscope and Flakseed
- Added missing outlines and backgrounds to several ammo clip sprites 
- Added impact VFX to Platinum Star's projectiles
- Added VFX for spawning in decoys via Glockarina
- Added VFX for slowing bullets via Glockarina
- Added better sounds and VFX for triggering training effects with Weighted Robes
- Made Alligator's cables scatter on the floor when detaching from enemies instead of just vanishing into thin air
- Made player's facing direction visually match whip's direction while firing Pistol Whip
- Prevented Missiletoe from being able to visually wrap / unwrap multiple presents at once
- Fixed bad lighting on petting sprites for Wolf, ice cream sprites for Bullet Kin, and tiny ammo box sprites from Scavenging Arms
- Cleaned up petting sprites for Wolf a bit
- Animated the Propellor Cap C:

#### Bugfixes and Misc:

- Fixed null deref and softlock caused by picking up Alyx's mastered form while standing in poison goop
- Fixed null deref and softlock caused by Scalding Jelly checking for Ignizol interactions in non-existent rooms
- Fixed null deref and softlock caused when loading a new floor after an Amazon Primer subscription expires
- Fixed Femtobyte digitizing stone tables as wooden tables
- Fixed auxiliary Gunflower beams (e.g., from Scattershot or Bouncy Bullets) not emitting light

## 1.22.0 (2025-03-13)

#### Features:

- Added Stereoscope, Scalding Jelly, and Flakseed
	- Stereoscope sprite thanks to Dallan!
- Added masteries for Racket Launcher, Jugglernaut, BlasTech F-4, Yggdrashell, Widowmaker, Oddjob, Sunderbuss, Wavefront, and Breegull

#### Balance Changes and Polish:

- Buffed Wavefront:
	- Increased clip size from 8 to 12
	- Increased projectiles' lingering time from 10 to 15 seconds
	- Allowed Wavefront to fire when facing walls (since projectiles phase through them anyway)
- Nerfed Widowmaker:
	- Reduced spider turret damage from 15 to 10
	- Reduced max ammo from 320 to 160
- Racket Launcher now onlys reflect the nearest valid projectile rather than all projectiles in range

#### Bugfixes and Misc:

- Fixed potential null deref caused by event handler for destroyed Allay companion
- Corrected misinformation in Wavefront description incorrectly stating projectiles lasted up to 30 seconds

## 1.21.2 (2025-01-30)

#### Features:

- Added 3 hats! (sprites thanks to Dallan)

#### Bugfixes and Misc:

- Fixed a typo in Tranquilizer's description
- Corrected a few inaccuracies in some hat names
- Potentially fixed issue with Widowmaker turrets exploding shortly after deploying when playing at a high refresh rate
- Fixed null deref caused by trying to digitize modded chests with Femtobyte
- Fixed null deref caused by Alex trying to spawn poison goop without a valid goop manager
- Fixed oil leak from Derail Gun mastery not working upon entering a new floor
- Updated required MtG version to 1.9.2
- Updated required Alexandria version to 0.4.19
- Updated required Gunfig version to 1.1.5

## 1.21.1 (2024-11-22)

#### Features:

- Added masteries for Ki Blast, Hallaeribut, Gunflower, Omnidirectional Laser, Blamethrower, Zag, Outbreak, Telefragger, Tranquilizer, and Aimu Hakurei

#### Balance Changes and Polish:

- Ki Blast projectiles now behave like charge projectiles, firing when the fire button is released (for compatibility with mastery)
- Blamethrower projectile damage increased from 2 to 4
- Blamethrower ammo increased from 300 to 800
- Enemies tranquilized by Tranquilizer now only have a 10% chance to drop their held gun (rather than 100%)
- Revamped Aimu Hakurei's graze detection to be more accurate and framerate independent (was easier to build graze at higher FPS before)

#### Bugfixes and Misc:

- Fixed null deref caused by Grandmaster's chess pieces trying to move destroyed projectiles
- Fixed (hopefully) null deref in Ki Blast's update logic
- Fixed potential bug where Utility Vest can destroy the player's currently active gun in an unsafe way
- Fixed debug log spam when reflecting projectiles using Ki Blast
- Optimized sprite trail creation code to reduce lag when lots of projectile trails are on the screen
- Optimized Zag projectile movement code to reduce lag when lots of Zag projectiles are on the screen

## 1.21.0 (2024-11-04)

#### Features:

- Added Plasmarble, Sunderbuss, Macheening, and Lichguard
	- Plasmarble sprite thanks to Dallan!
- Added five new Hecked Mode variants: Light, Remixed, Grenade, Molotov, and Retrashed Mode

#### Balance Changes and Polish:

- Tweaked Drifter's Headgear:
	- Removed enemy contact damage during dash
	- Allowed player to fire their weapon during dash
	- Allowed player to buffer additional dash inputs during dash
	- Allowed player to slide over tables during dash
- Added custom ammo clip sprites for all of Chroma's beams

#### Bugfixes and Misc:

- Fixed null deref caused by Credit Card trying to update UI for non-existent player
- Fixed infinitely looping audio issues on several guns wielded by enemies in Hecked Mode
- Fixed rare null deref caused by dying during Drifter's Headgear's dash
- Fixed Ticonderogun not actually having a valid projectile, causing a softlock while running SimpleStatsTweaked, whoops D:
- Mentioned in Tranquilizer's description that tranquilized enemies drop their guns and ammo (added in 1.16.2)
- Updated required Alexandria version to 0.4.15 for custom dodge roll API

## 1.20.0 (2024-10-15)

#### Features:

- Added Oddjob, Overflow, Detergent, Bottled Abyss, and Prismatic Scope
	- Oddjob, Detergent, Bottled Abyss, and Prismatic Scope sprites thanks to Dallan!

#### Balance Changes and Polish:

- Credit Card now allows the player to go up to 500 casings in debt instead of granting 500 temporary casings while held (UI tweak only - mechanically, it works the same)
- Cheato Page synergy now causes Breegull's normal eggs to properly display as infinite ammo
- Reduced Calculator's quality from B to C (stackable active items are relatively uncommon so utility is limited)
- Made Aimu Hakurei an infinite ammo gun rather than a 0-ammo-cost gun (to prevent duct taping it to other guns)
- Made attaching Chroma, Hallaeribut, or Yggdrashell to another gun using Duct Tape only transfer the currently active projectile type rather than all projectile types
- Tweaked some Ammonomicon descriptions for clarity and grammar 

#### Bugfixes and Misc:

- Fixed Maestro's ammo display being too short compared to its ammo
- Fixed Glockarina's ammo display sometimes displaying information from the previously-equipped gun
- Fixed K.A.L.I. explosion particles and other various glowing particles not being the correct color after the 1.19.0 update
- Fixed Chroma not emitting particles when firing green or blue beams
- Fixed Z-depth issue causing certain particles not to render after traveling downwards for a short period
- Removed Yggdrashell debug output from console
- Updated required Alexandria version to 0.4.14 for custom ammo display bugfixes and better UI sprite setup

## 1.19.3 (2024-10-07)

#### Features:

- Added masteries for Lightwing, Magunet, Derail Gun, Alien Nailgun, Vladimir, and Maestro
- Added 4 new synergies

#### Balance Changes and Polish:

- Vladimir now has piercing and can hit multiple enemies simultaneously
- Nycterian's projectiles now pierce once to more effectively draw fire
- Gorgun Eye can now target non-hostile enemies such as Keybullet Kin and Chance Kin
- Changed Gorgun Eye's quality from B to A (extremely powerful, especially in single enemy rooms or against strong enemies)
- Changed base damage of all of Breegull's eggs from 5 to 7

#### Bugfixes and Misc:

- Fixed potential null deref caused by Magunet being destroyed while holding debris in stasis
- Fixed Alien Nailgun losing all DNA information when dropped and picked back up
- Fixed [REDACTED]'s attacks only ever targeting the first player during co-op, even if they're not alive
- Fixed Gun Muncher icons persisting on minimap when digitized by Femtobyte
- Updated required Alexandria version to 0.4.12 for custom ammo display migration

## 1.19.2 (2024-09-29)

#### Features:

- Added new Bumbler hat C:

#### Balance Changes and Polish:

- Rebalanced Wavefront:
	- Increased projectile damage from 8 to 12
	- Increased projectile orbit radius by about 40%
	- Decreased max projectile lifetime from 30 seconds to 10 seconds
- Changed Amazon Primer's quality from B to A (extremely strong if you have a lot of casings during the latter floors)
- Changed Vacuum Cleaner's ammo display sprite for amount of debris vacuumed to a more suitable trash icon (hopefully)
- Made Insurance Policy sprites in Insurance Shop now match the player when possible (i.e., for vanilla characters)

#### Bugfixes and Misc:

- Fixed null deref caused by Soul Kaliber attempting to apply soul link damage to non-existent enemies
- Fixed invalid operation exception caused by logic for Hand Cannon's slap
- Fixed Hand Cannon being able to stun companions
- Fixed Frisbee colliding with orbitals (e.g., Guon Stones and certain companions)
- Fixed issue with enemies launched by Vladimir colliding with Vladimir itself, causing unpredictable knockback angles
- Fixed flipped sprites when facing left with The Infamous hat

## 1.19.1 (2024-09-22)

- Fixed major breakage in Yggdrashell and Hallaeribut caused by faulty refactor
- Fixed preparation sound still playing when attempting to throw unthrowable guns
- Updated readme since I forgot for 1.19.0

## 1.19.0 (2024-09-22)

#### Features:

- Added Chroma and Amethyst Shard
- Added masteries for Scotsman, Carpet Bomber, and Soul Kaliber
- Added new hat "The Infamous" (sprites thanks to gustavin!)

#### Balance Changes and Polish:

- Buffed Carpet Bomber
	- Increased ammo from 360 to 720
	- Increased explosion radius from 0.5 to 1.5
	- Increased drag from 0.8 to 0.9 (slows down less in the air)
- Increased Soul Kaliber's ammo from 250 to 444
- Made enemies tossed off of Vladimir deal damage to other enemies
- Tweaked visuals on Yggdrashell's Earth Armor activation
- Increased visibility of Iron Maid's reticle

#### Bugfixes:

- Fixed null deref caused by Racket Launcher projectiles trying to home in on their targets after the projectiles themselves were destroyed
- Fixed several emissive VFX not actually emitting light
- Fixed Soul Kaliber's soul link effect not working on bosses, Keybullet Kin, and Chance Kin
- Fixed (again, for real hopefully) null deref in Ki Blast caused by trying to redirect projectiles to nonexistent enemies
- Fixed Yggdrashell losing all built life force when dropped or when saving and reloading
- Fixed rare null deref when [REDACTED] [REDACTED] for [REDACTED]
- Fixed Quarter Pounder's gold shaders not working properly on Shotgun Kin variants

## 1.18.5 (2024-09-09)

- Fixed argument null exception caused by chests spawned in by Femtobyte trying to transform into invalid Mimics
- Fixed null deref caused by Alligator's cables trying to position themselves at the barrel of a non-existent gun
- Fixed softlock caused by opening a chest while Gyroscope dodge roll is active

## 1.18.4 (2024-09-07)

#### Balance Changes:

- Buffed Dead Ringer:
	- Changed quality from B to C (requires taking damage to activate damage buffs)
	- Dead Ringer now deals 5x damage for 2 seconds after breaking stealth
- Buffed Grandmaster's projectile damage from 5.5 to 10
- Buffed Scotsman's projectile explosion damage from 10 to 24

#### Bugfixes and Misc:

- Fixed buffs from Macchi Auto's coffee goop sometimes persisting when no longer standing in coffee
- Fixed guns with custom ammo displays not properly displaying whether they had infinite ammo (e.g., from Magazine Rack)
- Fixed (hopefully) null deref caused by King's Law updating nonexistent muzzle vfx
- Made a slight memory optimization to vanilla projectile spawning code to hopefully reduce lag spikes a little

## 1.18.3 (2024-09-05)

#### Features:

- Added masteries for Starmageddon, Subtractor Beam, and K.A.L.I.

#### Balance Changes:

- Reworked Macchi Auto: 
	- Coffee goop now slows time by 50% while the player is standing in it
	- Coffee goop now doubles movement speed, roll speed, reload speed, and rate of fire while the player is standing in it
	- (The net effect of the above changes slows down everything but the player by 50%, making overdosing enemies with caffeine more viable)
- Rebalanced Starmageddon: 
	- Projectiles intelligently avoid targeting enemies that other Starmageddon projectiles will kill 
	- Projectiles target random spots in the current room instead of the player when no enemies are found
- Rebalanced Yggdrashell:
	- Halved the amount of life force needed per level to increase vine strength (i.e., gun now builds strength twice as fast)
	- Increased base damage of each level by 50% (with above change, effectively builds strength 3x as fast)
	- Increased ammo consumption from 3 per second to 5 per second
- Changed Suncaster's quality from S to A (requires too much setup to be a truly S tier weapon)
- Changed Glockarina's quality from A to B (DPS is too low and other effects don't quite make up for it)
- Changed Scotsman's gun class from PISTOL to EXPLOSIVE
- Changed Seltzer Pelter's gun class from CHARGE to RIFLE
- Changed Carpet Bomber's gun class from CHARGE to EXPLOSIVE
- Changed Derail Gun's gun class from RIFLE to EXPLOSIVE
- Changed Subtractor Beam's gun class from FULLAUTO to RIFLE

#### Bugfixes:

- Fixed null deref caused by improperly updating Zag projectile trails upon colliding with walls

## 1.18.2 (2024-09-03)

#### Balance Changes:

- Rebalanced Kaliber's Justice:
	- Key blessing no longer grants keys if player possesses Shelleton Key
	- Blank blessing can no longer grant Blank Bullets or Elder Blank if the player already has them
	- Undroppable items can no longer be taken from the player
	- Kaliber's Justice now ignores invulnerability when taking health from the player
	- Removed ability to grant an active item since it was buggy and couldn't trigger under any sane circumstance anyway
- Buffed 4D Bullets:
	- Bullets now only lose 33% of their power (instead of 50%) after phasing through a wall
	- Bullets no longer lose power multiple times when phasing through multiple walls

#### Bugfixes:

- Fixed Yggdrashell's Earth Armor activating from damage sources that would not have dealt damage to the player
- Fixed game manager issue with custom VFX sometimes freezing in place and never disappearing when starting a new run
- Fixed looping charge sounds for Gunflower and Yggdrashell continuing to play while the game is paused
- Fixed Gunflower's lighting effects persisting if switched or dropped while firing

## 1.18.1 (2024-08-31)

#### Features:

- Added masteries for Ticonderogun, King's Law, Bubblebeam, and Deadline
- Added 2 new synergies

#### Balance Changes:

- Gave Gunbrella, King's Law, Maestro, Pistol Whip, Starmageddon, and Yggdrashell the ability to fire even when up against walls

#### Bugfixes and Misc:

- Fixed dummy items being able to appear in synergy chests
- Fixed Bubblebeam permanently changing the color of some projectiles fired by enemies even when they aren't enbubbled
- Fixed projectiles enbubbled by Bubblebeam getting stuck on the back wall of the Bullet King's boss room
- Fixed camera not properly resetting when killing a boss with Ticonderogun
- Fixed King's Law projectiles colliding with doors even when behind the player
- Fixed Groundhog being able to hit Gripmasters during their invulnerable phase, causing them to constantly damage the player once unstunned
- Updated credits page to include sprite credits for every individual gun

## 1.18.0 (2024-08-27)

- Added Bubblebeam, Groundhog, Derail Gun, and Yggdrashell
	- Derail Gun sprites thanks to Nevernamed!
- Fixed Femtobyte losing all of its stored data when dropped and picked back up
- Fixed some projectile trails getting destroyed early when the projectile they were attached to was destroyed

## 1.17.2 (2024-08-13)

- Hid ammo display for Vacuum Cleaner, Pistol Whip, Femtobyte, Vladimir, and Magunet
- Added custom ammo clip sprites for all remaining guns that didn't have them (Scotsman, Chekhov's Gun, K.A.L.I., R.C. Launcher, Breegull, Sub Machine Gun, Nycterian, Starmageddon, Widowmaker, Zag, BlasTech F-4, English, Hallaeribut, Paintball Cannon, Soul Kaliber, Jugglernaut, Pincushion, Bouncer, Deadline, Natascha, Ki Blast, Macchi Auto, Maestro, Gunflower, Telefragger, Omnidirectional Laser, Holy Water Gun, and Ticonderogun)
- Fixed Missiletoe not being able to wrap / unwrap presents if clip size is greater than current ammo
- Fixed issue with back half of Omnidirectional Laser sometimes appearing at strange offsets when switching from another gun
- Removed debug message printed to console log by Omnidirectional Laser

## 1.17.1 (2024-08-11):

#### Balance Changes and Polish:

- Made several guns with special firing mechanics interact with player bullet stat upgrades:
	- Made B.B. Gun projectiles account for damage and knockback stats
	- Made English's charge rate account for gun charge multiplier stat
	- Made English's rack projectiles account for projectile damage, speed, range, and knockback stats
	- Made Grandmaster projectiles' pause time between moves scale inversely with projectile speed stat
	- Made Gunbrella projectiles' fall speed scale with projectile speed stat
	- Made Gunbrella projectiles' impact deviation account for accuracy stat
	- Made Gunbrella projectiles' launch time and hang time scale inversely with projectile speed stat
	- Made Hand Cannon projectiles account for knockback stat
	- Made Iron Maid projectiles' launch speed scale with projectile speed stat
	- Made Ki Blast projectiles' launch speed scale with projectile speed stat
	- Made King's Law projectiles account for projectile speed stat
	- Made Lightwing projectiles' launch speed and turn speed scale with projectile speed stat
	- Made Maestro's reflected projectiles account for projectile damage, speed, range, and knockback stats
	- Made Magunet's debris launching account for accuracy stat
	- Made Magunet's debris projectiles account for projectile damage, speed, range, and knockback stats
	- Made Outbreak's infection projectiles fired from enemies account for projectile damage, speed, range, and knockback stats
	- Made Pistol Whip's melee hit and projectile account for projectile damage, speed, range, and knockback stats
	- Made Scotsman projectile explosions deal damage that scales with damage stat
	- Made Selter Pelter projectiles' soda streams account for projectile damage, speed, range, and knockback stats
	- Made Starmageddon projectiles' hang time scale inversely with projectile speed stat
	- Made Starmageddon projectiles' impact deviation account for accuracy stat
	- Made Wavefront projectiles account for projectile speed stat
	- Made Widowmaker's projectiles account for projectile damage, speed, range, and knockback stats
- Reduced Omnidirectional Laser's spread to 0 so all shots now fire precisely at the reticle

#### Bugfixes and Misc:

- Fixed null deref in Magunet caused by messing up some code in Magunet fix from 1.15.0
- Fixed English projectiles not spawned by English rapidly changing colors
- Fixed Femtobyte projectiles not spawned by Femtobyte missing shaders and visual effects
- Fixed Zag's projectile trails having extremely buggy visual offsets when navigating around walls
- Fixed other player being able to move during [REDACTED]'s effect in co-op mode
- Made [REDACTED] clear enemy projectiles when used to prevent visual oddities
- Blacklisted Widowmaker projectiles from being affected by Orbital Bullets (projectiles should just die if they can't deploy)
- Blacklisted Zag projectiles from being affected by Orbital Bullets (they never despawn properly due to their movement)
- Blacklisted Hallaeribut projectiles from being affected by Helix Bullets (which don't play nicely with frequent speed changes)
- Clarified in B.B. Gun's description that projectile damage and knockback scale with projectile speed (this was always true, but never mentioned)
- Clarified in Pincushion's description that projectiles deal fixed damage (this was always true, but never mentioned)

## 1.17.0 (2024-08-10)

#### Features:

- Added Gunflower and Hallaeribut
	- Gunflower sprites thanks to Nevernamed!

#### Balance Changes and Polish:

- Buffed Starmageddon projectile damage from 6 to 8
- Prevented Starmageddon from targeting invulernable enemies
- Prevented Femtobyte from being able to infinitely trigger table techs by repeatedly digitizing and materializing tables
- Added digitized shader to friendly enemies spawned by Femtobyte
- Added roll sounds to Talon Trot synergy
- Added launch VFX for projectiles launched by Maestro
- Gave Bulletbot Implant a 10 room cooldown rather than making it a one-time use item
- Made Jugglernaut properly render behind player when facing backwards

#### Bugfixes:

- Fixed vanilla bug with burst modules of tiered guns infinitely firing after releasing the mouse
- Fixed longstanding issue with custom dodge roll code creating a new list every frame and eating up memory extremely quickly
- Fixed projectile trails disappearing if projectiles slow down too much
- Fixed Femtobyte's text being cut off when displaying very long names
- Fixed Femtobyte projectiles being unable to hit enemies in mine carts
- Fixed Mimics spawned by Femtobyte being able to damage the player
- Fixed Femtobyte's Lookup Table synergy not remembering the last enemy killed when reloading a midgame save
- Fixed Kaliber's Justice being able to spawn active items with the passive item blessing
- Fixed King's Law Projectiles sometimes firing with a longer delay than they should when launched
- Fixed King's Law muzzle rune persisting when dropped while charging
- Potentially fixed rare issue with Breegull's eggs all costing 1 ammo regardless of type
- Potentially fixed rare issue with Alyx being able to dissipate immediately after being picked up

#### Misc:

- Updated required MtG API version to 1.8.2 for custom ammo pickup functionality
- Optimized projectile trail creation code to reduce memory usage and lag spikes
- Optimized setting a gun's CurrentStrengthTier in places it was being set too often
- Optimized code for finding enemies in rooms
- Tweaked volume on Suncaster's projectiles

## 1.16.4 (2024-08-01)

#### Features:

- Added masteries for English, Iron Maid, Alligator, and Quarter Pounder
- Added 7 new synergies

#### Balance Changes and Polish:

- Overhauled Alligator
	- Increased Alligator projectile speed from 36 to 50
	- Added Electric damage type to Alligator projectiles
	- Made Alligator electric charge output smoothly decay over time rather than jumping around
	- Added indicator for Alligator's current charge output to the ammo display
	- Added shader to visually indicate Alligator's charge level
- Made Femtobyte display enemy names taken from the Ammonomicon instead of using their internal names
- Added impact sounds for Quarter Pounder projectiles

#### Bugfixes and Misc:

- Updated required MtG API version to 1.7.7 to simplify non-active gun update logic in a few places
- Fixed bad hand offset for one frame of Alligator reload animation
- Removed some debug console output spat out when firing English

## 1.16.3 (2024-07-28)

- Fixed null deref caused by English trying to reuse despawned ball VFX after changing floors
- Fixed null deref in Scotsman and Maestro caused by trying to update targeting reticles during no-input states
- Fixed null deref caused by Echo Chamber trying to read sprites from projectiles that don't have them (e.g., Heck Blaster)
- Fixed some potential null derefs in [REDACTED] setup code caused by looking up sprite data for nonexistent sprites
- Added extra line of dialog to [REDACTED] explaining [REDACTED] a little better

## 1.16.2 (2024-07-27)

#### Features:

- Added masteries for Femtobyte, Uppskeruvel, and Blackjack

#### Balance Changes:

- Reworked Tranquilizer:
	- Tranquilized enemies now drop their guns with 5% of their ammo for the player to pickup
	- Tranquilized enemies with guns also have a 25% chance to drop a small ammo pickup
	- Changed to UTILITY gun class
- Tweaked Breegull:
	- Increased ammo from 320 to 480
	- Increased fire egg ignition chance from 50% to 100%
	- Reduced ammo cost of clockwork egg from 4 to 3
- Added Electric damage type to Zag projectiles
- Increased Zag's ammo from 400 to 600
- Increased King's Law's ammo from 700 to 1000
- Increased Macchi Auto's damage over time effect strength by about 50%
- Increased Telefragger's post-teleport invulnerability time from 1.25s to 1.5s
- Changed K.A.L.I.'s charge times from (1s, 2.5s, 4.5s) to (1s, 2s, 3s)
- Increased Nycterian's max distraction range
- Increased Nyterian's chance to distract enemies at all ranges
- Femtobyte now automatically flips any tables it materializes

#### Polish:

- Added impact sounds and VFX for Paintball Cannon, B.B. Gun, BlasTech F-4, Femtobyte, Iron Maid, Uppskeruvel, and Zag
- Added muzzle flashes for B.B. Gun, BlasTech F-4, and Zag
- Added better custom projectile for BlasTech F-4
- Made detonation sound for Scotsman a constant volume instead of scaling with number of projectiles

#### Bugfixes and Misc:

- Reduced lag spikes even further by tweaking mod dll export settings
- Updated required Alexandria version to 0.4.6 to fix a few hacky workarounds
- Fixed vanilla bug with ammo display vfx rendering below final clip sprites
- Fixed sawblades spawned by Femtobyte having no collision with enemies
- Fixed Scotsman projectiles sometimes failing to detonate and lingering on the map
- Fixed null deref caused by King's Law trying to set the alpha of nonexistent muzzle flashes
- Fixed some beam and sprite trails having weird offsets when moving at high speeds
- Fixed potential bug with being able to drop dummy items on death during co-op mode
- Fixed bad hand offset for one frame of Zag reload animation

## 1.16.1 (2024-07-25)

- Added 5 new synergies
- Optimized VFX creation code to reduce memory usage and lag spike frequency
- Optimized vanilla pointcast code to reduce memory usage and lag spike frequency
- Updated Quarter Pounder's turn-to-gold effect to use actual shaders instead of ad-hoc sprites
- Fixed Insurance Shop being able to spawn multiple times per run with new "Default" spawn setting
- Fixed null dereference in Femtobyte due to checking for collisions with nonexistent objects
- Fixed camera remaining static when switching away from or dropping Ticonderogun while firing it
- (Re-)fixed gun sprites being partially white in synergy notifications

## 1.16.0 (2024-07-20)

#### Features:

- Added English, Tryhard Snacks, Weighted Robes, Bulletproof Tablecloth, and Femtobyte
	- Tryhard Snacks, Bulletproof Tablecloth, and Femtobyte sprites thanks to Dallan!
	
#### Balance Changes and Polish:

- Added secondary reload button config option to make some of the mod's guns easier to use on controller
- Made Insurance Shop spawn randomly like other shops by default
	- Guaranteed spawns can be re-enabled in the Mod Config menu
- Made Gun Powderer take gun quality into account when determining how many ammo boxes to create
	- Changed quality of Gun Powderer from A to B to compensate
- Tweaked Iron Maid:
	- Made movement smoother for Iron Maid's targeting reticle
	- Increased lock on range for Iron Maid's targeting reticle
	- Fixed launch delay on Iron Maid's projectiles not accounting for projectiles that were destroyed while in stasis
- Rebalanced Vacuum Cleaner spawn rate by changing it from the CHARGE gun class to a new UTILITY gun class
- Made BlasTech F-4 ignore boss damage caps
	
#### Bugfixes and Misc:

- Fixed Wedding Ring being *completely nonfunctional* since version 1.0.0 D:
- Fixed ghost cables persisting if Alligator is dropped and picked back up
- Fixed coroutine continue failure error in the debug console when switching to Aimu Hakurei or Alyx
- Fixed Racket Launcher's idle animation playing when found in a chest
- Fixed vanilla bug where hovering guns shoot from the wrong place if created while the player is facing left
- Fixed broken midgame save serialization of guns
- Fixed Companion Shop having a weird floor when spawned in the Mines
- Blacklisted several guns from blessed runs that had the potential to cause softlocks if obtained
- Adjusted the volume of a few sounds

## 1.15.1 (2024-07-11)

- Made Insurance Policy chest spawn at the start of any run, even if the shortcut elevator is used
- Reduced Pincushion projectile damage from 0.5 to 0.35
- Fixed Gun Powderer being able to spawn more than 5 ammo boxes
- Fixed dummy items from appearing in the inventory on the victory / death screen
- Fixed Alligator, Maestro, and Vladimir having extremely janky animations when dropped and picked back up
- Fixed Missiletoe making items unable to be picked up if they were wrapped during their ground-bounce animation after being dropped

## 1.15.0 (2024-07-05)

#### Features:

- Added Volcanic Ammolet, BlasTech F-4, and Telefragger
	- Volcanic Ammolet sprite thanks to Dallan!
- Added masteries for Vacuum Cleaner, Paintball Cannon, Gunbrella, Alyx, and Pistol Whip
- Added 6 new synergies

#### Balance Changes and Polish:

- Overhauled Gunbrella:
	- Tweaked Gunbrella projectiles so that they actually fall closer to the reticle rather than overshooting it
		- Reduced Gunbrella projectile damage from 16 to 8 since it hits more accurately now
	- Made each of the 16 separate projectiles fired by Gunbrella cost 1 ammo
		- Increased Gunbrella's ammo from 60 to 960 to compensate for increased ammo usage
	- Made reticle for Gunbrella render behind objects
	- Added trails to Gunbrella projectiles

- Tweaked Vacuum Cleaner:
	- Added indicator for amount of debris vacuumed to Vacuum Cleaner ammo display
	- Vacuum Cleaner debris is now processed when it is actually absorbed, rather than when it is suctioned

#### Bugfixes and Misc:

- Fixed all GungeonCraft weapons appearing as "Semiautomatic" in the Ammonomicon
- Fixed impact particles for Pistol Whip's melee hit not appearing after the first time it is used
- Fixed R.C. Launcher projectiles slowing down too much at higher frame rates
- Fixed dropped Jugglernaut guns having collision that can interfere with the player's movement
- Fixed (hopefully) issue with Jugglernaut occasionally dropping its combo for no apparent reason
- Fixed [REDACTED] from using a certain attack too close to walls, making it undodgeable
- Fixed sprites for [REDACTED] being jittery on some GPUs
- Fixed sprites for [REDACTED] rendering too far above player before boss fight
- Fixed enemies that self-destruct (e.g., Pinhead and Nitra) not functioning properly when spawned in with Alien Nailgun
- Fixed several potential null derefs in guns and items that assume the player is standing in a valid room (as opposed to, e.g., a hallway)
- Fixed several null derefs caused by Magunet debris projectiles not resetting their sprites properly when launched

## 1.14.3 (2024-06-26)

- Fixed Zag projectiles not pathfinding properly when targeting enemies while in contact with a wall
- Fixed null deref due to Zag projectile trails trying to disconnect from nonexistent bodies
- Fixed null deref when switching to King's Law caused by trying to set the alpha of nonexistent muzzle VFX
- Fixed Chamber Jammer's effects not persisting when reloading a midgame save
- Fixed Armor Piercing Rounds skipping phases for certain modded bosses and breaking everything

## 1.14.2 (2024-06-17)

- Fixed null deref in 4D Bullets caused by trying to apply a shader to a nonexistent sprite (for real this time)
- Fixed null derefs in Comfy Slippers and Safety Gloves due to running updates while loading a new floor
- Fixed invalid access error in Voodoo Doll due to enemies being removed from an internal list if they are killed

## 1.14.1 (2024-06-14)

#### Balance Changes and Polish:

- Overhauled Jugglernaut:
	- Changed projectile sprites to match the juggled gun sprites
	- Added animations for dropping current juggle combo
	- Added projectile impact sounds
	- Added circus music :D
- Overhauled Blackjack:
	- Blackjack's projectile speed now scales with the player's accuracy stat
	- Clarified in Blackjack's description that projectile damage scales with accuracy (this was always true, but never mentioned)
	- Increased base speed of Blackjack's projectiles from 18 to 22
	- Reduced base damage of Blackjack's projectiles from 8 to 6
	- Added projectile impact sounds 
	- Added projectile impact particles
- Tweaked Iron Maid:
	- Added targeting reticle to Iron Maid
	- Made Iron Maid projectiles launch towards target enemy's current position at time of launch (rather than their old position at time of reload)
- Made visual effects for Uppskeruvel, Magunet, and Vacuum Cleaner framerate independent

#### Bugfixes and Misc:
- Fixed potential division by zero error in Blackjack if used by a character with perfect accuracy
- Fixed gun sprites sometimes being flipped upside down when finishing a Gyroscope dodge roll
- Fixed "Projecting, Much?" synergy being completely nonfunctional due to 4D Bullets checking for the wrong synergy
- Fixed null deref in 4D Bullets caused by trying to apply a shader to a nonexistent sprite

## 1.14.0 (2024-06-10)

#### Features and Polish:
- Added Widowmaker, MM: Aiming, Calculator, and Zag
	- MM: Aiming and Calculator sprites thanks to Dallan!
- Added masteries for Hand Cannon, Schrodinger's Gat, Hatchling Gun, Crapshooter, and Holy Water Gun
- Added 7 new synergies!

#### Balance Changes:
- Overhauled Ki Blast:
	- Changed quality from B to A
	- Changed ammo from infinite to 20
	- Now uses Ki as ammo, which recovers over time while the player is not shooting
	- Reflected projectiles now have yellow trails so they look less like enemy bullets
	- Clarified in description that projectiles can break boss DPS caps (this was always true, but never mentioned)
- Made Companion and Barter Shops spawn on random floors by default
	- Added configuration option to toggle "classic" guaranteed shop spawns
- Made Custodian's Badge undroppable to avoid cheesing for infinite casing chances
- Frisbee is now launched in the player's current movement direction if they are moving

#### Bugfixes and Misc:
- Fixed null deref in Ki Blast caused by trying to redirect projectiles to nonexistent enemies
- Fixed null deref in Subtractor Beam caused by trying to get the room of nonexistent owners
- Fixed null deref when switching to Jugglernaut caused by trying to find a nonexistent animation
- Fixed Astral Projector allowing the player to phase around boss doors and skip boss triggers
- Fixed Frisbee being rideable over Bello's shop counter, leading to getting stuck out of bounds
- Fixed Bubble Wand being able to give Bubble Blasters to bosses
- Fixed Bubble Wand throwing null derefs for enemies that try to predict the player's postion
- Fixed misaligned sprites for Companion Shop owner
- Fixed chicks spawned by Hatchling Gun vanishing immediately when colliding with enemies at specific angles
- Added impact VFX to Hand Cannon's projectiles
- Reduced volume on Magunet's attract sound

## 1.13.1 (2024-06-02)

#### Features and Polish:
- Added 3 new synergies!

#### Balance Changes:
- Prevented Alien Nailgun from spawning Jammed enemies
- Frisbee can now be instantly ridden if used during a dodge roll
- Added MM: Reloading and Blank Checks to list of items made free by Library Cardtridge
- Killing a Jammed enemy with the Holy Water Gun now reduces curse by 0.5 instead of 1
- Alligator now builds charge slightly faster while moving over carpeted floors

#### Bugfixes and Misc:
- Fixed null deref caused by throwing Suncaster while prisms are deployed
- Fixed certain projectiles from Alien Nailgun replicants not dealing damage
- Fixed weird Frisbee-riding offsets on some characters
- Fixed Insurance Policy issue where insuring an item would visually mark all items of the same type as insured
- Fixed several small typos in Ammonomicon descriptions and lore
- Tweaked Camping Supplies' campfire sprite to be a little less washed-out
- Lowered volume for Scotsman's shooting and detonation sounds
- Tweaked reticle rendering code for Gunbrella, Scotsman, and Maestro to make things look a little nicer

## 1.13.0 (2024-05-27)

#### Features and Polish:
- Added Frisbee, Maestro, and Starmageddon
	- Maestro and Starmageddon sprites thanks to Dallan!

#### Balance Changes:
- Changed Pincushion's class from BEAM to FULLAUTO
- Made Suncaster's prisms bounce off walls 

#### Bugfixes and Misc:
- Made logic for cleaning up temporary startup patches execute even if startup horrendously fails for some reason, preventing other mods from breaking
- Added failsafe to prevent Suncaster prisms from getting stuck inside walls or other objects
- Prevented Camping Supplies from recalculating stats every single frame while moving
- Fixed Holy Water gun registering duplicate enemy events every time it was dropped and picked up
- Fixed Macchi Auto Ammonomicon icon to match in-game sprite
- Fixed Camping Supplies causing lag by trying to invoke a non-existent method
- Fixed accidental removal of stunning behavior from Platinum Star
- Fixed sprites for 50-casing pickups being replaced by Scavenging Arms' small ammo pickup sprites
- Fixed mastery ritual not working in certain rooms that it should've worked in
- Fixed item tips breakage for items with colons and apostrophes in their names
- Slightly sped up Alligator's reload animation so it actually finishes playing
- Removed occlusion from Gunbrella projectiles to increase their visibility and make them look nicer
- Tweaked Chamber Jammer's description for clarity

## 1.12.0 (2024-05-05)

#### Features and Polish:
- Added Macchi Auto, Nycterian, Scavenging Arms, Armor Piercing Rounds, and MM: Reloading
	- Armor Piercing Rounds and MM: Reloading sprites thanks to Dallan!
- Added gun mastery system
- Added masteries for Grandmaster, Chekhov's Gun, Pincushion, Platinum Star, and Natascha
- Added 18 new hats
- Bulletbot Implant, Bionic Finger, and Gorgun's Eye can now spawn in Handy's shop from [Knife to a Gunfight](https://thunderstore.io/c/enter-the-gungeon/p/Skilotar/Knife_to_a_Gunfight/)!

#### Bugfixes and Misc:
- Fixed issue with knight pieces fired from Grandmaster getting stuck in infinite loops when no enemies are around
- Fixed Aimu Hakurei being able to build graze off of co-op partner's bullets
- Fixed null deref in Safety Gloves due to trying to play VFX above despawned enemies
- Fixed visual bug with misaligned muzzle flash for Holy Water Gun 
- Fixed bug (for the second time) where loading the secret floor after returning to the Breach would cause an infinite loading screen
- Lowered volume on Natascha spinup sound

## 1.11.5 (2024-04-20)

#### Bugfixes:
- Fixed null deref due to Plot Armor checking whether a non-existent room is a boss foyer
- Fixed null deref in Ki Blast when trying to apply knockback to enemies without KnockbackDoers
- Fixed null deref in Ki Blast when used against the Dragun
- Fixed not being able to play the Glockarina when using a controller
- Prevented Quarter Pounder from creating duplicate gold sprites for enemy types that have already been transmuted to gold

#### Balance Changes:
- Buffed Glocakrina's base damage from 4 to 7.5
- Buffed Magunet's debris damage from 1 to 2
- Buffed Magunet's corpse damage from 10 to 30
- Buffed Jugglernaut's fire rate from 1.0 seconds to 0.4 seconds
- Increased Jugglernaut's ammo from 150 to 240
- Pistol Whip now only passively increases curse by 2 instead of 3
- Enemies transmuted with Quarter Pounder now drop an extra casing upon death
- Aimu Hakurei can now build graze even when it's not the active gun
- Doubled Sub Machine Gun's charm chance, making enemies at 50% health or less 100% susceptible to charm
- Changed Scotsman's quality from A to B
- Changed Ammo Conservation Manual's quality from B to C
- Changed Dead Ringer's quality from A to B
- Changed Gyroscope's quality from A to B
- Changed Amazon Primer's quality from A to B
- Changed Chest Scanner's quality from B to C
- Removed Insurance Policy from normal item pool (so it can only be purchased at Insurance Shop)

## 1.11.4 (2024-04-12)

- Fixed null deref due to Gorgun Eye scanning for enemies without an active room
- Fixed null deref due to Seltzer Pelter projectiles trying to emit seltzer streams from expired projectiles
- Fixed null deref due to Insurance Policy trying to reuse previously-destroyed VFX on insured items
- Fixed null deref due to Aimu Hakurei (and potentially Alyx) trying to continue running a coroutine after being destroyed
- Fixed ghost sprites of items remaining on screen after activating Kaliber's Justice 

## 1.11.3 (2024-04-11)

#### Bugfixes:
- Fixed invalid operation exception when trying to apply Soul Kaliber's soul link status effect to enemies
- Fixed Magunet retaining it's charge when switched or dropped while charging
- Fixed Magunet charge particles getting stuck in the air when switched or dropped while charging
- Fixed softlock caused by getting hit after dropping Warrior's Gi and starting a new floor
- Fixed Ticonderogun being able to hit invulnerable enemies
- Fixed insurance chests spawning past the first floor when continuing a saved run
- Fixed null deref in Borrowed Time due to checking for whether non-existing rooms are combat rooms
- Fixed random invalid operation exceptions when trying to use Borrowed Time

#### Balance Changes:
- Added Ballot to list of items purchasable for free by Library Cardtridge
- Chicks spawned by Hatchling Gun no longer deal obscene collision damage to enemies
- Seltzer water from Seltzer Pelter's projectiles can now be electrified like normal water 
- Switched Bouncer from C to D quality since it's ridiculously hard to use effectively

#### Misc:
- Renamed Curator's Badge to Custodian's Badge, since I apparently forgot the difference between the two
- Updated description of Custodian's Badge to mention removal from inventory after letting too many breakables break
- Added directional sprites for a few hats
- Updated Thunderstore Icon, huge thanks to Lynceus for the new one! :D <3

## 1.11.2 (2024-04-07)

- Added 11 hats! (sprites thanks to Dallan)
- Migrated all cAPI code to Alexandria and updated required Alexandria version to 0.4.0
- Fixed null deref in Comfy Slippers due to trying to get position of nonexistent owner when updating on the ground
- Fixed null deref in Emergency Siren due to trying to check the user's current room without an active user
- Fixed Vladimir impale point extending too far beyond the end of the weapon
- Fixed wonky collision detection on shopkeepers
- Made Pistol Whip quieter when used with Scattershot
- Made Lightwing projectiles no longer restore ammo if fired for free (e.g., with Scattershot)

## 1.11.1 (2024-04-04)

- Fixed hang when trying to access the hat room after starting a run and returning to the Breach 

## 1.11.0 (2024-04-04)

- Added 56 cosmetic hats! :D
	- Hats are accessible via a new hat room near Winchester in the Breach
	- Hat sprites thanks to Dallan!
	- Hat room pedestal, entrance, and exit sprites thanks to Lynceus!
- Fixed Gorgun Eye constantly making noise every frame

## 1.10.4 (2024-03-27)

- Fixed Subtractor Beam being completely broken and causing numerous null reference exceptions when killing enemies with health values visible
- Fixed Hand Cannon being able to stun enemies that haven't spawned in yet
- Fixed Hand Cannon dealing 0 damage to enemies it can't stun
- Fixed Hand Cannon dealing 0 damage to enemies it can't knock back
- Fixed Quarter Pounder's midas effect applying to bosses and minibosses, potentially softlocking the game (e.g., against the Dragun)
- Fixed Omnidirectional Laser reticle and renderer completely breaking when loading a new floor
- Fixed Omnidirectional Laser renderer breaking when dropped and picked back up

## 1.10.3 (2024-03-24)

#### Bugfixes:
- Fixed several guns being able to target enemies internally marked as "not worth shooting at"
- Fixed idle animation speed for Ki Blast, Deadline, Schrodinger's Gat, Racket Launcher, Subtractor Beam, Wavefront, and Alien Nailgun not being set up correctly after a recent update
- Fixed Grandmaster, Scotsman, Bouncer, B. B. Gun, Seltzer Pelter, Carpet Bomber, Lightwing, and Blackjack projectiles freaking out when used with Helix Bullets
- Limited length of Aimu Hakurei, Subtractor Beam, and Omnidirectional Laser projectile trails after bouncing
- Fixed Carpet Bomber, Lightwing, and Blackjack projectiles freaking out when used with Orbital Bullets
- Fixed Pistol Whip, Jugglernaut, and Deadline projectiles not respecting aim deviation from items like Backup Gun or Scattershot
- Fixed King's Law and Iron Maid not launching bullets when player has instant reloads
- Blacklisted Racket Launcher and Seltzer Pelter projectiles from being affected by Orbital Bullets
- Fixed Scotsman and Ki Blast projectiles ignoring reticle when used with Remote Bullets
- Fixed Gunbrella projectiles being non-functional when fired using Duct Tape or Ring of Triggers
- Fixed Gunbrella firing Backup Gun projectiles forwards
- Silenced Gunbrella debug log spam when duct taped to another gun
- Fixed Scotsman projectiles all being sent in the same direction with Backup Gun
- Fixed Scotsman projectiles despawning immediately when not spawned with Scotsman (e.g., with Duct Tape or Chance Bullets)
- Fixed Ki Blast firing Backup Gun and Ring of Triggers projectiles forwards
- Fixed Ki Blast projectiles ignoring effects of Helix Bullets
- Fixed deployed Chekhov's Gun sightlines following the cursor when the player has Remote Bullets
- Fixed deployed Chekhov's Gun sprites appearing far away from the player's gun sprites with certain rotations
- Fixed null dereference caused by spawning Iron Maid projectiles from other guns (e.g., with Duct Tape or Chance Bullets)
- Fixed null dereference when dropping Iron Maid with active projectiles
- Fixed Natascha firing every single frame with certain projectile-duplicating items such as Scattershot, Backup Gun, and Helix Bullets
- Fixed extremely high fire rate for guns duct-taped to Natascha
- Fixed B. B. Gun being able to launch glitchy uncharged projectiles
- Fixed freebie projectiles from B. B. Gun being able to restore ammo
- Fixed Deadline projectiles not working properly when not spawned from Deadline (e.g., with Duct Tape or Chance Bullets)
- Fixed null dereference when K.A.L.I. projectiles are spawned by Ring of Triggers
- Fixed Grandmaster projectiles continuously bouncing against walls with Bouncy Bullets
- Fixed idle animation for Crapshooter playing way too fast
- Fixed Omnidirectional Laser secondary projectiles (e.g., from Backup Gun and Scattershot) always firing towards the reticle and overlapping the original projectile
- Fixed Pincushion firing Backup Gun projectiles forwards
- Fixed null dereference in Schrodinger's Gat due to incorrectly referencing projectiles belonging to despawned enemies
- Fixed chicks spawned from the Hatchling Gun not being marked as harmless enemies and being targeted by things they shouldn't be
- Fixed Subtractor Beam not displaying health for neutral enemies like Chance Kin or Key Bullet Kin
- Fixed Blamethrower being able to scapegoat enemies outside of the current room
- Fixed Wavefront projectiles noisily colliding with player companions
- Fixed Holy Water Gun having an invisible beam when used with Orbital Bullets

#### Balance Changes:
- Reduced Holy Water Gun ammo from 500 to 100
- Reduced Holy Water Gun knockback from 50 to 15
- Made Holy Water Gun respect bullet damage modifiers (previously dealt fixed damage)
- Tweaked Natascha spin up mechanics, spin up rate, and max spin up speed
- Made Gunbrella projectiles target a random enemy when no Gunbrella reticle is present
- Gave Bouncer projectiles stacking bounces with Bouncy Bullets
- Gave Deadline hitscan projectiles
- Gave Pistol Whip infinite ammo for its pistol projectile (whip itself already had infinite ammo)
- Made Pistol Whip take spread into account instead of having perfect accuracy

## 1.10.2 (2024-03-22)

#### Bugfixes:
- Fixed Insurance Shop sometimes spawning on the first floor even without an S or A tier item in the player's inventory
- Fixed Holy Water Gun's beam hitbox not extending all the way to its point of collision
- Fixed Holy Water Gun spamming the debug log due to improperly-set colliders
- Fixed Blank Checks inconsistently activating before / after triggering blanks (sometimes using one, sometimes not)
	- Blank Checks now consistently activates prior to triggering blanks, so using a blank with none in your inventory will always give 3 blanks and use 1 immediately
- Fixed Blank Checks spamming the debug log whenever attempting to use a blank
- Fixed Adrenaline Shot still being able to activate after it's been dropped
- Fixed King's Law breaking if it somehow manages to spawn hundreds of bullets in stasis

#### Polish:
- Bulletbot Implant, Carpet Bomber, King's Law, Ammo Conservation Manual, and Stunt Helmet can now appear in various vanilla / modded sub-shops
- Cuppajoe cooldown bar now shows caffeine time remaining while ticking down and crash time remaining while ticking up
- Added sound effect for tanking damage while Adrenaline Shot is active
- Gave Paintball Cannon a slightly larger projectile
- Tweaked Insurance Shopkeeper's animations

## 1.10.1 (2024-03-20)

- Fixed startup crash due to Omnidirectional Laser trying to read non-existent attach points

## 1.10.0 (2024-03-20)

#### Features and Polish:
- Added Omnidirectional Laser, R.C. Launcher, Breegull, Sub Machine Gun, Reflex Ammolet, Chest Scanner, and Bulletbot Implant
	- Omnidirectional Laser, Reflex Ammolet, Chest Scanner, and Bulletbot Implant sprites thanks to Dallan!
	- Omnidirectional Laser concept thanks to Duudle!

#### Bugfixes and Misc:
- Fixed issue with faulty sprite references for Alligator, Aimu Hakurei, Blamethrower, and [REDACTED] projectile impact VFX, causing a large white flame to appear instead
	- Added debug code to warn about faulty VFX sprite reuse to prevent similar bugs from occurring in the future
- Fixed misplaced projectile when firing Hand Cannon
- Fixed bad hook causing failures when trying to give certain enemies guns outside of Hecked Mode
- Cleaned up sound handling code for guns / projectiles to make adjusting sounds slightly easier in the future
- Substantially lowered Pincushion's firing sound

## 1.9.1 (2024-03-10)

- Fixed numerous items and guns causing issues when removed directly from the inventory (e.g., when using Gun Munchers)
- Fixed Blamethrower scapegoat effect persisting after dropping the gun

## 1.9.0 (2024-03-10)

#### Features and Polish:
- Added K.A.L.I., Alien Nailgun, Reserve Ammolet, and Gun Synthesizer
	- Alien Nailgun sprites thanks to Lynceus!
	- Reserve Ammolet and Gun Synthesizer sprites thanks to Dallan!
- Added Insurance Shop
	- Spawns if an A or S tier item is in your inventory at the beginning of a floor
	- Sells Insurance Policy items that let you carry over an item to your next run
	- Room and NPC design thanks to Lynceus!

#### Bugfixes:
- Fixed a null dereference when Alligator's projectiles hit enemies without HealthHavers
- Fixed certain guns in Hecked mode causing null reference exceptions when wielded by enemies
- Fixed some graphical glitches with Insurance Policy sprites
- Fixed some graphical glitches with various projectile trails

#### Misc:
- Dramatically improved startup time due to better sprite loading techniques
- Reduced RAM and VRAM usage by packing all graphics into a single texture
- Silenced Seltzer Pelter's projectiles after a few bounces so they don't cause a racket if they get stuck
- Made Deadline's fire and reload sounds quieter

## 1.8.2 (2024-02-21)
- Fixed accidentally making all bosses jammed whoops D:

## 1.8.1 (2024-02-20)
- Fixed null dereference causing Gyroscope and Drifter's Headgear to break after boss fights
- Fixed some pickup bounding box weirdness on Vladimir, Blamethrower, and Suncaster
- Added ammo clip sprites for Suncaster and Blamethrower thanks to Lynceus!
- Added some missing sprite credits

## 1.8.0 (2024-02-11)

#### Features and Polish:
- Added Vladimir, Blamethrower, and Suncaster
	- Vladimir sprite thanks to Dallan!

#### Bugfixes:
- Fixed another rare edge case where Astral Projector could get the player stuck in walls
- Fixed Gyroscope and Drifter's Headgear preventing the player from dodge rolling in the Aimless Void
- Fixed Gyroscope and Drifter's Headgear polluting the debug log with error messages when used
- Fixed Gorgun's Eye and Hand Cannon attempting to stun unstunnable enemies
- Fixed several projectiles ignoring the frame rate during physics calculations
- Fixed several visual effects ignoring the frame rate during physics calculations
- Fixed a few typos in item descriptions

#### Misc:
- Added instructions for enabling Hecked Mode to readme 
- Made companion shopkeeper no longer face towards player since the shading looked weird

## 1.7.0 (2024-01-20)

#### Features and Polish:
- Added Hecked Mode O_O
	- Enable it in the Mod Config pause menu (Options -> Mod Config -> GungeonCraft)
- Added Scotsman, Chekhov's Gun, Ring of Defenestration, Ammo Conservation Manual, and Stop Sign
	- Ring of Defenestration, Ammo Conservation Manual, and Stop Sign sprites thanks to Dallan!
- Added several new sprites thanks to Lynceus!
	- Brand new item sprites for John's Wick, Emergency Siren, and Amazon Primer
	- Polished item sprites for Cozy Camper and Gyroscope
	- Polished projectile sprites for Carpet Bomber
	- Custom ammo clip sprites for Wavefront
	
#### Balance Changes:
- Library Cardtridge now makes "books and paper-based items" free at shops, which now includes Origuni

#### Bugfixes and Misc:
- [Gunfig](https://enter-the-gungeon.thunderstore.io/package/CaptainPretzel/Gunfig/) is now a required dependency of GungeonCraft 
- Toned down volume of Pistol Whip sound effects
- Clarified in Emergency Siren's description that it opens sealed combat doors rather than actual locked doors
- Fixed potential null dereference when Magunet and Vacuum Cleaner try to grab inactive debris
- Fixed backwards sprite for Pincushion in Ammonomicon
- Fixed a few typos in various item descriptions in Ammonomicon
- Added some missing sprite credits in earlier Changelog entries

## 1.6.1 (2024-01-07)

#### Features and Polish:
- Overhauled sprites for companion shopkeeper NPC, thanks to Lynceus!
- Added critters to the companion shop and changed the room to be more outdoors-y

#### Balance Changes:
- Companion shop discount reduced from 50% to 30% (i.e., companions now cost 70% of full price)
- Reworked Natascha
	- Reloading now toggles whether the gun remains spun up when not firing, maintaining its increased fire rate and decreased movement speed
	- Projectiles now apply a slow effect on hit
	- Decreased max ammo from 2500 to 1500
	- Gun class changed from BEAM (why???) to FULLAUTO 
	- Added spin up and spin down sounds
	- Added casing sprites
- Reworked Bouncer
	- Projectiles now bounce 3 times instead of once, exploding only after bouncing 3 times
	- Projectile damage decreased by 40% to compensate for increased longevity
	- Projectile damage now scales with player's damage stat
	- Reload time increased from 0.8 seconds to 1.3 seconds
	- Clarified in Ammonomicon that projectiles' damage scales with speed (which was always true, but never mentioned)
	- Fixed bug where projectile acceleration and bounce time were tied to the frame rate (yikes)
	- Fixed projectiles sometimes getting stuck in walls
	- Added brand new reload animation and sounds
	- Tweaked firing animation and firing sound volume
- Reworked Tranquilizer
	- Hitting an enemy with multiple projectiles now speeds up the tranquilization process
	- Projectiles no longer deal any damage, only knockback
	- Added new projectile impact effects
	- Added new overhead effects for asleep enemies
	- Added new sound cue for when an enemy is fully tranquilized

#### Bugfixes and Misc:
- Fixed Blasphemy reload sound playing when reloading some guns
- Fixed visual bug during glow phase of Iron Maid causing projectiles to dim before glowing
- Fixed potential null dereference in Jugglernaut weapon panel sprite hook
- Fixed potential null dereference with Gyroscope and Drifter's Headgear custom dodge roll hook

## 1.6.0 (2023-12-30)

#### Features and Polish:
- Added Glockarina, Magunet, Wavefront, Cuppajoe, Safety Gloves, and Drab Outfit
- Added custom ammo clips for Alligator, Carpet Bomber, Crapshooter, Glockarina, King's Law, Lightwing, Subtractor Beam, and Uppskeruvel, thanks to Lynceus!
- Overhauled sprites for barter shopkeeper NPC thanks again to Lynceus!

#### Balance Changes:
- Nerfed Itemfinder: 
	- Reduced max items per floor from 6 (should have been 5 but I'm bad at programming) to 4
	- Reduced chances of finding more than 1 item per floor
- Nerfed Plot Armor: 
	- Reverted 1.2.1 changes so it once again gives at least 1 (not 2) armor and brings the player up to a minimum of 3 (not 4) armor
	- Kept change to A quality so it should still be more common than pre-1.2.1 Plot Armor
	
#### Bugfixes:
- Fixed barter shop not having items if first run was not started through quickstart
- Fixed barter shop and companion shop not spawning at all on the first run if the run was not started through quickstart
- Fixed cross-mod shop item injection not working if first run was not started through quickstart
- Fixed some modded guns with large idle animations dropping in weird places when spawned from chests
- Fixed null dereference in Library Cardtridge caused by scanning for items in nonexistent shops
- Fixed Iron Maid's offset in the weapon display panel

#### Misc:
- Made Adrenaline Shot use true UI sprites (rather than buggy fake UI sprites) for adrenaline hearts
- Made Mod actually properly shows up as "GungeonCraft" in the mod's list
- Removed debug output from Missiletoe that was polluting the console

## 1.5.1 (2023-12-20)
- Made Companion Shop items non-stealable
- Forced Barter Shop to go out of stock on successful steal
- Added steal dialogue to Barter Shop
- Added debug output to track down Barter Shop not having any items sometimes
- Fixed null dereference in Ki Blast caused by looking up sprites for non-existent enemies
- Fixed null dereference in Iron Maid caused by trying to update the Gun while the level is loading
- Fixed null dereference in Subtractor Beam caused by trying to update text to the position of non-existent enemies
- Fixed null dereference in Drifter's Headgear and Gyroscope from not checking if they actually have an owner when destroyed
- Fixed null dereference in Paintball Cannon during Dragun fight due to trying to color a non-existent AIActor

## 1.5.0 (2023-12-10)

#### Features and Polish:
- Added Companion Shop
- Added Bartering Shop
- Added Uppskeruvel

#### Bugfixes:
- Fixed vanilla bug where quick restarting wouldn't properly reset the chances of spawning "Once Per Run" rooms (e.g., specialty shops)
- Fixed vanilla bug in RNG where randomly selecting the first item from a list of two items would always return the first item
- Fixed guns having extremely large interaction ranges in shops
- Fixed beam sprite from Aimu Hakurei's max level not rendering 
- Fixed buggy ammo display caused by repeatedly switching between Aimu Hakurei and an infinite ammo gun 
- Fixed null dereference error caused by trying to use Borrowed Time in an unregistered room
- Fixed null dereference error caused by Alligator sparks trying to move towards a non-existent enemy 
- Fixed items not being added to modded shops that spawned on the first floor

#### Misc:
- Insurance Policy can no longer be sold to shops
- Optimized startup loading time to be about 1.5x faster
- Updated Alexandria to 0.3.26 for faster UI sprite loading (thanks SomeBunny!)

## 1.4.0 (2023-12-01)

#### Features and Polish:
- Added King's Law, Pincushion, Crapshooter, and Carpet Bomber
- Implemented proper mid-game saving behavior for Missiletoe, Alyx, Curator's Badge, Wedding Ring, Borrowed Time, and Warrior's Gi

#### Balance Changes:
- Reaching the floor's exit while Adrenaline Shot is active now automatically cancels the effect and brings you to half a heart

#### Bugfixes:
- Fixed vanilla bug preventing duct-taped guns from serializing properly
- Fixed issue with guns having extremely large pickup ranges
- Fixed issue with guns hovering far above / below reward pedestals if spawned

## 1.3.0 (2023-11-22)
- Added Subtractor Beam, Alligator, Lightwing, Stunt Helmet, Comfy Slippers, and Chamber Jammer
	- Stunt Helmet and Chamber Jammer sprites thanks to Dallan!
- Added muzzle effects for all guns!
- Added custom projectile and ammo clip to Platinum Star, thanks to Lynceus!
- Added hand animations for Ki Blast
- Added proper reload sound for Deadline
- Improved gun sprite for Alyx
- Improved projectile sprites for Soul Kaliber and Platinum Star
- Improved collision detection substantially for Deadline lasers

## 1.2.1 (2023-11-13)
- Buffed Plot Armor: changed from S to A quality, now always gives at least 2 armor and brings the player up to a minimum of 4 armor
- Added a sound cue to Vacuum Cleaner for when ammo is successfully restored
- Made blue bullets for boss' blue attack more...blue (they were purple-ish previously)
- Polished sprites for Itemfinder and gave it a small animation while active
- Fixed vanilla bug preventing second player in co-op turbo mode from receiving increased speed until their stats were changed at least once
- Fixed Vacuum Cleaner not being able to vacuum up some projectiles that were converted to debris
- Fixed occasional animation error when picking up Jugglernaut
- Fixed occasional null dereference error when Alyx tries to spawn poison goop
- Fixed boss floor music not looping cleanly

## 1.2.0 (2023-11-13)
- Added Platinum Star, Pistol Whip, Jugglernaut, and Adrenaline Shot
- Added synergy support for ItemTips in preparation for future synergies

## 1.1.3 (2023-11-06)
- Fixed chicks spawned from Hatchling Gun causing errors every frame of their existence D:
- Fixed items unwrapped from Missiletoe not being pickup-able
- Added more custom ammo clip sprites and Missiletoe projectile variants thanks to Lynceus!
- Nerfed Gunbrella ammo and Blackjack clip size slightly 

## 1.1.2 (2023-11-06)
- Fixed (kinda) null dereference randomly preventing the Dragun fight cutscene from triggering, and added some debug output in case anyone is ever able to reproduce it again
- Fixed Aimu Hakurei being able to graze bullets that have already despawned
- Fixed a few edge cases for Astral Projector that could get you stuck in walls when pausing, opening the Ammonomicon, or opening a debug console

## 1.1.1 (2023-11-05)
- New custom projectile and ammo clip sprites for several guns thanks to Lynceus :D
- Added full support for the [ItemTips](https://enter-the-gungeon.thunderstore.io/package/Glorfindel/ItemTips/?utm_source=discord) mod for all GungeonCraft items
- Updated to ModTheGungeon API 1.7.0 for faster load times (thanks SpecialAPI!)
- Fixed an issue where a few console commands from other mods would be disabled when GungeonCraft is loaded
- Fixed Tranquilizer being able to stun enemies immune to stun
- Fixed Thunderstore preview icon image link in Credits page

## 1.1.0 (2023-11-03)

#### Features and Polish:
- Added Aimu Hakurei, Seltzer Pelter, Missiletoe, Bubble Wand, Insurance Policy, and Ice Cream
	- Insurance Policy and Missiletoe sprites thanks to Dallan!
- Items can now be found in specialty shops (subshops), including support for modded shops (Planetside of Gunymede & Once More Into the Breach)
- Added a few new sprites for Bullet Kin (they love ice cream :>)
- All of the mod's guns now spawn with their idle animations playing by default (before, they would be stuck on the first frame)
- Use a new, faster exponent approximation in a few places to reduce lag

#### Balance Changes:
- Ticonderogun's damage now scales inversely with the number of enemies encircled rather than inversely with circle size, resulting in higher damage on average
- Dead Ringer now has a short grace period after activating to make it less likely you will immediately lose your cloak
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
