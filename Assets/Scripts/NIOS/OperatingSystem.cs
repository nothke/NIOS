﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;

using NIOS.StdLib;

namespace NIOS
{
    public interface IBootSectorProgram
    {
        void StartUp(Computer machine);
    }

    public partial class OperatingSystem : IBootSectorProgram
    {
        public static readonly byte[] bootSectorBytes = Utils.ProgramContents(645, 100);
        public static readonly string bootSectorBase64 = Convert.ToBase64String(bootSectorBytes);

        public string MachineName { get; private set; }

        List<ProgramBase> programs = new List<ProgramBase>();

        DirEntry rootDirectory;

        Session session;
        Session.ApiClass api { get { return session.Api; } }

        public Computer Machine { get; private set; }

        public FileSystemInitializer fileSystemInitializer;

        void AddProgram(ProgramBase program)
        {
            programs.Add(program);
        }

        public void StartUp(Computer machine)
        {
            this.Machine = machine;

            MachineName = "vm0387";

            NewThread(null, Main).Start();
        }

        public Thread NewThread(Session session, ThreadStart start)
        {
            return Machine.CreateThread(start);
        }

        void Main()
        {
            try
            {
                SetupSystem();
            }
            catch (ThreadInterruptedException) // e
            {
                //UnityEngine.Debug.Log("Thread interrupted. If shutting down the computer it is expected.\n" + e);
            }
            catch (Exception e)
            {
                // If the session doesn't exist yet we can't even write to console, so just throw
                if (session == null)
                    throw;

                api.Console.WriteLine();
                api.Console.WriteLine(e);

                throw;
            }
        }


        public class Process
        {
            public Session Session { get; set; }
            public void Start(string filePath, params string[] args)
            {
                var file = Session.Api.File.GetFileEntry(filePath);

                if (file.Exists)
                {
                    var data = file.ReadAllText();

                    Session.argsUsedToStart = args;
                    Session.cmdLineUsedToStart = filePath + " " + args.Join(" ");

                    if (data.StartsWith(FileSystemInitializer.MakeType))
                    {
                        var typeName = data.Substring(FileSystemInitializer.MakeType.Length);

                        Type type;
                        try
                        {
                            type = Type.GetType(typeName, true);
                        }
                        catch (TypeLoadException e)
                        {
                            throw e;
                        }

                        if (type == null)
                        {
                            throw new Error("unable to find type " + typeName);
                        }

                        var instance = Activator.CreateInstance(type);

                        if (instance == null)
                            throw new Error("failed to create instance of " + type);
                        var program = (ProgramBase)instance;

                        program.Session = Session;
                        program.Main(args);
                    }
                    else
                    {
                        throw new Error("'" + filePath + "', has unexpected data, can not start");
                    }
                }
                else
                {
                    throw new Error("'" + filePath + "', does not exist");
                }
            }
        }

        public Process NewProcess(Session session)
        {
            var p = new Process();
            p.Session = session.Clone();
            p.Session.Init();
            return p;
        }


        public IEnumerable<Device> Devices { get { return devices.Where(d => d.enabled); } }
        List<Device> devices = new List<Device>();

        public class Device
        {
            public Guid guid;
            public bool enabled;
            public string name;
            public Func<Stream> OpenRead;
            public Func<Stream> OpenWrite;
        }

        void AddDevice(IDevice id)
        {
            var device = new Device();
            device.enabled = true;
            device.OpenRead = id.OpenRead;
            device.OpenWrite = id.OpenWrite;
            device.guid = id.Guid;
            int nameIndex = 0;
            do
            {
                device.name = id.DeviceType.GetName(nameIndex);
                nameIndex++;
            } while (devices.Any(d => d.name == device.name));

            devices.Add(device);
        }
        void RemoveDevice(IDevice id)
        {
            devices.RemoveAll(d => d.guid == id.Guid);
        }

        void SetupSystem()
        {
            Machine.Devices.ForEach(AddDevice);
            Machine.OnDeviceConnected += AddDevice;
            Machine.OnDeviceDisconnected += RemoveDevice;

            rootDirectory = DirEntry.MakeRoot();
            var rootFs = new RootFs(this);
            rootDirectory.fileSystem = rootFs;
            rootDirectory.ResetCreationTime();
            rootDirectory.Refresh();

            var dev = rootDirectory.CreateSubdirectory("dev");
            var devFs = new DevFs(dev, this);
            dev.fileSystem = devFs;
            dev.isMountPoint = true;
            dev.Refresh();

            var sys = rootDirectory.CreateSubdirectory("sys");
            var sysFs = new SysFs(dev, this);
            sys.fileSystem = sysFs;
            sys.isMountPoint = true;
            sys.Refresh();

            Mount("/dev/" + DeviceType.SCSIDevice.GetName(), "/", "csv");



            session = new Session();
            session.operatingSystem = this;
            session.userName = "system";

            var input = GetFileEntry("/dev/" + DeviceType.Keyboard.GetName());
            if (input.Exists)
            {
                session.stdIn = input.OpenText();
            }
            else
            {
                session.stdIn = new StreamReader(Stream.Null);
            }

            var output = GetFileEntry("/dev/" + DeviceType.Display.GetName());
            if (output.Exists)
            {
                session.stdOut = session.stdErr = new StreamWriter(output.OpenWrite()) { AutoFlush = true };
            }
            else
            {
                var stdOut = GetFileEntry("/tmp/std-out-" + Machine.clock.UtcNow.Ticks + ".txt");
                session.stdOut = new StreamWriter(stdOut.OpenWrite()) { AutoFlush = true };
                var stdErr = GetFileEntry("/tmp/std-err-" + Machine.clock.UtcNow.Ticks + ".txt");
                session.stdErr = new StreamWriter(stdErr.OpenWrite()) { AutoFlush = true };
            }

            session.currentDirectory = "/";

            api.Console.WriteLine("niOS [version 0.0.1.0a]");
            api.Console.WriteLine("(c) 2016 Neitri Industries. All rights reserved.");
            api.Console.WriteLine(Machine.clock.UtcNow);
            //.WriteLine("This software is protected by following patents US14761 NI4674765 EU41546 US145-7756 US765-577")
            //.WriteLine("Any unauthorized reproduction of this software is strictly prohibited.")
            api.Console.WriteLine();

            if (!api.File.Exists("/bin/sh"))
            {
                api.Console.WriteLine("/bin/sh not found, re/installing system");

                // If no custom initializer is provided, use the default one
                if (fileSystemInitializer != null)
                    fileSystemInitializer.Install(session, "/");
                else
                    new FileSystemInitializer().Install(this.session, "/");
            }

            if (input.Exists)
                UserInteraction();
        }

