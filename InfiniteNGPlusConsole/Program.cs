using System.Numerics;
using System.Text.RegularExpressions;
using EldenRingBase;
using EldenRingBase.GameHook;
using EldenRingBase.Memory;
using EldenRingBase.Params;
using EldenRingInfiniteNGPlus;
using SoulsFormats;

namespace InfiniteNGPlusConsole;

internal static partial class Program
{        
    static string TitleText => 
        """


                                     E L D E N   R I N G

                          I N F I N I T E   N E W   G A M E   P L U S

                                            v2.0
                                       
                                         by Grimrukh
        
                NOTE: As of v2.0, this mod comes with a native DLL called 
                `EldenRingInfiniteNGPlusLauncher.dll`, which can be used with Elden Ring
                Mod Loader by placing it in the 'Game/mods' directory together with
                the complete `EldenRingInfiniteNGPlus` folder containing this executable
                and its dependencies. The Launcher DLL will automatically start and stop
                this executable in the background when you start and stop Elden Ring.
                You won't be able to use the console commands, but the default event flags
                will be monitored for automatic NG+ level adjustment (i.e. through Site
                of Grace menu options). You can check the `InfiniteNGPlusConsole.log` file
                to see the output of the latest instance of the executable.
                
                Thanks to Shadow Vincent for commissioning this 2024 mod update.

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

                Core Commands:
                    `=N`: Set NG+ level to exactly N (minimum of zero).
                    `-N`: Decrease NG+ level by N (minimum of zero).
                    `+N`: Increase NG+ level by N (no maximum!).
                    `info`: See detailed information about the stat scaling that will be 
                       applied by the mod at NG+8 and beyond.
                
                Debug Commands:
                    `internal N`: Set the internal NG+ level to N.
                         - Note that this mod automatically sets this to 0 for N = 0 and
                         1 for N > 0 (as its own scaling is applied using NG+1 mechanics).
                    `kill {entity_id}`: Kill a loaded enemy by entity ID.
                    `enable {flag}`: Enable an event flag. (Only map-specific flags supported.)
                    `disable {flag}`: Disable an event flag. (Only map-specific flags supported.)
                    `enemies {map_stem}`: Print all loaded enemies (or only those in specific map).
                    `save pos`: Save player position + rotation (in LOCAL MAP) for later restoration.
                    `restore pos`: Restore player position + rotation to last saved values.

                Current level is stored in a text file called 'LAST_NG_LEVEL.cfg' next
                this this executable. You can edit this file or just delete it to reset
                the NG+ level to 1 next time you run this.

                This mod does NOT check or edit `regulation.bin` or any other game files
                -- only running game memory. The only requirements for compatibility with other
                mods is that rows 7410-7600 (increments of 10) are present in 
                `SpEffectParam`, which are the NG+ scaling values modified in real time,
                and that the boss rune rewards in `GameAreaParam` are present (whatever
                values are found there will be scaled). As long as these two conditions
                are met, you are free to edit `regulation.bin` with other mods.

        """;

    static string InfoText => 
        """
              Standard scaling is applied from NG+1 to NG+7, except for boss rune
              rewards, which will be slightly above the normal values as it is
              difficult to perfectly match the hard-coded system for scaling these.

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
        """;

    static InfiniteNGPlusManager? InfiniteNgPlusManager { get; set; }
    static Thread? UpdateThread { get; set; }
    static long UpdateTimeMs { get; set; }
    static CancellationTokenSource? UpdateCts { get; set; }
    
    static EldenRingHook? Hook { get; set; }
    static FlagManager? FlagManager { get; set; }
    static ParamManager? ParamManager { get; set; }
    static PlayerManager? PlayerManager { get; set; }
    static EnemyMonitor? EnemyMonitor { get; set; }
    
    static Vector3? SavedPlayerPos { get; set; }
    static float? SavedPlayerRotY { get; set; }

