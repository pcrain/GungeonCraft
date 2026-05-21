#!/usr/bin/python
#Generates a changelog under 100,000 characters for thunderstore

CHANGELOG="./changelog.md"
MESSAGE="\n\n## Older Versions\n\nDue to server limitations when displaying long changelogs, the full changelog [can be found on GitHub](https://github.com/pcrain/GungeonCraft/blob/master/changelog.md)"
LIMIT=100_000 # current thunderstore limit
MAXCHARS = 100_000 - len(MESSAGE) - (LIMIT // 10) # allow a 10% margin of error
with open(CHANGELOG, 'r') as fin:
  lines = fin.read().split("\n")

charcount = 0
lastline = 0
for i, line in enumerate(lines):
  if line.startswith("## "): # version / date headers are the only appropriate break points
    if charcount >= MAXCHARS: break # if we've hit the max length, we're done
    lastline = i - 1 # the line before this one is a safe stopping point
  charcount += len(line) + 1 # count the characters in this line plus the newline character itself towards the total length

newlog = "\n".join(lines[:lastline]) + MESSAGE
print(newlog)
