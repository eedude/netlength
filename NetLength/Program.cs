using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace NetLength
{
    class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        static void Main(string[] args)
        {
            List<string> nets = new List<string>();

            if (args.Length == 0)
            {
                AllocConsole();
                Console.WriteLine("NetLength V2.1 - by Theodore Beauchant");
                Console.WriteLine("Usage: NetLength.exe pcb_info_file_path tracks [-countvias]");
                Console.WriteLine("\tpcb_info_file_path: Path to the Hyperlynx file for the PCB");
                Console.WriteLine("\ttracks: comma separated list of net names (each are treated as regexs)");
                Console.WriteLine("\tcountvias: Include the length of the vias in the calculation");
                Console.WriteLine("Example:");
                Console.WriteLine("NetLength.exe \"C:\\MyPCB.hyp\" \"DDR_.*,USB.*\"");
                Console.ReadLine();
                return;
            }

            string filename = args[0];

            string[] a = args[1].Split(',');
            foreach (string s in a)
                nets.Add(s);

            if (args.Length >= 3)
            {
                for (int i = 2; i < args.Length; i++)
                {
                    if (args[i] == "-countvias")
                        Board.ViaLength = true;
                }
            }

            try
            {
                Board.LoadBoard(args[0], nets, false);
                Board.FollowAll();

                if (Board.FollowResults.Count == 0)
                {
                    AllocConsole();
                    Console.Clear();
                    Console.WriteLine("Did not find any route");
                    Console.ReadLine();
                    return;
                }
            }
            catch (Exception e)
            {
                AllocConsole();
                Console.WriteLine("Woops! Got an exception:");
                Console.WriteLine(e.ToString());
                Console.ReadLine();
                return;
            }

            try
            {
                Microsoft.Office.Interop.Excel.Application ExcelObj = (Microsoft.Office.Interop.Excel.Application)System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
                Microsoft.Office.Interop.Excel.Workbook eBook = ExcelObj.ActiveWorkbook;
                Microsoft.Office.Interop.Excel.Worksheet sheet = eBook.Sheets["Import"];
                sheet.Cells.Clear();
                Microsoft.Office.Interop.Excel.Range range = sheet.get_Range("A1", Type.Missing);
                range = range.get_Resize(Board.FollowResults.Count, 2);
                string[,] data = new string[Board.FollowResults.Count, 2];
                for (int i = 0; i < Board.FollowResults.Count; i++)
                {
                    data[i, 0] = Board.FollowResults[i].name;
                    data[i, 1] = Board.FollowResults[i].length.ToString();
                }
                range.set_Value(Type.Missing, data);

                range = sheet.get_Range("C1", Type.Missing);
                range = range.get_Resize(4, 2);
                data = new string[4, 2];
                data[0, 0] = "File:";
                data[0, 1] = filename;
                data[1, 0] = "Nets:";
                data[1, 1] = args[1];
                data[2, 0] = "Date:";
                data[2, 1] = DateTime.Now.ToString();
                data[3, 0] = "Via length:";
                data[3, 1] = Board.ViaLength.ToString();
                range.set_Value(Type.Missing, data);
            }
            catch (Exception e)
            {
                AllocConsole();
                Console.WriteLine("Exception while trying to update Excel spreadhseet (is the sheet open?):");
                Console.WriteLine(e.Message);
                Console.ReadLine();
                return;
            }
        }
    }
}
