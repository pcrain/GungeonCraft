#!/usr/bin/python
#Generate item tips from source code

import os, json

MODURL="https://enter-the-gungeon.thunderstore.io/package/CaptainPretzel/GungeonCraft/"
MODDATAFILE=os.path.join("src","Common.cs")
MODSYNERGYFILE=os.path.join("src","Cwaff-Misc","CwaffSynergies.cs")
TIPDATAFILE=os.path.join("_thunderstore_package","itemtips-cg.tip")
ITEMDIRS  = [
  "Cwaff-Passives",
  "Cwaff-Actives",
  "Cwaff-Guns",
  ]

def main():
  modprefix = modname = modversion = None
  with open(MODDATAFILE, 'r') as fin:
    while line := next(fin):
      if "MOD_NAME" in line:
        modname = line.split('"')[1]
      elif "MOD_VERSION" in line:
        modversion = line.split('"')[1]
      elif "MOD_PREFIX" in line:
        modprefix = line.split('"')[1]
      if None not in [modprefix, modname, modversion]:
        break # we have all the metadata we need

  # Process metadata
  metadata = {
    "name"    : modname,
    "version" : modversion,
    "url"     : MODURL,
  }

  # Process items
  items = {}
  for itemdir in ITEMDIRS:
    for root, subFolders, files in os.walk(os.path.join("src",itemdir)):
      if root.endswith("/Unfinished"):
        continue # skip unfinished items
      for f in sorted(files):
        if f.startswith("_"):
          continue # skip reference items
        path = os.path.join(root, f)
        iid = name = desc = None
        with open(path, 'r') as fin:
          while line := next(fin):
            if "LongDescription" in line:
              desc = '"'.join(line.split('"')[1:-1])
              # print(f"DESC: {desc}")
              # print()
              break
            elif "ItemName" in line:
              name = line.split('"')[1]
              iid = f"{modprefix}:"+name.replace("-", "").replace(".", "").replace(" ", "_").lower()
              # print(f"NAME: {name} ({iid})")
        if None in [iid, name, desc]:
          raise Exception(f"failed to get item data for {f}")
        items[iid] = {
          "name"  : name,
          "notes" : desc,
        }

  # Process synergies
  synergies = {}
  with open(MODSYNERGYFILE, 'r') as fin:
    try:
      while line := next(fin):
        sline = line.strip()
        if sline.startswith("NewSynergy"):
          comment = lastline.replace("// ","").replace("//","")
          name = sline.split('"')[1]
          synergies[name] = {"notes" : comment }
        lastline = sline
    except StopIteration:
      pass

  tipdata = {
    "metadata"  : metadata,
    "items"     : items,
    "synergies" : synergies,
  }
  with open(TIPDATAFILE,'w') as fout:
    fout.write(json.dumps(tipdata, indent=2))

if __name__ == "__main__":
  main()
