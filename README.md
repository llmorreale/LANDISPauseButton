Adam's Weekly Log

3/26 - 4/1: This week we managed to run the model with pause-button changes on Luca's machine using his local Python installation.
Afterward, I changed the method of calling Python scripts through the extension to be agnostic to location of a user's Python installation.
Next on the list is to allow users to specify a script file and scripting language to call any custom script using the pause functionality.

4/2 - 4/9: This week I completed allowing users to specify information about external script
execution in the landuse.txt file associated with the LU+ module. Users can specify:
-A name of a script and an executable to a scripting engine e.g. python.exe, R.exe, etc
-A command for automatic entry into the Windows Command prompt (CMD.exe) for alternate execution

4/10 - 4/17: Notes, comments and planned code were added for Harvest allowance. I investigated the new module needing changes, the AllowHarvest
parameter on LandUse types. Harvest allowance is specified per LandUse types, controlling each raster cell of that type. In the 
wobegon example, four LandUse types exist: Garden, forest, urban, and no-harvest easement. Tentatively I believe changing two 
lines of code will allow users to change the harvest allowance for land-use types at any time-step. Harvest allowance is exposed
as a read-only property at line 52 of AllowHarvestSiteVars.cs (containing only a "get" method). By creating a "set" method at line 56
we can specify different LandUse types by index (0: forest, 1: urban, 2: Garden, 3: easement) and set the harvest allowance parameter
directly. It will be up to the calling function to know which landuse type corresponds to which parameter. The "set" method of the 
AllowHarvest property in LandUse.cs must also be made public for users to be able to set the value of the parameter externally. This
change is made in line line 18 of LandUse.cs. No changes to other extensions or the core model are projected to be necessary.

4/18 - 4/24: Updated Land-Use documentation, see .pdf for changes to example file, details on new parameters, and explanation of Pause
functionality. Command-line pause functionality automatically closes external shell--no longer need to wait for user to close shell to
continue model execution.