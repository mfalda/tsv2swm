<!-- Template:SearchEntry -->

<noinclude>

Template for searching $2

<hr />

[[Visible to::whitelist|'''Visible to: ''']]
[[Visible to group::viewers|viewers]]
[[Visible to group::editors|editors]]

[[Editable by::whitelist|'''Editable by: ''']]
[[Editable by group::editors|editors]]

</noinclude>

<includeonly>
Reported $2 satisfy the following criteria:
* field '''name''' {{#if: {{{Name|}}} | contains "{{{Name|}}}" | is unspecified }};
* field '''surname''' {{#if: {{{Surname|}}} | contains "{{{Surname|}}}" | is unspecified }};
* field '''date of birth''' {{#if: {{{Date of birth|}}} | is "{{{Date of birth|}}}" | is unspecified }};
* field '''place of birth''' {{#if: {{{Place of birth|}}} | contains "{{{Place of birth|}}}" | is unspecified }};

<div style="overflow-x: auto; white-space: nowrap;">

<tabber>

$2 =
{{#ask:
    [[Category:$2]]
    {{#if: {{{Name|}}} | [[Name::~{{{Name|}}}]] }}
    {{#if: {{{Surname|}}} | [[Surname::~{{{Surname|}}}]] }}
    {{#if: {{{Year of birth|}}} | [[Date of birth::>{{{Year of birth|}}}]] [[Date of birth::<{{{Year of birth|}}}-12-31 ]] }}
    {{#if: {{{Place of birth|}}} | [[Place of birth::{{{Place of birth|}}}]] }}
      |?Name = name
      |?Surname = surname
      |?Date of birth = date of birth
      |?Place of birth = place of birth
      |?Gender = gender
      |mainlabel = $1
      |format = table
      |class = datatable
}}
|-|
</tabber>

</div>

</includeonly>
