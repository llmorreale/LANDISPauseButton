
Pull down Land Use from LANDIS-II Foundation Github (https://github.com/LANDIS-II-Foundation/Extension-Land-Use-Change)

Here I am largely paraphrasing the instructions on that repository's README.md, which are somewhat convoluted

First, ensure the [LANDIS-II SDK][] is installed
[LANDIS-II SDK]: http://sourceforge.net/p/landis-ii-archive/wiki/SoftwareDevelopmentKit/
Set the environment variable %LANDIS_SDK% to the location of your SDK installation

Next, install the [Inno Setup QuickStart][], which builds the executable for installing a new extension to your default LANDIS-II model
[Inno Setup]: http://www.jrsoftware.org/isinfo.php

Open Visual Studio .csproj in /src
Add references to the folders "C:/Program Files/LANDIS-II/v6/bin/extensions" and "C:/Program Files/LANDIS-II/v6/bin/6.2"
to satisfy missing libraries in the "land-use" C# project
Change Assembly name in "Project Properties -> Application" to new version (for example, 1.1.1 from 1.1)
Finally, in "Project Properties -> Build Events -> Post-build event command line", there is a command which calls a script in the 
LANDIS_SDK. It should be tailored for my system (C:\Users\Adam\LANDIS-SDK\v6-r06) -- replace this part with the PATH to your LANDIS_SDK.
Their solution involved using the LANDIS_SDK environment variable but this was not working with visual studio for me. 

You should be able to build the project in Visual Studio after these steps.

IMPORTANT: in "/src/staging-lists.txt", change the libraries listed to fit the new version name of the extension. These should match the names
of the built libraries now located in src/bin/Release.

In /deploy change "Land Use.txt" to fit this new version, specifically the "Name", "Version", and "Assembly" fields
-For "Version", change the (official release) tag to (alpha X), where X is a version number (here, 1). 

In /deploy/docs change the name of the .pdf to "Land Use .1 vX.Y - User Guide.pdf" from "Land Use vX.Y - User Guide.pdf"

Open Inno Setup and use it to execute "/deploy/Land Use.iss"

If you successfully execute the script, double-click the .exe in the same folder to install the new version of 
the extension to LANDIS-II. use "landis-extensions list" to check if this has been done correctly. Two versions of "Land Use" should exist.

Finally, the "scenario.txt" file must be changed to specify the new version of the Land Use Extension (1.1.1) so that LANDIS-II
knows to use our compiled version. Otherwise, it will still use the version 1.1 we installed earlier. 

When the model is run, after loading Land Use the program should output "This is the Thompson lab's custom Land-Use module"