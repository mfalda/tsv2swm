using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Text;


namespace TSV2SMW
{
    using ParamField = ValueTuple<(GroupId param, HeaderOptions options), string>;

    public enum NamespaceType
    {
        [Description("Automatic")]
        AUTO = -10,
        [Description("Page")]
        PAGE = 0,
        [Description("MediaWiki")]
        MEDIAWIKI = 8,
        [Description("Template")]
        TEMPLATE = 10,
        [Description("Form")]
        FORM = 106,
        [Description("Widget")]
        WIDGET = 274,
        [Description("Module")]
        MODULE = 828
    }

    public class TemplateCall
    {
        string id;
        public string templateName;
        public bool isList;
        List<ParamField> fields;

        public TemplateCall(string id1, string templateName1, bool isList1, List<ParamField> fields1)
        {
            id = id1;
            templateName = templateName1;
            isList = isList1;
            fields = fields1;
        }

        public string getFields()
        {
            return string.Join(", ", from field in fields
                                    where field.Item1.ToString() != "ID"
                                    select field.Item2);
        }

        public override string ToString()
        {
            string fieldsS = string.Join("\n", from field in fields
                                               where field.Item1.param.ToString() != "ID"
                                               select $"      | {field.Item1.param} = {field.Item2}");

            return $@"
    {{{{{templateName}
{fieldsS}
    }}}}";
        }

        public static string Headers(List<string> headers1)
        {
            return "ID\t" + string.Join("\t", headers1) + "\n";
        }

        public string ToTSV()
        {
            return  id + "\t" + string.Join("\t", from field in fields select field.Item2) + "\n";
        }

    }

    public class RawPage
    {
        public int id;
        public string name;
         public NamespaceType nameSpace;
        public string fileName;
        public string text;
        public string templateXML;
        List<string> @params;

        public RawPage() {}

        public RawPage(int id1, string name1, NamespaceType namespace1, string fileName1, string text1, List<string> params1)
        {
            id = id1;
            name = name1;
            nameSpace = namespace1;
            fileName = fileName1;
            text = text1;
            @params = params1;

            var sb = new StringBuilder();
            string model = "raw";
            if (fileName1.EndsWith("js"))
                model = "js";
            else if (fileName1.EndsWith("css"))
                model = "css";
            using (var reader = new StreamReader($"templates/{model}_page.xml")) {
                templateXML = reader.ReadToEnd();
            }
        }

        public string ToXML()
        {
            if (text == "") {
                string dir = "";
                string lang = Program.langManager.GetLanguageString();

                if (File.Exists($"simple_pages/{lang}/{fileName}")) 
                    dir = lang + "/";

                using (var reader = new StreamReader($"simple_pages/{dir}{fileName}")) {
                    string line;
                    var sb = new StringBuilder();
                    bool firstLine = true;
                    while ((line = reader.ReadLine()) != null) {
                        if (firstLine && name == "" && (line.StartsWith("//") || line.StartsWith("--") 
                                || line.StartsWith("#") || line.StartsWith("/*") || line.StartsWith("<!--"))) {
                            name = line.Substring(3);
                            // fix for different prefix ends and remove possible comment ends
                            if (line.StartsWith("#"))
                                name = line.Substring(2);
                            else if (line.StartsWith("<!--"))
                                name = line.Substring(5, line.Length - 9);
                            else if (line.StartsWith("/*"))
                                name = line.Substring(3, line.Length - 6);
                            name = name.Trim();
                        }
                        else
                            sb.Append(line + "\n");

                        firstLine = false;
                    }
                    string text1 = sb.ToString();
                    int i = 1;
                    foreach (string p in @params) {
                        text1 = text1.Replace("$" + i, p);
                        i++;
                    }
                    text = text1;
                }
            }
            text = Program.convertEntities(text);
            var parts = name.Split(":");
            if (parts.Length > 1) {
                string ns = parts[GlobalConsts.FIRST_PART].Trim();
                nameSpace = AttributesHelperExtension.FromDescription<NamespaceType>(ns);
            }
            else if (nameSpace == NamespaceType.AUTO)
                nameSpace = NamespaceType.PAGE;

            return templateXML.Replace("«NAME»", name)
                           .Replace("«ID»", id.ToString())
                           .Replace("«NS»", ((int) nameSpace).ToString())
                           .Replace("«TIMESTAMP»", DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss"))
                           .Replace("«TEXT»", text);
        }
    }

