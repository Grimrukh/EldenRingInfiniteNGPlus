using System.Text;
using EldenRingBase;
using EldenRingBase.Memory;
using EldenRingBase.GameHook;
using EldenRingBase.Params;
using PropertyHook;
using SoulsFormats;

namespace EldenRingInfiniteNGPlus;

/// <summary>
/// Exposes methods and event flag checks for applying "infinite" NG+ levels to the game.
///
/// Affects the NG+ SpEffectParam area scaling and boss rewards. Does NOT yet affect c0000 characters that use special
/// scaling effects.
/// </summary>
public class InfiniteNGPlusManager : GameMonitor
{
    protected override int UpdateInterval => 100;
    const int TextSearchInterval = 10000; 
    const int TextSearchMaxAttempts = 5; 
    int TextSearchAttempts { get; set; }
    long LastTextSearchTime { get; set; }
    Thread? TextSearchThread { get; set; }
    
    // Stored persistently in game file (read on each hook). Must be initialized before requests are processed.
    uint? CurrentEffectLevel { get; set; }
    
    // New level pending write to game (GameParam update, text update, persistent flag write).
    uint? RequestedEffectLevel { get; set; }

    EldenRingHook Hook { get; }
    ParamManager ParamManager { get; }
    FlagManager FlagManager { get; }

    /// <summary>
    /// User can add (flag, +/- level) pairs to this dictionary to request a level change using in-game event flags.
    ///
    /// These default flags correspond with the edited Site of Grace TalkESD that comes with the mod.
    /// </summary>
    public Dictionary<uint, int> LevelChangeFlags { get; } = new()
    {
        [18002020] = +5,
        [18002021] = +1,
        [18002022] = -1,
        [18002023] = -5,
        // Flag 18002024 resets NG+ level to 0.
    };
    
    const uint LevelResetFlag = 18002024;

    // Current NG+ level stored in 32-bit (32-flag) unsigned integer starting at this flag.
    // We need persistent flags, not 2000 flags, which seem to clear on game executable close.
    const uint CurrentLevelStorageFlag = 18001700;  // last used flag is 18001731
    
    // For reference, the new `EventTextForTalk` IDs used in Site of Grace TalkESD:
    //   [15000650] = "+5 NG Level"
    //   [15000651] = "+1 NG Level"
    //   [15000652] = "-1 NG Level"
    //   [15000653] = "-5 NG Level"
    //   [15000660] = "Check NG Level"
    //   [15000670] = "Current NG Level: 4294967295"  // found using prefix and edited in memory

    bool EditLevelText { get; }
    IntPtr LevelTextPointer { get; set; }
    
    const int LevelTextLength = 28;  // length of FMG value (see above), excludes null terminator

    public InfiniteNGPlusManager(
        EldenRingHook hook, FlagManager flagManager, ParamManager paramManager, bool editLevelText = false)
    {
        Hook = hook;
        FlagManager = flagManager;
        ParamManager = paramManager;
        
        
        EditLevelText = editLevelText;
        
        Hook.OnHooked += OnHooked;  // scan for NG+ level text address
        Hook.OnUnhooked += OnUnhooked;  // discard NG+ level text address
        Hook.OnGameLoaded += OnGameLoaded;  // read last NG+ level set to this game file
    }
    
    void OnHooked(object? o, EventArgs? e)
    {
        if (!EditLevelText)
            return;

        // Initial text search.
        TextSearchAttempts++;
        Logging.Info($"Attempting to find NG+ level text in memory ({TextSearchAttempts} / {TextSearchMaxAttempts})...");
        LastTextSearchTime = 0;  // second attempt will start right away
        // Wait 5 seconds before searching for text pointer to let FMGs load.
        TextSearchThread = new Thread(() => FindTextPointer(5000)) { IsBackground = true };
        TextSearchThread.Start();
    }

    void OnUnhooked(object? o, EventArgs? e)
    {
        LevelTextPointer = IntPtr.Zero;
        TextSearchAttempts = 0;
    }

