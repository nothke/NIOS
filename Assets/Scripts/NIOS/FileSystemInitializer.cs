using System;
using NIOS.StdLib;

namespace NIOS
{
    public class FileSystemInitializer
    {
        public const string MakeType = "make_c_sharp_instance_of:";

        DirEntry root;
        DirEntry bin;

        /// <summary>
        /// Installs a program to /bin
        /// </summary>
        protected void InstallProgram(string filename, Type type)
        {
            var file = bin.GetFileEntry(filename);
            file.WriteAllText(MakeType + type.FullName);
        }

        public virtual void Install(Session s, string dirPath)
        {
            var api = s.Api;

            root = api.Directory.GetDirEntry(dirPath);

            api.Console.WriteLine("installing nios to " + root.FullName);

            // http://www.thegeekstuff.com/2010/09/linux-file-system-structure/?utm_source=tuicool
            bin = root.CreateSubdirectory("bin");

            {
                InstallProgram("sh", typeof(Shell));
                InstallProgram("echo", typeof(EchoProgram));
                InstallProgram("grep", typeof(GrepProgram));
                InstallProgram("pwd", typeof(PwdProgram));
                InstallProgram("sleep", typeof(SleepProgram));
                InstallProgram("date", typeof(DateProgram));
                InstallProgram("mkdir", typeof(MkDirProgram));
                InstallProgram("rm", typeof(RmProgram));
                InstallProgram("rmdir", typeof(RmDirProgram));
                InstallProgram("cat", typeof(CatProgram));
                InstallProgram("touch", typeof(TouchProgram));
                InstallProgram("ls", typeof(LsProgram));
                InstallProgram("brute", typeof(BruteForceAttackPasswdProgram));
            }

            var sbin = root.CreateSubdirectory("sbin");
            var etc = root.CreateSubdirectory("etc");
            {
                etc.GetFileEntry("passwd").MakeSureExists();
                etc.GetFileEntry("shadow").MakeSureExists();
            }
            var dev = root.CreateSubdirectory("dev");
            var proc = root.CreateSubdirectory("proc");
            var var = root.CreateSubdirectory("var");
            {
                var var_log = var.GetDirEntry("log");
                var var_lib = var.GetDirEntry("lib");
                var var_tmp = var.GetDirEntry("tmp");
            }
            var tmp = root.CreateSubdirectory("tmp");

            var usr = root.CreateSubdirectory("usr");
            var home = root.CreateSubdirectory("home");
            var boot = root.CreateSubdirectory("boot");

            var nios = boot.GetFileEntry("nios.img-0.0.0.1a");
            nios.WriteAllBytes(OperatingSystem.bootSectorBytes);

            var lib = root.CreateSubdirectory("lib");

            /*foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
			{
				FileEntry f = lib.CreateFile(a.GetName().Name.ToLower());
				f.WriteAllText(a.FullName);
			}*/

            var opt = root.CreateSubdirectory("opt");
            var mnt = root.CreateSubdirectory("mnt");
            var media = root.CreateSubdirectory("media");
            var srv = root.CreateSubdirectory("srv");

            api.Console.WriteLine("installation done");
        }
    }
}