    /// <summary>
    /// Hooks into `ELDENRING.exe`, finds the `SpEffectParam` rows that buff enemies in New Game Plus,
    /// and modifies them (in memory) upon death or other conditions.
    /// </summary>
    [STAThread]
    static void Main()
    {
        Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
        
        Logging.SetLogFile("InfiniteNGPlusConsole.log");
        Logging.Info("Started InfiniteNGPlusConsole log.");
        
        Console.WriteLine(TitleText);
        
        Hook = new EldenRingHook(1000, 1000);
        FlagManager = new FlagManager(Hook);
        PlayerManager = new PlayerManager(Hook);
        EnemyMonitor = new EnemyMonitor(Hook);
        Dictionary<string, PARAMDEF> paramdefs = ParamReader.GetParamdefs();
        ParamManager = new ParamManager(Hook, paramdefs.Values.ToList());
        
        InfiniteNgPlusManager = new InfiniteNGPlusManager(
            Hook, FlagManager, ParamManager, "LAST_NG_LEVEL.cfg", editLevelText: true);
        
        // Start update thread.
        HookAndStartUpdate();
        
        // Blocking loop to get player commands.
        GetPlayerCommands();
        
        // Cancel and join update thread.
        UpdateCts?.Cancel();
        UpdateThread?.Join();

        Console.WriteLine("Finished. Press Enter to exit.");
        Console.ReadLine();
        Console.WriteLine("Closing...");
    }

    static Regex SetLevelRe { get; } = SetLevelRegex();
    static Regex SetInternalLevelRe { get; } = SetInternalLevelRegex();
    static Regex EnableFlagRe { get; } = EnableFlagRegex();
    static Regex DisableFlagRe { get; } = DisableFlagRegex();
    static Regex KillRe { get; } = KillRegex();
    static Regex PrintEnemiesRe { get; } = PrintEnemiesRegex();
    static Regex GetParamRe { get; } = GetParamRegex();

    static void HookAndStartUpdate()
    {
        Hook!.Start();
        UpdateTimeMs = 0;
        
        // We block the main thread here until the initial hook succeeds.
        Logging.Info("Searching for running ELDEN RING process...");
        while (!Hook.Hooked)
            Thread.Sleep(100);
        Logging.Info("--> Connected to ELDEN RING successfully.");
        // NOTE: We don't load PARAMs here. They are fast to load on first use.

        UpdateCts = new CancellationTokenSource();
        UpdateThread = new Thread(() => Update(UpdateCts))
        {
            IsBackground = true,
        };
        UpdateThread.Start();
    }

    static void Update(CancellationTokenSource updateCts)
    {
        while (true)
        {
            if (updateCts?.IsCancellationRequested == true)
                return;
            
            if (Hook == null || InfiniteNgPlusManager == null || ParamManager == null)
            {
                Logging.Error("Hook or manager is null. Exiting.");
                return;
            }
            
            if (!Hook.Hooked)
            {
                Logging.Warning("Lost game connection. Waiting to reconnect...");
                while (!Hook.Hooked)
                {
                    Thread.Sleep(100);
                    UpdateTimeMs += 100;
                }
                Logging.Info("--> Reconnected to ELDEN RING successfully.");
                // NOTE: We don't load PARAMs here. They are fast to load on first use.
            }
        
            // Update monitors.
            try
            {
                EnemyMonitor?.CheckUpdate(UpdateTimeMs, Hook.LoadedTimeMs); // optional
                InfiniteNgPlusManager.CheckUpdate(UpdateTimeMs, Hook.LoadedTimeMs);
            }
            catch (Exception ex)
            {
                // Log traceback of error.
                Logging.Error($"Fatal error in update loop: {ex}");
                if (ex.StackTrace != null)
                    Logging.Error(ex.StackTrace);
                throw;
            }
        
            Thread.Sleep(33);
            UpdateTimeMs += 33;
        }
    }
    
