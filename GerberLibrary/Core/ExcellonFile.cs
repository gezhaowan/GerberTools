﻿using GerberLibrary.Core;
using GerberLibrary.Core.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerberLibrary
{
    public class ExcellonTool
    {
        public int ID;
        public double Radius;
        public List<PointD> Drills = new List<PointD>();

        public class SlotInfo
        {   
            public PointD Start = new PointD();
            public PointD End = new PointD();

            public override string ToString()
            {
                return $"({Start.X:N2},{Start.Y:N2})-({End.X:N2},{End.X:N2})";
            }
        }
        public List<SlotInfo> Slots = new List<SlotInfo>();
    };

    public class ExcellonFile
    {
        public void Load(ProgressLog log, string filename, double drillscaler = 1.0, double drillRadiusScaler = 1.0f)
        {
            var Load = log.PushActivity("Loading Excellon");
            var lines = File.ReadAllLines(filename);
            ParseExcellon(lines.ToList(), drillscaler,log, drillRadiusScaler);
            log.PopActivity(Load);
        }

        public void Load(ProgressLog log, StreamReader stream, double drillscaler = 1.0, double drillRadiusScaler = 1.0f)
        {
            List<string> lines = new List<string>();
            while (!stream.EndOfStream)
            {
                lines.Add(stream.ReadLine());
            }
            ParseExcellon(lines, drillscaler, log, drillRadiusScaler);
        }

        public static void MergeAll(List<string> Files, string output, ProgressLog Log)
        {
            var LogDepth = Log.PushActivity("Excellon MergeAll");
            if (Files.Count >= 2)
            {
                MultiMerge(Files[0], Files.Skip(1).ToList(), output, Log);
                Log.PopActivity(LogDepth);
                return;

            }
            if (Files.Count < 2)
            {
                if (Files.Count == 1)
                {
                    Log.AddString("Merging 1 file is copying... doing so...");
                    if (File.Exists(output)) File.Delete(output);
                    File.Copy(Files[0], output);
                }
                else
                {
                    Log.AddString("Need files to do anything??");
                }
                Log.PopActivity(LogDepth);
                return;
            }

            string LastFile = Files[0];
            List<string> TempFiles = new List<string>();
            for (int i = 1; i < Files.Count - 1; i++)
            {
                string NewFile = Path.GetTempFileName();
                TempFiles.Add(NewFile);
                Merge(LastFile, Files[i], NewFile, Log);
                LastFile = NewFile;
            }

            Merge(LastFile, Files.Last(), output, Log);
            Log.AddString("Removing merge tempfiles");

            foreach (string s in TempFiles)
            {
                File.Delete(s);
            }
            Log.PopActivity(LogDepth);
        }

        private static void MultiMerge(string file1, List<string> otherfiles, string output, ProgressLog Log)
        {
            int MM = Log.PushActivity("Excellon MultiMerge");
            if (File.Exists(file1) == false)
            {
                Log.AddString(String.Format("{0} not found! stopping process!", file1));
                Log.PopActivity(MM);
                return;
            }
            foreach (var otherfile in otherfiles)
            {
                if (File.Exists(otherfile) == false)
                {
                    Log.AddString(String.Format("{0} not found! stopping process!", otherfile));
                    Log.PopActivity(MM);
                    return;
                }
            }

            Log.AddString(String.Format("Reading {0}:", file1));
            ExcellonFile File1Parsed = new ExcellonFile();
            File1Parsed.Load(Log, file1);
            List<ExcellonFile> OtherFilesParsed = new List<ExcellonFile>();
            foreach (var otherfile in otherfiles)
            {

                Log.AddString(String.Format("Reading {0}:", otherfile));
                ExcellonFile OtherFileParsed = new ExcellonFile();
                OtherFileParsed.Load(Log, otherfile);
                OtherFilesParsed.Add(OtherFileParsed);
            }
            int MaxID = 0;
            foreach (var D in File1Parsed.Tools)
            {
                if (D.Value.ID > MaxID) MaxID = D.Value.ID + 1;
            }
            foreach (var F in OtherFilesParsed)
            {
                foreach (var D in F.Tools)
                {
                    File1Parsed.AddToolWithHoles(D.Value); ;
                    //                D.Value.ID += MaxID;
                    //              File1Parsed.Tools[D.Value.ID] = D.Value;
                }
            }
            File1Parsed.Write(output, 0, 0, 0, 0);

            Log.PopActivity(MM);
        }

        private void AddToolWithHoles(ExcellonTool d)
        {
            ExcellonTool T = FindMatchingTool(d);
            foreach(var a in d.Drills)
            {
                T.Drills.Add(new PointD(a.X, a.Y));
            }
            foreach(var s in d.Slots)
            {
                T.Slots.Add(new ExcellonTool.SlotInfo() { Start = new PointD(s.Start.X, s.Start.Y), End = new PointD(s.End.X, s.End.Y) });
            }
            
        }

        private ExcellonTool FindMatchingTool(ExcellonTool d)
        {
            int freeid = 10;
            foreach(var t in Tools)
            {
                if (d.Radius == t.Value.Radius) return t.Value;
                if (t.Key >= freeid) freeid = t.Key + 1;
            }
            var T = new ExcellonTool() { Radius = d.Radius , ID = freeid};
            
            Tools[T.ID] = T;

            return T;
        }

        public static void Merge(string file1, string file2, string outputfile, ProgressLog Log)
        {
            Log.PushActivity("Excellon Merge");
            if (File.Exists(file1) == false)
            {
                Log.AddString(String.Format("{0} not found! stopping process!", file1));
                Log.PopActivity();
                return;
            }

            if (File.Exists(file2) == false)
            {
                Log.AddString(String.Format("{0} not found! stopping process!", file2));
                Log.PopActivity();
                return;
            }

            Log.AddString(String.Format("Reading {0}:", file1));
            ExcellonFile File1Parsed = new ExcellonFile();
            File1Parsed.Load(Log, file1);
            Log.AddString(String.Format("Reading {0}:", file2));
            ExcellonFile File2Parsed = new ExcellonFile();
            File2Parsed.Load(Log, file2);

            Log.AddString(String.Format("Merging {0} with {1}", file1, file2));

            int MaxID = 0;
            foreach (var D in File1Parsed.Tools)
            {
                if (D.Value.ID > MaxID) MaxID = D.Value.ID + 1;
            }

            foreach (var D in File2Parsed.Tools)
            {
                D.Value.ID += MaxID;
                File1Parsed.Tools[D.Value.ID] = D.Value;
            }

            File1Parsed.Write(outputfile, 0, 0, 0, 0);

            Log.PopActivity();

        }

        public void Write(string filename, double DX, double DY, double DXp, double DYp, double AngleInDeg = 0)
        {
            double Angle = AngleInDeg * (Math.PI * 2.0) / 360.0;
            double CA = Math.Cos(Angle);
            double SA = Math.Sin(Angle);

            List<string> lines = new List<string>();
            lines.Add("%");
            lines.Add("M48");
            lines.Add("METRIC,000.000");
            //lines.Add("M71");
            foreach (var a in Tools)
            {
                lines.Add(String.Format("T{0}C{1}", a.Key.ToString("D2"), (a.Value.Radius * 2).ToString("N2").Replace(',', '.')));
            }
            lines.Add("%");
            GerberNumberFormat GNF = new GerberNumberFormat();
            GNF.SetMetricMode();
            GNF.OmitLeading = true;
            GNF.DigitsAfter = 3;
            GNF.DigitsBefore = 3;
            foreach (var a in Tools)
            {
                lines.Add(String.Format("T{0}", a.Key.ToString("D2")));
                double coordmultiplier = 1;

                foreach (var d in a.Value.Drills)
                {

                    double X = (d.X * coordmultiplier + DXp) / coordmultiplier;
                    double Y = (d.Y * coordmultiplier + DYp) / coordmultiplier;
                    if (Angle != 0)
                    {
                        double nX = X * CA - Y * SA;
                        double nY = X * SA + Y * CA;
                        X = nX;
                        Y = nY;
                    }
                    X = (X * coordmultiplier + DX) / coordmultiplier;
                    Y = (Y * coordmultiplier + DY) / coordmultiplier;

                    lines.Add(string.Format("X{0}Y{1}", GNF.Format(X), GNF.Format(Y).Replace(',', '.')));
                }

                foreach(var s in a.Value.Slots)
                {
                    double XS = (s.Start.X * coordmultiplier + DXp) / coordmultiplier;
                    double YS = (s.Start.Y * coordmultiplier + DYp) / coordmultiplier;
                    double XE = (s.End.X * coordmultiplier + DXp) / coordmultiplier;
                    double YE = (s.End.Y * coordmultiplier + DYp) / coordmultiplier;
                    if (Angle != 0)
                    {
                        double nX = XS * CA - YS * SA;
                        double nY = XS * SA + YS * CA;
                        XS = nX;
                        YS = nY;

                        double neX = XE * CA - YE * SA;
                        double neY = XE * SA + YE * CA;
                        XE = neX;
                        YE = neY;
                 
                    }
                    XS = (XS * coordmultiplier + DX) / coordmultiplier;
                    YS = (YS * coordmultiplier + DY) / coordmultiplier;
                    XE = (XE * coordmultiplier + DX) / coordmultiplier;
                    YE = (YE * coordmultiplier + DY) / coordmultiplier;

                    lines.Add(string.Format("X{0}Y{1}G85X{2}Y{3}", GNF.Format(XS), GNF.Format(YS).Replace(',', '.'),GNF.Format(XE), GNF.Format(YE).Replace(',', '.')));

                }


            }
            lines.Add("M30");
            Gerber.WriteAllLines(filename, lines);
        }
        public Dictionary<int, ExcellonTool> Tools = new Dictionary<int, ExcellonTool>();


        public int TotalDrillCount()
        {
            int T = 0;
            foreach(var Tool in Tools)
            {
                T += Tool.Value.Drills.Count;
            }
            return T;
        }

        private enum CutterCompensation
        {
            None = 0,
            Left,
            Right
        }

        private List<PointD> CutCompensation(List<PointD> path, CutterCompensation compensation, double offset)
        {
            if (compensation == CutterCompensation.None)
                return path;

            if (path.Count < 2)
                return path;

            /* remove contiguous duplicates */
            var unique = new List<PointD>(path.Count);
            PointD prev = null;
            foreach (var point in path)
            {
                if (prev == point)
                    continue;

                prev = point;
                unique.Add(point);
            }
            path = unique;

            /* create offset segments */
            var SegmentsOffset = path.Zip(path.Skip(1), (A, B) =>
            {
                var angle = A.Angle(B);

                if (compensation == CutterCompensation.Left)
                    angle += Math.PI / 2;
                else
                    angle -= Math.PI / 2;

                A += new PointD(offset * Math.Cos(angle), offset * Math.Sin(angle));
                B += new PointD(offset * Math.Cos(angle), offset * Math.Sin(angle));

                return new { A, B };
            });

            /* create segment pairs */
            var SegmentPairs = SegmentsOffset
                .Zip(SegmentsOffset.Skip(1), (First, Second) => new { First, Second })
                .Zip(path.Skip(1), (pair, Center) => new { pair.First, pair.Second, Center });

            var Path = new PolyLine();
            Path.Vertices.Add(SegmentsOffset.First().A);

            foreach (var segment in SegmentPairs)
            {
                /* segments are colinear */
                if (segment.First.B == segment.Second.A)
                    continue;

                var intersection = Helpers.SegmentSegmentIntersect(segment.First.A, segment.First.B, segment.Second.A, segment.Second.B);
                /* if segments intersect, */
                if (intersection != null)
                {
                    /* the intersection point is what connects first and second segments */
                    Path.Vertices.Add(intersection);
                }
                else
                {
                    /* otherwise connect segments with an arc */
                    var Center = segment.Center - segment.First.B;

                    var arc = Gerber.CreateCurvePoints(
                        segment.First.B.X, segment.First.B.Y,
                        segment.Second.A.X, segment.Second.A.Y,
                        Center.X, Center.Y,
                        compensation == CutterCompensation.Left ? InterpolationMode.ClockWise : InterpolationMode.CounterClockwise,
                        GerberQuadrantMode.Multi);

                    Path.Vertices.AddRange(arc);
                }
            }

            Path.Vertices.Add(SegmentsOffset.Last().B);

            return Path.Vertices;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="drillscaler"></param>
        /// <param name="log"></param>
        /// <param name="radiusAdjust">半径修正系数,用于实现电镀孔厚度,单位mm</param>
        /// <returns></returns>
        bool ParseExcellon(List<string> lines, double drillscaler,ProgressLog log,double radiusScaler = 1.0f )
        {
            var LogID = log.PushActivity("Parse Excellon");
            Tools.Clear();
            bool headerdone = false;
            int currentline = 0;
            ExcellonTool CurrentTool = null;
            GerberNumberFormat GNF = new GerberNumberFormat();
            GNF.DigitsBefore = 3;
            GNF.DigitsAfter = 3;
            GNF.OmitLeading = true;
            double Scaler = 1.0f;
            bool FormatSpecified = false;
            bool NumberSpecHad = false;
            double LastX = 0;
            double LastY = 0;
            CutterCompensation Compensation = CutterCompensation.None;
            List<PointD> PathCompensation = new List<PointD>();
            bool WarnIntersections = true;
            while (currentline < lines.Count)
            {
                switch(lines[currentline])
                {
                    //  case "M70":  GNF.Multiplier = 25.4; break; // inch mode
                    case "INCH":
                        if (Gerber.ExtremelyVerbose) log.AddString("Out of header INCH found!");
                        GNF.SetImperialMode();

                        break; // inch mode
                    case "METRIC":
                        if (Gerber.ExtremelyVerbose) log.AddString("Out of header METRIC found!");

                        GNF.SetMetricMode();
                        break;
                    case "M72":
                        if (Gerber.ExtremelyVerbose) log.AddString("Out of header M72 found!");
                        GNF.SetImperialMode();
                        break; // inch mode
                    case "M71":
                        if (Gerber.ExtremelyVerbose) log.AddString("Out of header M71 found!");
                        GNF.SetMetricMode();
                        break; // metric mode

                }
                if (lines[currentline] == "M48")
                {
                    //Console.WriteLine("Excellon header starts at line {0}", currentline);
                    currentline++;
                    while ((lines[currentline] != "%" && lines[currentline] != "M95"))
                    {
                        headerdone = true;
                        //double InchMult = 1;// 0.010;
                        switch (lines[currentline])
                        {
                            //  case "M70":  GNF.Multiplier = 25.4; break; // inch mode
                            case "INCH":
                                GNF.SetImperialMode();

                                //Scaler = 0.01;
                                break; // inch mode
                            case "METRIC":
                                GNF.SetMetricMode();
                                break;
                            case "M72":
                                //GNF.Multiplier = 25.4 * InchMult;
                                GNF.SetImperialMode();
                                //  Scaler = 0.01;
                                break; // inch mode
                            case "M71":
                                //GNF.Multiplier = 1.0;
                                GNF.SetMetricMode();
                                break; // metric mode

                            default:
                                {
                                    var S = lines[currentline].Split(',');
                                    if (S[0].IndexOf("INCH") == 0 || S[0].IndexOf("METRIC") == 0)
                                    {
                                        if (S[0].IndexOf("INCH") ==0)
                                        {
                                            GNF.SetImperialMode();
                                        }
                                        else
                                        {
                                            GNF.SetMetricMode();

                                        }
                                        if (S.Count() > 1)
                                        {
                                            for (int i = 1; i < S.Count(); i++)
                                            {if (S[i][0] == '0')
                                            {
                                                    log.AddString(String.Format("Number spec reading!: {0}", S[i]));
                                                var A = S[i].Split('.');
                                                if (A.Length == 2)
                                                {
                                                    GNF.DigitsBefore = A[0].Length;
                                                    GNF.DigitsAfter = A[1].Length;
                                                    NumberSpecHad = true;
                                                }
                                            }
                                                if (S[i] == "LZ")
                                                {
                                                    GNF.OmitLeading = false;
                                                }
                                                if (S[i] == "TZ")
                                                {
                                                    GNF.OmitLeading = true;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (lines[currentline][0] == ';')
                                        {
                                            if (Gerber.ShowProgress) log.AddString(lines[currentline]);

                                            if (lines[currentline].Contains(";FILE_FORMAT="))
                                            {
                                                var N = lines[currentline].Substring(13).Split(':');
                                                GNF.DigitsBefore = int.Parse(N[0]);
                                                GNF.DigitsAfter = int.Parse(N[1]);
                                                FormatSpecified = true;
                                            }
                                        }
                                        else
                                        {
                                            GCodeCommand GCC = new GCodeCommand();
                                            GCC.Decode(lines[currentline], GNF);
                                            if (GCC.charcommands.Count > 0)
                                                switch (GCC.charcommands[0])
                                                {
                                                    case 'T':
                                                        {
                                                            ExcellonTool ET = new ExcellonTool();


                                                            ET.ID = (int)GCC.numbercommands[0];

                                                            ET.Radius = GNF.ScaleFileToMM(GCC.GetNumber('C')) / 2.0f*radiusScaler;
                                                            Tools[ET.ID] = ET;
                                                        }
                                                        break;
                                                }

                                        }
                                    }
                                }
                                break;
                        }
                        currentline++;
                    }
                    //           Console.WriteLine("Excellon header stops at line {0}", currentline);
                    if (FormatSpecified == false && NumberSpecHad == false)
                    {
                        if (GNF.CurrentNumberScale == GerberNumberFormat.NumberScale.Imperial)
                        {
                          //  GNF.OmitLeading = true;
                            GNF.DigitsBefore = 2;
                            GNF.DigitsAfter = 4;
                        }
                        else
                        {
                            GNF.DigitsAfter = 3;
                            GNF.DigitsBefore = 3;
                        }
                    }
                }
                else
                {
                    if (headerdone)
                    {
                        GCodeCommand GCC = new GCodeCommand();
                        GCC.Decode(lines[currentline], GNF);
                        if (GCC.charcommands.Count > 0)
                        {
                            switch (GCC.charcommands[0])
                            {
                                case 'T':
                                    if ((int)GCC.numbercommands[0] > 0)
                                    {
                                        CurrentTool = Tools[(int)GCC.numbercommands[0]];
                                    }
                                    else
                                    {
                                        CurrentTool = null;
                                    }
                                    break;
                                case 'M':

                                default:
                                    {                                    
                                        GerberSplitter GS = new GerberSplitter();
                                        GS.Split(GCC.originalline, GNF, true);
                                        if (GS.Has("G") && GS.Get("G") == 85 && (GS.Has("X") || GS.Has("Y")))
                                        {
                                            GerberListSplitter GLS = new GerberListSplitter();
                                            GLS.Split(GCC.originalline, GNF, true);

                                            double x1 = LastX;
                                            double y1 = LastY;
                                            
                                            if (GLS.HasBefore("G", "X")) {x1 = GNF.ScaleFileToMM(GLS.GetBefore("G", "X") * Scaler);LastX = x1;}
                                            if (GLS.HasBefore("G", "Y")) {y1 = GNF.ScaleFileToMM(GLS.GetBefore("G", "Y") * Scaler); LastY = y1; }
                                            
                                            
                                            double x2 = LastX;
                                            double y2 = LastY;

                                            if (GLS.HasAfter("G", "X")) { x2 = GNF.ScaleFileToMM(GLS.GetAfter("G", "X") * Scaler); LastX = x2; }
                                            if (GLS.HasAfter("G", "Y")) { y2 = GNF.ScaleFileToMM(GLS.GetAfter("G", "Y") * Scaler); LastY = y2; }

                                            CurrentTool.Slots.Add(new ExcellonTool.SlotInfo() { Start = new PointD(x1 * drillscaler, y1 * drillscaler), End = new PointD(x2 * drillscaler, y2 * drillscaler) });

                                            LastX = x2;
                                            LastY = y2;
                                        }
                                        else if (GS.Has("G") && GS.Get("G") == 00 && (GS.Has("X") || GS.Has("Y")))
                                        {
                                            GerberListSplitter GLS = new GerberListSplitter();
                                            GLS.Split(GCC.originalline, GNF, true);

                                            double x1 = LastX;
                                            double y1 = LastY;

                                            if (GLS.HasAfter("G", "X")) { x1 = GNF.ScaleFileToMM(GLS.GetAfter("G", "X") * Scaler); LastX = x1; }
                                            if (GLS.HasAfter("G", "Y")) { y1 = GNF.ScaleFileToMM(GLS.GetAfter("G", "Y") * Scaler); LastY = y1; }

                                            /* cancel cutter compensation */
                                            Compensation = CutterCompensation.None;
                                            PathCompensation.Clear();
                                        }
                                        else if (GS.Has("G") && GS.Get("G") == 01 && (GS.Has("X") || GS.Has("Y")))
                                        {
                                            GerberListSplitter GLS = new GerberListSplitter();
                                            GLS.Split(GCC.originalline, GNF, true);

                                            double x1 = LastX;
                                            double y1 = LastY;
                                            double x2 = LastX;
                                            double y2 = LastY;

                                            if (GLS.HasAfter("G", "X")) { x2 = GNF.ScaleFileToMM(GLS.GetAfter("G", "X") * Scaler); LastX = x2; }
                                            if (GLS.HasAfter("G", "Y")) { y2 = GNF.ScaleFileToMM(GLS.GetAfter("G", "Y") * Scaler); LastY = y2; }
                                            if (Compensation == CutterCompensation.None)
                                                CurrentTool.Slots.Add(new ExcellonTool.SlotInfo() { Start = new PointD(x1 * drillscaler, y1 * drillscaler), End = new PointD(x2 * drillscaler, y2 * drillscaler) });
                                            else
                                                PathCompensation.Add(new PointD(x2 * drillscaler, y2 * drillscaler));

                                            LastX = x2;
                                            LastY = y2;
                                        }
                                        else if (GS.Has("G") && GS.Get("G") == 40) /* cutter compensation off */
                                        {
                                            var comp = CutCompensation(PathCompensation, Compensation, CurrentTool.Radius * drillscaler);

                                            if (WarnIntersections)
                                            {
                                                /* warn about path intersections */
                                                for (int i = 0; i < comp.Count - 1; i++)
                                                {
                                                    for (int j = i + 2; j < comp.Count - 1; j++)
                                                    {
                                                        var intersection = Helpers.SegmentSegmentIntersect(comp[i], comp[i + 1], comp[j], comp[j + 1]);
                                                        if (intersection != null)
                                                        {
                                                            log.AddString("Path with intersections found on cut compensation! Inspect output for accuracy!");
                                                            WarnIntersections = false;
                                                            break;
                                                        }
                                                    }

                                                    if (!WarnIntersections)
                                                        break;
                                                }
                                            }

                                            /* create line segments from set of points */
                                            var array = comp.Zip(comp.Skip(1), Tuple.Create);
                                            CurrentTool.Slots.AddRange(array.Select(i => new ExcellonTool.SlotInfo() { Start = i.Item1, End = i.Item2 }));

                                            Compensation = CutterCompensation.None;
                                            PathCompensation.Clear();
                                        }
                                        else if (GS.Has("G") && GS.Get("G") == 41) /* cutter compensation left: offset of the cutter radius is to the LEFT of contouring direction */
                                        {
                                            if (Compensation != CutterCompensation.None)
                                                log.AddString("Unterminated cutter compensation block found! Inspect output for accuracy!");

                                            Compensation = CutterCompensation.Left;
                                            PathCompensation.Clear();
                                            PathCompensation.Add(new PointD(LastX * drillscaler, LastY * drillscaler));
                                        }
                                        else if (GS.Has("G") && GS.Get("G") == 42) /* cutter compensation right: offset of the cutter radius is to the RIGHT of contouring direction */
                                        {
                                            if (Compensation != CutterCompensation.None)
                                                log.AddString("Unterminated cutter compensation block found! Inspect output for accuracy!");

                                            Compensation = CutterCompensation.Right;
                                            PathCompensation.Clear();
                                            PathCompensation.Add(new PointD(LastX * drillscaler, LastY * drillscaler));
                                        }
                                        else
                                        {
                                            //Deal with the repeat code
                                            if (GS.Has("R") && (GS.Has("X") || GS.Has("Y")))
                                            {
                                                double repeatX = 0;
                                                double repeatY = 0;

                                                if (GS.Has("X"))
                                                    repeatX = GNF.ScaleFileToMM(GS.Get("X") * Scaler);
                                                if (GS.Has("Y"))
                                                    repeatY = GNF.ScaleFileToMM(GS.Get("Y") * Scaler);

                                                for (int repeatIndex = 1; repeatIndex <= GS.Get("R"); repeatIndex++)
                                                {
                                                    double X = LastX;
                                                    if (GS.Has("X"))
                                                        X += repeatX;

                                                    double Y = LastY;
                                                    if (GS.Has("Y"))
                                                        Y += repeatY;

                                                    CurrentTool.Drills.Add(new PointD(X * drillscaler, Y * drillscaler));
                                                    LastX = X;
                                                    LastY = Y;
                                                }
                                            }
                                            else if (GS.Has("X") || GS.Has("Y"))
                                            {
                                                double X = LastX;
                                                if (GS.Has("X")) X = GNF.ScaleFileToMM(GS.Get("X") * Scaler);
                                                double Y = LastY;
                                                if (GS.Has("Y")) Y = GNF.ScaleFileToMM(GS.Get("Y") * Scaler);
                                                if (Compensation == CutterCompensation.None)
                                                    CurrentTool.Drills.Add(new PointD(X * drillscaler, Y * drillscaler));
                                                else
                                                    PathCompensation.Add(new PointD(X * drillscaler, Y * drillscaler));
                                                LastX = X;
                                                LastY = Y;
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
                currentline++;
            }
            log.PopActivity(LogID);
            return headerdone;
        }


        public static void WriteContainedOnly(string inputfile, PolyLine Boundary, string outputfilename, ProgressLog Log)
        {
            Log.PushActivity("Excellon Clipper");
            if (File.Exists(inputfile) == false)
            {
                Log.AddString(String.Format("{0} not found! stopping process!", Path.GetFileName(inputfile)));
                Log.PopActivity();
                return;
            }
            Log.AddString(String.Format("Clipping {0} to {1}", Path.GetFileName(inputfile), Path.GetFileName(outputfilename)));

            ExcellonFile EF = new ExcellonFile();
            EF.Load(Log, inputfile);
            EF.WriteContained(Boundary, outputfilename, Log);
            Log.PopActivity();
        }

        private void WriteContained(PolyLine boundary, string outputfilename, ProgressLog log)
        {
            ExcellonFile Out = new ExcellonFile();

            foreach(var T in Tools)
            {
                Out.Tools[T.Key] = new ExcellonTool() { ID = T.Value.ID, Radius = T.Value.Radius };
                foreach(var d in T.Value.Drills)
                {
                    if (boundary.PointInPoly(new PointD(d.X , d.Y)))
                      {
                        Out.Tools[T.Key].Drills.Add(d);
                    }
                }
                foreach (var d in T.Value.Slots)
                {
                    if (boundary.PointInPoly(d.Start) || boundary.PointInPoly(d.End))
                    {
                        Out.Tools[T.Key].Slots.Add(d);
                    }
                }
            }

            Out.Write(outputfilename, 0, 0, 0, 0);
        }
    }
}
