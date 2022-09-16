using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace ERInfiniteNGPlus
{
    static class Program
    {        
        static string TitleText { get; } = @"

                                  E L D E N   R I N G

                       I N F I N I T E   N E W   G A M E   P L U S

                                         v1.0
                                    
                                      by Grimrukh

        Keep this companion app running alongside Elden Ring to automatically
        scale up NG+ enemy buffs (and rune drops) each time you die.

        You can also type commands like '=5', '+1', and '-2' into this console
        to set, increase, or decrease the current NG+ level, respectively.

        Current level is stored in a text file called 'LAST_NG_LEVEL.cfg' in
        your game directory. You can edit this file or just delete it to reset
        the NG+ level next time you run this executable.

        This mod does NOT edit `regulation.bin` or any other game files -- only
        running game memory -- and uses its own base scaling NG+ values. You 
        are free to edit `regulation.bin` with other mods, but make sure the 
        NG+ SpEffect scaling rows (7400-7600) are present!
";

        static ModManager Manager { get; set; }

        /// <summary>
        /// Hooks into `ELDENRING.exe`, finds the `SpEffectParam` rows that buff enemies in New Game Plus,
        /// and modifies them (in memory) upon death or other conditions.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            Console.WriteLine(TitleText);

            string gameDir;

            if (!File.Exists("GameDirectory.cfg"))
            {
                Console.WriteLine("Press ENTER to select your ELDEN RING executable.");
                Console.ReadLine();
                gameDir = SetGameDir("GameDirectory.cfg");
            }
            else
            {
                gameDir = File.ReadAllText("GameDirectory.cfg");
                Console.WriteLine($"Using saved game directory from 'GameDirectory.cfg':\n    '{gameDir}'");
                if (!Directory.Exists(gameDir))
                {
                    Console.WriteLine($"Game directory '{gameDir} does not exist. Edit 'GameDirectory.cfg' or delete it to start fresh.");
                    Console.WriteLine("Press Enter to close.");
                    Console.ReadLine();
                    return;
                }
            }
            if (gameDir == "")
            {
                Console.WriteLine("No game directory specified. Press ENTER to close this console.");
                Console.ReadLine();
                return;
            }

            Manager = new ModManager(gameDir);
            Manager.Start();  // will also start monitor thread            

            GetPlayerCommands();

            Console.WriteLine("Finished. Press Enter to exit.");
            Console.ReadLine();
        }

        static void GetPlayerCommands()
        {
            Regex commandRe = new Regex(@"([+-=])(\d+)");

            while (true)
            {
                if (Manager == null)
                {
                    Console.WriteLine("ModManager not running. Aborting.");
                    break;
                }

                Console.Write(">> ");
                string cmd = Console.ReadLine();
                if (cmd != null)
                {
                    Match match = commandRe.Match(cmd);
                    if (match.Success)
                    {
                        int level = int.Parse(match.Groups[2].Value);
                        switch (match.Groups[1].Value)
                        {
                            case "+":
                                Console.WriteLine($"Increment by {level}");
                                Manager.RequestNewEffectLevel(level, isRelative: true);
                                break;
                            case "-":
                                Console.WriteLine($"Decrement by {level}");
                                Manager.RequestNewEffectLevel(-level, isRelative: true);
                                break;
                            case "=":
                                Console.WriteLine($"Set to {level}");
                                Manager.RequestNewEffectLevel(level, isRelative: false);
                                break;
                            default:
                                break;
                        }
                        Thread.Sleep(500);  // allow time for output to print
                    }
                    else
                        Console.WriteLine("Invalid command.");
                }
            }
        }

        static string SetGameDir(string saveToFile = "")
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "EXE Files|*.exe"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string name = Path.GetDirectoryName(ofd.FileName) + "\\";
                if (saveToFile != "")
                    File.WriteAllText(saveToFile, name);
                return name;
            }
            else
            {
                return "";
            }
        }
    }
}