    static void GetPlayerCommands()
    {
        while (true)
        {
            if (InfiniteNgPlusManager == null || FlagManager == null || ParamManager == null)
            {
                Logging.Error("Manager(s) are null. Exiting.");
                return;
            }
            
            Console.Write(">> ");
            string? cmd = Console.ReadLine();

            switch (cmd)
            {
                case null:
                    continue;
                case "exit":
                    Logging.Info("Exiting...");
                    return;
                case "info":
                    Console.WriteLine(InfoText);
                    continue;
            }
            
            Match killMatch = KillRe.Match(cmd);
            if (killMatch.Success)
            {
                int entityID = int.Parse(killMatch.Groups[1].Value);
                KillEntity(entityID);
                continue;
            }
            
            Match enableMatch = EnableFlagRe.Match(cmd);
            if (enableMatch.Success)
            {
                uint flag = uint.Parse(enableMatch.Groups[1].Value);
                FlagManager.Enable(flag);
                continue;
            }

            Match disableMatch = DisableFlagRe.Match(cmd);
            if (disableMatch.Success)
            {
                uint flag = uint.Parse(disableMatch.Groups[1].Value);
                FlagManager.Disable(flag);
                continue;
            }
            
            Match printMatch = PrintEnemiesRe.Match(cmd);
            if (printMatch.Success)
            {
                string mapStem = printMatch.Groups[1].Value.TrimStart();
                PrintEnemies(onlyMapStem: mapStem);
                continue;
            }
            
            Match getParamMatch = GetParamRe.Match(cmd);
            if (getParamMatch.Success)
            {
                string paramName = getParamMatch.Groups[1].Value;
                int rowID = int.Parse(getParamMatch.Groups[2].Value);

                // Get `ParamType` matching the parameter name.
                ParamType paramType;
                try
                {
                    paramType = Enum.Parse<ParamType>(paramName, true);
                }
                catch (ArgumentException)
                {
                    Logging.Error($"Invalid or unsupported PARAM name: {paramName}");
                    continue;
                }
                ParamInMemory? param = ParamManager.GetParam(paramType);
                if (param == null)
                {
                    Logging.Error($"Failed to read {paramName} from ELDEN RING game memory.");
                    continue;
                }
                param.PrintRow(rowID);
                continue;
            }

            if (cmd == "save pos")
            {
                SavePlayerPos();
                Logging.Info("Player position saved.");
                continue;
            }
            
            if (cmd == "restore pos")
            {
                RestorePlayerPos();
                Logging.Info("Player position restored.");
                continue;
            }
            
            Match internalMatch = SetInternalLevelRe.Match(cmd);
            if (internalMatch.Success)
            {
                uint level = uint.Parse(internalMatch.Groups[1].Value);  // regex enforces non-negative string
                Logging.Info($"Setting internal NG+ level to {level}");
                InfiniteNgPlusManager.InternalNewGamePlusLevel = level;
                continue;
            }

            if (cmd == "internal")
            {
                Logging.Info($"Current internal NG+ level: {InfiniteNgPlusManager.InternalNewGamePlusLevel}");
                continue;
            }
            
            Match match = SetLevelRe.Match(cmd);
            if (match.Success)
            {
                int level = int.Parse(match.Groups[2].Value);
                switch (match.Groups[1].Value)
                {
                    case "+":
                        Logging.Info($"Increment by {level}");
                        InfiniteNgPlusManager.RequestEffectLevelChange(level);
                        break;
                    case "-":
                        Logging.Info($"Decrement by {level}");
                        InfiniteNgPlusManager.RequestEffectLevelChange(-level);
                        break;
                    case "=":
                        Logging.Info($"Set to {level}");
                        InfiniteNgPlusManager.RequestEffectLevel(level);
                        break;
                }
                Thread.Sleep(500);  // allow time for output to print
            }
            else
                Logging.Info("Invalid command.");
        }
    }
    
