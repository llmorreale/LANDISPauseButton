Adam's Weekly Log

3/26 - 4/1: This week we managed to run the model with pause-button changes on Luca's machine using his local Python installation.
Afterward, I changed the method of calling Python scripts through the extension to be agnostic to location of a user's Python installation.
Next on the list is to allow users to specify a script file and scripting language to call any custom script using the pause functionality.

4/2 - 4/9: This week I completed allowing users to specify information about external script
execution in the landuse.txt file associated with the LU+ module. Users can specify:
-A name of a script and an executable to a scripting engine e.g. python.exe, R.exe, etc
-A command for automatic entry into the Windows Command prompt (CMD.exe) for alternate execution