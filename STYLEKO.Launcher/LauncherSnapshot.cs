using System.Collections.Generic;

namespace Styleko.Launcher
{
    internal sealed class LauncherSnapshot
    {
        internal int Percent { get; set; }
        internal bool Ready { get; set; }
        internal string Status { get; set; }
        internal List<string> Notices { get; set; }

        internal LauncherSnapshot()
        {
            Status = "Surum kontrol ediliyor...";
            Notices = new List<string>();
        }

        internal LauncherSnapshot Clone()
        {
            return new LauncherSnapshot
            {
                Percent = Percent,
                Ready = Ready,
                Status = Status,
                Notices = new List<string>(Notices)
            };
        }
    }
}
