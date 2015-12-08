#BetterCrewAssignment
KSP mod to help with crew assignments in the VAB/SPH.

 * It makes better automatic default choices for crew assignments (e.g. "labs need scientists" or "drills need engineers").
 * It remembers your assignments, so that the next time you launch that ship, it will try to do the same thing.  (No more discovering, *after* you're in orbit, that your gosh-darn rescue ship filled up all the slots and you've got nowhere to put the stranded kerbal!)
 * You can customize the default behavior with ModuleManager config.


##How to install
Unzip into your GameData folder, same as any mod.


##How to use
Just play KSP!  The mod is deliberately minimalistic.  It adds no UI, it doesn't require any special actions to use.  It just silently makes the crew-assignment experience better.

The only thing that affects you at all is:  if you go into the "crew" tab of the editor and change crew assignments, then your choices won't be persisted unless you hit the "save" button before launching the ship.  That's it, that's all there is to know.


##Cool things it does by default

* Make sure there's a pilot on board, if you don't have any SAS-capable probe cores.
* Staffs science labs with scientists.
* If you have any non-rerunnable science experiments on board, make sure there's at least one scientist.
* If you have any parts that need an engineer (ISRU, drills), make sure there's at least one engineer.
* Try to pick the highest-level crewmembers available. (Except for pilots; if you have an SAS-capable probe core and your pilots are all lower-level than the core, it picks the *lowest* pilot available.)
* Tries to fill all command pods; doesn't try to fill passenger cabins.
* If you do manual assignments in the crew tab and then save the ship, it remembers your choices the next time you load the ship.  Empty slots will be left empty.  It will try to assign specific kerbals by name (e.g. "Jeb goes in slot 0 of this command pod"), and if that crewmember is unavailable, will try to assign another kerbal of the same profession (e.g. "I want Jeb, but he's on a mission already, so I'll use this pilot here.")


##How it decides
The mod works with two types of assignments:  default choices, and player choices.

###Default choices
Default choices are controlled by [ModuleManager](http://forum.kerbalspaceprogram.com/index.php?/topic/50533-105-module-manager-2613-november-9th-with-more-sha-and-less-bug-upgrade/) config in a file that comes with the mod (see "How to customize", below).  There are two flavors of default choices, *assignments* and *requirements*.

**Assignments** are default choices for crew slots in specific crewable modules.  The default config that comes with the mod assigns scientists to science labs, and all crewmembers to command pods. The default config deliberately leaves passenger cabins empty, though you can tweak this by adding your own config if you like.

**Requirements** are added to parts that are not themselves crewable, but which need a particular type of kerbal to operate them.  If your vessel has any parts that specify requirements, then the mod will try to ensure that at least one of the specified kerbal type is present in the crew. The default config that comes with the mod adds a "scientist" requirement to all non-rerunnable science experiments (Mystery Goo, Science Jr.), and an "engineer" requirement to ISRU units and ore drills.

###Player choices
When you load a new ship, or add a new part, then everything is controlled by the default behavior and assignments will be updated dynamically as you switch stuff around on your ship. It can do this because you haven't *observed* the assignments and it's therefore free to shuffle assignments around without invalidating any of your choices.

However, the moment you switch to the "crew" tab in the editor and see what the assignments are, it then nails all the assignments in place.  (It's a [Heisenbergian](https://en.wikipedia.org/wiki/Observer_effect_%28physics%29) sort of thing.)  Basically, what it's doing is assuming that the moment you *see* the assignments, they become your conscious choices rather than something the program assigned.

Once you see the assignments (and make any changes of your own), those get persisted to the ship, and will be saved when you hit the "save" button.  Such specific choices are assumed to be for a specific kerbal, or for a kerbal of that profession if the kerbal isn't available.


##How to customize
Since all crew assignments/requirements are controlled by [ModuleManager](http://forum.kerbalspaceprogram.com/index.php?/topic/50533-105-module-manager-2613-november-9th-with-more-sha-and-less-bug-upgrade/) config,  you can add your own .cfg file to change the behavior to whatever you like.

If you'd like to customize the behavior, the following references may be helpful:

* **BetterCrewAssignment.cfg:** This is the ModuleManager config that is installed with the mod. It includes detailed comments explaining how it works, so that you can write your own config.
* **ModuleManager documentation:**  You can find helpful information [here](https://github.com/sarbian/ModuleManager/wiki/Module-Manager-Syntax) and [here](https://github.com/sarbian/ModuleManager/wiki/Module-Manager-Handbook).
