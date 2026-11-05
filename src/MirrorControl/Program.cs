using System;
using System.Windows.Forms;

namespace MirrorControl;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MirrorForm());
    }
}
