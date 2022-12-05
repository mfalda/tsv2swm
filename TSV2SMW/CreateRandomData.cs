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
using System.IO;
using System.Linq;
using System.Globalization;
using System.Diagnostics;


namespace TSV2SMW
{
    using ParamField = ValueTuple<(GroupId param, HeaderOptions options), string>;
    using SectionsDict = Dictionary<SectionId, Dictionary<GroupId, List<MainLine>>>;

    public partial class Program
    {
        static string randomText(int words)
        {
            var random = new Bogus.Randomizer();
            var lorem = new Bogus.DataSets.Lorem(locale: "it");

            return lorem.Sentence(random.Number(1, 1 + words));
        }

        static string randomFile()
        {
            var faker = new Bogus.Faker("it");
            string res;

            do {
                res = faker.System.CommonFileName();
            } while (res != convertEntities(res));

            return res;
        }

        static string randomDate()
        {
            var bogus = new Bogus.Faker();
            // TODO: English or Italian
            return bogus.Date.Past(80).ToString("yyyy/MM/dd");
            //.ToString("dd/MM/yyyy");
        }

        static int randomIntRange(int from, int to)
        {
            var random = new Bogus.Randomizer();
            return random.Int(from, to);
        }

        static double randomRange(double from, double to)
        {
            var random = new Bogus.Randomizer();
            return random.Double(from, to);
        }

        static string randomBool()
        {
            var random = new Bogus.Randomizer();

            if (random.Bool())
                return langManager.Get("Yes");
            else
                return "No";
        }

        static double roundToSignificantDigits(double d, int digits)
        {
            if(d == 0)
                return 0;

            double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1);

            return scale * Math.Round(d / scale, digits);
        }

        static (string, string) createData(string propertyName, InputType type, string domain, List<OptionType> options, Dictionary<GroupId, List<string>> categories, Bogus.Person person, int numSubPages)
        {
            var res = new List<string>();
            int iter = 1;
            string elems = "";

            if (type == InputType.OPTION && !options.Contains(OptionType.EXCLUSIVE))
                iter = randomIntRange(1, numSubPages);

            for (int i = 0; i < iter; i++) {
                switch (type) {
                    case InputType.PAGE:
                        if (categories != null && categories.Count() > 0 && domain.Contains(":")) {
                            var cat = new GroupId(domain.Split(":")[GlobalConsts.SECOND_PART]);
                            res.Add(categories[cat][randomIntRange(0, categories[cat].Count() - 1)]);
                        }
                        else
                            res.Add(randomText(1));
                        break;
                    case InputType.FILE:
                        res.Add(randomFile());
                        break;
                    case InputType.TEXT: // texts can refer to several data
                        if (propertyName.ToLower().Contains(Program.langManager.Get("Lastname")))
                            res.Add(person.LastName);
                        else if (propertyName.ToLower().Contains(Program.langManager.Get("Firstname")))
                            res.Add(person.FirstName);
                        else if (propertyName.ToLower().Contains("Email"))
                            res.Add(person.Email);
                        else if (propertyName.ToLower().Contains(Program.langManager.Get("Phone")))
                            res.Add(person.Phone);
                        else if (propertyName.ToLower().Contains(Program.langManager.Get("Address")))
                            res.Add(person.Address.Street);
                        else if (options.Contains(OptionType.TEXTAREA))
                            res.Add(randomText(25));
                        else
                            res.Add(randomText(3));
                        break;
                    case InputType.LIST:
                    case InputType.TREE:
                        if (propertyName.ToLower().EndsWith(Program.langManager.Get("Gender")))
                            res.Add(person.Gender.ToString().Substring(0, 1));
                        else if (propertyName.ToLower().Contains("lives in"))
                            res.Add(person.Address.City);
                        else {
                            if (domain.Contains(Program.langManager.Get("Category") + ":") && categories != null) {
                                var cat = new GroupId(domain.Split(":")[GlobalConsts.SECOND_PART]);
                                res.Add(categories[cat][randomIntRange(0, categories[cat].Count() - 1)]);
                            }
                            else {
                                var values = domain.Split(",");
                                res.Add(values[randomIntRange(0, values.Length - 1)]);
                            }
                        }
                        break;
                    case InputType.OPTION:
                        var values1 = domain.Split(",");
                        string option = values1[randomIntRange(0, values1.Length - 1)];
                        if (!res.Contains("no") && !res.Contains(option))
                            res.Add(option);
                        break;
                    case InputType.DATE:
                        res.Add(randomDate());
                        break;
                    case InputType.NUMBER:
                    case InputType.VECTOR:
                        if (!options.Contains(OptionType.COMPUTED)) {
                            if (propertyName.ToLower().Contains(Program.langManager.Get("Months")))
                                res.Add(((DateTime.Now.Year - person.DateOfBirth.Year) * 12).ToString());
                            else if (propertyName.Contains(Program.langManager.Get("Years")) || propertyName.Contains(Program.langManager.Get("Age")))
                                res.Add((DateTime.Now.Year - person.DateOfBirth.Year).ToString());
                            else {
                                NumberFormatInfo nfi = new NumberFormatInfo();
                                nfi.NumberDecimalSeparator = ".";

                                var constrs = domain.Split(",");
                                double min = 0.0, max = 100.0;
                                foreach (string constr in constrs) {
                                    var parts = constr.Split("=");
                                    if (parts[GlobalConsts.FIRST_PART] == "min")
                                        double.TryParse(parts[GlobalConsts.SECOND_PART], out min);
                                    else if (parts[GlobalConsts.FIRST_PART] == "max")
                                        double.TryParse(parts[GlobalConsts.SECOND_PART], out max);
                                    else if (parts[GlobalConsts.FIRST_PART] == "elems")
                                        elems = parts[GlobalConsts.SECOND_PART];
                                }
                                if (options.Contains(OptionType.POSITIVE))
                                    min = 0.0;

                                string finalStr = "";
                                if (options.Contains(OptionType.VECTOR)) {
                                    var pieces = new List<string>();
                                    foreach (string elem in elems.Split(":")) {
                                        double num = randomRange(min, max);
                                        if (options.Contains(OptionType.INTEGER)) {
                                            num = Convert.ToInt32(Math.Round(num));
                                            pieces.Add(num.ToString());
                                        }
                                        else
                                            pieces.Add(num.ToString("N3", nfi));
                                    }
                                    finalStr = string.Join(",", pieces);
                                }
                                else {
                                    double num = randomRange(min, max);
                                    if (options.Contains(OptionType.INTEGER))
                                        num = Convert.ToInt32(Math.Round(num));
                                    else
                                        num = roundToSignificantDigits(num, 3);
                                    finalStr = num.ToString(nfi);
                                }
                                res.Add(finalStr);
                            }
                        }
                        else
                            res.Add("");
                        break;
                    case InputType.BOOL:
                        res.Add(randomBool());
                        break;
                    case InputType.REGEXP:
                        var pattern = domain.Split(":")[GlobalConsts.SECOND_PART];
                        res.Add($"PATTERN /{pattern}/: " + randomText(1));
                        break;
                    case InputType.SUBPAGE: // this is managed by link properties (e.g.: Has Patient)
                        res.Add("");
                        break;
                    default: // it is able to deduce the correct type (e.g.: email, telephone etc.)
                        res.Add("???");
                        break;
                }
            }

            return (string.Join(", ", res), elems);
        }