        public void Mount(string device, string target, string fileSystem = "csv")
        {
            var deviceFile = GetFileEntry(device);
            var toDir = GetDirEntry(target);

            switch (fileSystem)
            {
                case "csv":
                    toDir.fileSystem = new CsvFileSystem(toDir, deviceFile, this);
                    break;
            }
            toDir.isMountPoint = true;
            toDir.Refresh();
        }

        void UserInteraction()
        {
            while (true)
            {
                SystemSanityCheck();

                var userSession = AuthenticateNewSession();

                var shell = NewProcess(userSession);
                shell.Session = userSession;
                shell.Start("/bin/sh");
            }
        }


        void SystemSanityCheck()
        {
            if (!api.Directory.Exists("/bin")) api.Console.WriteLine("warning: /bin not found, some programs will not be available");
        }

        // https://www.digitalocean.com/community/tutorials/how-to-use-passwd-and-adduser-to-manage-passwords-on-a-linux-vps
        Session AuthenticateNewSession()
        {
            //FindOrCreateFile("/etc/passwd").WriteAllText("test");
            //var b = FindOrCreateFile("/etc/passwd").ReadAllText();

            var csv = CSV.FromLines(GetFileEntry("/etc/passwd").ReadAllLinesEmptyIfDoesntExist());

            string userName;
            enterUserName:
            this.api.Console.Write("log in as user: ");

            userName = Utils.SanitizeInput(this.api.Console.ReadLine());
            this.api.Console.WriteLine();
            if (userName.Length < 3 || !Regex.IsMatch(userName, @"^[\w\d]+$"))
            {
                this.api.Console.WriteLine("user name must be minimum of 3 characters long and must contain only characters and numbers");
                goto enterUserName;
            }

            var userExists = csv.Any(l => l[0] == userName);

            if (userExists)
            {
                enterPassword:
                this.api.Console.Write("enter password: ");
                var pass = EnterPassword();
                this.api.Console.WriteLine();
                if (csv.Any(l => l[1] == pass) == false)
                {
                    this.api.Console.WriteLine("wrong password, try again");
                    goto enterPassword;
                }
            }
            else
            {
                this.api.Console.WriteLine("user does not exist, creating new user");
                enterNewPassword:
                this.api.Console.Write("enter new password: ");
                var pass = EnterPassword();
                this.api.Console.WriteLine();
                this.api.Console.Write("enter new password again: ");
                var passAgain = EnterPassword();
                this.api.Console.WriteLine();
                if (pass != passAgain)
                {
                    this.api.Console.WriteLine("passwords do not match, try again");
                    goto enterNewPassword;
                }

                csv.Add(userName, pass);
                GetFileEntry("/etc/passwd").WriteAllLines(csv.GetLinesToSave());
            }

            var session = this.session.Clone();
            session.userName = userName;

            var api = session.Api;

            api.Console.WriteLine("logged in as user '" + api.Environment.UserName + "'");
            api.Console.WriteLine();

            return session;
        }

        string EnterPassword()
        {
            var password = api.Console.ReadPassword('*');
            return Utils.GetStringSha256Hash(password);
        }

        public const string MakeType = "make_c_sharp_instance_of:";

        /// <summary>
        /// A utility for quickly installing a program by CSharp class into /bin
        /// </summary>
        public bool TryInstallProgram(string name, Type csClass)
        {
            if (!api.Directory.Exists("/bin"))
            {
                api.Console.WriteLine("warning: attempting to install a program, but /bin not found");
                return false;
            }

            var dir = api.Directory.GetDirEntry("/bin");

            if (File.Exists(dir.FullName + name))
            {
                api.Console.WriteLine("warning: attempting to install a program, but program already exists at the same path");
                return false;
            }

            var file = dir.GetFileEntry(name);
            file.WriteAllText(MakeType + csClass.AssemblyQualifiedName);

            return true;
        }
    }
}