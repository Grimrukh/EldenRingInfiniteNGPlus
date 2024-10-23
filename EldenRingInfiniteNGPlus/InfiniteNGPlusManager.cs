using EldenRingBase.Events;
using EldenRingBase.GameHook;
using EldenRingBase.Params;
using SoulsFormats;

namespace EldenRingInfiniteNGPlus;

/// <summary>
/// TODO: Bring over advanced WarBetween tech, particularly Param and Flag management.
///  - Should never need to WRITE regulation. But will READ default values from it.
///  - Maybe check the value of some field of some dummy param as a way to detect prior injection?
///  - Or, forget about it, since we're just overwriting previous values from mod-managed (and recorded) NG+ level
///    anyway.
///  - Then, write Site of Grace TalkESD (use ESDLang) for level adjustment options.
///     - These just enable flags that are picked up here.
///  - No need to improve/enable player death trigger system... But may as well retain.
/// </summary>
internal class InfiniteNGPlusManager
{
    public int CurrentEffectLevel { get; private set; } = 1;
    
    string LastLevelPath { get; }
    int RequestedEffectLevel { get; set; } = 1;

    LogFile LogFile { get; }
    EldenRingHook Hook { get; }
    Thread? MonitorThread { get; set; }
    bool StopThread { get; set; }
    ParamManager ParamManager { get; }
    FlagManager FlagManager { get; }

    // TODO: Set request flags for +1, +5, +10, etc. May as well sync with Grace menu options.
    const uint RequestFlag = 1;
    
    // TODO: I'm not using the latest FlagManager, which I rewrote at the 11th hour to NOT use ASM
    //  injection, but instead access the arrays directly!
    // TODO: That also means EldenRingBase may actually be out of date in other areas... Check commits.
    
    Dictionary<long, uint> DefaultBossRewards { get; }

    /// <summary>
    /// TODO: Side-load this, and pass in game directory.
    /// </summary>
    /// <param name="gameDirectory"></param>
    public InfiniteNGPlusManager(string gameDirectory)
    {
        LogFile = new LogFile(Path.Combine(gameDirectory, "InfiniteNGPlus.log"));
        LastLevelPath = Path.Combine(gameDirectory, "InfiniteNGPlusLevel.txt");
        
        Hook = new EldenRingHook(5000, 5000);
        MonitorThread = new Thread(RunMonitorThread);

        FlagManager = new FlagManager(Hook);
        
        List<PARAMDEF> paramdefs = ParamReader.GetParamdefs();
        ParamManager = new ParamManager(Hook, paramdefs);
        
        // We need to read the regulation to get some default values for NG+ scaling.
        string regulationPath = Path.Combine(gameDirectory, "regulation.bin");
        BND4 gameParamBnd = SFUtil.DecryptERRegulation(regulationPath);
        PARAM gameAreaParam = ParamReader.ReadParamType(gameParamBnd, "GameAreaParam");
        DefaultBossRewards = new Dictionary<long, uint>();
        // Store default reward values that will be multiplied in memory.
        foreach (PARAM.Row row in gameAreaParam.Rows)
        {
            if (!ScalingValues.BossNgRuneScaling.ContainsKey(row.ID))
                continue;  // not an affected boss
            DefaultBossRewards[row.ID] = (uint)row["bonusSoul_single"].Value;
        }
        
        //PrintVanillaParams();
        
        // TODO: Auto `Start()` here for sideload? How does sideload entry point work?
    }

    #region Public Methods
    public void Start()
    {
        Hook.Start();
        Console.WriteLine("\nSearching for running ELDEN RING process...");
        while (!Hook.Hooked)
            Thread.Sleep(100);
        Console.WriteLine("--> Connected to ELDEN RING successfully.");

        RequestedEffectLevel = ReadLevelTextFile();
        Console.WriteLine($"# REQUESTED INITIAL NG+ LEVEL: {RequestedEffectLevel}");

        MonitorThread = new Thread(RunMonitorThread);
        MonitorThread.Start();
    }

    public void Stop()
    {
        StopThread = true;
        MonitorThread?.Join();
        MonitorThread = null;
    }

    /// <summary>
    /// Increase (if positive) or decrease (if negative) current NG+ level.
    /// </summary>
    /// <param name="levelChange"></param>
    /// <returns></returns>
    public int RequestNewGamePlusLevelChange(int levelChange)
    {
        if (levelChange == 0)
            return CurrentEffectLevel;

        return RequestNewEffectLevel(CurrentEffectLevel + levelChange);
    }