        static void createRandomData(Options options, int numPages, int numSubPages, int numSubTemplates, bool inTSV)
        {
            string templateXML;
            using (var reader = new StreamReader(@"templates/siteinfo.xml")) {
                templateXML = reader.ReadToEnd();
            }
            int endOfSiteInfo = templateXML.IndexOf("</siteinfo>") + 12;
            string templateXMLprefix = templateXML.Substring(0, endOfSiteInfo);
            string templateXMLsuffix = "</mediawiki>";

            SectionId currSection = new SectionId("MAIN");

            string currList = "";
            string currSubPage = "";
            string currSubPageCat = "";
            string currCat = "";
            var subPageFields = new Dictionary<GroupId, List<MainLine>>();
            var categories = new Dictionary<GroupId, List<string>>();
            var subFields = new List<MainLine>();
            var catElements = new List<string>();
            var auxPages = new Dictionary<string, List<Page>>();
            int id = 1;
            if (options.StartID > 0)
                id = options.StartID;

            Language language = AttributesHelperExtension.FromDescription<Language>(options.Language);
            Program.langManager = new LangManager();
            Program.langManager.SetLanguage(language);

            var sections = new SectionsDict();

            var currCats = new List<string>() { currCat };
            var emptyFields = new List<ParamField>();
            var emptyStringArray = new string[] {};

            var state = ParseSchemaState.START;

            StreamWriter sw = null;
            if (options.OutputFile == null) {
                sw = new StreamWriter(Console.OpenStandardOutput());
                sw.AutoFlush = true;
                Console.SetOut(sw);
            }
            else
                sw = new StreamWriter(options.OutputFile);

            /*void fillAuxPages(StreamWriter sw, string title, SectionId section)
            {
                bool isFirstRow = true;

                for (int i = 1 ; i <= numSubPages; i++) {
                    var (paramFields, subPages) = processGroups(section, options.TFName);
                    var auxPage = new Page(id++, string.Format("{0} {1}", title, i.ToString("D4")) + " - " + paramFields[0].Item2, "", paramFields, options.TFName, subPages, "", null, new List<(string, string)>());
                    if (!inTSV)
                        sw.Write(auxPage.ToXML(true));
                    else {
                        if (isFirstRow) {
                            sw.Write($"\nSubpage:{title}\n");
                            sw.Write("ID\t" + string.Join("\t", from param in paramFields select param.Item1.Item1));
                            isFirstRow = false;
                        }
                        sw.Write(auxPage.ToTSV(true));
                    }
                }
            }*/

            string linkPropName = Program.langManager.Get("Has") + " " + options.TFName;
            var linkProperties = new List<string>();

            int lineNum = 2;
            if (!File.Exists(options.InputFile)) {
                Console.WriteLine(string.Format("Cannot find input file '{0}'", options.InputFile));
                return;
            }
            using (var reader = new StreamReader(options.InputFile)) {
                var fields = new List<string>();

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine().TrimEnd();
                    if (!inTSV)
                        line = convertEntities(line);

                    var values = line.Split('\t', StringSplitOptions.None);
                    if (line.TrimEnd() == "") {
                        if (currList != "" || currSubPage != "") {
                            var key = new GroupId((currList != "") ? currList : currSubPage);
                            subPageFields.Add(key, new List<MainLine>(subFields));
                            subFields.Clear();
                            state = ParseSchemaState.START;
                        }
                        else if (currCat != "") {
                            var key = new GroupId(currCat);
                            categories.Add(key, new List<string>(catElements));
                            catElements.Clear();
                            state = ParseSchemaState.START;
                        }

                        currCat = "";
                        currList = "";
                        state = ParseSchemaState.START;
                    }
                    else if (line.StartsWith("Group") || (values.Length == GlobalConsts.DOMAIN + 1 && values[GlobalConsts.DOMAIN] == "Note"))
                        continue;
                    else if (line.StartsWith("-")) {
                        linkPropName = values[GlobalConsts.PROPERTY];
                        linkProperties.Add(linkPropName);
                    }                        
                    else if (state == ParseSchemaState.LIST) {
                        Debug.Assert(currList != "");
                        string domain = "";
                        string notes = "";
                        string info = "";
                        string showOnSelect = "";
                        if (values.Length >= 5)
                            domain = values[GlobalConsts.DOMAIN];
                        if (values.Length >= 6)
                            notes = values[GlobalConsts.NOTES];
                        if (values.Length >= 7)
                            info = values[GlobalConsts.INFO];
                        if (values.Length >= 8)
                            showOnSelect = values[GlobalConsts.SHOW_ON_SELECT];
                        var mainLine = new MainLine(new GroupId(values[GlobalConsts.GROUP]), values[GlobalConsts.SUPER_PROPERTY], 
                                                values[GlobalConsts.PROPERTY], values[GlobalConsts.TYPE], domain, notes, info, 
                                                showOnSelect);
                        subFields.Add(mainLine);
                    }
                    else if (state == ParseSchemaState.CATEGORY) {
                        Debug.Assert(currCat != "");
                        var parentS = "";
                        if (values.Length > 1)
                            parentS = values[GlobalConsts.PARENT_CATEGORY];
                        catElements.Add(values[GlobalConsts.CATEGORY]);
                    }
                    else if (values[GlobalConsts.FIRST_PART].StartsWith("List:") || values[GlobalConsts.FIRST_PART].StartsWith("Elenco:")) {
                        var values1 = values[GlobalConsts.FIRST_PART].Split(':');
                        currList = values1[GlobalConsts.SECOND_PART];
                        state = ParseSchemaState.LIST;
                    }
                    else if (values[GlobalConsts.FIRST_PART].Contains("Category:")) {
                        var values1 = values[GlobalConsts.FIRST_PART].Split(':');
                        currCat = values1[GlobalConsts.SECOND_PART].Split('|')[GlobalConsts.FIRST_PART];
                        state = ParseSchemaState.CATEGORY;
                    }
                    else if (values[GlobalConsts.FIRST_PART].StartsWith("Subpage:")) {
                        var values1 = values[GlobalConsts.FIRST_PART].Split(':');
                        var values2 = values1[GlobalConsts.SECOND_PART].Split('/');
                        currSubPage = values2[GlobalConsts.FIRST_PART];
                        currSubPageCat = values2[GlobalConsts.SECOND_PART];
                        state = ParseSchemaState.SUBPAGE;
                        currSection = new SectionId("subpage-" + currSubPage);
                        sections.Add(currSection, new Dictionary<GroupId, List<MainLine>>());
                    }
                    else if (values[GlobalConsts.GROUP].StartsWith("Section:")) {
                        currSection = new SectionId(values[GlobalConsts.GROUP].Split(":")[GlobalConsts.SECOND_PART]);
                        if (!sections.ContainsKey(currSection))
                            sections.Add(currSection, new Dictionary<GroupId, List<MainLine>>());
                        Page.resetHeaders("");
                        //sections.Clear();
                        currSubPage = "";
                        currList = "";
                        currCat = "";
                        state = ParseSchemaState.MAIN;
                    }
                    else if (state == ParseSchemaState.MAIN || state == ParseSchemaState.SUBPAGE) {
                        var groupkey = new GroupId(values[GlobalConsts.GROUP]);
                        // add a list the first time
                        if (!sections[currSection].ContainsKey(groupkey))
                            sections[currSection].Add(groupkey, new List<MainLine>());
                        string domain = "";
                        string notes = "";
                        string info = "";
                        string showOnSelect = "";
                        if (values.Length < 4)
                            throw new Exception($"At line {lineNum}: Too few fields!");                        
                        if (values.Length >= 5)
                            domain = values[GlobalConsts.DOMAIN];
                        if (values.Length >= 6)
                            notes = values[GlobalConsts.NOTES];
                        if (values.Length >= 7)
                            info = values[GlobalConsts.INFO];
                        if (values.Length >= 8)
                            showOnSelect = values[GlobalConsts.SHOW_ON_SELECT];
                        var mainLine = new MainLine(new GroupId(values[GlobalConsts.GROUP]), values[GlobalConsts.SUPER_PROPERTY], values[GlobalConsts.PROPERTY],
                                                    values[GlobalConsts.TYPE], domain, notes, info, showOnSelect);
                        sections[currSection][groupkey].Add(mainLine);
                    }
                    else
                        throw new Exception($"At line {lineNum}: INVALID STATE (id = {id})!");
                    id++;
                    lineNum++;
                }
            }

