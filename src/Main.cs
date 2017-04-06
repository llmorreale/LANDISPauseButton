// This file is part of the Land Use extension for LANDIS-II.
// For copyright and licensing information, see the NOTICE and LICENSE
// files in this project's top-level directory, and at:
//   https://github.com/LANDIS-II-Foundation/Extension-Land-Use-Change
//
//  Pause Extension for Thomspon lab
//   https://github.com/llmorreale/LANDISPauseButton
//

using Landis.Core;
using Landis.Library.Succession;
using Landis.SpatialModeling;
using log4net;

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;

namespace Landis.Extension.LandUse
{
    public class Main
        : Landis.Core.ExtensionMain
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Main));
        private static readonly bool isDebugEnabled = log.IsDebugEnabled;

        public static readonly ExtensionType ExtType = new ExtensionType("disturbance:land use");
        public static readonly string ExtensionName = "Land Use";

        private Parameters parameters;
        private string inputMapTemplate;

        //---------------------------------------------------------------------

        public Main()
            : base(ExtensionName, ExtType)
        {
        }

        //---------------------------------------------------------------------

        public override void LoadParameters(string dataFile,
                                            ICore modelCore)
        {
            Model.Core = modelCore;
            Landis.Library.BiomassHarvest.Main.InitializeLib(Model.Core);
            Model.Core.UI.WriteLine("  Loading parameters from {0}", dataFile);
            ParameterParser parser = new ParameterParser(Model.Core.Species);
            parameters = Landis.Data.Load<Parameters>(dataFile, parser);
        }

        //---------------------------------------------------------------------

        public override void Initialize()
        {
            Model.Core.UI.WriteLine("***********************************************************");
            Model.Core.UI.WriteLine("***********************************************************");
            Model.Core.UI.WriteLine("This is the Thompson lab's custom Land-Use module");
            Model.Core.UI.WriteLine("-----------------------------------------------------------");
            Model.Core.UI.WriteLine("Modification of form is admitted to be a matter of time.");
            Model.Core.UI.WriteLine("\t-Alfred Russel Wallace");
            Model.Core.UI.WriteLine("***********************************************************");
            Model.Core.UI.WriteLine("***********************************************************");
            
            Model.Core.UI.WriteLine("Initializing {0}...", Name);

            SiteVars.Initialize(Model.Core);
            Timestep = parameters.Timestep;
            inputMapTemplate = parameters.InputMaps;
            if (parameters.SiteLogPath != null)
                SiteLog.Initialize(parameters.SiteLogPath);

            // Load initial land uses from input map for timestep 0
            ProcessInputMap(
                delegate(Site site,
                         LandUse initialLandUse)
                {
                    SiteVars.LandUse[site] = initialLandUse;
                    return initialLandUse.Name;
                });
        }

        //---------------------------------------------------------------------

        public override void Run()
        {
            if (SiteLog.Enabled)
                SiteLog.TimestepSetUp();

            //ProcessInputMap(
            ProcessInputMapAsync(
                delegate(Site site,
                         LandUse newLandUse)
                {
                    LandUse currentLandUse = SiteVars.LandUse[site];
                    if (newLandUse != currentLandUse)
                    {
                        SiteVars.LandUse[site] = newLandUse;
                        string transition = string.Format("{0} --> {1}", currentLandUse.Name, newLandUse.Name);
                        if (!currentLandUse.AllowEstablishment && newLandUse.AllowEstablishment)
                        {
                            string message = string.Format("Error: The land-use change ({0}) at pixel {1} requires re-enabling establishment, but that's not currently supported",
                                                           transition,
                                                           site.Location);
                            throw new System.ApplicationException(message);
                        }
                        else if (currentLandUse.AllowEstablishment && !newLandUse.AllowEstablishment)
                            Reproduction.PreventEstablishment((ActiveSite) site);

                        if (isDebugEnabled)
                            log.DebugFormat("    LU at {0}: {1}", site.Location, transition);
                        newLandUse.LandCoverChange.ApplyTo((ActiveSite)site);
                        if (SiteLog.Enabled)
                            SiteLog.WriteTotalsFor((ActiveSite)site);
                        return transition;
                    }
                    else
                        return null;
                });

            if (SiteLog.Enabled)
                SiteLog.TimestepTearDown();
        }

        //---------------------------------------------------------------------

        // A delegate for processing a land use read from an input map.
        public delegate string ProcessLandUseAt(Site site, LandUse landUse);

        //---------------------------------------------------------------------

        public void ProcessInputMap(ProcessLandUseAt processLandUseAt)
        {
            string inputMapPath = MapNames.ReplaceTemplateVars(inputMapTemplate, Model.Core.CurrentTime);
            Model.Core.UI.WriteLine("  Reading map \"{0}\"...", inputMapPath);

            IInputRaster<MapPixel> inputMap;
            Dictionary<string, int> counts = new Dictionary<string, int>();

            using (inputMap = Model.Core.OpenRaster<MapPixel>(inputMapPath))
            {
                MapPixel pixel = inputMap.BufferPixel;
                foreach (Site site in Model.Core.Landscape.AllSites)
                {
                    inputMap.ReadBufferPixel();
                    if (site.IsActive)
                    {
                        LandUse landUse = LandUseRegistry.LookUp(pixel.LandUseCode.Value);
                        if (landUse == null)
                        {
                            string message = string.Format("Error: Unknown map code ({0}) at pixel {1}",
                                                           pixel.LandUseCode.Value,
                                                           site.Location);
                            throw new System.ApplicationException(message);
                        }
                        string key = processLandUseAt(site, landUse);
                        if (key != null)
                        {
                            int count;
                            if (counts.TryGetValue(key, out count))
                                count = count + 1;
                            else
                                count = 1;
                            counts[key] = count;
                        }
                    }
                }
            }
            foreach (string key in counts.Keys)
                Model.Core.UI.WriteLine("    {0} ({1:#,##0})", key, counts[key]);
        }

        //------------------------------------------------------------------------

        /*The Python module can write to the location this method refers to,
            which appears to be indexed by the model's current time step, or 
             we can provide a consistent path to the Python module's output */
        public void ProcessInputMapAsync(ProcessLandUseAt processLandUseAt)
        {          
            string inputMapPath = MapNames.ReplaceTemplateVars(inputMapTemplate, Model.Core.CurrentTime);
            
            Model.Core.UI.WriteLine("Current time: ", Model.Core.CurrentTime);
            Model.Core.UI.WriteLine("  Reading map \"{0}\"...", inputMapPath);
            
            //Create an empty lockfile at the appropriate path - may need a separate path for lockfile and rasterfile
            StreamWriter lock_file = new StreamWriter(System.IO.Directory.GetCurrentDirectory() + "/lockfile");
            lock_file.WriteLine(Model.Core.CurrentTime.ToString());
            lock_file.Close();

            /*FileStream lockfile = new FileStream(System.IO.Directory.GetCurrentDirectory() + "/lockfile", FileMode.Create);
            lockfile.WriteByte(Convert.ToByte(Model.Core.CurrentTime));
            lockfile.Close();*/

            //Need to get string argument from some LANDIS inputfile so users can specify custom shell scripts.
            Process python_process = CallShellScript("/K python external_module.py");
            python_process.WaitForExit();
            python_process.Close(); //Not sure if terminates process of makes results inaccessible
            ProcessMapAsync(processLandUseAt, inputMapPath);
        }

        //Using a command shell to evoke arbitrary processes specified by the user
        public Process CallShellScript(string shell_command)
        {
            Model.Core.UI.WriteLine("Starting external shell...");
            Process shell_process = new Process();

            shell_process.StartInfo.UseShellExecute = true;
            shell_process.StartInfo.CreateNoWindow = true;
            shell_process.StartInfo.FileName = "CMD.exe";
            shell_process.StartInfo.Arguments = shell_command;
            shell_process.StartInfo.RedirectStandardOutput = false;

            try
            {
                shell_process.Start(); // start the process 
            }
            catch (Win32Exception w)
            {
                Model.Core.UI.WriteLine(w.Message);
                Model.Core.UI.WriteLine(w.ErrorCode.ToString());
                Model.Core.UI.WriteLine(w.NativeErrorCode.ToString());
                Model.Core.UI.WriteLine(w.StackTrace);
                Model.Core.UI.WriteLine(w.Source);
                Exception e = w.GetBaseException();
                Model.Core.UI.WriteLine(e.Message);
            }

            return shell_process;
        }

        public Process CallPythonModule()
        {
            Model.Core.UI.WriteLine("Activating python...");
            Process python_process = new Process();
            Model.Core.UI.WriteLine("Attempt to locate user Python installation...");
            string python_path = "C:/Program Files/Python36/python.exe";
            python_path = "C:/Python27/ArcGIS10.4/python.exe";

            Model.Core.UI.WriteLine("Using Luca's Python 2.7 install: " + python_path);

            python_process.StartInfo.FileName = python_path;
            python_process.StartInfo.UseShellExecute = false;
            python_process.StartInfo.CreateNoWindow = true;
            python_process.StartInfo.Arguments = "external_module.py";
            python_process.StartInfo.RedirectStandardOutput = true;

            Model.Core.UI.WriteLine(python_process.StartInfo.FileName);
            try
            {
                python_process.Start(); // start the process (the python program)
            }
            catch (Win32Exception w)
            {
                Model.Core.UI.WriteLine(w.Message);
                Model.Core.UI.WriteLine(w.ErrorCode.ToString());
                Model.Core.UI.WriteLine(w.NativeErrorCode.ToString());
                Model.Core.UI.WriteLine(w.StackTrace);
                Model.Core.UI.WriteLine(w.Source);
                Exception e = w.GetBaseException();
                Model.Core.UI.WriteLine(e.Message);
            }

            return python_process;        
        }

        //--------------------------------------------------------------------------

        /*The main point is that this process must spin until we are sure that the
             * Python module has finished running and completely written the raster file
             * to memory. The programmar has proposed we do this using a lockfile, probably
             * the simplest (platform-independent) version of a semaphore*/
        public void ProcessMapAsync(ProcessLandUseAt processLandUseAt, string inputMapPath)
        {
            IInputRaster<MapPixel> inputMap;
            Dictionary<string, int> counts = new Dictionary<string, int>();

            using (inputMap = Model.Core.OpenRaster<MapPixel>(inputMapPath))
            {
                MapPixel pixel = inputMap.BufferPixel;
                foreach (Site site in Model.Core.Landscape.AllSites)
                {
                    inputMap.ReadBufferPixel();
                    if (site.IsActive)
                    {
                        LandUse landUse = LandUseRegistry.LookUp(pixel.LandUseCode.Value);
                        if (landUse == null)
                        {
                            string message = string.Format("Error: Unknown map code ({0}) at pixel {1}",
                                                           pixel.LandUseCode.Value,
                                                           site.Location);
                            throw new System.ApplicationException(message);
                        }
                        string key = processLandUseAt(site, landUse);
                        if (key != null)
                        {
                            int count;
                            if (counts.TryGetValue(key, out count))
                                count = count + 1;
                            else
                                count = 1;
                            counts[key] = count;
                        }
                    }
                }
            }
            foreach (string key in counts.Keys)
                Model.Core.UI.WriteLine("    {0} ({1:#,##0})", key, counts[key]);
        }

        //---------------------------------------------------------------------

        public new void CleanUp()
        {
            if (SiteLog.Enabled)
                SiteLog.Close();
        }
    }

    //----------------------------------------------------

    //One simple approach would be to loop until the file is deleted
    /*Model.Core.UI.WriteLine("   Waiting for external output...");
    int refreshRate = 100;
    int refreshLimit = 10000;   //Let spin for ten seconds
    int refreshCount = 0;
    while (File.Exists(inputMapPath + "/lockfile") || refreshCount > refreshLimit)
    {
        System.Threading.Thread.Sleep(refreshRate);
        Model.Core.UI.WriteLine("Spinning");
        refreshCount += refreshRate;
    }

    if (refreshCount > refreshLimit)
    { Model.Core.UI.WriteLine("Failed to find input"); }
    else
    { ProcessMapAsync(processLandUseAt, inputMapPath); }*/

    //Otherwise we can register events to this file system watcher to execute when something changes
    //FileSystemWatcher file_watcher = new FileSystemWatcher(inputMapPath);
    //file_watcher.EnableRaisingEvents = true;
    //System.EventArgs args = new System.EventArgs(ProcessLandUseAt processAt, string inputMapPath);
    //file_watcher.Deleted += new System.IO.FileSystemEventHandler(ProcessMapAsync);
    //Complicated and requires bending over backwards to create an EventHandler of a certain kind
    // -------------------------------------------------
}
