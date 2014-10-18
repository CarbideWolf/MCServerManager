using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading;

namespace MCServerManager
{
    class Program
    {
        //Version
        string version = "1.0.0";

        //Create the process to run the JRE
        static Process mcServer = new Process();

        //Create a string holding the current directory
        static string dir = Directory.GetCurrentDirectory() + "\\";

        //Split dir into an array
        static string[] splitChar = new String[] {"\\"};
        static string[] dirArray = dir.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);

        //Create a global string to hold the backup location
        static string gBackupLocation = "";

        //Initialise string variables
        static string minRAM = "1024";
        static string maxRAM = "1024";
        static string permGen = "128";

        static void Main(string[] args)
        {
            //Use dirArray to generate backup location
            dirArray[dirArray.Length - 1] = "backup";

            string backupLocation = "";

            int i = 0;

            while (i <= dirArray.Length - 1)
            {
                backupLocation = backupLocation + dirArray[i];
                backupLocation = backupLocation + "\\";

                i++;
            }

            //Set global backup location variable equal to generated backup location
            gBackupLocation = backupLocation;

            //Create string to hold inputs temporarily
            string temp = "";

            //Create threads
            Thread backupThread = new Thread(new ThreadStart(BackupThread));
            Thread commandThread = new Thread(new ThreadStart(CommandThread));

            //Welcome user
            Console.WriteLine("Welcome to the MC Server Manager by Carbide Wolf");
            Console.WriteLine("Before proceeding please ensure that the server jar file is named \"server.jar\".\n");

            //Prompt user for custom arguments and read them in
            Console.WriteLine("Please enter the minimum amount of RAM to allocate to the server.");
            Console.WriteLine("Default: 1024");
            temp = Console.ReadLine();
            if (temp != "")
            {
                minRAM = temp;
            }
            Console.WriteLine("Please enter the maximum amount of RAM for the server to use.");
            Console.WriteLine("Default: 1024");
            temp = Console.ReadLine();
            if (temp != "")
            {
                maxRAM = temp;
            }
            Console.WriteLine("Please enter the maximum size for the java permgen.");
            Console.WriteLine("Default: 128");
            temp = Console.ReadLine();
            if (temp != "")
            {
                permGen = temp;
            }

            Console.WriteLine("Starting server... \n");

            //Start the server
            StartServer();

            //Start threads
            backupThread.Start();
            commandThread.Start();

            //Tell the program not to exit until the server is stopped
            mcServer.WaitForExit();

            //Stop threads
            commandThread.Abort();
            backupThread.Abort();

            //Backup files
            Backup();
        }

        public static void StartServer()
        {
            bool started = false;
            int n = 10;

            while(started == false)
            {
                started = true;

                //Create strings to hold the jar location, java location and jvm arguments
                string javaVersion = "jre" + n;
                string javaLocation = "C:\\Program Files\\Java\\" + javaVersion + "\\bin\\javaw.exe";
                string serverArgs = String.Format("-jar server.jar -Xms{0}m -Xmx{1}m -XX:MaxPermSize={2}m", minRAM, maxRAM, permGen);

                try
                {
                    //Set the process arguments
                    mcServer.StartInfo.FileName = javaLocation;
                    mcServer.StartInfo.Arguments = serverArgs;
                    mcServer.StartInfo.UseShellExecute = false;
                    mcServer.StartInfo.RedirectStandardInput = true;

                    //Start the server
                    mcServer.Start();
                }
                catch (Win32Exception)
                {
                    started = false;
                    n--;
                }
            }
        }

        public static void Backup()
        {
            //Create new process
            Process backup = new Process();

            //Tell process what program to run and with what arguments
            backup.StartInfo.FileName = "xcopy.exe";
            backup.StartInfo.Arguments = String.Format("* \"{0}\" /e /h /y /i", gBackupLocation);

            //Start the process
            try
            {
                backup.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void BackupThread()
        {
            while(true)
            {
                //Wait an hour
                Thread.Sleep(3600000);

                //Save and shutdown the server
                mcServer.StandardInput.WriteLine("save-all");
                Thread.Sleep(3000);
                mcServer.StandardInput.WriteLine("stop");

                //Wait for server to shutdown
                Thread.Sleep(10000);

                //Backup files
                Backup();

                //Start the server again
                StartServer();
            }
        }

        public static void CommandThread()
        {
            while(true)
            {
                //Read anything typed into the console
                string command = Console.ReadLine();

                //Send it to the server's standard input
                mcServer.StandardInput.WriteLine(command);
            }
        }
    }
}
