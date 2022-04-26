<!-- Main Page -->

Welcome to the test semantic database '''$1'''. Main commands are on the left panel. To modify a page select the "Edit" command in the "Actions" button on the right.

<!--
Some computation on current data:

* '''Positives:''' {{#vardefineecho: pos |{{#ask: [[Category:Samplings]]
[[result::positive]]
 |?Result
 |format=count
 |limit=5000
 |offset=0
}} }}
* '''Positives ratio:''' {{#expr: {{#var: pos}} / {{#ask: [[Category:Samplings]]
 |?Result
 |format=count
 |limit=5000
 |offset=0
}} * 100.0 round 3}}%
-->

<!-- or [[Special:RunQuery/SearchEntry|search]] -->
* to add an entry: [[Special:Search|search]] for it in order to avoid duplicate entries or look up it in the [[Data tables]]. Once you are sure that it is not present, you can [[Special:FormEdit/$2|add]] the entry. If necessary, in the form there is a security check to avoid entries duplication that is based on the fields of the ID ("Codice Fiscale" in Italian)
* to modify an entry: [[Special:Search|search]] for it and then select the correct entry from the results in order to avoid duplication. If you do not find it you can [[Special:FormEdit/$2|add]] it. The commands are on the left.
* search data: [[Special:Ask|Semantic search]]
* perform a faceted search: [[Special:BrowseData/$3|Explore data]]
* have an overview of the current data: [[Data tables]]
* frequency distributions:
** [[Pie charts]]
** [[Bar charts]]
** [[Histograms]]
<!-- ** [[Word clouds]] -->
* statistics on bivariate data ('''note:''' without analyses about initial hypotheses, such as normality of data)
** classes: [[Boxplots]]
** numerical data: [[Scatterplots]]
* distribution of data:
** temporal distribution: [[Timelines]]
** spatial distribution: [[Maps]]
* [[Export data|Export data]]

[[Special:Categories|Categories]] allow to obtain entry lists and exploit trasversal on hierarchies of concepts.

<hr />

[[Visible to::whitelist|'''Visible to: ''']]
[[Visible to group::viewers|viewers]]
[[Visible to group::editors|editors]]

[[Editable by::whitelist|'''Editable by: ''']]
[[Editable by group::editors|editors]]
