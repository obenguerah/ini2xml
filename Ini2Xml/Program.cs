using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ini2Xml
{
    /// <summary>
    /// Transform an Ini file to an Xml file
    /// </summary>
    class Program
    {

        #region Help text constant

        const string helpText
= @"Ini2Xml
Transform an Ini file to XMl File

Params
    Ini_File        Ini File to be transformed
    Xml_File        Xml File to be written 
    Group_Pattern   Regular Expression, if provided, search for sub section / group of properties within property names
                    Example, to have:
                        [section]
                        2_foo=val1
                        2_boo=val2

                    becoming:
                        <section>
                          <group name=""2"">
                            <property name=""foo"" value=""val1"" />
                            <property name=""boo"" value=""val2"" />
                          </group>
                        </section>
                     
                     use the following Group_Pattern: ""^(?<name>\d+)_(?<value>.+)""
";
        #endregion

        // Ini as a nested dictionnary
        static Dictionary<string, Dictionary<string, string>> Sections = new Dictionary<string, Dictionary<string, string>>();
        static string GroupPattern = String.Empty;

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine(helpText);
            }
            else
            {
                //---------------------- PARAMS -------------------------

                // Arg1 = IniPath
                string inifilepath = Path.GetFullPath(args[0]);

                // Arg2 = XmlPath
                string xmlfilepath = Path.GetFullPath(args[1]);

                // Arg2 = Property Group to find
                if (args.Length > 2 && args[2] != null)
                    GroupPattern = args[2];

                //--------------------------------------------------------

                string[] file_read = File.ReadAllLines(inifilepath);

                string curSection = "";

                foreach (string line in file_read)
                {
                    // --- Section detection
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        //Getting Section Name
                        curSection = line.Trim(new Char[] { '[', ']' });
                    }
                    // --- Property detection, not a comment or empty line
                    else if (!line.StartsWith(";") && !string.IsNullOrWhiteSpace(line))
                    {
                        //Get property Item & Value
                        string[] res = line.Split(new Char[] { '=' }, 2);

                        //Add property to Dictionnary
                        if (res.Length == 2 && res[1] != null)
                            AddToDic(curSection, res[0], res[1]);
                        else
                            AddToDic(curSection, res[0], string.Empty);
                    }
                }

                //write to Xml
                Dic2Xml().Save(xmlfilepath);
            }
        }

        /// <summary>
        /// Add found property to Dictionary
        /// </summary>
        static void AddToDic(string section, string fieldname, object value)
        {
            if (!Sections.ContainsKey(section))
                Sections.Add(section, new Dictionary<string, string>());

            if (!Sections[section].ContainsKey(fieldname))
                Sections[section].Add(fieldname, value == null ? string.Empty : value.ToString());
        }

        /// <summary>
        /// Transform Dictionnary to XML
        /// </summary>
        static XmlDocument Dic2Xml()
        {
            
            XmlDocument doc = new XmlDocument();
            XmlNode rootnode = doc.AppendChild(doc.CreateElement("root"));

            //Parsing Sections in dictionnary
            foreach (string section in Sections.Keys)
            {
                //New section, name as an attribute, no name if properties without section
                XmlElement sectionElement = doc.CreateElement("section");
                if (!string.IsNullOrWhiteSpace(section))
                    sectionElement.SetAttribute("name", section);

                //Set the container for properties
                XmlNode sectionNode = rootnode.AppendChild(sectionElement);
                XmlNode groupNode = sectionNode;

                string groupName = String.Empty;

                //Parsing properties in that section
                foreach (string field in Sections[section].Keys)
                {
                    string propertyName = field;

                    // Are we searching for a Group (sub section within property name)
                    if (!string.IsNullOrWhiteSpace(GroupPattern))
                    {
                        //Do we have a group of properties?
                        Match groupMatch = Regex.Match(field, GroupPattern);

                        //If yes, we create a group which is a sub section
                        if (groupMatch.Groups.Count==3)
                        {
                            propertyName = groupMatch.Groups["value"].Value;

                            //Is this a new group ?
                            if (groupName != groupMatch.Groups["name"].Value)
                            {
                                //We create the group as a subsection, and make it the container for subelements
                                groupName = groupMatch.Groups["name"].Value;

                                XmlElement groupElement = doc.CreateElement("group");
                                if (!string.IsNullOrWhiteSpace(section))
                                    groupElement.SetAttribute("name", groupName);

                                //We define this created group as the container for properties
                                groupNode = sectionNode.AppendChild(groupElement);
                            }
                        }
                        else
                        {
                            groupNode = sectionNode;
                        }
                    }

                    // Adding properties
                    XmlElement propertyElement = doc.CreateElement("property");

                    propertyElement.SetAttribute("name", propertyName);
                    if (Sections[section][field].Length > 0)
                        propertyElement.SetAttribute("value", Sections[section][field]);

                    XmlNode propertyNode = groupNode.AppendChild(propertyElement);
                }
            }

            return doc;
        }
    }

}


