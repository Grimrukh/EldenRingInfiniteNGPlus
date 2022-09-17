using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using GameHook;
using PropertyHook;
using SoulsFormats;

namespace ERInfiniteNGPlus
{
    internal class ModManager
    {
        public int CurrentEffectLevel { get; private set; } = 1;
        int RequestedEffectLevel { get; set; } = 1;

        Hook Hook { get; set; }
        Thread MonitorThread { get; set; }
        bool StopThread { get; set; } = false;
        string GameDirectory { get; set; }
        bool PlayerDead { get; set; }
        List<PARAMDEF> Paramdefs { get; }
        PARAM SpEffectParam { get; } = null;
        PARAM GameAreaParam { get; } = null;

        /// <summary>
        /// Stores addresses of all binary copies of `SpEffectParam` in game memory (usually two).
        /// </summary>
        IntPtr[] SpEffectParamAddresses { get; set; } = null;
        IntPtr[] GameAreaParamAddresses { get; set; } = null;

        string GameRegulationPath => Path.Combine(GameDirectory, "regulation.bin");
        string LastLevelPath => Path.Combine(GameDirectory, "LAST_NG_LEVEL.cfg");

        // TODO: May not always be the correct region, but is in my experience so far.
        const long ParamMemoryRegion = 0x7FF400000000;

        static int[] EffectRows { get; } = new int[]  // 21 tiers
        {
            7400, 7410, 7420, 7430, 7440, 7450, 7460, 7470, 7480, 7490,
            7500, 7510, 7520, 7530, 7540, 7550, 7560, 7570, 7580, 7590,
            7600,
        };

        /// <summary>
        /// All the fields that change in NG+ effects.
        /// </summary>
        static string[] EffectFields { get; } = new string[]
        {
            "maxHpRate",
            "maxStaminaRate",
            "haveSoulRate",
            "physicsAttackPowerRate",
            "magicAttackPowerRate",
            "fireAttackPowerRate",
            "thunderAttackPowerRate",
            "physicsDiffenceRate",
            "magicDiffenceRate",
            "fireDiffenceRate",
            "thunderDiffenceRate",
            "staminaAttackRate",
            "registPoizonChangeRate",
            "registDiseaseChangeRate",
            "registBloodChangeRate",
            "darkDiffenceRate",
            "darkAttackPowerRate",
            "registFreezeChangeRate",
            "registSleepChangeRate",
            "registMadnessChangeRate",
        };

        Dictionary<int, uint> DefaultBossRewards { get; } = new Dictionary<int, uint>();

        void LogWithPrompt(string msg)
        {
            Console.Write(msg + "\n>> ");
        }

        public ModManager(string gameDir)
        {
            GameDirectory = gameDir;
            Paramdefs = LoadParamdefs("Defs");  // TODO: Copy to build folder.
            Hook = new Hook(5000, 5000);
            MonitorThread = new Thread(RunMonitorThread);
            
            SpEffectParam = ReadGameParam("SpEffectParam");
            if (SpEffectParam != null)
                SpEffectParam.ApplyParamdefCarefully(Paramdefs);
            GameAreaParam = ReadGameParam("GameAreaParam");
            if (GameAreaParam != null)
            {
                GameAreaParam.ApplyParamdefCarefully(Paramdefs);
                DefaultBossRewards.Clear();
                // Store default reward values that will be multiplied in memory.
                foreach (PARAM.Row row in GameAreaParam.Rows)
                    DefaultBossRewards[row.ID] = (uint)row["bonusSoul_single"].Value;
            }   

            PrintVanillaParams();
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

            ParamScan();

            MonitorThread = new Thread(RunMonitorThread);
            MonitorThread.Start();
        }

        public void Stop()
        {
            StopThread = true;
            if (MonitorThread != null)
                MonitorThread.Join();
        }

