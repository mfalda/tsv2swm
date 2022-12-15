using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using TSV2SMW;

using OD;
using System;
using System.Reflection;
using System.IO;

namespace UnitTests
{
    using GroupsDict = OrderedDictionary<GroupId, List<MainLine>>;
    using SectionsDict = OrderedDictionary<SectionId, OrderedDictionary<GroupId, List<MainLine>>>;
    using ParamField = ValueTuple<(GroupId param, HeaderOptions options), string>;

    public class UnitTest1    
    {
        private readonly ITestOutputHelper output;

        public UnitTest1(ITestOutputHelper output)
        {
            this.output = output;
            output.WriteLine("Loading LangManager.");
            Program.langManager = new LangManager(GetProjectPath("."));
        }

        public static string GetProjectPath(string relativePath)
        {
            var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().Location);
            var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
            var path = Path.GetDirectoryName(codeBasePath) + "/../../../../";

            return Path.Combine(path, relativePath);
        }

        [Fact]
        public void ConvertEntities_is_OK()
        {
            string result = Program.convertEntities("test&'àèéìòù");

            // accents are preserved
            Assert.Equal("test&amp;&#39;àèéìòù", result);
        }

        [Fact]
        public void FormField_is_OK()
        {
            string result = new FormField("Prop 1", InputType.NUMBER, "prop 1", "min=1,max=10", new List<OptionType>() { OptionType.MANDATORY }, "info 1", "Entry", "").ToString();

            Assert.Equal(@"    ! style=""width: 30%""| Prop 1* {{#info: info 1|note}}
        | style=""width: 70%""| {{{field|prop 1|input type=number|property=prop 1|mandatory|min=1|max=10|step=any}}}
        |-
", result, false, true, true);
        }

        [Fact]
        public void ReadPanelMenu_is_OK()
        {
            string name = "MediaWiki:Sidebar";
            var result = new RawPage(1, name, NamespaceType.MEDIAWIKI, "sidebar_param.md", "", new List<string>{}, GetProjectPath("."));
            //output.WriteLine("This is output from ReadPanelMenu_is_OK: '{0}'", result.ToXML());

            string timeStamp = System.DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss");
            Assert.Equal($@" <page>
    <title>MediaWiki:Sidebar</title>
    <ns>8</ns>
    <id>1</id>
    <revision>
      <timestamp>{timeStamp}</timestamp>
      <contributor>
        <username>WikiSysop</username>
        <id>1</id>
      </contributor>
      <model>wikitext</model>
      <format>text/x-wiki</format>
      <comment>Imported version.</comment>
      <text xml:space=""preserve"">
&lt;!-- MediaWiki:Sidebar --&gt;

* Navigation
** mainpage|mainpage-description
** recentchanges-url|recentchanges
** randompage-url|randompage
* Database
** Special:Search|Search an entry
** Special:FormEdit/$1|Add an entry
** Modify an entry|Modify an entry
** Special:PropChainsHelper|Property chains helper
** Special:Ask|Semantic search
** Special:BrowseData/$2|Explore data
** Data tables|Data tables
** Plots|Plots
** Maps|Maps
** Export data|Export data
* SEARCH
* TOOLBOX


&lt;hr /&gt;

[[Visible to::whitelist|'''Visible to: ''']]
[[Visible to group::viewers|viewers]]
[[Visible to group::editors|editors]]

[[Editable by::whitelist|'''Editable by: ''']]
[[Editable by group::editors|editors]]

      </text>
    </revision>
  </page>
", result.ToXML(false), false, true, true);
        }

        [Fact]
        public void Category_is_OK()
        {
            var form = new Form(1, "Form 1", "message", new SectionsDict(), "", new List<CoreForm>(), "note", "category", "", GetProjectPath("."));
            var result = new Category(1, "Category 1", "Main Cat", "", "Parent Cat", form, new List<TemplateField>(), false, GetProjectPath("."));

            //Console.Write(result.ToXML());

            string timeStamp = System.DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss");

            Assert.Equal($@"     <page>
    <title>Category:Category 1</title>
    <ns>14</ns>
    <id>1</id>
    <revision>
      <timestamp>{timeStamp}</timestamp>
      <contributor>
        <username>WikiSysop</username>
        <id>1</id>
      </contributor>
      <model>wikitext</model>
      <format>text/x-wiki</format>
      <comment>Imported version.</comment>
      <text xml:space=""preserve"">
{{{{#default_form:Form 1}}}}







[[Category:Parent Cat]]
      </text>
    </revision>
  </page>
", result.ToXML(), false, true, true);
        }
    
        [Fact]
        public void Property_is_OK()
        {
        }

        [Fact]
        public void TemplateField_is_OK()
        {
            string result = new TemplateField(new SectionId("1"), new GroupId("Group 1"), "Property 1", "Prop_1", "ParamProp_1", InputType.NUMBER, "min=0, max=100", "info about Property 1", "", "Cat of Prop_1", new List<OptionType> { OptionType.POSITIVE }, new SortedSet<string>(), new SortedSet<string>(), "").ToString("Template 1", false, false);
            //output.WriteLine("This is output from TemplateField_is_OK: '{0}'", result);

            Assert.Equal(@"'''[[Property:Prop_1|Property 1]]'''{{#info: info about Property 1|note}}: [[Prop_1::Category:{{{ParamProp_1|}}}|{{{ParamProp_1|}}}]]
", result, false, true, true);
        }

        [Fact]
        public void Template_is_OK()
        {
            var tf1 = new TemplateField(new SectionId("Section 1"), new GroupId("Group 1"), "Property 1", "Prop_1", "ParamProp_1", InputType.NUMBER, "min=0, max=100", "info about Property 1", "", "Cat of Prop_1", new List<OptionType> { OptionType.POSITIVE }, new SortedSet<string>(), new SortedSet<string>(), "");
            var tf2 = new TemplateField(new SectionId("Section 2"), new GroupId("Group 2"), "Property 2", "Prop_2", "ParamProp_2", InputType.DATE, "min=0, max=100", "info about Property 2", "", "Cat of Prop_2", new List<OptionType>(), new SortedSet<string>(), new SortedSet<string>(), "");

            var result = new Template(1, "Template 1", "msg", new List<TemplateField>() { tf1, tf2 }, "Has Link", new List<string> { "Category 1" }, new HashSet<string>(), true, GetProjectPath(".")).ToXML();
            //output.WriteLine("This is output from Template_is_OK: '{0}'", result);

            string timeStamp = System.DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss");
            Assert.Equal(@"   <page>
    <title>Template:Template 1</title>
    <ns>10</ns>
    <id>1</id>
    <revision>
      <timestamp>" + timeStamp + @"</timestamp>
      <contributor>
        <username>WikiSysop</username>
        <id>1</id>
      </contributor>
      <model>wikitext</model>
      <format>text/x-wiki</format>
      <comment>Imported version.</comment>
      <text xml:space=""preserve"">
&lt;noinclude&gt;
msg
&lt;/noinclude&gt;

&lt;includeonly&gt;


__NOCACHE__

__NOEDITSECTION__

{{DISPLAYTITLE: {{#show: {{PAGENAME}} |?Has Link.Uid}} ({{PAGENAME}}) }}




{{#vardefine: rating |
  {{#expr:
   ({{#if: {{{ParamProp_1|}}} | 1 | 0 }}
    + {{#if: {{{ParamProp_2|}}} | 1 | 0 }}
) / 2 * 5
  }}
}}



{{#subobject:
 |Has Link={{PAGENAME}}
 |Property 1={{{Prop_1|}}}
 |Property 2={{{Prop_2|}}}
}}
&lt;div id=""sec-Section-1""&gt;


==Section 1==

&lt;tabber&gt;
  Group 1 =
    {| class=""wikitable"" style=""width: 95%; margin-left: 20px;""

    ! style=""width: 30%""| [[Property:Prop_1|Property 1]]{{#info: info about Property 1|note}}:
    | style=""width: 70%""| [[Prop_1::Category:{{{ParamProp_1|}}}|{{{ParamProp_1|}}}]]
    |-
    |}
&lt;/tabber&gt;
&lt;/div&gt;

&lt;div id=""sec-Section-2""&gt;


==Section 2==

&lt;tabber&gt;
  Group 2 =
    {| class=""wikitable"" style=""width: 95%; margin-left: 20px;""

    ! style=""width: 30%""| [[Property:Prop_2|Property 2]]{{#info: info about Property 2|note}}:
    | style=""width: 70%""| [[Prop_2::{{{ParamProp_2|}}}|{{#time: l d F Y | {{{ParamProp_2|}}} }}]]
    |-
    |}
&lt;/tabber&gt;
&lt;/div&gt;



[[Property:Rating|Completeness]]: {{#rating: {{#var: rating}} }}
{{#set: rating = {{#var: rating}} }}

&lt;hr /&gt;

[[Visible to::whitelist|'''Visible to: ''']]
[[Visible to group::viewers|viewers]]
[[Visible to group::editors|editors]]

[[Editable by::whitelist|'''Editable by: ''']]
[[Editable by group::editors|editors]]

[['''Semantic Dependency'''::{{{Has Link}}}|Part of {{{Has Link}}}]]

{{#if: Category 1 | [[Category:Category 1]] |}}
&lt;/includeonly&gt;
</text>
    </revision>
  </page>
", result, false, true, true); 
        }

        [Fact]
        public void Page_is_OK()
        {
            ParamField field1 = new ParamField((new GroupId("elem 1"), new HeaderOptions(true, true, "elem1")), "1");
            ParamField field2 = new ParamField((new GroupId("elem 2"), new HeaderOptions(true, true, "elem2")), "2");

            string result = new Page(1, "Page 1", "message", new List<ParamField> { field1, field2 }, "Patient", new List<TemplateCall>(), "MainCat", "Cat1", new List<(string, string)>(), GetProjectPath(".")).ToXML(true);
            //output.WriteLine("This is output from Page_is_OK: '{0}'", result);

            string timeStamp = System.DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss");
            Assert.Equal($@"  <page>
    <title>Page 1</title>
    <ns>0</ns>
    <id>1</id>
    <revision>
      <timestamp>{timeStamp}</timestamp>
      <contributor>
        <username>WikiSysop</username>
        <id>1</id>
      </contributor>
      <model>wikitext</model>
      <format>text/x-wiki</format>
      <comment>Imported version.</comment>
      <text xml:space=""preserve"">{{{{Patient
  | elem 1 = 1
  | elem 2 = 2

}}}}



message



{{{{#if: Cat1 | [[Category:Cat1]] |}}}}

      </text>
    </revision>
  </page>
", result, false, true, true);
        }

        [Fact]
        public void Parsing_is_OK()
        {
            string parsed = TemplateField.parseFormula("{{#ask: [[Category:Samplings]][[Has Patient::{{{Has Patient}}}]][[Number::<{{{Number|}}}]] |?x |?y#ISO |format=agedelta| dist=1 |unit=d }}, ?x=Number, ?y=Date", "Has Patient", new SortedSet<string> { "Number", "Date" }, new SortedSet<string> { "x", "y" });

            Assert.Equal("{{#ask: [[Category:Samplings]][[Has Patient::{{{Has Patient}}}]][[Number::<{{{Number|}}}]] |?Number |?Date#ISO |format=agedelta| dist=1 |unit=d }}", parsed);
        }
        
        [Fact]
        public void LangManager_is_OK()
        {
            Program.langManager.SetLanguage(Language.Italiano);            

            Assert.Equal("Indirizzo", Program.langManager.Get("Address"));
        }

        [Fact]
        public void Capitalize_is_OK()
        {
            var capitalized = Program.capitalize("maria");
            //output.WriteLine("This is output from Capitalize_is_OK: '{0}'", capitalized);

            Assert.Equal("Maria", capitalized); 
        }

        [Fact]
        public void ID_Normalization_is_OK()
        {
            var normalizedID = Program.normalizeIDs("Test (1-2-3): 4 [5]");
            //output.WriteLine("This is output from ID_Normalization_is_OK: '{0}'", normalizedID);

            Assert.Equal("Test_-1-2-3-:_4_-5-", normalizedID);
        }

        [Fact]
        public void Name_Normalization_is_OK()    
        {
            var normalizedName = Program.normalizeNames("Test (1-2-3).4 [5]");
            //output.WriteLine("This is output from Name_Normalization_is_OK: '{0}'", normalizedName);

            Assert.Equal("Test ⟮1-2-3⟯·4 ⟮5⟯", normalizedName);
        }

        [Fact]
        public void CoreForm_is_OK()    
        {
            string result = new CoreForm("Template 1", new List<MainLine>(), false, "Patient").ToString();
            //output.WriteLine("This is output from CoreForm_is_OK: '{0}'", result);

            Assert.Equal(@"
    {{{for template|Template 1|multiple|add button text=Add Template 1|embed in field=Patient[Template 1]}}}
    {| class=""formtable"" style=""width: 95%; margin-left: 20px;""
    
    |}
    {{{end template}}}", result, false, true, true);
        }

        [Fact]
        public void SimpleForm_is_OK()    
        {
            string result = new SimpleForm(1, "Name 1", "Message 1", "Text 1", new List<MainLine>(), "Template 1", GetProjectPath(".")).ToXML();
            //output.WriteLine("This is output from SimpleForm_is_OK: '{0}'", result);

            string timeStamp = System.DateTime.UtcNow.ToString  ("yyyy'-'MM'-'dd'T'HH':'mm':'ss");
            Assert.Equal(@"  <page>
    <title>Form:Name 1</title>
    <ns>106</ns>
    <id>1</id>
    <revision>
      <timestamp>" + timeStamp + @"</timestamp>
      <contributor>
        <username>WikiSysop</username>
        <id>1</id>
      </contributor>
      <model>wikitext</model>
      <format>text/x-wiki</format>
      <comment>Imported version.</comment>
      <text xml:space=""preserve"">&lt;noinclude&gt;
Message 1
&lt;/noinclude&gt;

&lt;includeonly&gt;
&lt;div id=""wikiPreview"" style=""display: none; padding-bottom: 25px; margin-bottom: 25px; border-bottom: 1px solid #AAAAAA;""&gt;&lt;/div&gt;

Text 1

{| class=""formtable"" style=""width: 95%; margin-left: 20px;""
|}
{{{end template}}}

{{{standard input|save}}} {{{standard input|cancel}}}

&lt;hr /&gt;

[[Visible to::whitelist|'''Visible to: ''']]
[[Visible to group::viewers|viewers]]
[[Visible to group::editors|editors]]

[[Editable by::whitelist|'''Editable by: ''']]
[[Editable by group::editors|editors]]

&lt;/includeonly&gt;</text>
    </revision>
  </page>
", result, false, true, true);
        }

        [Fact]
        public void TemplateCall_is_OK()    
        {
            ParamField field1 = new ParamField((new GroupId("elem 1"), new HeaderOptions(true, true, "elem1")), "1");
            ParamField field2 = new ParamField((new GroupId("elem 2"), new HeaderOptions(true, true, "elem2")), "2");

            string result = new TemplateCall("TemplateCall1", "Template_name", true, new List<ParamField>() { field1, field2 }).ToString();
            //output.WriteLine("This is output from TemplateCall_is_OK: '{0}'", result);

            Assert.Equal(@"
    {{Template_name
        | elem 1 = 1
        | elem 2 = 2
    }}", result, false, true, true);
        }

        [Fact]
        public void TemplateCall_getFields_is_OK()    
        {
            ParamField field1 = new ParamField((new GroupId("elem 1"), new HeaderOptions(true, true, "elem1")), "Field 1");
            ParamField field2 = new ParamField((new GroupId("elem 2"), new HeaderOptions(true, true, "elem2")), "Field 2");

            var tc = new TemplateCall("TemplateCall1", "Template_name", true, new List<ParamField>() { field1, field2 });
            string result = tc.getFields();
            //output.WriteLine("This is output from TemplateCall_getFields_is_OK: '{0}'", result);

            Assert.Equal("Field 1, Field 2", result, false, true, true);
        }

        
        [Fact]
        public void TemplateCall_Headers_is_OK()    
        {
            string result = TemplateCall.Headers(new List<string> { "Header 1", "Header 2" });
            //output.WriteLine("This is output from TemplateCall_Headers_is_OK: '{0}'", result);

            Assert.Equal("ID\tHeader 1\tHeader 2\n", result, false, true, true);
        }

        
        [Fact]
        public void TemplateCall_ToTSV_is_OK()    
        {
            ParamField field1 = new ParamField((new GroupId("elem 1"), new HeaderOptions(true, true, "elem1")), "1");
            ParamField field2 = new ParamField((new GroupId("elem 2"), new HeaderOptions(true, true, "elem2")), "2");

            string result = new TemplateCall("TemplateCall1", "Template_name", true, new List<ParamField>() { field1, field2 }).ToTSV();
            //output.WriteLine("This is output from TemplateCall_ToTSV_is_OK: '{0}'", result);
            
            Assert.Equal("TemplateCall1\t1\t2\n", result, false, true, true);
        }

        [Fact]
        public void Page_Add_Fields_is_OK()    
        {
            ParamField field1 = new ParamField((new GroupId("elem 1"), new HeaderOptions(true, true, "elem1")), "1");
            ParamField field2 = new ParamField((new GroupId("elem 2"), new HeaderOptions(true, true, "elem2")), "2");
            var page = new Page(1, "Page 1", "message", new List<ParamField> { field1, field2 }, "Patient", new List<TemplateCall>(), "MainCat", "Cat1", new List<(string, string)>(), GetProjectPath("."));

            ParamField field31 = new ParamField((new GroupId("elem 31"), new HeaderOptions(true, true, "elem31")), "31");
            ParamField field32 = new ParamField((new GroupId("elem 32"), new HeaderOptions(true, true, "elem32")), "32");
            var tc3 = new TemplateCall("TemplateCall3", "Template_name", true, new List<ParamField>() { field31, field32 });

            page.add(new List<ParamField> { field1 }, new List<TemplateCall>() { tc3 });
            string result = page.ToXML(false);
            //output.WriteLine("This is output from Page_Add_Fields_is_OK: '{0}'", result);

            string timeStamp = System.DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss");
            Assert.Equal(@"  <page>
    <title>Page 1</title>
    <ns>0</ns>
    <id>1</id>
    <revision>
      <timestamp>" + timeStamp + @"</timestamp>
      <contributor>
        <username>WikiSysop</username>
        <id>1</id>
      </contributor>
      <model>wikitext</model>
      <format>text/x-wiki</format>
      <comment>Imported version.</comment>
      <text xml:space=""preserve"">{{Patient
  | elem 1 = 1
  | elem 2 = 2
  | elem 1 = 1
  | Template_name = 31, 32

}}



message



{{#if: Cat1 | [[Category:Cat1]] |}}

      </text>
    </revision>
  </page>
", result, false, true, true);
        }

        [Theory]
        [InlineData("_", true, true, "")]
        [InlineData(" ", true, true, " ")]
        public void manageNA_is_OK(string inputString, bool mandatory, bool fill, string res)    
        {
            string result = Page.manageNA(inputString, mandatory, fill);
            //output.WriteLine("This is output from manageNA_is_OK: '{0}'", result);

            Assert.Equal(res, result);
        }

        [Fact]
        public void generateUnivariateChartPage_is_OK()    
        {
            var sections = new SectionsDict() { 
                [new SectionId("Section 1")] = new GroupsDict() {
                    [new GroupId("Group 11")] = new List<MainLine>() { new MainLine(new GroupId("Group 1"), "",
                        "prop 1", "Text", "", "", "info 1", "") }
                },
                [new SectionId("Section 2")] = new GroupsDict() {
                    [new GroupId("Group 21")] = new List<MainLine>() { new MainLine(new GroupId("Group 21"), "",
                        "prop 2", "Text", "", "", "info 2", "") }
                },
            };

            var result = Program.generateUnivariateChartPage("Page 1", sections, InputType.NUMBER, true, true);
            //output.WriteLine("This is output from generateUnivariateChartPage_is_OK: '{0}'", result);

            Assert.Equal("Property distribution", result.Item1, false, true, true);
            Assert.Equal(@"{{#widget:ShinyPlotSrv
  |prop1_label=Property|prop1_data=Page 1
  |prop2_label=Class|prop2_data=
|prop3_label=|prop3_data=
  |plot=barsSrvAPI|title=Property distribution
}}
", result.Item2, false, true, true);
        }

        [Fact]
        public void generateBivariateChartPage_is_OK()    
        {
            var sections = new SectionsDict() { 
                [new SectionId("Section 1")] = new GroupsDict() {
                    [new GroupId("Group 11")] = new List<MainLine>() { new MainLine(new GroupId("Group 1"), "",
                        "prop 1", "Number", "", "", "info 1", "") }
                },
                [new SectionId("Section 2")] = new GroupsDict() {
                    [new GroupId("Group 21")] = new List<MainLine>() { new MainLine(new GroupId("Group 21"), "",
                        "prop 2", "Text", "", "", "info 2", "") }
                },
            };

            string result = Program.generateBivariateChartPage("Page 1", sections, InputType.NUMBER, InputType.TEXT, true);
            //output.WriteLine("This is output from generateBivariateChartPage_is_OK: '{0}'", result);

            Assert.Equal(@"{{#widget:ShinyPlotSrv |prop1_label=|prop1_data=Prop 1
  |prop2_label=|prop2_data=Prop 2
  |prop3_label=|prop3_data=
  |plot=SrvAPI|title=
}}", result, false, true, true);
        }

        [Fact]
        public void generateTimelinePage_is_OK()    
        {
            var sections = new SectionsDict() { 
                [new SectionId("Section 1")] = new GroupsDict() {
                    [new GroupId("Group 11")] = new List<MainLine>() { new MainLine(new GroupId("Group 1"), "",
                        "prop 1", "Date", "", "", "info 1", "") }
                },
                [new SectionId("Section 2")] = new GroupsDict() {
                    [new GroupId("Group 21")] = new List<MainLine>() { new MainLine(new GroupId("Group 21"), "",
                        "prop 2", "Date", "", "", "info 2", "") }
                },
            };

            var result = Program.generateTimelinePage("Timeline 1", sections);
            //output.WriteLine("This is output from generateTimelinePage_is_OK: '{0}'", result);

            Assert.Equal(@"{{#formlink:form=Timeline|link text=EditProperty|}}

{{#ask: [[Category:Timeline 1]] |?{{{Property chart|}}}= |format=timeline |limit=10000|headers=hide|timelinesize=300px|timelineposition=middle|timelinebands=MONTH,YEAR,DECADE }}
", result.Item1, false, true, true);
            Assert.Equal(@"<includeonly>
<div id='wikiPreview' style='display: none; padding-bottom: 25px; margin-bottom: 25px; border-bottom: 1px solid #AAAAAA;'></div>
{{{info|page name=Timeline}}}

{{{for template|Timeline}}}
'''Property''' {{{field|Property|input type=combobox|values=Prop 1,Prop 2 }}}
{{{end template}}}

{{{standard input|save}}} {{{standard input|cancel}}}
</includeonly>", result.Item2, false, true, true);
        }

        [Fact]
        public void generateChartsPage_is_OK()    
        {
            var sections = new SectionsDict() { 
                [new SectionId("Section 1")] = new GroupsDict() {
                    [new GroupId("Group 11")] = new List<MainLine>() { new MainLine(new GroupId("Group 1"), "",
                        "prop 1", "Date", "", "", "info 1", "") }
                },
                [new SectionId("Section 2")] = new GroupsDict() {
                    [new GroupId("Group 21")] = new List<MainLine>() { new MainLine(new GroupId("Group 21"), "",
                        "prop 2", "Date", "", "", "info 2", "") }
                },
            };

            string result = Program.generateChartsPage("Patients", sections);
            //output.WriteLine("This is output from generateChartsPage_is_OK: '{0}'", result);

            Assert.Equal(@"
==Group 11==

===Prop 1===
{{#widget:Iframe|url=http://172.25.0.181:3838/timelinesSrvAPI/?title={{urlencode: Property histogram ""Prop 1"" }}&data={{urlencode: {{#ask: [[Category:Patients]] |?Prop 1= |format=array|mainlabel=-|sep=,|headers=hide|hidegaps=all|limit=10000}} }}|width=800|height=400}}



==Group 21==

===Prop 2===
{{#widget:Iframe|url=http://172.25.0.181:3838/timelinesSrvAPI/?title={{urlencode: Property histogram ""Prop 2"" }}&data={{urlencode: {{#ask: [[Category:Patients]] |?Prop 2= |format=array|mainlabel=-|sep=,|headers=hide|hidegaps=all|limit=10000}} }}|width=800|height=400}}


", result, false, true, true);
        }

        [Fact]
        public void getMapCode_is_OK()    
        {
            string result = Program.getMapCode("Maps", new List<string>(), "DBNS_layer", true);
            //output.WriteLine("This is output from getMapCode_is_OK: '{0}'", result);

            Assert.Equal(@"
{{#ask: [[Maps]]

 |format=leaflet
 |offset=0
 |link=all
 |headers=show
 |width=auto
 |height=auto
 |clustermaxzoom=1
 |markercluster=on
 |layers=DBNS_layer
 |scrollwheelzoom=1
 |pagelabel=true
 |copycoords=1
 |clicktarget=javascript:alert('Lat: %lat%, long: %long%')
 |showtitle=1
}}

", result, false, true, true);
        }

        [Fact]
        public void createListPagesInCat_is_OK()    
        {
            string result = Program.createListPagesInCat("MainCat", "Patients");
            //output.WriteLine("This is output from createListPagesInCat_is_OK: '{0}'", result);

            Assert.Equal("\n\n{{#categorytree:{{PAGENAME}}|mode=all|showcount=on}}", result, false, true, true);
        }

        [Fact]
        public void generateExportLinks_is_OK()    
        {
            var tf1 = new TemplateField(new SectionId("Section 1"), new GroupId("Group 1"), "Property 1", "Prop_1", "ParamProp_1", InputType.NUMBER, "min=0, max=100", "info about Property 1", "", "Visits", new List<OptionType> { OptionType.POSITIVE }, new SortedSet<string>(), new SortedSet<string>(), "");
            var tf2  = new TemplateField(new SectionId("Section 2"), new GroupId("Group 2"), "Property 2", "Prop_2", "ParamProp_2", InputType.TEXT, "", "info about Property 2", "", "Visits", new List<OptionType> { OptionType.POSITIVE }, new SortedSet<string>(), new SortedSet<string>(), "");

            var auxTF11 = new TemplateField(new SectionId("A11"), new GroupId("Aux group 11"), "Aux property 1", "AuxProp_1", "ParamAuxProp_11", InputType.NUMBER, "min=0, max=100", "info about Property 11", "", "Visits", new List<OptionType> { OptionType.POSITIVE }, new SortedSet<string>(), new SortedSet<string>(), "");
            var auxTF12 = new TemplateField(new SectionId("A12"), new GroupId("Aux group 12"), "Aux property 2", "AuxProp_2", "ParamAuxProp_12", InputType.TEXT, "", "info about Property 12", "", "Visits", new List<OptionType> { OptionType.POSITIVE }, new SortedSet<string>(), new SortedSet<string>(), "");
            var auxT1 = new Template(1, "Template 1", "msg", new List<TemplateField>() { auxTF11, auxTF12 }, "Has Link", new List<string> { "Category 1" }, new HashSet<string>(), true, GetProjectPath("."));

            var auxTF21 = new TemplateField(new SectionId("A21"), new GroupId("Aux group 21"), "Aux property 21", "AuxProp_1", "ParamAuxProp_21", InputType.NUMBER, "min=0, max=10", "info about Property 21", "", "Visits", new List<OptionType> { OptionType.POSITIVE }, new SortedSet<string>(), new SortedSet<string>(), "");
            var auxTF22 = new TemplateField(new SectionId("A22"), new GroupId("Aux group 22"), "Aux property 22", "AuxProp_2", "ParamAuxProp_22", InputType.TEXT, "", "info about Property 22", "", "Visits", new List<OptionType> { OptionType.POSITIVE }, new SortedSet<string>(), new SortedSet<string>(), "");
            var auxT2 = new Template(1, "Template 2", "msg", new List<TemplateField>() { auxTF21, auxTF22 }, "Has Link", new List<string> { "Category 1" }, new HashSet<string>(), true, GetProjectPath("."));

            string result = Program.generateExportLinks("Patients", new List<TemplateField>() { tf1, tf2 }, new List<Template> { auxT1, auxT2 });
            //output.WriteLine("This is output from generateExportLinks_is_OK: '{0}'", result);

            Assert.Equal(@"
==Excel spreadsheets==

{{#ask:
  [[Category:Patients]]
  |?Prop_1 = Property 1
  |?Prop_2 = Property 2
  |searchlabel=Category 'Patients - Section 1' as Excel XLSX file
  |format=spreadsheet
}}
* {{#ask:
  [[Category:Patients]]

  |searchlabel=Category 'Patients - Section 2' as Excel XLSX file
  |format=spreadsheet
}}
===Auxiliary data===


* {{#ask:
  [[Category:Category 1]]
    |?Has Link = Parent
    |?AuxProp_1 = Aux property 1
    |?AuxProp_2 = Aux property 2
  |searchlabel=Category 'Category 1' as Excel XLSX file
  |format=spreadsheet
  |sort=Has Link
}}

* {{#ask:
  [[Category:Category 1]]
    |?Has Link = Parent
    |?AuxProp_1 = Aux property 21
    |?AuxProp_2 = Aux property 22
  |searchlabel=Category 'Category 1' as Excel XLSX file
  |format=spreadsheet
  |sort=Has Link
}}

==R dataframes==

{{#ask:
  [[Category:Patients]]
  |?Prop_1 = Property 1
  |?Prop_2 = Property 2
  |searchlabel=Category 'Patients - Section 1' as R dataframe
  |format=dataframe
}}
* {{#ask:
  [[Category:Patients]]

  |searchlabel=Category 'Patients - Section 2' as R dataframe
  |format=dataframe
}}
===Auxiliary data===


* {{#ask:
  [[Category:Category 1]]
    |?Has Link = Parent
    |?AuxProp_1 = Aux property 1
    |?AuxProp_2 = Aux property 2
  |searchlabel=Category 'Category 1' as R dataframe
  |format=dataframe
  |sort=Has Link
}}

* {{#ask:
  [[Category:Category 1]]
    |?Has Link = Parent
    |?AuxProp_1 = Aux property 21
    |?AuxProp_2 = Aux property 22
  |searchlabel=Category 'Category 1' as R dataframe
  |format=dataframe
  |sort=Has Link
}}

==Prolog predicates==

{{#ask:
  [[Category:Patients]]
  |?Prop_1 = Property 1
  |?Prop_2 = Property 2
  |searchlabel=Category 'Patients - Section 1' as Prolog predicates
  |format=prolog
}}
* {{#ask:
  [[Category:Patients]]

  |searchlabel=Category 'Patients - Section 2' as Prolog predicates
  |format=prolog
}}
===Auxiliary data===


* {{#ask:
  [[Category:Category 1]]
    |?Has Link = Parent
    |?AuxProp_1 = Aux property 1
    |?AuxProp_2 = Aux property 2
  |searchlabel=Category 'Category 1' as Prolog predicates
  |format=prolog
  |sort=Has Link
}}

* {{#ask:
  [[Category:Category 1]]
    |?Has Link = Parent
    |?AuxProp_1 = Aux property 21
    |?AuxProp_2 = Aux property 22
  |searchlabel=Category 'Category 1' as Prolog predicates
  |format=prolog
  |sort=Has Link
}}", result, false, true, true);
        }

        [Fact]
        public void createPropChainHelperConf_is_OK()    
        {
            var auxTF1 = new TemplateField(new SectionId("A1"), new GroupId("Aux group 1"), "Aux property 1", "AuxProp_1", "ParamAuxProp_1", InputType.NUMBER, "min=0, max=100", "info about Property 1", "", "Visits", new List<OptionType> { OptionType.POSITIVE }, new SortedSet<string>(), new SortedSet<string>(), "");
            var auxTF2 = new TemplateField(new SectionId("A2"), new GroupId("Aux group 2"), "Aux property 2", "AuxProp_2", "ParamAuxProp_2", InputType.TEXT, "", "info about Property 2", "", "Visits", new List<OptionType> { OptionType.POSITIVE }, new SortedSet<string>(), new SortedSet<string>(), "");

            var tf1 = new TemplateField(new SectionId("Section 1"), new GroupId("Group 1"), "Property 1", "Prop_1", "ParamProp_1", InputType.NUMBER, "min=0, max=100", "info about Property 1", "", "Visits", new List<OptionType> { OptionType.POSITIVE }, new SortedSet<string>(), new SortedSet<string>(), "");
            var tf2  = new TemplateField(new SectionId("Section 2"), new GroupId("Group 2"), "Property 2", "Prop_2", "ParamProp_2", InputType.TEXT, "", "info about Property 2", "", "Visits", new List<OptionType> { OptionType.POSITIVE }, new SortedSet<string>(), new SortedSet<string>(), "");
            var t1 = new Template(1, "Template 1", "msg", new List<TemplateField>() { tf1, tf2 }, "Has Link", new List<string> { "Category 1" }, new HashSet<string>(), true, GetProjectPath("."));

            var result = Program.createPropChainHelperConf("Patients", new List<TemplateField>() { auxTF1, auxTF2 }, new List<Template>() { t1 });
            //output.WriteLine("This is output from createPropChainHelperConf_is_OK: '{0}'", result);

            Assert.Equal(@"$pchCatLevels = [
  'Patients' => 0,
  'Category 1' => 1,
];

$pchPropLevels = [
  ""AuxProp_1"" => [0, 0],
  ""AuxProp_2"" => [0, 0],
  ""Prop_1"" => [0, 1],
  ""Prop_2"" => [0, 1],
];

$pchLinkProps = [
    ['Has Link']
];", result, false, true, true);
        }

        [Fact]
        public void SimpleTemplate_is_OK()    
        {
            var result = new SimpleTemplate(1, "Name 1", "message", "body", "Has Link", new List<string>() { "Cat 1", "Cat 2" }, new HashSet<string>(), GetProjectPath(".")).ToXML();           
            //output.WriteLine("This is output from SimpleTemplate_is_OK: '{0}'", result);

            string timeStamp = System.DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss");
            Assert.Equal(@"  <page>
    <title>Template:Name 1</title>
    <ns>10</ns>
    <id>1</id>
    <revision>
      <timestamp>" + timeStamp + @"</timestamp>
      <contributor>
        <username>WikiSysop</username>
        <id>1</id>
      </contributor>
      <model>wikitext</model>
      <format>text/x-wiki</format>
      <comment>Imported version.</comment>
      <text xml:space=""preserve"" bytes=""615"">&lt;noinclude&gt;
message
&lt;/noinclude&gt;

&lt;includeonly&gt;


body

&lt;hr /&gt;

[[Visible to::whitelist|'''Visible to: ''']]
[[Visible to group::viewers|viewers]]
[[Visible to group::editors|editors]]

[[Editable by::whitelist|'''Editable by: ''']]
[[Editable by group::editors|editors]]

{{#if: Cat 1 | [[Category:Cat 1]] |}}

{{#if: Cat 2 | [[Category:Cat 2]] |}}

&lt;/includeonly&gt;
</text>
    </revision>
  </page>
", result, false, true, true);
        }
    }
}
