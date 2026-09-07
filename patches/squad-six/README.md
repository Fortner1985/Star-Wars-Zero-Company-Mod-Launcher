# Squad Six patches

**Squad Six is not distributed here, and is not covered by this repository's
Apache 2.0 licence.**

It is a third-party community mod (`community.squad-six.core` /
`community.squad-six.runtime`, v1.1.4) by another author. The distributed
package carries no licence file, which means no redistribution right. Only our
own patches live in this folder; they are our work and are Apache 2.0.

To use them you need Squad Six from its original source.

## Status

Squad Six self-reports incomplete on the current game build:

```
[ZCOM_SQUAD_SIX] status version=1.1.4 complete=false data_container=partial
                 slot_hook=false proxy_hook=true lineup_hook=true
                 placement_hook=true duplicate=none guard=reflection=0/names=10
```

Two things stand out. `reflection=0/names=10` means reflection lookup is finding
nothing and it is falling back entirely to name matching. `slot_hook=false`
before the mission screen has opened is **normal**, not a failure — anything
reporting it as one is lying to you.

## To the author of Squad Six

These patches are offered upstream — they belong in your mod, not in a fork.
Open an issue and they are yours. If you would rather this folder did not exist,
say so and it goes.

## What is here

Nothing yet. Patches will be added as separate files against a named upstream
version, each stating what it changes and why.
