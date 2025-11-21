using System.Windows;
using PdfSharp.Fonts;

namespace GanttProgram
{
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;
            base.OnStartup(e);
        }
    }
}