        /// <summary>
        /// Modify `EffectLevel` and re-apply/rewrite SpEffectParam.
        /// 
        /// If `isRelative == true`, `level` will be added to `EffectLevel` instead of
        /// used to replace it (or subtracted if `level` is negative).
        /// </summary>
        /// <param name="level"></param>
        /// <param name="isRelative"></param>
        /// <returns></returns>
        public int RequestNewEffectLevel(int level, bool isRelative = false)
        {
            if (level < 0 && !isRelative)
            {
                // Shouldn't be possible with player inputs anyway, since '=-X' is not a valid command.
                Console.WriteLine("ERROR: Cannot set `level` to a negative value.");
                return CurrentEffectLevel;
            }

            int oldEffectLevel = CurrentEffectLevel;
            int newEffectLevel;

            if (isRelative)
                newEffectLevel = oldEffectLevel + level;
            else
                newEffectLevel = level;

            if (newEffectLevel != oldEffectLevel)
            {
                RequestedEffectLevel = newEffectLevel;
                Console.WriteLine($"# REQUESTED NG+ LEVEL: {RequestedEffectLevel}\n");
            }   

            // Level has not been set to memory yet, but request can be documented.
            
            return newEffectLevel;
        }
        #endregion

        void ParamScan()
        {
            AOBScanner scanner = new AOBScanner(Hook.Process);
            foreach (int offsetMultiplier in Enumerable.Range(0, 0xF))
            {
                long offset = ParamMemoryRegion + (long)0x10000000 * offsetMultiplier;
                //Console.WriteLine($"Adding memory region at offset: {offset:X}");
                scanner.AddMemRegion(new IntPtr(offset), 0xFFFFFFF);
            }

            // Find SpEffectParam in memory. Try vanilla Param first, then current scaling,
            // in case the game has been previously injected into by this app.
            Console.WriteLine("# Scanning for params in memory. This could take up to 30 seconds. Please wait...");
            bool success = FindParamAddresses(SpEffectParam, "SpEffectParam", scanner, out IntPtr[] spEffectParamAddresses);
            if (!success)
            {
                UpdateParams(RequestedEffectLevel);
                CurrentEffectLevel = RequestedEffectLevel;
                success = FindParamAddresses(SpEffectParam, "SpEffectParam", scanner, out spEffectParamAddresses);
            }
            if (!success)
            {
                Console.WriteLine("ERROR: Failed to find SpEffectParam in memory. Try restarting Elden Ring and then relaunching this app.");
                return;
            }
            SpEffectParamAddresses = spEffectParamAddresses;

            // If vanilla SpEffectParam was not found above, GameAreaParam will already have been re-scaled in the attempt.
            success = FindParamAddresses(GameAreaParam, "GameAreaParam", scanner, out IntPtr[] gameAreaParamAddresses);
            if (!success)
            {
                Console.WriteLine("ERROR: Failed to find GameAreaParam in memory. Try restarting Elden Ring and then relaunching this app.");
                return;
            }
            GameAreaParamAddresses = gameAreaParamAddresses;
            Console.WriteLine("# Param scan complete.");
        }

