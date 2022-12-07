/*
    This file is part of tsv2smw.

    tsv2smw is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    tsv2smw is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with tsv2smw.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommandLine;
using System.Web;
using libc.translation;
using System.IO;

namespace TSV2SMW
{
    public static class GlobalConsts
    {
        public const string VERSION = "1.0.0-beta";
        public const string COPYRIGHT = "";//Marco Falda (marco.falda@unipd.it)";
        public const string NA = "N/D";
        //public const string IP_ADDRESS = "localhost";
        //public const string IP_ADDRESS = "172.25.0.170";
        public const string IP_ADDRESS = "172.25.0.181";
        public const string DB = "Virus";

        // position in the input TSV
        public const int GROUP = 0;
        public const int SUPER_PROPERTY = 1;
        public const int PROPERTY = 2;
        public const int TYPE = 3;
        public const int DOMAIN = 4;
        public const int NOTES = 5;
        public const int INFO = 6;
        public const int SHOW_ON_SELECT = 7;

        public const int CATEGORY = 0;
        public const int PARENT_CATEGORY = 1;

        // gneric positions for slices
        public const int FIRST_PART = 0;
        public const int SECOND_PART = 1;
        public const int THIRD_PART = 2;

        public const int ID = 0;
    }

    // newtype emulation (also as far as the runtime complexity is concerned: is equivalent to the base type)
    public struct UserId
    {
        public string Id { get; private set; }

        public UserId(string id) : this() { Id = id; }

        public override string ToString() { return Id; }
    }

    public struct SectionId: IComparable
    {
        string id;

        // comparer for SortedDictionary
        public int CompareTo(Object other)
        {
             return this.id.CompareTo(((SectionId) other).id);
        }

        public SectionId(string id1) : this() { id = id1; }

#region Equality definitions for SectionId

        public override bool Equals(object obj)
        {
            return this.Equals((SectionId) obj);
        }

        public bool Equals(SectionId sec)
        {
            // If parameter is null, return false.
            if (Object.ReferenceEquals(sec, null))
                return false;

            // Optimization for a common success case.
            if (Object.ReferenceEquals(this, sec))
                return true;

            // If run-time types are not exactly the same, return false.
            if (this.GetType() != sec.GetType())
                return false;

            // Return true if the fields match.
            // Note that the base class is not invoked because it is
            // System.Object, which defines Equals as reference equality.
            return (id == sec.id);
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        public static bool operator ==(SectionId a, SectionId b)
        {
            return a.id == b.id;
        }

        public static bool operator !=(SectionId a, SectionId b)
        {
            return a.id != b.id;
        }

#endregion

        public override string ToString() { return id; }
    }

    public struct GroupId: IComparable
    {
        string id;

        // comparer for SortedDictionary
        public int CompareTo(Object other)
        {
             return this.id.CompareTo(((GroupId) other).id);
        }

        public GroupId(string id1) : this() { id = id1; }

#region Equality definitions for GroupId

        public override bool Equals(object obj)
        {
            return this.Equals((GroupId) obj);
        }

        public bool Equals(GroupId grp)
        {
            // If parameter is null, return false.
            if (Object.ReferenceEquals(grp, null))
                return false;

            // Optimization for a common success case.
            if (Object.ReferenceEquals(this, grp))
                return true;

            // If run-time types are not exactly the same, return false.
            if (this.GetType() != grp.GetType())
                return false;

            // Return true if the fields match.
            // Note that the base class is not invoked because it is
            // System.Object, which defines Equals as reference equality.
            return (id == grp.id);
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        public static bool operator ==(GroupId a, GroupId b)
        {
            return a.id == b.id;
        }

        public static bool operator !=(GroupId a, GroupId b)
        {
            return a.id != b.id;
        }

#endregion

        public override string ToString() { return id; }
    }

    public struct HeaderOptions {
        public bool mandatory;
        public bool normalizeName;
        public string elems;

        public HeaderOptions(bool mandatory1, bool normalizeName1, string elems1)
        {
            mandatory = mandatory1;
            normalizeName = normalizeName1;
            elems = elems1;
        }
    }

    // command-line options
    public class Options
    {
        [Option('s', "schema", Required = false, HelpText = "Process the schema file.")]
        public bool ProcessSchema { get; set; }

        [Option('r', "random-sizes", Required = false, HelpText = "Set the number of pages and sub-pages (format: p,s).")]
        public string CreateRandomSizes { get; set; }

        [Option('t', "random-TSV", Required = false, HelpText = "Create random data in TSV (default is XML).")]
        public bool CreateRandomTSVData { get; set; }

        [Option('b', "begin-ID", Required = false, HelpText = "Set the initial ID.")]
        public int StartID { get; set; }

        [Option('i', "input", Required = true, HelpText = "The input schema or data TSV file.")]
        public string InputFile { get; set; }

        [Option('l', "language", Required = false, Default = "en",  HelpText = "The language of the interface (available: en, it).")]
        public String Language { get; set; }

        [Option('o', "output", Required = false, HelpText = "The output XML file.")]
        public string OutputFile { get; set; }

        [Option('w', "wiki", Required = true, HelpText = "The name of the wiki.")]
        public string WikiName { get; set; }

        [Option('c', "cat-name", Required = false, HelpText = "The name of the main category.")]
        public string CatName { get; set; }

        [Option('u', "users-file", Required = false, HelpText = "The TSV file with the user IDs.")]
        public string UsersFile { get; set; }

        [Option('f', "ft-name", Required = false, HelpText = "The name of the main form and template.")]
        public string TFName { get; set; }
    }

    public enum Language
    {
        [Description("Neutral")]
        Neutral,
        [Description("it")]
        Italiano,
        [Description("en")]
        English
    }

    public enum InputType
    {
        [Description("Page")]
        PAGE,
        [Description("Text")]
        TEXT,
        [Description("List")]
        LIST,
        [Description("Number")] // my extension
        NUMBER,
        [Description("Vector")] // my extension
        VECTOR,
        [Description("File")]
        FILE,
        [Description("Hierarchy")]
        TREE,
        [Description("Date")]
        DATE,
        [Description("Boolean")]
        BOOL,
        [Description("Tokens")]
        TOKENS,
        [Description("Regex")]
        REGEXP,
        [Description("Geographic coordinates")]
        COORDS,
        [Description("External identifier")]
        EXTERNAL,
        [Description("Option")]
        OPTION,
        [Description("Repeated")]
        REPEATED,
        [Description("Subpage")]
        SUBPAGE,
        [Description("Record")] // TODO: needs [[Has fields::A;B;C]]
        RECORD,
        [Description("Quantity")]
        QUANTITY,
        [Description("URL")]
        URL,
        [Description("Email")]
        EMAIL,
        [Description("Telephone number")]
        TELEPHONE,
        [Description("Temperature")]
        TEMPERATURE,
        [Description("Constant")]
        LITERAL,
        [Description("Nexus")]
        NEXUS
    }

    public enum OptionType {
        [Description("Mandatory")]
        MANDATORY,
        [Description("Exclusive")] // radio or checkbuttons
        EXCLUSIVE,
        [Description("Identifier")] // not exported, possibly encrypted
        IDENTIFIER,
        [Description("Multiple")]
        MULTIPLE,
        [Description("Subpages")]
        SUBPAGES,
        [Description("Extended")] // text areas
        TEXTAREA,
        [Description("List")]
        LIST,
        [Description("Computed")]
        COMPUTED,
        [Description("Module")] // Scribunto
        MODULE,
        [Description("Integer")]
        INTEGER,
        [Description("Positive")]
        POSITIVE,
        [Description("Restricted")]
        RESTRICTED,
        [Description("Defined")] // do not add NA to options
        DEFINED,
        [Description("Hidden")] // my extension
        HIDDEN,
        [Description("Vector")] // my extension
        VECTOR
    }

    /// <summary>
    /// Class <c>AttributesHelperExtension</c> is used for transforming Enums to strings and vice versa.
    /// </summary>
    public static class AttributesHelperExtension
    {
        public static T FromDescription<T>(string enumString)
        {
            var members = typeof(T).GetMembers()
                                           .Where(x => x.GetCustomAttributes(typeof(DescriptionAttribute), false).Length > 0)
                                           .Select(x =>
                                                new
                                                {
                                                    Member = x,
                                                    Attribute = x.GetCustomAttributes(typeof(DescriptionAttribute), false)[0] as DescriptionAttribute
                                                });

            foreach(var item in members) {
                if (item.Attribute.Description.Equals(enumString))
                    return (T)Enum.Parse(typeof(T), item.Member.Name);
            }

            throw new Exception("Enum member " + enumString + " was not found.");
        }
        public static string ToDescription(this Enum value)
        {
            var da = (DescriptionAttribute[])(value.GetType().GetField(value.ToString())).GetCustomAttributes(typeof(DescriptionAttribute), false);
            return da.Length > 0 ? da[0].Description : value.ToString();
        }
    }

    /// <summary>
    /// Struct <c>MainLine</c> models an input line of a TSV file.
    /// </summary>
    public struct MainLine
    {
        public GroupId grp;
        public string superProperty;
        public string prop;
        public string label;
        public InputType type;
        public string category;
        public string domain;
        public List<OptionType> options;
        public string info;
        public string showOnSelect;

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="grp1">the group of the property.</param>
        /// <param name="superProperty1">the name of the parent property.</param>
        /// <param name="property1">a name of the property.</param>
        /// <param name="type1">the type of the property.</param>
        /// <param name="domain1">the domain of the property (modalities, ranges, et c.).</param>
        /// <param name="options1">a comma-separated list of options.</param>
        /// <param name="info1">a tooltip to show near the label.</param>
        /// <param name="showOnSelect1">the property visibility depands on another field.</param>
        public MainLine(GroupId grp1, string superProperty1, string property1, string type1, string domain1, string options1, string info1, string showOnSelect1)
        {
            grp = grp1;
            superProperty = superProperty1;
            prop = Program.capitalize(Program.normalizeNames(property1));
            label = property1;
            type = AttributesHelperExtension.FromDescription<InputType>(type1);
            category = "";

            if (domain1.Contains("ategory:"))
                category = domain1.Split(":")[GlobalConsts.SECOND_PART];
            else if (domain1.Contains("Subpage:")) {
                var value1 = domain1.Split(":")[GlobalConsts.SECOND_PART];
                category = value1.Split("/")[GlobalConsts.SECOND_PART];
            }
            domain = domain1;

            options = new List<OptionType>();

            switch (type) {
                case InputType.TOKENS:
                    options.Add(OptionType.LIST);
                    break;
                case InputType.REPEATED:
                    options.Add(OptionType.MULTIPLE);
                    break;
                case InputType.SUBPAGE:
                    options.Add(OptionType.SUBPAGES);
                    break;
            }
            if (options1 != "") {
                foreach (var option in options1.Split(","))
                    options.Add(AttributesHelperExtension.FromDescription<OptionType>(option));
            }

            info = info1;
            showOnSelect = showOnSelect1;
        }
    }

    /// <summary>
    /// Class <c>LangManager</c> is used for managing the localization.
    /// </summary>
    public class LangManager {
        ILocalizer localizer;
        string langString;
        string basePath;
        Language language;

        /// <summary>
        /// The constructor of the class.
        /// </summary>
        /// <param name="basePath1">the path of the localizatin file (mainly for unit tests).</param>
        public LangManager(string basePath1 = ".")
        {
            basePath = basePath1;
            SetLanguage(Language.English);
        }

        /// <summary>
        /// Method that gets the language string (it, en).
        /// </summary>
        /// <returns>the language string.</return>
        public string GetLanguageString()
        {
            return langString;
        }

        /// <summary>
        /// Method that gets the language.
        /// </summary>
        /// <returns>the language Enum.</return>
        public Language GetLanguage()
        {
            return language;
        }

        /// <summary>
        /// Method that sets the language.
        /// </summary>
        /// <param name="lang1">the desired language Enum.</param>
        public void SetLanguage(Language lang1)
        {
            language = lang1;
            langString = (lang1 == Language.Italiano) ? "it" : "en";
            var source = new LocalizationSource(new FileInfo(basePath + "/localization.json"), LocalizationSourcePropertyCaseSensitivity.CaseInsensitive);
            localizer = new Localizer(source, "it");
        }

        /// <summary>
        /// Method that gets the localized string starting from the English one.
        /// </summary>
        /// <param name="key">the string in English.</param>
        /// <returns>the localized string.</return>
        public string Get(string key)
        {
            return localizer.Get(langString, key);
        }
    }

    public partial class Program
    {
        public static LangManager langManager;

        // <summary>
        /// A function for replacing HTML entities with the correct UTF-8 characters.
        /// <param name="text">the input text.</param>
        /// <returns>the the converted text.</returns>
        /// </summary>
        public static string convertEntities(string text)
        {
            return HttpUtility.HtmlEncode(text)
                .Replace("&#224;", "à") // (&agrave; is not a valid XML entity)
                .Replace("&#232;", "è")
                .Replace("&#233;", "é")
                .Replace("&#236;", "ì")
                .Replace("&#242;", "ò")
                .Replace("&#249;", "ù");
                //.Replace("&#39;", "'"); // ' -> ´ (it is used in wiki texts and JS, DO NOT MODIFY in ´)
        }

        // <summary>
        /// A function for capitalize the names.
        /// <param name="str">the input name.</param>
        /// <returns>the normalized name.</returns>
        /// </summary>
        public static string capitalize(string str)
        {
            if (str.Length == 0)
                return "";
            else if (str.Length == 1)
                return str.ToUpper();
            else
                return char.ToUpper(str[0]) + str.Substring(1);
        }

        // <summary>
        /// A function for normalizing the names.
        /// <param name="name">the input name.</param>
        /// <returns>the normalized name.</returns>
        /// </summary>
        public static string normalizeNames(string name)
        {
            // standard namespaces are not modified
            if (name == "" || name.StartsWith("MediaWiki:") || name.StartsWith("Widget:"))
                return name;

            /*if (name.Contains("#"))
                throw new Exception("ERROR: found a '#' symbol in name!");*/

            // also '#' should be avoided, but it is used in HTML escaping
            if (name.Contains(".") || name.Contains("?") || name.Contains("[") || name.Contains("]")
                        || name.Contains("|") || name.Contains(":") || name.Contains("<") || name.Contains(">")
                        // special chars have already been escaped, but must be still catched to prevent they are transformed back
                        || name.Contains("&lt;") || name.Contains("&gt;") || name.Contains("&#39;")
                        || name.Contains("{") || name.Contains("}") || name.Contains("%") || name.Contains("+")
                        || name.Contains("(") || name.Contains(")") || name.Contains("*") || name.Contains("/")
                        || name.Contains("'") || name.Contains("–")) {
                Console.Write($"INFO: invalid character in property name '{name}', modified in ");
                name = name.Replace(".", "·").Replace("?", "？").Replace("[", "⟮").Replace("]", "⟯")
                           .Replace("|", "❘").Replace(":", "：").Replace("<", "‹").Replace(">", "›")
                           .Replace("&lt;", "‹").Replace("&gt;", "›").Replace("&#39;", "´")
                           .Replace("{", "⟮").Replace("}", "⟯").Replace("%", "﹪").Replace("+", "＋")
                           .Replace("(", "⟮").Replace(")", "⟯").Replace("*", "＊").Replace("/", "⁄")
                           .Replace("'", "´").Replace("–", "-");
                Console.WriteLine($"'{name}'");
            }

            if (name == "")
                throw new Exception("ERROR: a normalized name is empty!");

            return name;
        }

        /* ID and NAME tokens must begin with a letter ([A-Za-z]) and may be followed by 
         * any number of letters, digits ([0-9]), hyphens ("-"), underscores ("_"), 
         * colons (":"), and periods ("."). */
        // <summary>
        /// A function for normalizing the IDs.
        /// <param name="name">the input ID.</param>
        /// <returns>the normalized ID.</returns>
        /// </summary>
        public static string normalizeIDs(string name)
        {
            // standard namespaces are not modified
            if (name == "" || name.StartsWith("MediaWiki:") || name.StartsWith("Widget:"))
                return name;

            /*if (name.Contains("#"))
                throw new Exception("ERROR: found a '#' symbol in name!");*/

            // also '#' should be avoided, but it is used in HTML escaping
            string res = "";
            foreach (char c in name) {
                if (c == ' ')
                    res += '_';
                else if (!char.IsLetterOrDigit(c) && c != '-' && c != '_' && c != ':' && c != '.')
                    res += '-';
                else
                    res += c;
            }

            if (res != name)
                Console.Write($"INFO: invalid character in property name '{name}', modified in '{res}'");

            return res;
        }

        // <summary>
        /// The entry point that parses the command-line options.
        /// <param name="args">the list with the command line arguments.</param>
        /// </summary>
        static void Main(string[] args)
        {
            var parser = new CommandLine.Parser(with => with.HelpWriter = null);
            var parserResult = parser.ParseArguments<Options>(args);
            parserResult
                .WithParsed<Options>(options => {
                    Console.WriteLine("TSV2SMW " + GlobalConsts.VERSION);
                    Console.WriteLine(GlobalConsts.COPYRIGHT);
                    Run(options);
                })
                .WithNotParsed(errs => DisplayHelp<Options>(parserResult, errs));
        }

        // <summary>
        /// Shows the command syntax and help.
        /// <param name="result">needed parameter.</param>
        /// <param name="errs">needed parameter.</param>
        /// </summary>
        static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
        {  
            var helpText = CommandLine.Text.HelpText.AutoBuild(result, h =>
            {
                h.AdditionalNewLineAfterOption = false;
                h.Heading = "TSV2SMW " + GlobalConsts.VERSION;
                h.Copyright = GlobalConsts.COPYRIGHT;
                return CommandLine.Text.HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);
            Console.WriteLine(helpText);
        }

        // <summary>
        /// The common method for running the three sub-programs.
        /// <param name="o">the structure with the command-line options.</param>
        /// </summary>
        private static void Run(Options o)
        {
            if (o.ProcessSchema) {
                processSchema(o);
            }
            else if (o.CreateRandomSizes != null || o.CreateRandomTSVData) {
                var splitted = o.CreateRandomSizes.Split(",");
                createRandomData(o, int.Parse(splitted[GlobalConsts.FIRST_PART]),
                    int.Parse(splitted[GlobalConsts.SECOND_PART]), int.Parse(splitted[GlobalConsts.THIRD_PART]),
                    o.CreateRandomTSVData);
            }
            else
                processData(o);
        }
    }
}
