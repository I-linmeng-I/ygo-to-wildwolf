using System.Diagnostics;
using System.Runtime.InteropServices;
using Timer = System.Threading.Timer;

namespace ygo_to_wildwolf
{
    public partial class Form1 : Form
    {
        // DLL��������
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

        // ====================== ���� ======================

        private Process TargetProcess;
        private IntPtr ProcessHandle;
        private IntPtr ModuleBaseAddress;

        private string TargetProcessName;
        private int[] OffsetAddress;
        private string ModuleName;


        private Timer updateTimer;

        // ====================== ���� ======================

        private const string TARGET_NAME_YGO = "ygopro";
        private const string TARGET_MODULE_NAME_YGO = "ygopro.exe";
        private readonly int[] OFFSET_ADDRESS_YGO = { 0x688384, 0x0E54 };

        private const string TARGET_NAME_YGO2 = "ygopro2";
        private const string TARGET_MODULE_NAME_YGO2 = "UnityPlayer.dll";
        private readonly int[] OFFSET_ADDRESS_YGO2 = { 0x01AA0D40, 0x38, 0x40, 0x30, 0x18, 0x28, 0x4A8, 0x230 };

        private const string TARGET_NAME_MD = "masterduel";
        private const string TARGET_MODULE_NAME_MD = "GameAssembly.dll";
        private readonly int[] OFFSET_ADDRESS_MD = { 0x02E3D790, 0xB8, 0x0, 0x130, 0x40, 0x168, 0x18, 0x8c };



        // ====================== ��ʼ�� ======================
        public Form1()
        {
            InitializeComponent();
        }

        // ====================== UI���� ======================
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

        // ����ģ����ҷ���
        private IntPtr GetModuleBase(string moduleName)
        {
            foreach (ProcessModule module in TargetProcess.Modules)
            {
                if (module.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    return module.BaseAddress;
                }
            }
            throw new Exception($"δ�ҵ�ģ��: {moduleName}");
        }

        public void Initialize()
        {
            // ��ȡygopro����
            Process[] processes = Process.GetProcessesByName(TargetProcessName);
            if (processes.Length == 0)
            {
                Console.WriteLine($"δ�ҵ�{TargetProcessName}����");
                return;
            }

            TargetProcess = processes[0];

            // ��ȡUnityPlayer.dll��ַ
            ModuleBaseAddress = GetModuleBase(ModuleName);
            Console.WriteLine($"ģ���ַ: 0x{ModuleBaseAddress.ToInt64():X}");


            ProcessHandle = OpenProcess(PROCESS_WM_READ, false, TargetProcess.Id);

            if (ProcessHandle == IntPtr.Zero)
            {
                Console.WriteLine("�޷��򿪽��̣����Թ���ԱȨ������");
                return;
            }

            // ������ʱ����ÿ500ms����һ�Σ�
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
                    // ����ָ���ַ��ygopro.exe + 688384
                    pointerAddress = IntPtr.Add(pointerAddress, nextOffset);
                    Console.WriteLine($"��ǰ��ַ��0x{pointerAddress:X}");
                    if (nextOffset == OffsetAddress.Last())
                    {
                        break;
                    }
                    addressValue = ReadPointer(pointerAddress);
                    pointerAddress = addressValue;
                }

                Console.WriteLine($"��ȡ��ַ��0x{pointerAddress:X}");
                int health = ReadMemoryInt32(pointerAddress);
                Console.WriteLine($"��ǰѪ����{health}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"��ȡʧ�ܣ�{ex.Message}");
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
        // �Ľ���ָ���ȡ���Զ�����32/64λ��
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

        // ������λ��
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

