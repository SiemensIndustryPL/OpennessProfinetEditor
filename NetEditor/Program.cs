using System;
using System.Windows.Forms;

namespace NetEditor
{
    static class Program
    {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static int Main()
        {
            WinApi.TimeBeginPeriod(1);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
            WinApi.TimeEndPeriod(1);
            return 0;
        }
    }
}
