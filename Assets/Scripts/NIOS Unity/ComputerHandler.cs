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
    public class ComputerHandler : NeitriBehavior
    {
        public string computerId;

        TextDisplayDevice terminal;

        Computer machine;
        bool typingEnabled;

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
            machine.ConnectDevice(new RealFileDevice(Application.dataPath + "/../VirtualDevicesData/computer_" + computerId + "_disc_1.txt"));
            machine.ConnectDevice(new RealFileDevice(Application.dataPath + "/../VirtualDevicesData/computer_" + computerId + "_disc_2.txt"));

            if (bootAtStart)
                StartCoroutine(BootCo());
        }

        IEnumerator BootCo()
        {
            yield return new WaitForSeconds(0.5f);
            BootUp();
        }

        public void BootUp()
        {
            machine.Bootup();
        }

        protected override void OnDisable()
        {
            ShutDown();
        }

        public void ShutDown()
        {
            machine.ShutDown();
        }

    }
}