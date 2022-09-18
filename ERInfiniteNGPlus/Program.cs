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

        Keep this companion app running alongside Elden Ring and input commands
        to set or modify the current NG+ level, which will scale up enemy stats,
        enemy rune drops, and boss rune rewards as high as you want to go.

        Note that your game should be in exactly NG+1 while using this mod.
        If you install the bundled `event/common.emevd.dcx` file as part of this
        mod and load a save file currently in NG only, it will automatically 
        change the internal NG+ level to 1. It WILL NOT REDUCE your NG+ level
        if you are already past NG+1 -- you will need to use Cheat Engine or
        another tool for such playthroughs! If you use this mod while in NG+2
        or beyond, the game will apply ADDITIONAL scaling on top of the 'normal'
        scaling applied by this mod, so be wary of this!

        Note that you can effectively disable NG+ entirely and return to NG
        stats/runes by setting NG+ level to 0 using this console.
        
        Commands:
            `=N`: Set NG+ level to exactly N (minimum of zero).
            `-N`: Decrease NG+ level by N (minimum of zero).
            `+N`: Increase NG+ level by N.
            `death N`: Set NG+ level to automatically change by `N` upon death.
                Use `death 0` to disable. `N` can be negative.
            `info`: See detailed information about the stat scaling that will be 
                applied by the mod at NG+8 and beyond.

        Current level is stored in a text file called 'LAST_NG_LEVEL.cfg' in
        your game directory. You can edit this file or just delete it to reset
        the NG+ level to 1 next time you run this executable.

        This mod does NOT edit `regulation.bin` or any other game files -- only
        running game memory -- and uses its own base scaling NG+ values. You 
        are free to edit `regulation.bin` with other mods, but make sure the 
        standard NG+ SpEffect scaling rows (7400-7600) are present!
";

        static string InfoText { get; } = @"
        Standard scaling is applied from NG+1 to NG+7, except for boss rune
        rewards, which will be slightly above the normal values (I haven't
        determined precisely how the game changes these yet).

        At NG+8 and beyond, each level of NG+ increases the multiplier applied
        to each enemy stat by the following values. These do NOT compound: each
        stat is only multiplied ONCE. The NG+ level just steadily increases the 
        value of that multiplier. For example, if an enemy's max HP multiplier
        is currently 2.75, it will be 2.8 at the next NG+ level, then 2.85, and
        so on.

            Max HP: 0.05
            Max Stamina: 0.05
            Rune Drop: 0.025
            Attack Power: 0.05 (physical, all elemental types, and stamina damage)
            Defense: 0.05
            Resistance: 0.015 (all)
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

        static Regex SetLevelRe { get; } = new Regex(@"([+-=])(\d+)");
        static Regex ReloadRe { get; } = new Regex(@"reload (\w+)");
        static Regex DeathRe { get; } = new Regex(@"death ([-\d]+)");

        static void GetPlayerCommands()
        {
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
                    if (cmd == "info")
                    {
                        Console.WriteLine(InfoText);
                        continue;
                    }

                    Match match;

                    match = DeathRe.Match(cmd);
                    if (match.Success)
                    {
                        try
                        {
                            Manager.AutoChangeOnDeath = int.Parse(match.Groups[1].Value);
                            Console.WriteLine($"NG+ level will change by {Manager.AutoChangeOnDeath} upon death.");
                            continue;
                        }
                        catch (FormatException)
                        {
                            Console.WriteLine($"Invalid command. Could not parse number: {match.Groups[1].Value}");
                            continue;
                        }
                    }

                    match = ReloadRe.Match(cmd);
                    if (match.Success)
                    {
                        // Read given param in regulation and reload it.
                        Manager.ReloadParam(match.Groups[1].Value);
                        Thread.Sleep(500);  // allow time for output to print
                        continue;
                    }

                    match = SetLevelRe.Match(cmd);
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
