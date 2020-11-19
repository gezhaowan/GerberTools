﻿using GerberLibrary;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GerberToImage
{
    class GerberToImage
    {
        enum Arguments
        {
            dpi,
            noxray,
            nonormal,
            silk,
            mask,
            trace,
            copper, 
            None
        }

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Count() < 1)
            {
                Console.WriteLine("need files to render...");
                Console.WriteLine("GerberToImage <files> [--dpi N] [--noxray] [--nopcb] [--silk color] [--trace color] [--copper color] [--mask color]");
                return;
            }

            int dpi = 400;
            Arguments NextArg = Arguments.None;
            bool xray = true;
               bool normal = true;
            string pcbcolor = "green";
            string silkcolor = "white";
            string tracecolor = "auto";
            string coppercolor = "gold";
            List<string> RestList = new List<string>();
            for (int i = 0; i < args.Count() ; i++)
            {
                switch (NextArg)
                {

                    case Arguments.dpi: dpi = Int32.Parse(args[i]); NextArg = Arguments.None; break;
                    case Arguments.silk: silkcolor = args[i];NextArg = Arguments.None;break;
                    case Arguments.mask: pcbcolor = args[i]; NextArg = Arguments.None; break;
                    case Arguments.trace: tracecolor = args[i]; NextArg = Arguments.None; break;
                    case Arguments.copper: coppercolor= args[i]; NextArg = Arguments.None; break;
                    case Arguments.None:
                        switch (args[i].ToLower())
                        {
                            case "--dpi": NextArg = Arguments.dpi; break;
                            case "--silk": NextArg = Arguments.silk;break;
                            case "--trace": NextArg = Arguments.trace; break;
                            case "--copper": NextArg = Arguments.copper; break;
                            case "--mask": NextArg = Arguments.mask; break;
                            case "--noxray": xray = false; NextArg = Arguments.None; break;
                            case "--nopcb": normal = false; NextArg = Arguments.None; break;

                            default:
                                RestList.Add(args[i]);break;
                        }
                        break;
                }
            }


            Gerber.SaveIntermediateImages = false;
            Gerber.ShowProgress = false;

            if (RestList.Count() == 1 && File.Exists(RestList[0]) && Path.GetExtension(RestList[0]).ToLower()!= ".zip")
            {
              //  Gerber.WriteSanitized = true;
                Gerber.ExtremelyVerbose = false;
                //Gerber.Verbose = true;
                Gerber.WaitForKey = true;
                Gerber.ShowProgress = true;

               CreateImageForSingleFile(new StandardConsoleLog(),RestList[0], Color.Black, Color.White);
                if (Gerber.WaitForKey)
                {
                    Console.WriteLine("Press any key to continue");
                    Console.ReadKey();
                }
                return;
            }

            for (int t = 0; t < 1; t++)
            {
                CreateImageForSingleFile11(dpi, RestList);
            }
            Console.ReadKey();
        }
        private static void CreateImageForSingleFile11(double dpi, List<string> RestList)
        {
            string pcbcolor = "green";
            string silkcolor = "white";
            string tracecolor = "auto";
            string coppercolor = "gold";
            GerberImageCreator GIC = new GerberImageCreator();
            string TargetFileBaseName = "";
            if (RestList.Count() >= 1) TargetFileBaseName = RestList[0];

            List<String> FileList = new List<string>();

            foreach (var a in RestList)
            {
                if (Directory.Exists(a))
                {
                    FileList.AddRange(Directory.GetFiles(a, "*.*"));
                }
                else
                {
                    if (File.Exists(a))
                    {
                        FileList.Add(a);
                    }
                }

            }

            for (int i = 0; i < FileList.Count; i++)
            {
                if (FileList[i][FileList[i].Count() - 1] == '\\')
                {
                    FileList[i] = Path.GetDirectoryName(FileList[i]);
                }
            }
            GIC.AddBoardsToSet(FileList, new StandardConsoleLog(), true,false, 0.8);
            BoardRenderColorSet colors = new BoardRenderColorSet();

            if (pcbcolor == "") pcbcolor = "black";
            colors.SetupColors(pcbcolor, silkcolor, tracecolor, coppercolor);


            GIC.SetColors(colors);
            //GIC.WriteImageFiles(TargetFileBaseName, 96, false, xray, normal, new StandardConsoleLog());
            GIC.DrawAllFilesForPrintPcb(TargetFileBaseName, dpi);
            Console.WriteLine("Done writing {0}", TargetFileBaseName);
            GIC.Dispose();
            GC.Collect();

        }
        private static void CreateImageForSingleFile(ProgressLog log, string arg, Color Foreground, Color Background)
        {
            
            if (arg.ToLower().EndsWith(".png") == true) return;
            GerberImageCreator.AA = false;
            //Gerber.Verbose = true;
            if (Gerber.ThrowExceptions)
            {
                Gerber.SaveGerberFileToImageUnsafe(log, arg, arg + "_render.png", 1000, Foreground, Background);
            }
            else
            {
                Gerber.SaveGerberFileToImage(log, arg, arg + "_render.png", 1000, Foreground, Background);
            }

            if (Gerber.SaveDebugImageOutput)
            {
                Gerber.SaveDebugImage(arg, arg + "_debugviz.png", 1000, Foreground, Background, new StandardConsoleLog());
            }
        }

        public string TheName;
        GerberToImage(string name)
        {
            TheName = name;
        }
    }
}
