using System.Diagnostics;
using System.Runtime.InteropServices;
using Timer = System.Threading.Timer;

namespace ygo_to_wildwolf
{
    public partial class Form1 : Form
    {
        // DLL导入声明
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize,
            out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool IsWow64Process(IntPtr processHandle, out bool wow64Process);

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int dwSize,
            out int lpNumberOfBytesRead);

        const int PROCESS_WM_READ = 0x0010;

        // ====================== 变量 ======================

        private Process TargetProcess;
        private IntPtr ProcessHandle;
        private IntPtr ModuleBaseAddress;

        private string TargetProcessName;
        private int[] OffsetAddress;
        private string ModuleName;


        private Timer updateTimer;

        // ====================== 常量 ======================

        private const string TARGET_NAME_YGO = "ygopro";
        private const string TARGET_MODULE_NAME_YGO = "ygopro.exe";
        private readonly int[] OFFSET_ADDRESS_YGO = { 0x688384, 0x0E54 };

        private const string TARGET_NAME_YGO2 = "ygopro2";
        private const string TARGET_MODULE_NAME_YGO2 = "UnityPlayer.dll";
        private readonly int[] OFFSET_ADDRESS_YGO2 = { 0x01AA0D40, 0x38, 0x40, 0x30, 0x18, 0x28, 0x4A8, 0x230 };

        private const string TARGET_NAME_MD = "masterduel";
        private const string TARGET_MODULE_NAME_MD = "GameAssembly.dll";
        private readonly int[] OFFSET_ADDRESS_MD = { 0x02E3D790, 0xB8, 0x0, 0x130, 0x40, 0x168, 0x18, 0x8c };



        // ====================== 初始化 ======================
        public Form1()
        {
            InitializeComponent();
        }

        // ====================== UI内容 ======================
        private void button1_Click(object sender, EventArgs e)
        {
            Initialize();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboBox1.SelectedIndex)
            {
                case 0:
                    TargetProcessName = TARGET_NAME_YGO;
                    OffsetAddress = OFFSET_ADDRESS_YGO;
                    ModuleName = TARGET_MODULE_NAME_YGO;
                    break;
                case 1:
                    TargetProcessName = TARGET_NAME_YGO2;
                    OffsetAddress = OFFSET_ADDRESS_YGO2;
                    ModuleName = TARGET_MODULE_NAME_YGO2;
                    break;
                case 2:
                    TargetProcessName = TARGET_NAME_MD;
                    OffsetAddress = OFFSET_ADDRESS_MD;
                    ModuleName = TARGET_MODULE_NAME_MD;
                    break;
            }
        }

        // 新增模块查找方法
        private IntPtr GetModuleBase(string moduleName)
        {
            foreach (ProcessModule module in TargetProcess.Modules)
            {
                if (module.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    return module.BaseAddress;
                }
            }
            throw new Exception($"未找到模块: {moduleName}");
        }

        public void Initialize()
        {
            // 获取ygopro进程
            Process[] processes = Process.GetProcessesByName(TargetProcessName);
            if (processes.Length == 0)
            {
                Console.WriteLine($"未找到{TargetProcessName}进程");
                return;
            }

            TargetProcess = processes[0];

            // 获取UnityPlayer.dll基址
            ModuleBaseAddress = GetModuleBase(ModuleName);
            Console.WriteLine($"模组基址: 0x{ModuleBaseAddress.ToInt64():X}");


            ProcessHandle = OpenProcess(PROCESS_WM_READ, false, TargetProcess.Id);

            if (ProcessHandle == IntPtr.Zero)
            {
                Console.WriteLine("无法打开进程，请以管理员权限运行");
                return;
            }

            // 创建定时器（每500ms更新一次）
            updateTimer = new Timer(UpdateHealth, null, 0, 500);
        }

        private void UpdateHealth(object state)
        {
            try
            {
                IntPtr addressValue = -1;
                IntPtr pointerAddress = ModuleBaseAddress;
                foreach (var nextOffset in OffsetAddress)
                {
                    // 计算指针地址：ygopro.exe + 688384
                    pointerAddress = IntPtr.Add(pointerAddress, nextOffset);
                    Console.WriteLine($"当前地址：0x{pointerAddress:X}");
                    if (nextOffset == OffsetAddress.Last())
                    {
                        break;
                    }
                    addressValue = ReadPointer(pointerAddress);
                    pointerAddress = addressValue;
                }

                Console.WriteLine($"读取地址：0x{pointerAddress:X}");
                int health = ReadMemoryInt32(pointerAddress);
                Console.WriteLine($"当前血量：{health}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取失败：{ex.Message}");
            }
        }

        private int ReadMemoryInt32(IntPtr address)
        {
            byte[] buffer = new byte[4];
            if (ReadProcessMemory(ProcessHandle, address, buffer, buffer.Length, out _))
            {
                return BitConverter.ToInt32(buffer, 0);
            }
            return 0;
        }
        // 改进的指针读取（自动处理32/64位）
        private IntPtr ReadPointer(IntPtr address)
        {
            bool is64bit = Is64BitProcess(TargetProcess);
            byte[] buffer = new byte[is64bit ? 8 : 4];

            if (ReadProcessMemory(ProcessHandle, address, buffer, buffer.Length, out _))
            {
                if (is64bit)
                    return (IntPtr)BitConverter.ToInt64(buffer, 0);
                else
                    return (IntPtr)BitConverter.ToInt32(buffer, 0);
            }
            return IntPtr.Zero;
        }

        // 检测进程位数
        private bool Is64BitProcess(Process process)
        {
            if (Environment.Is64BitOperatingSystem)
            {
                bool isWow64;
                if (!IsWow64Process(process.Handle, out isWow64)) return false;
                return !isWow64;
            }
            return false;
        }
    }
}

