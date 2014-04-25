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
        const string PROJECT_SDL = "HpL06.sdl";

        static void Main(string[] args)
        {
            Console.WriteLine("This application will:\n\tScan ProjectStaticSetupStruct.h" +
                "\n\tGenerate a numbered list for HpFactoryDefaults.c" +
                "\n\tSet the SETUP_DATA_LAST_USED_ELEMENT Token in the HP project sdl.");
            Console.Write("Press any key to continue...");
            Console.ReadKey();

            List<SetupVariable> SetupVariables = new List<SetupVariable>();

            ModProjectSetupStruct(SetupVariables);
            //ModFactoryDefaults(SetupVariables);
            //ModProjectSDL(TODO);
        }

        /// <summary>
        /// Parses the ProjectSetupStruct.h file to create a list of all the setup variables and their offsets.
        /// Creates an updated version of that file that includes the appropriate offsets in a comment next to the variable definition.
        /// </summary>
        /// <param name="SetupVariables">List of setup variables, their size, their name and their offset.</param>
        private static void ModProjectSetupStruct(List<SetupVariable> SetupVariables)
        {
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
                    bool done = false;
                    string fileCopy = null;
                    fs = new FileStream(F_SETUP_STRUCT, FileMode.Open, FileAccess.Read);
                    using (StreamReader setupRead = new StreamReader(fs))
                    {
                        string line = null;
                        uint offset = 0x0001;
                        uint size = 0;
                        string name = null;

                        while (!setupRead.EndOfStream)
                        {
                            fs = null;
                            size = 0;
                            name = null;
                            line = setupRead.ReadLine();

                            if (!done && line.Trim().StartsWith("//")) // Ignore commented lines
                            {
                                fileCopy += line + "\n";
                                continue;
                            }
                            else if (!done && line.Trim() == "") // Ignore empty lines
                            {
                                fileCopy += line + "\n";
                                continue;
                            }
                            else if (!done && line.Trim().Contains("UnusedVariables[SETUP_DATA_UNUSED_ELEMENTS];")) // Means we hit the end of the setup variables
                            {
                                fileCopy += line + "\n";
                                done = true;
                                continue;
                            }
                            else if (!done)
                            {
                                string[] vals = line.Trim().Split(new Char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
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

                                SetupVariables.Add(new SetupVariable(name, size, offset));

                                // Check for existing comment:
                                if (vals.Length > 2 && vals[2] != null)
                                {
                                    if (vals[2].Contains("0x"))
                                    {
                                        // Find out if there's an offset in the comment and check if it's correct already.
                                        int i = vals[2].IndexOf("0x");
                                        string offsetAsHex = string.Format("0x{0:X4}", offset);
                                        if (vals[2].Substring(i, 6) != offsetAsHex)
                                        {
                                            // Remove the original offset from the comment if it's wrong
                                            string temp = vals[2].Remove(i, 6);
                                            // Remove "//" from the original comment
                                            i = temp.IndexOf("//");
                                            temp = temp.Remove(i, 2);

                                            fileCopy += string.Format("  {0,-7} {1,-42}// 0x{2:X4}, {3}\n", vals[0], vals[1], offset, temp);
                                        }
                                        else
                                        {
                                            fileCopy += line + "\n";
                                        }
                                    }
                                    else
                                    {
                                        int i = vals[2].IndexOf("//");
                                        string temp = vals[2].Trim().Remove(i, 2); // Trim and remove "//" from original comment"
                                        fileCopy += string.Format("  {0,-7} {1,-42}// 0x{2:X4}, {3}\n", vals[0], vals[1], offset, vals[2]);
                                    }
                                }
                                else
                                {
                                    fileCopy += string.Format("  {0,-7} {1,-42}// 0x{2:X4}\n", vals[0], vals[1], offset);
                                }
                            }
                            else // This just adds the line to the file copy after we're done with modifications
                            {
                                fileCopy += line + "\n";
                            }

                            offset += size;
                            line = null;
                        }
                    }
                    // Rename the old file for backup copy
                    string backupFile = F_SETUP_STRUCT + ".bak";
                    if (!File.Exists(backupFile))
                    {
                        File.Move(F_SETUP_STRUCT, backupFile);
                    }
                    else
                    {
                        // Keep up to 2 backups (this is probably not needed but is temporary
                        string backup2 = backupFile + "2";
                        if (File.Exists(backup2))
                        {
                            File.Delete(backup2);                            
                        }
                        File.Move(backupFile, backup2);
                        File.Move(F_SETUP_STRUCT, backupFile);
                    }

                    // Write the updated line to the file:
                    fs = new FileStream(F_SETUP_STRUCT, FileMode.Create, FileAccess.Write);
                    using (StreamWriter setupWrite = new StreamWriter(fs))
                    {
                        fs = null;
                        setupWrite.Write(fileCopy);
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

        /// <summary>
        /// Modifies the SETUP_DATA_LAST_USED_ELEMENT token in the project SDL.
        /// </summary>
        /// <param name="lastUsedElement">Offset of the last member of SetupVariables</param>
        private static void ModProjectSDL(int lastUsedElement)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Parses HpFactoryDefaults.c to compare against the list created from ProjectStaticSetupStruct.h.
        /// Updates the file with the correct offsets in the comments
        /// </summary>
        /// <param name="SetupVariables">List of setup variables, their size, their name and their offset.</param>
        private static void ModFactoryDefaults(List<SetupVariable> SetupVariables)
        {
            throw new NotImplementedException();
            // Read down to static const SETUP_DATA SetupData_FactoryDefaults
            // Should be Value:3 | Offset:53 | Type:1 | Field:3 (| = column)
            // Try to keep the existing formatting approx every 8 lines there's a blank line (some variation)
            // L01 has the values in hex, not sure if everyone should follow this (probably should follow whatever is there)
            FileStream fs = null;

            try
            {
                if (!File.Exists(F_HP_FACTORY_DEFAULTS))
                {
                    Console.WriteLine("Cannot find {0}!\nPlease run this utility in the root of your project directory.", F_HP_FACTORY_DEFAULTS);
                }
                else
                {
                    bool done = false;
                    bool started = false;
                    string fileCopy = null;
                    fs = new FileStream(F_HP_FACTORY_DEFAULTS, FileMode.Open, FileAccess.Read);
                    using (StreamReader setupRead = new StreamReader(fs))
                    {
                        string line = null;
                        uint offset = 0x0001;
                        uint size = 0;
                        string name = null;

                        while (!setupRead.EndOfStream)
                        {
                            fs = null;
                            size = 0;
                            name = null;
                            line = setupRead.ReadLine();

                            // We've hit the beginning of the setup variables
                            if (line.Contains("static const SETUP_DATA SetupData_FactoryDefaults"))
                            {
                                started = true;                                
                            }
                            else if (done || !started || line.Trim().StartsWith("//") || line.Trim() == "") // Simply write the line out to the temp file
                            {
                                fileCopy += line + "\n";
                            }
                            else if (line.Trim().Contains("UINT8 UnusedVariables[SETUP_DATA_UNUSED_ELEMENTS];")) // We've hit the end
                            {
                                // TODO: Update the comment to show the unused offset
                                done = true;
                            }
                            else 
                            {
                                // TODO: Error checking to make sure everything is aligned properly 
                                    // Check the existing comments to make sure the name and expected name match
                                    // Make sure the number values match any possible arrays
                                // TODO: Add the offset value to the comment but leave the rest intact.
                                // TODO(?): Print the values in hex
                            }

                            line = null;
                        }
                    }
                    // Rename the old file for backup copy
                    string backupFile = F_HP_FACTORY_DEFAULTS + ".bak";
                    if (!File.Exists(backupFile))
                    {
                        File.Move(F_HP_FACTORY_DEFAULTS, backupFile);
                    }
                    else
                    {
                        // Keep up to 2 backups (this is probably not needed but is temporary
                        string backup2 = backupFile + "2";
                        if (File.Exists(backup2))
                        {
                            File.Delete(backup2);
                        }
                        File.Move(backupFile, backup2);
                        File.Move(F_HP_FACTORY_DEFAULTS, backupFile);
                    }

                    // Write the updated line to the file:
                    fs = new FileStream(F_HP_FACTORY_DEFAULTS, FileMode.Create, FileAccess.Write);
                    using (StreamWriter setupWrite = new StreamWriter(fs))
                    {
                        fs = null;
                        setupWrite.Write(fileCopy);
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
        private uint offset;

        public string Name { get { return name; } }
        public uint Size { get { return size; } }
        public uint Offset { get { return offset; } set { value = offset; } }

        public SetupVariable(string name, uint size)
        {
            this.name = name;
            this.size = size;
        }

        public SetupVariable(string name, uint size, uint offset)
            : this(name, size)
        {
            this.offset = offset;
        }
    }
}
