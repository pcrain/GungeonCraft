#!/usr/bin/python

import os, re

SOURCE_DIR = "/home/pretzel/workspace/gungy-cwaffing/src"
ITEM_SPRITE_DIR = "/home/pretzel/workspace/gungy-cwaffing/RawResources/ItemSprites/"
GUN_SPRITE_DIR = "/home/pretzel/workspace/gungy-cwaffing/RawResources/Ammonomicon Encounter Icon Collection/"

WIKI_PARAMS = {
  "summary" :
'''
GungeonCraft is a content mod for Enter the Gungeon, focused primarily on weapons, items, and NPCs that introduce novel and engaging mechanics.

You can download the mod or read more about it [https://thunderstore.io/c/enter-the-gungeon/p/CaptainPretzel/GungeonCraft/ on Thunderstore], view the source for the mod [https://github.com/pcrain/GungeonCraft on GitHub], and report issues with the mod [https://github.com/pcrain/GungeonCraft/issues on the GitHub issue tracker] or by pinging Captain Pretzel in the [https://discord.gg/uT7AwbcpyC Mod the Gungeon Discord].

<b>NOTE: the contents of this page are generated automatically using a script.</b> Manual edits might be overwritten (sorry)! Please contact Captain Pretzel in the Mod the Gungeon Discord if anything looks amiss!
''',

  "iconwidth" : "88px",

  "tablestyle" : '''class="wikitable sortable mw-collapsible" style="text-align:center; width: 100%;"''',
}

VANILLA_ITEMS = {
  "hyper_light_blaster" : ("Hyper Light Blaster", "Hyper_Light_Blaster.png"),
  "sunlight_javelin"    : ("Sunlight Javelin", "Sunlight_Javelin.png"),
  "laser_lotus"         : ("Laser Lotus", "Laser_Lotus.png"),
  "bubble_blaster"      : ("Bubble Blaster", "Bubble_Blaster.png"),
  "gun_soul"            : ("Gun Soul", "Gun_Soul.png"),
}

def applyGunDataOverrides(gunData):
  gunData["Bouncer"]["speed"]            = "Varies"
  gunData["Carpet Bomber"]["speed"]      = "Varies"
  gunData["Aimu Hakurei"]["spread"]      = "Varies"
  gunData["Aimu Hakurei"]["firerate"]    = "Varies"
  gunData["Aimu Hakurei"]["ammo"]        = "{{infinity}}"
  gunData["Quarter Pounder"]["ammo"]     = "# of casings"
  gunData["Subtractor Beam"]["damage"]   = "Varies"
  gunData["Pistol Whip"]["damage"]       = "30 (Whip), 15 (Proj.)"
  gunData["Pistol Whip"]["firerate"]     = "0.42" # 0.40 + 1/60, rounded
  gunData["Vacuum Cleaner"]["damage"]    = "N/A"
  gunData["Vacuum Cleaner"]["speed"]     = "N/A"
  gunData["Vacuum Cleaner"]["range"]     = "N/A"
  gunData["Vacuum Cleaner"]["knockback"] = "N/A"
  gunData["Pincushion"]["firerate"]      = "0.02" # 1/60, rounded
  gunData["Pincushion"]["damage"]        = "0.5" # as set by _NEEDLE_DAMAGE

def main():
  passiveData, passiveDataByClass = scanPassives()
  WIKI_PARAMS["passives"] = "".join([PASSIVE_TEMPLATE.format(**v) for k,v in passiveData.items()])

  activeData, activeDataByClass = scanActives()
  WIKI_PARAMS["actives"] = "".join([ACTIVE_TEMPLATE.format(**v) for k,v in activeData.items()])

  masteryData = scanMasteries()

  gunData, gunDataByClass = scanGuns(masteryData)
  applyGunDataOverrides(gunData)
  WIKI_PARAMS["guns"] = "".join([GUN_TEMPLATE.format(**v) for k,v in gunData.items()])

  synergyData = scanSynergies(passiveDataByClass, activeDataByClass, gunDataByClass)
  WIKI_PARAMS["synergies"] = "".join([SYNERGY_TEMPLATE.format(**v) for k,v in synergyData.items()])

  npcData = getNPCs()
  WIKI_PARAMS["npcs"] = "".join([NPC_TEMPLATE.format(**v) for k,v in npcData.items()])

  wikitext = WIKI_TEMPLATE.format(**WIKI_PARAMS)
  print(wikitext)

