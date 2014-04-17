using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SetupDataParser
{
    class Program
    {
        const string F_SETUP_STRUCT = "ProjectStaticSetupStruct.h";
        const string F_HP_FACTORY_DEFAULTS = "HpFactoryDefaults.c";
        const string PROJECT_SDL = "HpLo6.sdl";

        static void Main(string[] args)
        {
            Console.WriteLine("This application will:\n\tScan ProjectStaticSetupStruct.h" +
                "\n\tGenerate a numbered list for HpFactoryDefaults.c" +
                "\n\tSet the SETUP_DATA_LAST_USED_ELEMENT Token in the HP project sdl.");
            Console.Write("Press any key to continue...");
            Console.ReadKey();

            List<SetupVariable> SetupVariables = new List<SetupVariable>();

            // Open ProjectStaticSetupStruct.h
            FileStream fs = null;

            try
            {
                if (!File.Exists(F_SETUP_STRUCT))
                {
                    Console.WriteLine("Cannot find " + F_SETUP_STRUCT + "in this directory!\nPlease run this utility in the root of your project directory.");
                }
                else
                {
                    fs = new FileStream(F_SETUP_STRUCT, FileMode.Open, FileAccess.Read);
                    using (StreamReader setupStruct = new StreamReader(fs))
                    {
                        fs = null;
                        string line = null;
                        uint location = 0x0001;
                        uint size = 0;
                        string name = null;

                        while (!setupStruct.EndOfStream)
                        {
                            size = 0;
                            name = null;
                            line = setupStruct.ReadLine().Trim();

                            if (line.StartsWith("//")) // Ignore commented lines
                                continue;
                            else if (line == "") // Ignore empty lines
                                continue;
                            else if (line.Contains("UnusedVariables[SETUP_DATA_UNUSED_ELEMENTS];")) // Means we hit the end of the setup variables
                                break;
                            else
                            {
                                string[] vals = line.Split(new Char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
                                name = vals[1];
                                switch (vals[0])
                                {
                                    case "UINT16":
                                        size = 2;
                                        break;
                                    case "UINT32":
                                        size = 4;
                                        break;
                                    default: //UINT8
                                        size = 1;
                                        break;
                                }
                                if (name.Contains('[') && name.Contains(']')) // Is an array
                                {
                                    string val = name.Substring(name.Length - 3, 1);
                                    size *= Convert.ToUInt16(val);
                                }

                                SetupVariables.Add(new SetupVariable(name, size, location));
                            }

                            location += size;
                            line = null;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught exception: \n" + e.Message);
            }
            finally
            {
                if (fs != null)
                    fs.Dispose();
            }            
        }
    }

    class SetupVariable
    {
        private uint size;
        private string name;
        private uint location;

        public string Name { get { return name; } }
        public uint Size { get { return size; } }
        public uint Location { get { return location; } }

        public SetupVariable(string name, uint size)
        {
            this.name = name;
            this.size = size;
        }

        public SetupVariable(string name, uint size, uint location)
            : this(name, size)
        {
            this.location = location;
        }
    }
}
