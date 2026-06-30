using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PLCSimulator
{
    // PLC Simulator
    // 4 x ANALOG INPUT : ADDR001 - ADDR004
    // 4 x ANALOG OUTPUT: ADDR005 - ADDR008
    // 4 x DIGITAL INPUT: ADDR009, ADDR011 - ADDR013
    // 4 x DIGITAL OUTPUT: ADDR010, ADDR014 - ADDR016
    public class PLCSimulatorManager
    {
        private Dictionary<string, double> addressValues;   // cuva trenutnu vrednost za adresu
        private Dictionary<string, string> addressTypes;    // tip adrese (AI/AO/DI/DO)
        private readonly object locker = new object();
        // volatile - moze se promjeniti iz druge niti
        private volatile bool running = true;   // zastavica koju threadovi provjeravaju
        private Thread t1;  // generisati ce analogne ulaze
        private Thread t2;  // digitalne ulaze

        private void Register(string address, string type)
        {
            addressValues.Add(address, 0);
            addressTypes.Add(address, type);
        }

        public PLCSimulatorManager()
        {
            addressValues = new Dictionary<string, double>();
            addressTypes  = new Dictionary<string, string>();

            // svako dodavanje registruje adresu i njen tip na jednom mjestu
            Register("ADDR001", "AI");
            Register("ADDR002", "AI");
            Register("ADDR003", "AI");
            Register("ADDR004", "AI");

            Register("ADDR005", "AO");
            Register("ADDR006", "AO");
            Register("ADDR007", "AO");
            Register("ADDR008", "AO");

            Register("ADDR009", "DI");
            Register("ADDR011", "DI");
            Register("ADDR012", "DI");
            Register("ADDR013", "DI");

            Register("ADDR010", "DO");
            Register("ADDR014", "DO");
            Register("ADDR015", "DO");
            Register("ADDR016", "DO");
        }

        public void StartPLCSimulator()
        {
            t1 = new Thread(GeneratingAnalogInputs) { IsBackground = true };
            // IsBackground = True - zatvori se automatski kada se yatvori aplikacija
            t1.Start();

            t2 = new Thread(GeneratingDigitalInputs) { IsBackground = true };
            t2.Start();
        }

        private void GeneratingAnalogInputs()
        {
            while (running)   // umjesto while(true) - petlja gleda zastavicu
            {
                Thread.Sleep(100);
                lock (locker) // dictionary nije thread-safe
                {
                    // oscilatorni signal [-100, 100] - pritisak, vibracije
                    addressValues["ADDR001"] = 100 * Math.Sin((double)DateTime.Now.Second / 60 * Math.PI);

                    // linearno rastuci signal [0, 98], punjenje rezervoara
                    addressValues["ADDR002"] = 100 * DateTime.Now.Second / 60;

                    // oscilatorni signal pomjeren za 90 [-50, 50] - protok
                    addressValues["ADDR003"] = 50 * Math.Cos((double)DateTime.Now.Second / 60 * Math.PI);

                    // slucajni sumni signal [0, 50] - nestabilni senzor
                    addressValues["ADDR004"] = RandomNumberBetween(0, 50);
                }
            }
        }

        private void GeneratingDigitalInputs()
        {
            while (running)   // umjesto while(true) - petlja gleda zastavicu
            {
                Thread.Sleep(1000);
                lock (locker)
                {
                    Toggle("ADDR009");  // preokreni 0->1, 1->0
                    Toggle("ADDR011");
                    Toggle("ADDR012");
                    Toggle("ADDR013");
                }
            }
        }

        private void Toggle(string addr)  
        {
            addressValues[addr] = addressValues[addr] == 0 ? 1 : 0;
        }

        public double GetValue(string address)
        {
            lock (locker)
            {
                return addressValues.ContainsKey(address) ? addressValues[address] : -1;
            }
        }

        public void SetValue(string address, double value)  // kada se rucno zada vrednost
        {
            lock (locker)
            {
                if (addressValues.ContainsKey(address))
                    addressValues[address] = value;
            }
        }

        private static double RandomNumberBetween(double minValue, double maxValue)
        {
            return minValue + new Random().NextDouble() * (maxValue - minValue);
        }

        public IEnumerable<string> GetAddressesForType(string tagType)
        {
            return addressTypes
                .Where(kv => kv.Value == tagType)   // naci tip
                .Select(kv => kv.Key);  // samo sdrese
        }

        public void Stop()
        {
            running = false;      // niti zavrse trenutrni krug i izadju
            t1?.Join(2000);       // max cekanje da nit nesto uradi(zavrsi krug)
            t2?.Join(2000);
        }
    }
}