    /// <summary>
    /// Modify `CurrentEffectLevel` and edit GameParams in memory.
    /// </summary>
    /// <param name="level"></param>
    /// <returns></returns>
    public int RequestNewEffectLevel(int level)
    {
        if (level < 0)
        {
            LogFile.Log("NG+ level request clamped to minimum (0).");
            level = 0;
        }

        if (level == CurrentEffectLevel)
        {
            LogFile.Log($"# NG+ level is already set to {level}.");
            return level;
        }

        RequestedEffectLevel = level;
        LogFile.Log($"NG+ level requested: {RequestedEffectLevel}");

        // Level has not been set to memory yet, but request can be documented.
            
        return level;
    }

    #endregion

    void RunMonitorThread()
    {
        // TODO: This thread will now just need to monitor event flags triggered at Grace.

        while (true)
        {
            if (StopThread)
                return;

            if (!Hook.Hooked)
            {
                Console.WriteLine("\nLost game connection. Waiting to reconnect...");
                while (!Hook.Hooked)
                    Thread.Sleep(100);
                Console.WriteLine("--> Reconnected to ELDEN RING successfully.");
            }

            if (RequestedEffectLevel == CurrentEffectLevel)
            {
                Thread.Sleep(100);
                continue;
            }
            CurrentEffectLevel = RequestedEffectLevel;
            UpdateParams(CurrentEffectLevel);
            LogFile.Log($"NG+ level updated: {CurrentEffectLevel}"); 
            WriteLevelTextFile();  // store mod state
            
            Thread.Sleep(100);
        }
    }

    /// <summary>
    /// Update scaling in SpEffectParam from `CurrentEffectLevel`.
    /// </summary>
    void UpdateParams(int level)
    {
        // Update SpEffectParam area-based NG+ scaling rows.
        // At level 0, these will all be set to 1f (i.e. REMOVING the default scaling) to simulate NG+0
        // when NG+ is actually activated in the engine.
        ParamInMemory spEffectParam = ParamManager.GetParam(ParamType.SpEffectParam);
        foreach ((int rowID, Dictionary<string, float> fieldValues) in ScalingValues.AreaInitialScaling)
        {
            foreach ((string fieldName, float ng1Scaling) in fieldValues)
            {
                float scaledValue = ScalingValues.CalculateStackedScaling(ng1Scaling, fieldName, level);
                spEffectParam.FastSet(rowID, fieldName, scaledValue);
            }
        }

        // Update GameAreaParam boss rune rewards.
        ParamInMemory gameAreaParam = ParamManager.GetParam(ParamType.GameAreaParam);
        foreach ((long bossRowID, float runeScaling) in ScalingValues.BossNgRuneScaling)
        {
            int defaultRunes = (int)DefaultBossRewards[bossRowID];
            float rewardScaling = ScalingValues.CalculateStackedScaling(bossRowID, "haveSoulRate", level);
            uint scaledReward = (uint)(defaultRunes * rewardScaling);
            
            gameAreaParam.FastSet((int)bossRowID, "bonusSoul_single", scaledReward);
            gameAreaParam.FastSet((int)bossRowID, "bonusSoul_multi", scaledReward);

            if (bossRowID == 18000850)  // Soldier of Godrick
                LogFile.LogDebug($"Soldier of Godrick reward: {scaledReward}");
        }
    }

    void WriteLevelTextFile()
    {
        File.WriteAllText(LastLevelPath, $"{CurrentEffectLevel}");
    }

    int ReadLevelTextFile()
    {
        return File.Exists(LastLevelPath) 
            ? int.Parse(File.ReadAllText(LastLevelPath).Trim()) 
            : 1;
    }

    void PrintVanillaParams(PARAM spEffectParam)
    {
        Console.WriteLine("Dictionary<int, Dictionary<string, float>> DefaultValues = new()\n{");
        foreach (int rowId in ScalingValues.ScalingEffectRows)
        {
            PARAM.Row? row = spEffectParam.Rows.Find(x => x.ID == rowId);
            if (row == null) continue;
            Console.WriteLine($"    [{rowId}] = new Dictionary<string, float>()\n    {{");
            foreach (string field in ScalingValues.EffectFields)
            {
                object fieldValue = row[field].Value;
                Console.WriteLine($"        [\"{field}\"] = {fieldValue}f,");
            }
            Console.WriteLine("    },");
        }
        Console.WriteLine("};");
    }
}