    void OnGameLoaded(object? o, EventArgs? e)
    {
        // Read last NG+ level set to this game file.
        if (!ReadLevelFromFlags())
            return;
        
        Logging.Info($"Read current NG+ level from game: {CurrentEffectLevel}");
        // Write NG+ level text (if found). If text pointer is found later, it will update then.
        if (CurrentEffectLevel != null) 
            UpdateText(CurrentEffectLevel.Value);
    }

    #region Public Methods

    /// <summary>
    /// Increase (if positive) or decrease (if negative) current NG+ level.
    /// </summary>
    /// <param name="levelChange"></param>
    /// <returns></returns>
    public void RequestEffectLevelChange(int levelChange)
    {
        if (levelChange == 0)
            return;
        if (!CurrentEffectLevel.HasValue)
            return;
        
        // Modify `LastSetEffectLevel` by `levelChange`, keeping it in `uint` range.
        long newLevel = CurrentEffectLevel.Value + levelChange;
        if (newLevel > uint.MaxValue)
            newLevel = uint.MaxValue;
        else if (newLevel < 0)
            newLevel = 0;
        
        RequestEffectLevel((uint)newLevel);
    }

    /// <summary>
    /// Modify `RequestedEffectLevel` to trigger an edit in next `OnUpdate()`.
    /// </summary>
    /// <param name="level"></param>
    /// <returns></returns>
    public void RequestEffectLevel(uint level)
    {
        RequestedEffectLevel = level;
        Logging.Info($"NG+ level requested: {RequestedEffectLevel}");
    }
    
    public uint InternalNewGamePlusLevel
    {
        get => Hook.NewGamePlusLevel;
        set => Hook.NewGamePlusLevel = value;
    }

    #endregion

    protected override bool OnUpdate(long updateTime, long gameLoadedTime)
    {
        if (!Hook.Loaded)
        {
            // Can't update until game is loaded. (Yes, Params are loaded anyway, but it's pointless to update them.)
            return false;
        }

        if (!CurrentEffectLevel.HasValue)
        {
            if (!ReadLevelFromFlags())
                return false;  // failed to read level from flags; requests not processed
        }
        
        CheckFlagRequests();

        // Check if we should look for text pointer again.
        if (ShouldLookForTextPointer(updateTime))
        {
            TextSearchAttempts++;
            Logging.Info($"Attempting to find NG+ level text in memory ({TextSearchAttempts} / {TextSearchMaxAttempts})...");
            LastTextSearchTime = updateTime;
            TextSearchThread = new Thread(() => FindTextPointer(0)) { IsBackground = true };
            TextSearchThread.Start();
        }
        
        if (RequestedEffectLevel == null)
            return true;  // no request to process
        
        UpdateParams(RequestedEffectLevel.Value);
        Logging.Info($"NG+ level updated: {RequestedEffectLevel.Value}");

        UpdateText(RequestedEffectLevel.Value);

        // Record and clear request after it's been processed.
        CurrentEffectLevel = RequestedEffectLevel.Value;
        WriteLevelToFlags();  // store mod state in game file
        RequestedEffectLevel = null;
        return true;
    }

    /// <summary>
    /// Check for level change requests from in-game flags.
    /// If multiple delta flags are enabled at once, all the deltas will be applied in sequence.
    /// </summary>
    void CheckFlagRequests()
    {
        if (FlagManager.IsEventFlag(LevelResetFlag))
        {
            Logging.Info($"Flag {LevelResetFlag} enabled. Requesting NG+ level reset to 0.");
            RequestEffectLevel(0);
            FlagManager.Disable(LevelResetFlag);
            return;
        }
        
        foreach ((uint flag, int levelChange) in LevelChangeFlags)
        {
            if (FlagManager.IsEventFlag(flag))
            {
                Logging.Info($"Flag {flag} enabled. Requesting NG+ level change: {levelChange}");
                RequestEffectLevelChange(levelChange);
                FlagManager.Disable(flag);
            }
        }
    }