        void RunMonitorThread()
        {
            // TODO: Run the loop below in a separate thread, and use this main
            //  thread as a console input for players to manually change NG+ level.
            //  The main thread queues up requests that will be processed by the
            //  hook loop (in addition to its automatic death detection).

            while (true)
            {
                if (StopThread)
                    return;

                if (!Hook.Hooked)
                {
                    Console.WriteLine("\nLost game connection. Waiting to reconnect...");
                    // Clear addresses.
                    SpEffectParamAddresses = null;
                    GameAreaParamAddresses = null;
                    while (!Hook.Hooked)
                        Thread.Sleep(100);
                    Console.WriteLine("--> Reconnected to ELDEN RING successfully.");

                    ParamScan();
                }

                // Monitor death.
                //if (PlayerDead && Hook.PlayerHP > 0)
                //{
                //    PlayerDead = false;
                //    LogWithPrompt("# Player is alive again.");
                //}                    
                //else if (!PlayerDead && Hook.PlayerHP == 0)
                //{
                //    // Player has just died. Increase level.
                //    PlayerDead = true;  // wait for health to go back above zero
                //    RequestNewEffectLevel(1, isRelative: true);
                //    LogWithPrompt("# Player died. Increasing level.");
                //}

                if (RequestedEffectLevel != CurrentEffectLevel)
                {
                    CurrentEffectLevel = RequestedEffectLevel;
                    UpdateParams(CurrentEffectLevel);
                    InjectParams(SpEffectParam, "SpEffectParam", SpEffectParamAddresses);  // write directly to memory                    
                    InjectParams(GameAreaParam, "GameAreaParam", GameAreaParamAddresses);  // write directly to memory                    
                    LogWithPrompt($"# NEW NG+ LEVEL: {CurrentEffectLevel}"); 
                    WriteLevelTextFile();  // store mod state
                    //WriteRegulation();  // write `regulation.bin` to game directory                    
                }
            }
        }

        /// <summary>
        /// Update scaling in SpEffectParam from `CurrentEffectLevel`.
        /// </summary>
        void UpdateParams(int level)
        {
            if (SpEffectParam == null)
            {
                Console.WriteLine("ERROR: `SpEffectParam` has not been loaded. Cannot update NG+ scaling.");

            }
            else
            {
                foreach (KeyValuePair<int, Dictionary<string, float>> rowFields in ScalingValues.AreaInitialScaling)
                {
                    PARAM.Row row = SpEffectParam.Rows.Find(x => x.ID == rowFields.Key);
                    if (row == null)
                    {
                        Console.WriteLine($"ERROR: Cannot find row {rowFields.Key} to update in `SpEffectParam`.");
                        continue;
                    }                        
                    foreach (KeyValuePair<string, float> fieldValue in rowFields.Value)
                    {
                        float scaledValue = ScalingValues.CalculateScaling(rowFields.Key, fieldValue.Key, level);
                        row[fieldValue.Key].Value = scaledValue;
                        //if (rowFields.Key == 7400)
                        //{
                        //    Console.WriteLine($"Set Tier 1 '{fieldValue.Key}' to: {scaledValue}");
                        //}
                    }
                }
            }

            // Update GameAreaParam.
            foreach (PARAM.Row row in GameAreaParam.Rows)
            {
                if (!ScalingValues.BossNGRuneScaling.ContainsKey(row.ID))
                    continue;  // eg 0 or 1
                float rewardScaling = ScalingValues.CalculateBossRewardScaling(row.ID, level);
                int scaledReward = (int)(DefaultBossRewards[row.ID] * rewardScaling);
                row["bonusSoul_single"].Value = scaledReward;
                row["bonusSoul_multi"].Value = scaledReward;

                //if (row.ID == 18000850)  // Soldier of Godrick
                //{
                //    Console.WriteLine($"Soldier of Godrick reward: {scaledReward}");
                //}
            }
        }

        /// <summary>
        /// Update SpEffectParam in memory and in game directory, based on current EffectLevel.
        /// </summary>
        void InjectParams(PARAM param, string paramName, IntPtr[] addresses)
        {
            if (param == null)
            {
                Console.WriteLine($"ERROR: `{paramName}` has not been loaded. Cannot inject into memory.");
            }
            else if (addresses == null)
            {
                Console.WriteLine($"Address of `{paramName}` is unknown. Memory not updated.");
            }
            byte[] paramData = param.Write();
            foreach (IntPtr addr in addresses)
            {
                // TODO: Check address data has param start.
                Kernel32.WriteBytes(Hook.Handle, addr, paramData);
                Console.WriteLine($"# Wrote `{paramName}` to memory at: {addr.ToInt64():X}");
            }
        }

