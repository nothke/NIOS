using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using NIOS.StdLib;

namespace NIOS
{
    public class Computer
    {
        List<IDevice> devices = new List<IDevice>();
        List<WeakReference> threads = new List<WeakReference>();

        public string computerId;

        public bool isRunning;

        public IEnumerable<IDevice> Devices { get { return devices; } }

        public event Action<IDevice> OnDeviceConnected;
        public event Action<IDevice> OnDeviceDisconnected;

        public ISystemClock clock;

        public IDevice GetFirstDeviceByPrefix(string prefix)
        {
            foreach (var d in devices)
            {
                if (d.DeviceType.NamePrefix.Equals(prefix))
                    return d;
            }

            return null;
        }

        void Write(Action<StreamWriter> streamWriterAction)
        {
            var d = GetFirstDeviceByPrefix("display");
            if (d == null) return;
            var sr = new StreamWriter(d.OpenWrite());
            streamWriterAction.Raise(sr);
            sr.Dispose();
        }

        public void Write(string a)
        {
            Write(sr => sr.Write(a));
        }
        public void WriteLine(string a)
        {
            Write(sr => sr.WriteLine(a));
        }
        public void Clear()
        {
            Write(sr => new StdLib.Ecma48.Client(sr).EraseDisplay());
        }

        public void ConnectDevice(IDevice device)
        {
            devices.Add(device);
            OnDeviceConnected.Raise(device);
        }

        public void DisconnectDevice(IDevice device)
        {
            devices.Remove(device);
            OnDeviceDisconnected.Raise(device);
        }

        public Thread CreateThread(ThreadStart start)
        {
            var t = new Thread(start);
            t.IsBackground = true;
            t.Priority = ThreadPriority.Lowest;
            t.Name = computerId + "_#" + threads.Count;
            Utils.SetDefaultCultureInfo(t);
            threads.Add(new WeakReference(t));
            return t;
        }


        public void ShutDown()
        {
            isRunning = false;

            foreach (var w in threads)
            {
                if (w.IsAlive)
                {
                    var t = w.Target as Thread;
                    if (t != null && t.IsAlive)
                        t.Interrupt();
                }
            }
            threads.Clear();

            Clear();
        }

        public void Bootup(IDevice preferredBootDevice = null)
        {
            isRunning = true;

            // If no clock is specified before booting, create a real system clock
            if (clock == null)
                clock = new RealClock();

            CreateThread(() =>
            {
                IBootSectorProgram bootProgram = null;

                // Find a bootable drive
                if (preferredBootDevice == null)
                {
                    foreach (var d in devices)
                    {
                        if (d.DeviceType == DeviceType.SCSIDevice &&
                            d is RealFileDevice rfd)
                        {
                            preferredBootDevice = d;
                            bootProgram = rfd.bootProgram;
                            break;
                        }
                    }
                }

                if (preferredBootDevice == null)
                    WriteLine("unable to boot up, no bootable devices attached");

#if READ_BOOT_SECTOR
                string bootSector;
                using (var sr = new StreamReader(preferredBootDevice.OpenRead()))
                    bootSector = sr.ReadToEnd();

                if (bootSector == OperatingSystem.bootSectorBase64)
                    throw new Exception("Boot sector found");
#endif

                if (bootProgram != null)
                {
                    WriteLine("found operating system");
                    WriteLine("booting up");

                    for (int i = 0; i < 20; i++)
                    {
                        Write(".");
                        Thread.Sleep(50);
                    }

                    Clear();

                    bootProgram.StartUp(this);
                }
                else
                    WriteLine("no operating system found");

            }).Start();
        }
    }
}