def getSourceFiles(sourcedir):
  for f in sorted(os.listdir(sourcedir)):
    if f.startswith("_"):
      continue
    path = os.path.join(sourcedir, f)
    if not os.path.isfile(path):
      continue
    yield path

def readAllLines(path):
  with open(path, 'r') as fin:
    return fin.read()

def findPattern(text, patt, default = "???", resolveVars = False):
  m = re.search(patt, text)
  if m is None:
    return default
  res = m.groups()[0]
  if not resolveVars:
    return res
  return resolveVariable(res, text)

def cleanItemName(item):
  return item.replace(" ","_").replace(".","").replace("'","").replace(":","").replace("-","").lower()

def imageFor(filepath):
  return f"""[[File:{filepath}]]"""

def iconForItem(item, nameOnly = False):
  name = f"""{cleanItemName(item)}_icon.png"""
  if nameOnly:
    return name
  return os.path.join(ITEM_SPRITE_DIR, name)

def iconForGun(gun, nameOnly = False):
  name = f"""{cleanItemName(gun)}_ammonomicon.png"""
  if nameOnly:
    return name
  return os.path.join(GUN_SPRITE_DIR, name)

def computeUses(text):
  if findPattern(text, r"""item\.consumable\s*=\s*(.*);""") != "true":
    return "{{infinity}}"
  uses = findPattern(text, r"""item\.numberOfUses\s*=\s*(.*);""", default=None)
  if uses is None:
    return 1
  return int(uses)

def resolveVariable(var, text):
  if re.match(r"""[0-9\.]+f""", var):
    var = var[:-1] # remove trailing f from floats

  try: return int(var)
  except: pass

  try: return float(var)
  except: pass

  varbase = var.split(".")[-1]
  res = findPattern(text, rf"""{varbase}\s*=\s*([0-9\.]+)""", default=None)
  if res is not None:
    return resolveVariable(res, text)

  # print(f" could not resolve {var}")
  return "???"

def computeItemCooldown(text):
  timed = findPattern(text, r"""ItemBuilder\.CooldownType\.Timed\s*,\s*(.+)\)""", default=None)
  if timed is not None:
    timed = resolveVariable(timed, text)
    return f"""{timed} Second{("" if (float(timed) == 1) else "s")}"""

  damage = findPattern(text, r"""ItemBuilder\.CooldownType\.Damage\s*,\s*(.+)\)""", default=None)
  if damage is not None:
    damage = resolveVariable(damage, text)
    return f"""{damage} Damage"""

  room = findPattern(text, r"""ItemBuilder\.CooldownType\.PerRoom\s*,\s*(.+)\)""", default=None)
  if room is not None:
    room = resolveVariable(room, text)
    return f"""{room} Room{("" if (float(room) == 1) else "s")}"""

  return "Instant"

def computeClipSize(text):
  size = findPattern(text, r"""clipSize\s*:\s*(.*?)[\s,\)]""")
  size = resolveVariable(size, text)
  if size == -1:
    return "{{infinity}}"
  return size

def computeAmmo(text):
  infAmmo = findPattern(text, r"""infiniteAmmo\s*:\s*(.*?)(?:\.0)?f?[,\)]""") # range of pea shooter, the default gun
  if infAmmo == "true":
    return "{{infinity}}"
  ammo = findPattern(text, r"""ammo\s*:\s*(.*?)[,\)]""", resolveVars = True)
  return ammo

def computeRange(text):
  gunRange = findPattern(text, r"""range\s*:\s*(.*?)(?:\.0)?f?[,\)]""", default="20") # range of pea shooter, the default gun
  gunRange = resolveVariable(gunRange, text)
  if (gunRange > 1000):
    return "{{infinity}}"
  return gunRange

def computeReloadTime(text):
  reloadTime = findPattern(text, r"""reloadTime\s*:\s*(.*?)(?:\.0)?f?[,\)]""", default="1.5", resolveVars = True) # reload time of pea shooter, the default gun
  if (reloadTime == 0.0):
    return "Instant"
  return reloadTime

