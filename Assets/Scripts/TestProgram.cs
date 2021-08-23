using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NIOS;

public class TestProgram : ProgramBase
{

    public override void Main(string[] arguments)
    {
        Console.WriteLine("It works?");

        if (arguments.Length > 0)
        {
            if (arguments[0] == "-h")
                Console.WriteLine("No help for you!");

        }

        //var dir = Session.Api.Directory.GetDirEntry("bin");
        Console.WriteLine(Session.Api.Environment.CurrentDirectory);
    }
}

public class InstallProgramProgram : ProgramBase
{
    public override void Main(string[] arguments)
    {
        if (arguments.Length == 1)
        {
            if (arguments[0] == "-h" || arguments[0] == "help")
            {
                Help();
                return;
            }

            arguments = arguments[0].Split(' ');

            if (arguments.Length == 2)
            {
                string name = arguments[0];
                string typeStr = arguments[1];

                System.Type type = System.Type.GetType(typeStr);

                if (type == null)
                {
                    Console.WriteLine("Type " + typeStr + " not found");
                    return;
                }

                if (Session.Api.OperatingSystem.TryInstallProgram(name, type))
                    Console.WriteLine("Successfully installed " + type + " to /bin/" + name);
                else
                    Console.WriteLine("Failed installing " + type);
            }
            else WriteWrongArguments();
        }
        else WriteWrongArguments();
    }

    void WriteWrongArguments()
    {
        Console.WriteLine("2 arguments are required: <filename> and <csharp_type>");
    }

    void Help()
    {
        Console.WriteLine("Installs executable programs from charp classes into /bin.");
        WriteWrongArguments();
    }
}

public class AddFileProgram : ProgramBase
{
    public override void Main(string[] arguments)
    {
        if (arguments.Length > 0)
        {
            arguments = arguments[0].Split(' ');

            switch (arguments[0])
            {
                case "-h": Help(); break;
                case "help": Help(); break;
                default:
                    if (arguments.Length != 2)
                    {
                        Console.WriteLine("Requires 2 arguments. See help.");
                        return;
                    }

                    if (!System.IO.File.Exists(arguments[0]))
                    {
                        Console.WriteLine("Real file at path " + arguments[1] + " doesn't exist.");
                    }
                    else
                        Console.WriteLine("Found " + arguments[1]);

                    if (File.Exists(arguments[1]))
                    {
                        Console.WriteLine("file at path " + arguments[0] + " already exists.");
                        return;
                    }
                    
                    // TODO: incomplete

                    break;
            }

            Console.WriteLine("Current dir: " + Session.Api.Environment.CurrentDirectory);

            var dir = Session.Api.Directory.GetDirEntry("bin");
        }
        else
            Console.WriteLine("Arugments expected");
    }

    void Help()
    {
        // TODO: Add this
        //Console.WriteLine("<real_filepath>                    Copies files from real life system into current folder retaining the name");
        Console.WriteLine("<real_filepath> <game_filepath>    Copies files from real life system into game disk, relative to current folder");
        Console.WriteLine("-h, help                           Help");
    }
}