    static void PrintEnemies(bool livingOnly = false, string onlyMapStem = "")
    {
        if (EnemyMonitor == null)
        {
            Logging.Error("Cannot print enemies: Enemy monitor not loaded.");
            return;
        }
        foreach (EnemyIns enemy in EnemyMonitor.Enemies)
        {
            if (livingOnly && enemy.CurrentHP == 0)
                continue;
            if (onlyMapStem != "" && enemy.MapStem != onlyMapStem)
                continue;
            string mapNameID = $"<{enemy.MapStem}> {enemy.Name} ({enemy.EntityID})".PadRight(38);
            string hp = $"[{enemy.CurrentHP} / {enemy.MaxHP}]".PadRight(18);
            string pos = $"({enemy.EnemyLocalPosition.X:0.00}, {enemy.EnemyLocalPosition.Y:0.00}, {enemy.EnemyLocalPosition.Z:0.00})";
            Console.WriteLine($"{mapNameID}: {hp} | {pos}");
        }
    }

    static bool KillEntity(int entityID)
    {
        if (EnemyMonitor == null)
        {
            Logging.Error("Cannot kill enemy: Enemy monitor not loaded.");
            return false;
        }
        EnemyIns? entity = EnemyMonitor.FindEnemyEntityID(entityID);
        if (entity != null)
        {
            if (entity.CurrentHP == 0)
            {
                Logging.Info($"Entity {entityID} already has zero HP.");
                return false;
            }
            entity.CurrentHP = 0;
            Logging.Info($"Entity {entityID} killed (HP = 0).");
            return true;
        }
        Logging.Error($"Could not find an enemy to kill with entity ID {entityID}.");
        return false;
    }

    static void SavePlayerPos()
    {
        if (PlayerManager == null)
        {
            Logging.Error("Cannot save player position: player manager not loaded.");
            return;
        }
        if (PlayerManager.PlayerIns == null)
        {
            Logging.Error("Cannot save player position: player not loaded.");
            return;
        }

        SavedPlayerPos = PlayerManager.PlayerIns.ModuleBase.PhysicsData.RelativePosition;
        SavedPlayerRotY = PlayerManager.PlayerIns.ModuleBase.PhysicsData.RotationY;
    }
    
    static void RestorePlayerPos()
    {
        if (PlayerManager == null)
        {
            Logging.Error("Cannot restore player position: player manager not loaded.");
            return;
        }
        if (PlayerManager.PlayerIns == null)
        {
            Logging.Error("Cannot restore player position: player not loaded.");
            return;
        }
        
        if (SavedPlayerPos == null || SavedPlayerRotY == null)
        {
            Logging.Error("Cannot restore player position: no saved position/rotation.");
            return;
        }

        PlayerManager.PlayerIns.ModuleBase.PhysicsData.RelativePosition = SavedPlayerPos.Value;
        PlayerManager.PlayerIns.ModuleBase.PhysicsData.RotationY = SavedPlayerRotY.Value;
    }
    
    [GeneratedRegex(@"^([+-=])(\d+)$")]
    private static partial Regex SetLevelRegex();
    
    [GeneratedRegex(@"^internal (\d+)$")]
    private static partial Regex SetInternalLevelRegex();
    
    [GeneratedRegex(@"^enable (\d+)$")]
    private static partial Regex EnableFlagRegex();
    
    [GeneratedRegex(@"^disable (\d+)$")]
    private static partial Regex DisableFlagRegex();
    
    [GeneratedRegex(@"^kill (\d+)$")]
    private static partial Regex KillRegex();
    
    [GeneratedRegex(@"^enemies( m\d\d_\d\d_\d\d_\d\d)?$")]
    private static partial Regex PrintEnemiesRegex();
    
    [GeneratedRegex(@"^getparam (\w+) (\d+)$")]
    private static partial Regex GetParamRegex();
}