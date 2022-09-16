using PropertyHook;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameHook
{
    internal class Hook : PHook
    {
        class MemBuilder
        {
            readonly List<byte> mem = new List<byte>();
            readonly Dictionary<string, (int, int)> reserved = new Dictionary<string, (int, int)>();

            public int Offset => mem.Count;

            List<byte> ParseHexString(string hex)
            {
                List<byte> result = new List<byte>();
                foreach (string b in hex.Split(' '))
                    result.Add(byte.Parse(b, System.Globalization.NumberStyles.AllowHexSpecifier));
                return result;
            }

            public void Write(IEnumerable<byte> bytes)
            {
                mem.AddRange(bytes);
            }

            public void Write(string hex)
            {
                Write(ParseHexString(hex));
            }
            
            public void Write(byte b)
            {
                mem.Add(b);
            }

            public byte[] Finish()
            {
                if (reserved.Count > 0)
                    throw new Exception("Cannot call `MemBuilder.Finish()` when reserved names remain.");
                return mem.ToArray();
            }

            public void Reserve(string name, int size)
            {
                if (reserved.ContainsKey(name))
                    throw new Exception($"Name '{name}' is already reserved.");
                reserved[name] = (size, Offset);
                for (int i = 0; i < size; i++)
                    Write(0xFE);  // placeholder bytes
            }

            public void Fill(string name, IEnumerable<byte> value)
            {
                if (!reserved.ContainsKey(name))
                    throw new Exception($"Cannot fill unreserved name '{name}'");
                (int size, int offset) = reserved[name];
                byte[] valueArray = value.ToArray();
                if (valueArray.Length != size)
                    throw new Exception($"Number of bytes to write to reserved '{name}' does not match reserved size: {size}");
                for (int i = 0; i < valueArray.Length; i++)
                    mem[offset + i] = valueArray[i];
                reserved.Remove(name);
            }

            public void Fill(string name, string hex)
            {
                Fill(name, ParseHexString(hex));
            }
        }
        long BaseAddress { get; set; }
        PHPointer WorldChrMan { get; set; }
        PHPointer PlayerIns { get; set; }
        PHPointer PlayerModuleBase { get; set; }
        PHPointer PlayerData { get; set; }
        
        const string windowName = "ELDEN RING™";
        public Hook(int refreshInterval, int minLifetime) :
            base(refreshInterval, minLifetime, p => p.MainWindowTitle == windowName)
        {
            OnHooked += ERHook_OnHooked;
            OnUnhooked += ERHook_OnUnhooked;

            // Get player health pointer.
            WorldChrMan = RegisterRelativeAOB(Offsets.WorldChrManAoB, Offsets.RelativePtrAddressOffset, Offsets.RelativePtrInstructionSize, 0x0);
            PlayerIns = CreateChildPointer(WorldChrMan, Offsets.PlayerInsOffset);
            PlayerModuleBase = CreateChildPointer(PlayerIns, (int)Offsets.EnemyIns.ModuleBase);
            PlayerData = CreateChildPointer(PlayerModuleBase, (int)Offsets.ModuleBase.EnemyData);
        }

        void ERHook_OnHooked(object sender, PHEventArgs e)
        {
            BaseAddress = Process.MainModule.BaseAddress.ToInt64();
            //Console.WriteLine($"Elden Ring base address: {BaseAddress:X}");
        }

        void ERHook_OnUnhooked(object sender, PHEventArgs e)
        {

        }

        public bool Focused => Hooked && User32.GetForegroundProcessID() == Process.Id;

        public int PlayerHP
        {
            get => PlayerData.ReadInt32((int)Offsets.EnemyData.Hp);
            set => PlayerData.WriteInt32((int)Offsets.EnemyData.Hp, value);
        }
    }
}