def makePrettyDescription(text):
  return text.replace("\\n","<br/>")

def scanPassives():
  data = {}
  classdata = {}
  for f in getSourceFiles(os.path.join(SOURCE_DIR,"Cwaff-Passives")):
    text = readAllLines(f)
    itemname = findPattern(text, r"""ItemName\s*=\s*\"(.*)\";""")
    classname = findPattern(text, r"""public class (.*) : Cwaff""")
    entry = {
      "classname"   : classname,
      "filename"    : imageFor(iconForItem(itemname, nameOnly = True)),
      "size"        : "42",
      "itemname"    : itemname,
      "quality"     : findPattern(text, r"""item\.quality\s*=\s*ItemQuality\.(.);"""),
      "description" : makePrettyDescription(findPattern(text, r"""LongDescription\s*=\s*\"(.*)\";""")),
      }
    data[itemname] = entry
    classdata[classname] = entry
  return data, classdata

def scanActives():
  data = {}
  classdata = {}
  for f in getSourceFiles(os.path.join(SOURCE_DIR,"Cwaff-Actives")):
    text = readAllLines(f)
    itemname = findPattern(text, r"""ItemName\s*=\s*\"(.*)\";""")
    classname = findPattern(text, r"""public class (.*) : Cwaff""")
    entry = {
      "classname"   : classname,
      "filename"    : imageFor(iconForItem(itemname, nameOnly = True)),
      "size"        : "42",
      "itemname"    : itemname,
      "quality"     : findPattern(text, r"""item\.quality\s*=\s*ItemQuality\.(.);"""),
      "description" : makePrettyDescription(findPattern(text, r"""LongDescription\s*=\s*\"(.*)\";""")),
      "cooldown"    : computeItemCooldown(text),
      "numuses"     : computeUses(text),
      }
    data[itemname] = entry
    classdata[classname] = entry
  return data, classdata

def scanMasteries():
  data = {}
  masteryFile = os.path.join(SOURCE_DIR, "Cwaff-Misc", "CwaffSynergies.cs")
  lines = readAllLines(masteryFile).split("\n")
  mrx = re.compile(r"""^\s*NewMastery.*,\s*([A-Za-z0-9]+)\.ItemName""")
  for i, line in enumerate(lines):
    if not (r := mrx.match(line)):
      continue
    gun = r.groups()[0]
    desc = re.sub(r"""^\s*//\s*""","", lines[i-1])
    data[gun] = desc
  return data

def scanSynergies(passives, actives, guns):
  data = {}
  synergyFile = os.path.join(SOURCE_DIR, "Cwaff-Misc", "CwaffSynergies.cs")
  lines = readAllLines(synergyFile).split("\n")
  srx = re.compile(r"""^\s*NewSynergy.*?,\s*\"([^\"]+)\",\s*new\[\]\{(.*)\}""")
  for i, line in enumerate(lines):
    if not (r := srx.match(line)):
      continue
    synergyname = r.groups()[0]
    mandatories = [s.strip() for s in r.groups()[1].split(",")]
    filenames = []
    itemnames = []
    for m in mandatories:
      if m.startswith("IName"): # one of our items
        itemname = m.split("(")[1].split(".")[0]
        if (e := passives.get(itemname, None)) is not None:
          filenames.append(e["filename"])
          itemnames.append(e["itemname"])
          continue
        if (e := actives.get(itemname, None)) is not None:
          filenames.append(e["filename"])
          itemnames.append(e["itemname"])
          continue
        if (e := guns.get(itemname, None)) is not None:
          filenames.append(e["filename"])
          itemnames.append(e["itemname"])
          continue
        continue
      if (e := VANILLA_ITEMS.get(m.replace('"',""), None)) is not None:
          filenames.append(f"""[[File:{e[1]}]]""")
          itemnames.append(e[0])
    if len(filenames) < 2:
      continue
    desc = re.sub(r"""^\s*//\s*""","", lines[i-1])
    # data[gun] = desc
    entry = {
      "synergyname" : synergyname,
      "filename1"   : filenames[0],
      "item1"       : itemnames[0],
      "filename2"   : filenames[1],
      "item2"       : itemnames[1],
      "description" : desc,
      }
    data[synergyname] = entry

  # Sort synergies by name before returning
  return { k : data[k] for k in sorted(data.keys())}

