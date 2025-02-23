using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ygo_to_wildwolf
{
    public partial class Form1 : Form
    {
        // Windows API声明
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        const int PROCESS_VM_READ = 0x0010;

        private Process targetProcess;

        private IntPtr processHandle;

        // 需要修改的地址（通过Cheat Engine获取）
        private IntPtr baseAddress = (IntPtr)0x12345678;  // 示例地址："Game.exe"+0x12345678
        private int[] offsets = { 0x10, 0x20, 0x30 };     // 多级指针偏移

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