    /// <summary>
    /// Update scaling in SpEffectParam from `ngPlusLevel`.
    /// </summary>
    void UpdateParams(uint ngPlusLevel)
    {
        // Update SpEffectParam area-based NG+ scaling rows.
        // At level 0, these will all be set to 1f (i.e. REMOVING the default scaling) to simulate NG+0
        // when NG+ is actually activated in the engine.
        ParamInMemory? spEffectParam = ParamManager.GetParam(ParamType.SpEffectParam);
        if (spEffectParam == null)
        {
            Logging.Error("Failed to read SpEffectParam from ELDEN RING game memory.");
            return;
        }

        if (!spEffectParam.ContainsRowID(7400))
        {
            // Seeing this error sometimes.
            Logging.Error("SpEffectParam[7400] not found. Cannot apply NG+ scaling. " +
                          $"SpEffectParam has {spEffectParam.Rows.Count} rows.");
            return;
        }
        foreach ((int rowID, Dictionary<string, float> fieldValues) in ScalingValues.AreaInitialScaling)
        {
            foreach ((string fieldName, float ng1Scaling) in fieldValues)
            {
                float scaledValue = ScalingValues.CalculateStackedScaling(ng1Scaling, fieldName, ngPlusLevel);
                spEffectParam.FastSet(rowID, fieldName, scaledValue);
            }
        }
        
        if (spEffectParam.ContainsRowID(20007400))
        {
            // DLC scaling rows are present. Update them all.
            foreach ((int rowID, Dictionary<string, float> fieldValues) in ScalingValues.DLCAreaInitialScaling)
            {
                foreach ((string fieldName, float ng1Scaling) in fieldValues)
                {
                    float scaledValue = ScalingValues.CalculateStackedScaling(ng1Scaling, fieldName, ngPlusLevel);
                    spEffectParam.FastSet(rowID, fieldName, scaledValue);
                }
            }
        }

        // Update internal NG+ level to either 0 (NG+0) or 1 (all NG+X).
        // We simulate ALL NG+ levels, including 2-7, in internal NG+1.
        InternalNewGamePlusLevel = ngPlusLevel >= 1 ? (uint)1 : 0;

        // Update GameAreaParam boss rune rewards.
        ParamInMemory? gameAreaParam = ParamManager.GetParam(ParamType.GameAreaParam);
        if (gameAreaParam == null)
        {
            Logging.Error("Failed to read GameAreaParam from ELDEN RING game memory.");
            return;
        }
        
        foreach ((long bossRowID, float _) in ScalingValues.BossNgRuneScaling)
            UpdateBossReward(gameAreaParam, (int)bossRowID, ngPlusLevel);

        if (gameAreaParam.ContainsRowID(20000800))
        {
            // DLC boss rows are present. Update them all.
            foreach ((long bossRowID, float _) in ScalingValues.DLCBossNgRuneScaling)
                UpdateBossReward(gameAreaParam, (int)bossRowID, ngPlusLevel);
        }
    }

    bool ShouldLookForTextPointer(long updateTime)
    {
        return EditLevelText
               && LevelTextPointer == IntPtr.Zero
               && TextSearchThread is not { IsAlive: true }
               && updateTime - LastTextSearchTime > TextSearchInterval
               && TextSearchAttempts < TextSearchMaxAttempts;
    }

    static void UpdateBossReward(ParamInMemory gameAreaParam, int bossRowID, long ngPlusLevel)
    {
        // NOTE: NG+1 boss reward scaling seems to be 0.0125 * x^2, where x is the base reward.
        int defaultBonusSoul = (int)ScalingValues.DefaultBossRewards[bossRowID];

        uint bonusSoul;
        if (ngPlusLevel is 0 or 1)
        {
            // We don't touch the reward at NG+0 or NG+1, as NG+1 scaling (or lack thereof) will change the value
            // appropriately already, and we can't toggle it.
            bonusSoul = (uint)defaultBonusSoul;
        }
        else
        {
            float additionalScaling = ScalingValues.CalculateAdditionalBossRewardScaling(ngPlusLevel);
                
            // NOTE: The internal NG+1 scaling will use `bonusSoul` and `defaultBonusSoul` as follows:
            // `reward = ng1Scaling * bonusSoul * (bonusSoul / defaultBonusSoul)`
            // We want `reward = ng1Scaling * additionalScaling * defaultBonusSoul`.
            // Solving for `bonusSoul`, we get:
            // `bonusSoul = sqrt(additionalScaling) * defaultBonusSoul`
            // However, since `bonusSoul` is `uint`, we can't get the precision required to truly bake the
            // additional scaling into it. So, despite all of that, we just apply the additional scaling as normal
            // and let the player get a few more Runes than they would in 'natural' NG+2 to NG+7.
            bonusSoul = (uint)(additionalScaling * defaultBonusSoul);
        }
            
        gameAreaParam.FastSet(bossRowID, "bonusSoul_single", bonusSoul);
        gameAreaParam.FastSet(bossRowID, "bonusSoul_multi", bonusSoul);

        // if (bossRowID == 18000850) // Soldier of Godrick
        //     Logging.Debug($"Soldier of Godrick 'bonusSoul': {bonusSoul}");
    }