def scanGuns(masteryData):
  # clipSize: -1
  data = {}
  classdata = {}
  for f in getSourceFiles(os.path.join(SOURCE_DIR,"Cwaff-Guns")):
    text = readAllLines(f)
    itemname = findPattern(text, r"""ItemName\s*=\s*\"(.*)\";""")
    classname = findPattern(text, r"""public class (.*) : Cwaff""")
    entry = {
      "classname"   : classname,
      "filename"    : imageFor(iconForGun(itemname, nameOnly = True)),
      "size"        : "42",
      "itemname"    : itemname,
      "quality"     : findPattern(text, r"""quality\s*:\s*ItemQuality\.(.)"""),
      "type"        : findPattern(text, r"""shootStyle\s*:\s*ShootStyle\.([A-Za-z_]+)""").replace("SemiAutomatic", "Semi-Automatic"),
      "class"       : findPattern(text, r"""GunClass\.([A-Z_]+)[,\)]"""),
      "magazine"    : computeClipSize(text),
      "ammo"        : computeAmmo(text),
      "damage"      : findPattern(text, r"""damage\s*:\s*(.*?)(?:\.0)?f?[,\)]""", default="4", resolveVars = True), # damage of pea shooter, the default gun
      "speed"       : findPattern(text, r"""speed\s*:\s*(.*?)(?:\.0)?f?[,\)]""", default="20"), # velocity of pea shooter, the default gun
      "range"       : computeRange(text),
      "knockback"   : findPattern(text, r"""force\s*:\s*([0-9\.]+?)(?:\.0)?f?[,\)]""", default="20", resolveVars = True), # force of pea shooter, the default gun
      "firerate"    : findPattern(text, r"""cooldown\s*:\s*(.*?)(?:\.0)?f?[,\)]""", default="0.15", resolveVars = True), # cooldown of pea shooter, the default gun
      "reloadspeed" : computeReloadTime(text),
      "spread"      : findPattern(text, r"""angleVariance\s*:\s*(.*?)(?:\.0)?f?[,\)]""", default="10"), # spread of pea shooter, the default gun
      "description" : makePrettyDescription(findPattern(text, r"""LongDescription\s*=\s*\"(.*)\";""")),
      "mastery"     : "",
      }
    masteryDesc = masteryData.get(entry["classname"], None)
    if masteryDesc is not None:
      entry["mastery"] = f"{MASTERY_TEMPLATE}{masteryDesc}"
    data[itemname] = entry
    classdata[classname] = entry
  return data, classdata

def getNPCs():
  return {
    "cammy" : {
      "filename"    : imageFor("cammy_idle_001.png"),
      "npcname"     : "Cammy",
      "icon"        : imageFor("cammy_icon.png"),
      "type"        : "Shopkeeper",
      "condition"   : "Guaranteed to spawn in Keep of the Lead Lord.",
      "description" : "Sells 3 random unlocked companions at a 30% discount. Cannot be stolen from.",
    },
    "bart" : {
      "filename"    : imageFor("bart_idle_001.png"),
      "npcname"     : "Bart",
      "icon"        : imageFor("bart_icon.png"),
      "type"        : "Shopkeeper",
      "condition"   : "Guaranteed to spawn in the Gungeon Proper or Mines.",
      "description" : "Barters 3 random D-A quality items for any higher-tiered item. Bartering is done by dropping a single item on the floor within the shop room and attempting to purchase an item. Bartered items are destroyed upon a successful trade.",
    },
    "kevlar" : {
      "filename"    : imageFor("kevlar_idle_001.png"),
      "npcname"     : "Kevlar",
      "icon"        : imageFor("kevlar_icon.png"),
      "type"        : "Shopkeeper",
      "condition"   : "Guaranteed to spawn when entering a floor while possessing an {{Quality|S}} or {{Quality|A}} quality item.",
      "description" : "Sells Insurance Policies for 30 casings each. Insurance Policies can be used on any grounded item or gun to insure it, making the item spawn in a special chest at the beginning of your next run.",
    },
    "skeleton" : {
      "filename"    : "",
      "npcname"     : "???",
      "icon"        : "",
      "type"        : "Friend? Enemy?",
      "condition"   : "Maybe Bello knows something?",
      "description" : "A mysterious figure from a different time and place that somehow ended up in the Gungeon.",
    },
  }

