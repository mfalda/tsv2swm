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
    using SectionsDict = OrderedDictionary<SectionId, OrderedDictionary<GroupId, List<MainLine>>>;
    using ParamField = ValueTuple<(GroupId param, HeaderOptions options), string>;

    public class UnitTest1    
    {
        private readonly ITestOutputHelper output;

        public UnitTest1(ITestOutputHelper output)
        {
            this.output = output;
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
            Program.langManager = new LangManager(GetProjectPath("."));

            string name = "MediaWiki:Sidebar";
            var result = new RawPage(1, name, NamespaceType.MEDIAWIKI, "sidebar_param.md", "", new List<string>{}, GetProjectPath("."));

            output.WriteLine("This is output from ReadPanelMenu_is_OK: {0}", result.ToXML());

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
", result.ToXML(true), false, true, true);
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

            output.WriteLine("This is output from TemplateField_is_OK: {0}", result);

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

            output.WriteLine("This is output from Page_is_OK: '{0}'", result);

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
            Program.langManager = new LangManager(GetProjectPath("."));
            Program.langManager.SetLanguage(Language.Italiano);            

            Assert.Equal("Indirizzo", Program.langManager.Get("Address"));
        }

        [Fact]
        public void Capitalize_is_OK()
        {
            var capitalized = Program.capitalize("maria");
            output.WriteLine("This is output from Capitalize_is_OK: {0}", capitalized);

            Assert.Equal("Maria", capitalized); 
        }

        [Fact]
        public void ID_Normalization_is_OK()
        {
            var normalizedID = Program.normalizeIDs("Test (1-2-3): 4 [5]");
            output.WriteLine("This is output from ID_Normalization_is_OK: {0}", normalizedID);

            Assert.Equal("Test_-1-2-3-:_4_-5-", normalizedID);
        }

        [Fact]
        public void Name_Normalization_is_OK()    
        {
            var normalizedName = Program.normalizeNames("Test (1-2-3).4 [5]");
            output.WriteLine("This is output from Name_Normalization_is_OK: {0}", normalizedName);

            Assert.Equal("Test ⟮1-2-3⟯·4 ⟮5⟯", normalizedName);
        }
    }
}
