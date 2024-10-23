using System.Text.RegularExpressions;

namespace EldenRingInfiniteNGPlus;

internal static partial class Program
{        
    static string TitleText => @"

                                  E L D E N   R I N G

                       I N F I N I T E   N E W   G A M E   P L U S

                                         v2.0
                                    
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
            `+N`: Increase NG+ level by N (no maximum!).
            `info`: See detailed information about the stat scaling that will be 
                applied by the mod at NG+8 and beyond.

        Current level is stored in a text file called 'LAST_NG_LEVEL.cfg' in
        your game directory. You can edit this file or just delete it to reset
        the NG+ level to 1 next time you run this executable.

        This mod does NOT edit `regulation.bin` or any other game files -- only
        running game memory. The only requirements for compatibility with other
        mods is that rows 7410-7600 (increments of 10) are present in 
        `SpEffectParam`, which are the NG+ scaling values modified in real time,
        and that the boss rune rewards in `GameAreaParam` are present (whatever
        values are found there will be scaled). As long as these two conditions
        are met, you are free to edit `regulation.bin` with other mods!
";

    static string InfoText => @"
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

    static InfiniteNGPlusManager? Manager { get; set; }

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

        Manager = new InfiniteNGPlusManager(gameDir);
        Manager.Start();  // will also start monitor thread            

        GetPlayerCommands();

        Console.WriteLine("Finished. Press Enter to exit.");
        Console.ReadLine();
    }

    static Regex SetLevelRe { get; } = SetLevelRegex();
    
    static void GetPlayerCommands()
    {
        while (true)
        {
            if (Manager == null)
            {
                Console.WriteLine("Manager is null. Exiting.");
                return;
            }
            
            Console.Write(">> ");
            string? cmd = Console.ReadLine();

            switch (cmd)
            {
                case null:
                    continue;
                case "info":
                    Console.WriteLine(InfoText);
                    continue;
            }
            
            Match match = SetLevelRe.Match(cmd);
            if (match.Success)
            {
                int level = int.Parse(match.Groups[2].Value);
                switch (match.Groups[1].Value)
                {
                    case "+":
                        Console.WriteLine($"Increment by {level}");
                        Manager.RequestEffectLevelChange(level);
                        break;
                    case "-":
                        Console.WriteLine($"Decrement by {level}");
                        Manager.RequestEffectLevelChange(-level);
                        break;
                    case "=":
                        Console.WriteLine($"Set to {level}");
                        Manager.RequestEffectLevel(level);
                        break;
                }
                Thread.Sleep(500);  // allow time for output to print
            }
            else
                Console.WriteLine("Invalid command.");
        }
    }
    
    static string SetGameDir(string saveToFile = "")
    {
        var ofd = new OpenFileDialog
        {
            Filter = "EXE Files|*.exe",
        };

        if (ofd.ShowDialog() != DialogResult.OK) return "";
            
        string name = Path.GetDirectoryName(ofd.FileName) + "\\";
        if (saveToFile != "")
            File.WriteAllText(saveToFile, name);
        return name;

    }
    
    [GeneratedRegex(@"([+-=])(\d+)")]
    private static partial Regex SetLevelRegex();
}