            (List<ParamField>, List<TemplateCall>) processGroups(SectionId secKey, string parentPage)
            {
                var person = new Bogus.Faker("it").Person;
                var templateFields = new List<TemplateField>();
                var paramFields = new List<ParamField>();
                var subPages = new List<TemplateCall>();

                // add a field for parent page if in an auxiliary page
                if (parentPage != "") {
                    string has = Program.langManager.Get("Has");
                    paramFields.Add(new ParamField((new GroupId($"{has} {options.TFName}"), new HeaderOptions(true, true, "")), parentPage));
                }

                foreach (var group in sections[secKey].Values) {
                    foreach (var fg in group) {
                        var key1 = new GroupId(fg.prop);
                        if (fg.type != InputType.REPEATED) {
                            bool mandatory = fg.options.Contains(OptionType.MANDATORY);
                            if (fg.options.Contains(OptionType.LIST) && inTSV)
                                paramFields.Add(new ParamField((key1, new HeaderOptions(false, true, "")), "_"));
                            else if (fg.type != InputType.SUBPAGE && !fg.options.Contains(OptionType.COMPUTED)) {
                                var (data, elems) = createData(fg.prop, fg.type, fg.domain, fg.options, categories, person, numSubPages);
                                paramFields.Add(new ParamField((key1, new HeaderOptions(mandatory, false, elems)), data));
                            }
                        }
                        else if (!inTSV) {
                            var key2 = new GroupId(fg.domain.Split(":")[GlobalConsts.SECOND_PART]);
                            for (int j = 1 ; j <= randomRange(0, numSubPages); j++) {
                                var subFields1 = new List<ParamField>();
                                foreach (var subPageField in subPageFields[key2]) {
                                    bool mandatory = subPageField.options.Contains(OptionType.MANDATORY);
                                    if (!subPageField.options.Contains(OptionType.LIST)) {
                                        if (fg.type != InputType.SUBPAGE && !fg.options.Contains(OptionType.COMPUTED))
                                        subFields1.Add(new ParamField((new GroupId(subPageField.prop), new HeaderOptions(mandatory, true, "")),
                                                                    createData(subPageField.prop, subPageField.type, subPageField.domain, subPageField.options, categories, person, numSubPages).Item1));
                                    }
                                    else
                                        subFields1.Add(new ParamField((new GroupId(subPageField.prop), new HeaderOptions(false, false, "")), "_"));
                                }
                                // it is never a list since commas have been generated in createData function above
                                subPages.Add(new TemplateCall("-", key2.ToString(), false, subFields1));
                            }
                        }
                    }
                }

                return (paramFields, subPages);
            }

