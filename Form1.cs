using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ygo_to_wildwolf
{
    public partial class Form1 : Form
    {
        // Windows API����
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        const int PROCESS_VM_READ = 0x0010;

        private Process targetProcess;

        private IntPtr processHandle;

        // ��Ҫ�޸ĵĵ�ַ��ͨ��Cheat Engine��ȡ��
        private IntPtr baseAddress = (IntPtr)0x12345678;  // ʾ����ַ��"Game.exe"+0x12345678
        private int[] offsets = { 0x10, 0x20, 0x30 };     // �༶ָ��ƫ��

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
