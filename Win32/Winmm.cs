namespace CoApp.Toolkit.Win32 {
    using System.Runtime.InteropServices;

    public class Winmm {
        protected delegate void MidiCallback(int handle, int msg, int instance, int param1, int param2);

        [DllImport("winmm.dll")]
        private static extern int midiOutOpen(ref int handle, int deviceID, MidiCallback proc, int instance, int flags);

        [DllImport("winmm.dll")]
        protected static extern int midiOutShortMsg(int handle, int message);
              
    }
}