            foreach (var secKey in sections.Keys) {
                var pages = new List<Page>();
                var pageIds = new List<string>();
                Page.resetHeaders("");
                for (int i = 1 ; i <= numPages; i++) {
                    string entry = string.Format("{0} {1}", options.TFName, i.ToString("D5"));
                    if (secKey.ToString().StartsWith("subpage-")) {
                        for (int j = 1 ; j <= randomRange(1, numSubPages); j++) {
                            string title = secKey.ToString().Split("-")[GlobalConsts.SECOND_PART];
                            title = string.Format("{0} {1} - {2}", title, j.ToString("D4"), entry);
                            var (paramFields, subPages) = processGroups(secKey, entry);
                            pages.Add(new Page(id++, title, "", paramFields, options.TFName, subPages, "", null, new List<(string, string)>()));
                            pageIds.Add($"{options.TFName} {i:D4}");
                        }
                    }
                    else {
                        var (paramFields, subPages) = processGroups(secKey, "");
                        pages.Add(new Page(id++, entry, "", paramFields, options.TFName, subPages, "", null, new List<(string, string)>()));
                        pageIds.Add($"{options.TFName} {i:D4}");
                    }
                }

                if (!inTSV) {
                    sw.Write(templateXMLprefix.Replace("«HOST»", GlobalConsts.IP_ADDRESS).Replace("«NAME»", options.WikiName));
                    sw.Write(templateXMLsuffix);
                }
                else {
                    foreach (var group in sections[secKey].Values) {
                        foreach (var fg in group) {
                            bool mandatory = fg.options.Contains(OptionType.MANDATORY);
                            var key1 = new GroupId(fg.prop);
                            if (fg.options.Contains(OptionType.LIST)) {
                                string headers = fg.domain.Replace(",", "\t");
                                sw.Write($"\nList:{fg.prop}\nID\n");
                                headers = fg.domain.Replace(",", "\t");
                                var person = new Bogus.Faker("it").Person;
                                foreach (var pageId in pageIds) {
                                    for (int j = 1; j <= randomRange(1, numSubPages); j++) {
                                        List<ParamField> subFields1 = (from value in fg.domain.Split(",")
                                                                    select new ParamField((new GroupId(fg.prop), new HeaderOptions(mandatory, true, "")),
                                                                                            createData(fg.prop, fg.type, fg.domain, fg.options, categories, person, numSubPages).Item1)).ToList();
                                        // it is never a list since commas have been generated in createData function above
                                        sw.Write(new TemplateCall(pageId, fg.prop, false, subFields1).ToTSV());
                                    }
                                }
                            }
                            else if (fg.options.Contains(OptionType.MULTIPLE)) {
                                GroupId key2 = new GroupId(fg.domain.Split(":")[GlobalConsts.SECOND_PART]);
                                sw.Write($"\nList:{key2}\n");
                                string headers = TemplateCall.Headers((from subPageField in subPageFields[key2]
                                                            select subPageField.prop).ToList());
                                sw.Write(headers);
                                var person = new Bogus.Faker("it").Person;
                                foreach (var pageId in pageIds) {
                                    for (int j = 1; j <= randomRange(1, numSubPages); j++) {
                                        List<ParamField> subFields1 = (from subPageField in subPageFields[key2]
                                                                    select new ParamField((new GroupId(subPageField.prop), new HeaderOptions(mandatory, true, "")),
                                                                                            createData(subPageField.prop, subPageField.type, subPageField.domain, subPageField.options, categories, person, numSubPages).Item1)).ToList();
                                        // it is never a list since commas have been generated in createData function above
                                        sw.Write(new TemplateCall(pageId, key2.ToString(), false, subFields1).ToTSV());
                                    }
                                }
                            }
                        }
                    }
                    if (secKey.ToString().StartsWith("subpage-")) {
                        string title = secKey.ToString().Split("-")[GlobalConsts.SECOND_PART];
                        sw.Write($"\nSubpage:{title}\n");
                    }
                    else
                        sw.Write($"\nSection:{secKey}\n");
                    sw.Write(Page.Headers());
                }

                foreach (var page in pages) {
                    if (!inTSV)
                        sw.Write(page.ToXML(true));
                    else
                        sw.Write(page.ToTSV(true));
                }
            }

            sw.Flush();

            Console.WriteLine($"\n\nLast ID: {id}\n");
        }

    }

}
