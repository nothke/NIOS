using UnityEngine;

using System;

using NIOS;
using MoonSharp.Interpreter;

public class MoonSharpProgram : ProgramBase
{
    public override void Main(string[] arguments)
    {
        if (arguments.Length == 0)
        {
            Console.WriteLine("No arguments");
            return;
        }

        //foreach (var arg in arguments)
        //Console.WriteLine(arg);

        //var args = string.Join(" ", arguments).Split(' ');
        var args = arguments[0].Split(' ');

        if (args.Length == 1)
        {
            if (args[0] == "-h" || args[0] == "help")
                Help();
            else // run immediatelly
            {
                string code = string.Join(" ", args);
                Run(code);
            }
        }
        else if (args.Length > 1)
        {
            if (args[0] == "-r" || args[0] == "run")
            {
                string path = Session.currentDirectory + args[1];

                if (File.Exists(path))
                {
                    var file = File.GetFileEntry(path);
                    string code = file.ReadAllText();
                    //Console.Write(code);
                    Debug.Log(code);
                    Run(code);
                }
                else
                {
                    Console.WriteLine("File " + path + " not found");
                }
            }
        }

        long TimeTicks()
        {
            return OperatingSystem.Machine.clock.Now.Ticks;
        }

        void Run(string str)
        {
            CoreModules modules = CoreModules.Preset_HardSandbox;
            Script script = new Script(modules);
            script.Options.DebugPrint = (s) => Console.WriteLine(s);

            script.Globals["sleep"] = (Action<int>)Thread.Sleep;
            script.Globals["clear"] = (Action)OperatingSystem.Machine.Clear;
            //script.Globals["os"] = DynValue.NewTable(script);
            //script.Globals["os.test"] = (Func<string>)(() => "Nice test you got there, would be a shame if something happened to it");
            script.Globals["os_time"] = (Func<long>)TimeTicks;

            try
            {
                script.DoString(str);

                Console.WriteLine("--- lua program finished ---");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                //throw;
            }
        }

        void Help()
        {
            Console.WriteLine("Note: Use ' (single quotes) instead of double quotes for strings!");
            Console.WriteLine("");
            Console.WriteLine("Usage:");
            Console.WriteLine("'<code>'               Executes code immediatelly");
            Console.WriteLine("-r, run                Compile and run code from file");
            Console.WriteLine("-h, help               Help");
            Console.WriteLine("");
            Console.WriteLine("Lua special functions:");
            Console.WriteLine("sleep(ms)              Make the thread sleep for number of miliseconds");
            Console.WriteLine("clear()                Clears the terminal");
            Console.WriteLine("os_time()              Current time in ticks");
        }
    }
}
