using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace NIOS.Unity
{
    public class MyInitializer : FileSystemInitializer
    {
        public override void Install(Session s, string dirPath)
        {
            base.Install(s, dirPath);

            InstallProgram("installcs", typeof(InstallProgramProgram));
        }
    }

    public class ComputerHandler : NeitriBehavior
    {
        public string computerId;

        Computer machine;

        public TextDisplayDevice[] displays;
        public InputDevice input;
        public DeviceTest input2;

        public bool bootAtStart;

        protected override void OnEnable()
        {
            if (string.IsNullOrEmpty(computerId)) computerId = this.GetInstanceID().ToString();

            machine = new Computer();
            machine.computerId = computerId;

            if (input) machine.ConnectDevice(input);
            if (input2) machine.ConnectDevice(input2);
            displays.ForEach(machine.ConnectDevice);

            var disk1 = new RealFileDevice(Application.dataPath + "/../VirtualDevicesData/computer_" + computerId + "_disc_1.txt");
            disk1.bootProgram = new OperatingSystem() { fileSystemInitializer = new MyInitializer() };

            machine.ConnectDevice(disk1);
            machine.ConnectDevice(new RealFileDevice(Application.dataPath + "/../VirtualDevicesData/computer_" + computerId + "_disc_2.txt"));

            if (bootAtStart)
                StartCoroutine(BootCo());
        }

        IEnumerator BootCo()
        {
            yield return new WaitForSeconds(0.5f);
            BootUp();
        }

        [ContextMenu("Boot")]
        public void BootUp()
        {
            machine.Bootup();
        }

        protected override void OnDisable()
        {
            ShutDown();
        }

        [ContextMenu("Shutdown")]
        public void ShutDown()
        {
            machine.ShutDown();
        }

    }
}