PASSIVE_TEMPLATE='''
|-
|<div style="min-height: 56px; transform-origin: top; transform: scale(2);">{filename}</div>{itemname}
|{{{{Quality|{quality}}}}}
|{description}
'''

ACTIVE_TEMPLATE='''
|-
|<div style="min-height: 56px; transform-origin: top; transform: scale(2);">{filename}</div>{itemname}
|{{{{Quality|{quality}}}}}
|{cooldown}
|{numuses}
|{description}
'''

GUN_TEMPLATE='''
|-
|<div style="min-height: 56px; transform-origin: top; transform: scale(2);">{filename}</div>{itemname}
|{{{{Quality|{quality}}}}}
|{type}
|{class}
|{magazine}
|{ammo}
|{damage}
|{speed}
|{range}
|{knockback}
|{firerate}
|{reloadspeed}
|{spread}
|{description}{mastery}
'''

SYNERGY_TEMPLATE='''
|-
|{synergyname}
|<div style="min-height: 56px; transform-origin: top; transform: scale(2);">{filename1}</div>{item1}
|<div style="min-height: 56px; transform-origin: top; transform: scale(2);">{filename2}</div>{item2}
|{description}
'''

MASTERY_TEMPLATE='''<br/>[[File:Mastery.png]] <b><span style="color:#ee6099">Mastery</span></b>: '''

NPC_TEMPLATE='''
|- style="vertical-align:middle; height: 128px;"
|<div style="transform-origin: center; transform: scale(2);">{filename}</div>
|{npcname}
|<div style="transform-origin: center; transform: scale(2);">{icon}</div>
|{type}
|{condition}
|{description}
'''

WIKI_TEMPLATE='''
[[Category:Modding]]
{{{{DISPLAYTITLE:GungeonCraft}}}}{{{{Modding}}}}<!--Non-official notice-->

{summary}

== Guns ==

{{| {tablestyle}
!style="width: {iconwidth}"|Name
!Qual.
!Type
!Class
!{{{{Hover|[[File:Drum_Clip.png]]|Clip Size}}}}
!{{{{Hover|[[File:Ammo_Belt.png]]|Ammo Capacity}}}}
!{{{{Hover|[[File:-1_Bullets.png]]|Projectile Damage}}}}
!{{{{Hover|[[File:Rocket-Powered Bullets.png]]|Projectile Velocity}}}}
!{{{{Hover|[[File:Grappling_Hook.png]]|Projectile Range}}}}
!{{{{Hover|[[File:Heavy_Bullets.png]]|Projectile Knockback}}}}
!{{{{Hover|[[File:Lichy Trigger Finger.png]]|Rate of fire: delay between shots; lower number means higher fire rate}}}}
!{{{{Hover|[[File:Oiled_Cylinder.png]]|Reload Speed: Number of seconds it takes to reload}}}}
!{{{{Hover|[[File:Scope.png]]|Spread: higher number means less accuracy}}}}
!style="width: 50%"|Effect
{guns}
|}}

== Active Items ==

{{| {tablestyle}
!style="width: {iconwidth}"|Name
!style="width: 48px;"|Quality
!Cooldown
!Uses
!style="width: 50%"|Effect
{actives}
|}}

== Passive Items ==

{{| {tablestyle}
!style="width: {iconwidth}"|Name
!style="width: 48px;"|Quality
!Effect
{passives}
|}}

== Synergies ==

{{| {tablestyle}
!style="width: {iconwidth}"|Name
!colspan="2"|Items
!Effect
{synergies}
|}}

== NPCs ==

{{| {tablestyle}
!style="font-weight: bold; width: 112px;"|Icon
!Name
!Room Icon
!Type
!Spawn Condition
!style="width: 50%"|Description
{npcs}
|}}
'''

if __name__ == "__main__":
  main()