        /// <summary>
        /// Convert to array of ints for AOBScanner.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        static int[] BytesToPattern(byte[] bytes)
        {
            int[] pattern = new int[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
                pattern[i] = bytes[i];
            return pattern;
        }

        /// <summary>
        /// Load ParamDefs from Paramdex XMLs.
        /// </summary>
        /// <returns></returns>
        static List<PARAMDEF> LoadParamdefs(string defsDirectory)
        {
            List<PARAMDEF> defs = new List<PARAMDEF>();
            foreach (string file in Directory.GetFiles(defsDirectory))
            {
                PARAMDEF paramdef = PARAMDEF.XmlDeserialize(file);
                defs.Add(paramdef);
            }
            return defs;
        }

        static bool FindParamAddresses(PARAM param, string paramName, AOBScanner scanner, out IntPtr[] addresses)
        {
            if (param == null)
            {
                Console.WriteLine($"ERROR: Cannot scan for NG+ special effect data when {paramName} is not loaded.");
                addresses = null;
                return false;
            }

            // Pack entire Param.
            int[] paramData = BytesToPattern(param.Write());

            addresses = scanner.ScanMultiple(paramData);
            if (addresses.Length == 0)
            {
                return false;
            }
            else
            {
                Console.WriteLine($"Found {addresses.Length} {paramName} addresses:");
                foreach (IntPtr addr in addresses)
                    Console.WriteLine($"    {addr.ToInt64():X}");
                return true;
            }
        }

        PARAM ReadGameParam(string name)
        {
            if (!File.Exists(GameRegulationPath))
            {
                Console.WriteLine($"ERROR: Could not find game `regulation.bin` file: {GameRegulationPath}");
                return null;
            }

            BND4 parambnd = SFUtil.DecryptERRegulation(GameRegulationPath);
            
            BinderFile paramFile = parambnd.Files.Find(x => x.Name == $@"N:\GR\data\Param\param\GameParam\{name}.param");
            if (paramFile == null)
            {
                Console.WriteLine($"ERROR: Could not find `{name}.param` in `regulation.bin` params. This is very unusual.");
                return null;
            }
            return PARAM.Read(paramFile.Bytes);
        }

        void WriteLevelTextFile()
        {
            File.WriteAllText(LastLevelPath, $"{CurrentEffectLevel}");
        }

        int ReadLevelTextFile()
        {
            if (!File.Exists(LastLevelPath))
                return 1;
            return int.Parse(File.ReadAllText(LastLevelPath).Trim());
        }

        void WriteRegulation()
        {
            if (SpEffectParam == null)
            {
                Console.WriteLine("ERROR: `SpEffectParam` has not been loaded from `regulation.bin`. Cannot write it.");
                return;
            }
            BND4 parambnd = SFUtil.DecryptERRegulation(GameRegulationPath);
            BinderFile spEffectFile = parambnd.Files.Find(x => x.Name == $@"N:\GR\data\Param\param\GameParam\SpEffectParam.param");
            if (spEffectFile == null)
            {
                Console.WriteLine("ERROR: Could not find `SpEffectParam` in game `regulation.bin`. This is very unusual. New params not written.");
                return;
            }
            spEffectFile.Bytes = SpEffectParam.Write();
            SFUtil.EncryptERRegulation(GameRegulationPath, parambnd);
        }

        void PrintVanillaParams()
        {
            Console.WriteLine("Dictionary<int, Dictionary<string, float>> DefaultValues = new()\n{");
            foreach (int rowID in EffectRows)
            {
                PARAM.Row row = SpEffectParam.Rows.Find(x => x.ID == rowID);
                Console.WriteLine($"    [{rowID}] = new Dictionary<string, float>()\n    {{");
                foreach (string field in EffectFields)
                {
                    object fieldValue = row[field].Value;
                    Console.WriteLine($"        [\"{field}\"] = {fieldValue}f,");
                }
                Console.WriteLine("    },");
            }
            Console.WriteLine("};");
        }
    }
}
