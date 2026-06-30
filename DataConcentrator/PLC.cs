using System.Collections.Generic;
using PLCSimulator;

namespace DataConcentrator
{
    public class PLC
    {
        // static- deli se izmedju svih objekata
        private static PLCSimulatorManager instance;
        private static readonly object instanceLock = new object();

        // kreira simulator
        public static PLCSimulatorManager Instance
        {
            get
            {
                if (instance == null)   // nema simulatora
                {
                    lock (instanceLock)   
                    {
                        if (instance == null)   // dvostruka provjera - drugi thread mogao kreirati simulator dok smo cekali lock
                        {
                            instance = new PLCSimulatorManager();
                            instance.StartPLCSimulator();
                        }
                    }
                }
                return instance;
            }
        }

        public static IEnumerable<string> GetAddressesForType(string tagType)
            => Instance.GetAddressesForType(tagType);

        public static void StopSimulator()
        {
            instance?.Stop();   // ?. stiti od null ako simulator nikad nije pokrenut
        }
    }
}