    public class Page
    {
        public int id;
        public string name;
        public string message;
        static List<string> headers;
        static string parentPage;
        public List<ParamField> fields;
        public List<TemplateCall> subTemplates;
        public static string templateXML;
        string templateName;
        string mainCategory;
        string categories;
        List<(string, string)> categoryProps;

        public Page() {}

        public Page(int id1, string name1, string message1, List<ParamField> fields1, string templateName1, List<TemplateCall> subTemplates1, string mainCategory1, string categories1, List<(string, string)> categoryProps1)
        {
            id = id1;
            name = name1;
            message = message1;
            if (headers == null) {
                headers = new List<string> {};
                foreach (var field in fields1) {
                    string header = field.Item1.param.ToString();
                    if (field.Item1.options.elems != "")
                        header += " [elems:" + field.Item1.options.elems.Replace(":", ",") + "]";
                    headers.Add(header);
                }
            }
            fields = fields1;
            templateName = templateName1;
            subTemplates = subTemplates1;
            mainCategory = mainCategory1;
            categories = "";
            if (categories1 != null) {
                foreach (var cat in categories1.Split("|")) {
                    if (mainCategory == "")
                        mainCategory = cat;
                    categories += $"{{{{#if: {cat} | [[Category:{cat}]] |}}}}\n";
                }
            }
            categoryProps = categoryProps1;

            if (templateXML == null) {
                using (var reader = new StreamReader(@"templates/instance.xml")) {
                    templateXML = reader.ReadToEnd();
                }
            }
        }

        public static void resetHeaders(string parentPage1)
        {
            if (headers != null) {
                headers.Clear();
                headers = null;
            }
            if (parentPage1 != "")
                parentPage = Program.langManager.Get("Has") + " " + parentPage1;
            else
                parentPage = "";
        }

        public void add(List<ParamField> fields1, List<TemplateCall> subTemplates1)
        {
            headers.AddRange(from field in fields1 select field.Item1.ToString());
            fields.AddRange(fields1.ToList());
            subTemplates = subTemplates1;
        }

        private string manageNA(string inputString, bool mandatory, bool fill)
        {
            if (inputString == "_")
                return "";
            else if (mandatory && fill && inputString == "")
                return GlobalConsts.NA;
            else
                return inputString;
        }

        public string ToXML(bool fill, int userID=1, string userName="WikiSysop")
        {
            var listFields = new Dictionary<string, string>();
            // if a field is a list skip it and annotate apart
            string subTemplatesS = "";
            foreach (var subTemplate in subTemplates) {
                if (subTemplate.isList) {
                    string tmpValue = "";
                    if (!listFields.TryGetValue(subTemplate.templateName, out tmpValue))
                        listFields.Add(subTemplate.templateName, subTemplate.getFields());
                    else
                        listFields[subTemplate.templateName] = tmpValue + ", " + subTemplate.getFields();
                }
                else {
                    string tmpValue = "";
                    if (!listFields.TryGetValue(subTemplate.templateName, out tmpValue))
                        listFields.Add(subTemplate.templateName, subTemplate.ToString());
                    else
                        listFields[subTemplate.templateName] = tmpValue + subTemplate.ToString();
                }
            }

            // if a field is a list join all values in its sub-template
            string fieldsS = "";
            foreach (var field in fields) {
                var (name, options) = field.Item1;

                string tmpValue = field.Item2;
                if (options.normalizeName)
                    tmpValue = Program.normalizeNames(field.Item2);

                fieldsS += $"  | {name} = " + manageNA(tmpValue, options.mandatory, fill) + "\n";
            }

            foreach (string k in listFields.Keys) {
                string tmpValue = listFields[k];
                fieldsS += $"  | {k} = {tmpValue}" + "\n";
            }

            if (!name.StartsWith(templateName))
                name = Program.normalizeNames(name);

            return templateXML.Replace("«INAME»", name)
                              .Replace("«ID»", id.ToString())
                              .Replace("«TIMESTAMP»", DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss"))
                              .Replace("«USERNAME»", userName)
                              .Replace("«USERID»", userID.ToString())
                              .Replace("«MESSAGE»", message)
                              .Replace("«TNAME»", templateName)
                              .Replace("«PAGES»", "")
                              .Replace("«PARAMETERS»", fieldsS)
                              .Replace("«TLIST»", subTemplatesS)
                              .Replace("«CATEGORIES»", categories);
        }

        public static string Headers()
        {
            return "ID\t" + string.Join("\t", headers) + "\n";
        }

        public string ToTSV(bool fill)
        {
            return name + "\t" + string.Join("\t", from field in fields
                                                   select manageNA(field.Item2, field.Item1.Item2.mandatory, fill)) + "\n";
        }

    }
}