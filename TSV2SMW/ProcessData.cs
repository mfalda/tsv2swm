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
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;


namespace TSV2SMW
{
    using ParamField = ValueTuple<(GroupId param, HeaderOptions options), string>;

    enum ParseDataState {
        START,
        LIST,
        MAIN,
        SUBPAGE,
        NONE
    }

    public partial class Program
    {
        static void processData(Options options)
        {
            string templateXML;
            bool isList = false;
            using (var reader = new StreamReader(@"templates/siteinfo.xml")) {
                templateXML = reader.ReadToEnd();
            }
            int endOfSiteInfo = templateXML.IndexOf("</siteinfo>") + 12;
            string templateXMLprefix = templateXML.Substring(0, endOfSiteInfo);
            string templateXMLsuffix = "</mediawiki>";

            GroupId currList = new GroupId(""); // on stack since it is a structure
            GroupId currProp = new GroupId("");
            string currSubPage = "";
            var headers = new List<(GroupId, HeaderOptions)>();
            int notesIndex = -1;
            var complexProperties = new Dictionary<GroupId, Dictionary<UserId, List<TemplateCall>>>();
            var auxLists = new List<TemplateCall>();
            int id = options.StartID;
            var pages = new Dictionary<string, Page>();
            var auxPages = new Dictionary<string, Page>();

            Language language = AttributesHelperExtension.FromDescription<Language>(options.Language);
            Program.langManager = new LangManager();
            Program.langManager.SetLanguage(language);
            
            var state = ParseDataState.START;

            var users = new Dictionary<string, int>();

            if (options.UsersFile != null) {
                if (!File.Exists(options.UsersFile)) {
                    Console.WriteLine(string.Format("Cannot find users file '{0}'", options.UsersFile));
                    return;
                }                
                using (var reader = new StreamReader(options.UsersFile)) {
                    while (!reader.EndOfStream) {
                        var line = reader.ReadLine();
                        var values = line.Split('\t');
                        users.Add(Program.capitalize(values[GlobalConsts.SECOND_PART]), int.Parse(values[GlobalConsts.FIRST_PART]));
                    }
                }
            }

            if (!File.Exists(options.InputFile)) {
                Console.WriteLine(string.Format("Cannot find input file '{0}'", options.InputFile));
                return;
            }
            using (var reader = new StreamReader(options.InputFile)) {
                var fields = new List<string>();

                while (!reader.EndOfStream)
                {
                    var line = convertEntities(reader.ReadLine());
                    var values = line.Split('\t');
                    if (line.TrimEnd() == "") {
                        state = ParseDataState.NONE;
                        continue;
                    }
                    else if (values[GlobalConsts.GROUP].StartsWith("Section:")) {
                        var values1 = values[GlobalConsts.GROUP].Split(':');
                        state = ParseDataState.MAIN;
                        currProp = new GroupId("");
                    }
                    else if (values[GlobalConsts.GROUP].StartsWith("Subpage:")) {
                        var values1 = values[GlobalConsts.GROUP].Split(':');
                        currSubPage = values1[GlobalConsts.SECOND_PART];
                        state = ParseDataState.SUBPAGE;
                    }
                    else if (values[GlobalConsts.CATEGORY].StartsWith("List:") || values[GlobalConsts.CATEGORY].StartsWith("Elenco:")) {
                        var values1 = values[GlobalConsts.CATEGORY].Split(':');
                        currList = new GroupId(values1[GlobalConsts.SECOND_PART]);
                        isList = values1[GlobalConsts.FIRST_PART] == "List";
                        complexProperties.Add(currList, new Dictionary<UserId, List<TemplateCall>>());
                        state = ParseDataState.LIST;
                    }
                    else if (state == ParseDataState.LIST) {
                        if (values[GlobalConsts.CATEGORY] == "ID") {
                            headers.Clear();
                            foreach (var value in values) {
                                bool mandatory = false;
                                bool normalizeName = false;
                                string value1 = value;

                                if (value.EndsWith("*")) {
                                    throw new Exception("CHECK");
                                    /*mandatory = true;
                                    value1 = value.Remove(value.Length - 1);*/
                                }

                                int elemPos = value1.IndexOf("[elems:");
                                string elems = "";
                                if (elemPos > 0) {
                                    value1 = value.Substring(0, elemPos - 1);
                                    elems = value.Substring(elemPos + 7, value.Length - elemPos - 8);
                                }

                                if (value1.Contains("&#176;"))
                                    value1 = value.Replace("&#176;", "");
                                value1 = Program.normalizeNames(value1);
                                value1 = Program.capitalize(value1);

                                headers.Add((new GroupId(value1), new HeaderOptions(mandatory, normalizeName, elems)));
                            }
                            // if there is only a field for headers then a list is deduced
                            if (headers.Count() < 2)
                                headers.Add((currList, new HeaderOptions(true, true, "")));
                        }
                        else {
                            var fields1 = new List<ParamField>();
                            foreach (var (header, value) in headers.Zip(values))
                                fields1.Add((header, value));
                            var key = new UserId(values[GlobalConsts.GROUP]);
                            if (complexProperties[currList].TryGetValue(key, out var auxList)) {
                                // a simple template is equivalent to a core page
                                auxList.Add(new TemplateCall("-", currList.ToString(), isList, fields1));
                            }
                            else
                                complexProperties[currList].Add(key, new List<TemplateCall>());
                        }
                    }
                    else if (state == ParseDataState.MAIN || state == ParseDataState.SUBPAGE) {
                        if (values[GlobalConsts.CATEGORY] == "ID") {
                            headers.Clear();
                            int i = 0;
                            notesIndex = -1; // reset
                            foreach (var value in values) {
                                if (value == "")
                                    break;
                                if (value.Contains("NOTE"))
                                    notesIndex = i;
                                else {
                                    bool mandatory = false;
                                    bool normalizeNames = false;
                                    string value1 = value;

                                    if (value1.Contains("*")) {
                                        mandatory = true;
                                        value1 = value1.Replace("*", "");
                                    }

                                    int elemPos = value1.IndexOf("[elems:");
                                    string elems = "";
                                    if (elemPos > 0) {
                                        value1 = value.Substring(0, elemPos - 1);
                                        elems = value.Substring(elemPos + 7, value.Length - elemPos - 8);
                                    }

                                    if (value1.Contains("&#176;")) {
                                        normalizeNames = true;
                                        value1 = value1.Replace("&#176;", "");
                                    }
                                    value1 = Program.normalizeNames(value1);
                                    value1 = Program.capitalize(value1);

                                    headers.Add((new GroupId(value1), new HeaderOptions(mandatory, normalizeNames, elems)));
                                }
                                i++;
                            }
                        }
                        else {
                            string notes = "";
                            if (notesIndex >= 0 && values.Length > notesIndex) {
                                notes = $"\n{values[notesIndex]}";
                            }

                            var fields1 = new List<ParamField>();

                            int i = 0;
                            foreach (var (header, value) in headers.Zip(values)) {
                                if (notesIndex < 0 || (notesIndex > 0 && i < notesIndex)) {
                                    string value1 = value;
                                    if (header.Item2.normalizeName) {
                                        value1 = Program.normalizeNames(value);
                                        value1 = Program.capitalize(value1);
                                    }

                                    string[] values1 = value1.Split(",");
                                    string[] elems = header.Item2.elems.Split(",");
                                    if (elems.Length > 1 && values1.Length == elems.Length) {
                                        for (int j = 0; j < elems.Length; j++) {
                                            //int nearest5Multiple = (int)Math.Round((Convert.ToDouble(values1[j]) / (double)5), MidpointRounding.AwayFromZero) * 5;
                                            //fields1.Add(((new GroupId(header.Item1.ToString() + " " + elems[j]), header.Item2), nearest5Multiple.ToString()));
                                            fields1.Add(((new GroupId(header.Item1.ToString() + " " + elems[j]), header.Item2), values1[j]));
                                        }
                                    }
                                    else {
                                        try {
                                            //int nearest5Multiple = (int)Math.Round((Convert.ToDouble(value1) / 5), MidpointRounding.AwayFromZero) * 5;
                                            //fields1.Add((header, nearest5Multiple.ToString()));
                                            // deal with (improper) dates
                                            if (Regex.IsMatch(value1, @"(\d\d)/(\d\d)/(\d\d\d\d)")) {
                                                var cultureInfo = new CultureInfo("it-IT");
                                                var dateTime = DateTime.Parse(value1, cultureInfo);
                                                // TODO: English or Italian
                                                value1 = dateTime.ToString("yyyy/MM/dd");
                                                //value1 = dateTime.ToString("dd/MM/yyyy");
                                            }
                                            fields1.Add((header, value1));
                                        }
                                        catch (FormatException) {
                                            fields1.Add((header, value1));
                                        }
                                    }
                                }
                                i++;
                            }

                            var userId = new UserId(values[GlobalConsts.ID]);
                            var subPages = new List<TemplateCall>();
                            // swap group and user keys
                            foreach (var complexProperty in complexProperties.Values) {
                                if (complexProperty.TryGetValue(userId, out var auxTemplateCalls))
                                    subPages.AddRange(auxTemplateCalls);
                            }

                            string name = values[GlobalConsts.GROUP].ToString();
                            if (state == ParseDataState.MAIN) {
                                if (pages.TryGetValue(name, out var page))
                                    page.add(fields1, subPages);
                                else
                                    pages.Add(name, new Page(id++, name, notes, fields1, options.TFName, subPages, "", null, new List<(string, string)>()));
                            }
                            else /* if (state == ParseDataState.SUBPAGE) */ {
                                if (auxPages.TryGetValue(name, out var auxPage))
                                    auxPage.add(fields1, subPages);
                                else
                                    auxPages.Add(name, new Page(id++, name, notes, fields1, currSubPage, subPages, "", null, new List<(string, string)>()));
                            }
                        }
                    }
                    id++;
                }
            }

            StreamWriter sw = null;
            if (options.OutputFile == null) {
                sw = new StreamWriter(Console.OpenStandardOutput());
                sw.AutoFlush = true;
                Console.SetOut(sw);
            }
            else
                // @"../data_LiberoD.xml"
                sw = new StreamWriter(options.OutputFile);

            sw.Write(templateXMLprefix.Replace("«HOST»", GlobalConsts.IP_ADDRESS).Replace("«NAME»", options.WikiName));

            if (options.UsersFile != null) {
                foreach (var page in pages.Values) {
                    string userName = Program.capitalize(page.fields[4].Item2);
                    int userID = users[userName];
                    sw.Write(page.ToXML(true, userID, userName));
                }
            }
            else {
                foreach (var page in pages.Values)
                    sw.Write(page.ToXML(true));
            }

            foreach (var auxPage in auxPages.Values)
                sw.Write(auxPage.ToXML(true));

            sw.Write(templateXMLsuffix);

            sw.Flush();

            Console.WriteLine($"\n\nUltimo ID: {id}\n");
        }
    }
}