    void UpdateText(uint level)
    {
        if (LevelTextPointer == IntPtr.Zero || LevelTextLength <= 0)
            return;  // cannot update text (NOTE: might still be searching memory for text)
        
        string levelText = $"Current NG Level: {level}";
        if (levelText.Length > LevelTextLength)
        {
            Logging.Warning($"NG+ level text is too long: {levelText}. Cutting down to {LevelTextLength} characters.");
            levelText = levelText[..LevelTextLength];
        }
        else if (levelText.Length < LevelTextLength)
        {
            // Pad with early null bytes.
            levelText += new string('\0', LevelTextLength - levelText.Length);
        }
        
        // Add final expected terminator null byte.
        levelText += "\0";
        byte[] levelTextBytes = Encoding.Unicode.GetBytes(levelText);
        // Validate final length.
        if (levelTextBytes.Length != LevelTextLength * 2 + 2)  // account for added terminator
        {
            Logging.Error($"Final NG+ level text length is incorrect: {levelTextBytes.Length}, should be {LevelTextLength * 2 + 2}");
            return;
        }
        // Write bytes to found text address.
        Kernel32.WriteBytes(Hook.Process.Handle, LevelTextPointer, levelTextBytes);
        Logging.Info($"Wrote NG+ level text to game text data: {levelText}");
    }

    void FindTextPointer(int delay)
    {
        if (delay > 0)
            Thread.Sleep(delay);
        
        // We search from 0x7FF3... up to the MainModule.
        IntPtr startAddr = (IntPtr)0x7FF300000000;
        if (Hook.MainModuleBaseAddress < startAddr)
        {
            Logging.Warning("Process main module base address lower than expected. NG+ text update will fail.");
            return;
        }
        ulong regionSize = (ulong)((long)Hook.MainModuleBaseAddress - startAddr);  // guaranteed cast by above check
        // Convert string 'Current NG Level:' to a Unicode byte array.
        byte[] textAOB = Encoding.Unicode.GetBytes("Current NG Level: ");
        Logging.Info("Searching for 'Current NG Level: ' string in game memory (may take ~10 seconds)...");
        IntPtr levelTextPointer = AOBChunkScanner.SearchMemory(Hook.Process, startAddr, regionSize, textAOB);
        if (levelTextPointer != IntPtr.Zero)
        {
            LevelTextPointer = levelTextPointer;
            // We can't check the length dynamically as null bytes are used to pad lower number values.
            Logging.Info($"--> Success. NG+ Level text will be updated in-game with max length {LevelTextLength}.");

            if (CurrentEffectLevel.HasValue)
            {
                // Update text now to current level set.
                UpdateText(CurrentEffectLevel.Value);   
            }
            
            return;
        }
        Logging.Error("--> Failed to find NG+ level text in game memory. NG+ level will not be displayed in-game.");
    }

    void WriteLevelToFlags()
    {
        if (CurrentEffectLevel.HasValue)
        {
            FlagManager.WriteUInt32AtFlag(CurrentLevelStorageFlag, CurrentEffectLevel.Value);
            Logging.Info($"Wrote current NG+ level {CurrentEffectLevel} to game file.");
        }
    }

    bool ReadLevelFromFlags()
    {
        CurrentEffectLevel = FlagManager.ReadUInt32AtFlag(CurrentLevelStorageFlag);
        return CurrentEffectLevel != null;
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