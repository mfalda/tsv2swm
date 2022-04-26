<!-- Template:CercaVoce -->

<noinclude>

Template per la ricerca dei $2

<hr />

[[Visible to::whitelist|'''Visible to: ''']]
[[Visible to group::viewers|viewers]]
[[Visible to group::editors|editors]]

[[Editable by::whitelist|'''Editable by: ''']]
[[Editable by group::editors|editors]]

</noinclude>

<includeonly>
I $2 riportati soddisfano i seguenti criteri:
* il campo '''nome''' {{#if: {{{Nome|}}} | contiene "{{{Nome|}}}" | ha un valore qualsiasi }};
* il campo '''cognome''' {{#if: {{{Cognome|}}} | contiene "{{{Cognome|}}}" | ha un valore qualsiasi }};
* il campo '''data di nascita''' {{#if: {{{Anno di nascita|}}} | ha l'anno pari a "{{{Anno di nascita|}}}" | ha un valore qualsiasi }};
* il campo '''Comune di nascita''' {{#if: {{{Comune di nascita|}}} | contiene "{{{Comune di nascita|}}}" | ha un valore qualsiasi }};

<div style="overflow-x: auto; white-space: nowrap;">

<tabber>

Risultati =
{{#ask:
    [[Category:$2]]
    {{#if: {{{Nome|}}} | [[Nome::~{{{Nome|}}}]] }}
    {{#if: {{{Cognome|}}} | [[Cognome::~{{{Cognome|}}}]] }}
    {{#if: {{{Anno di nascita|}}} | [[Data di nascita::>{{{Anno di nascita|}}}]] [[Data di nascita::<{{{Anno di nascita|}}}-12-31 ]] }}
    {{#if: {{{Comune di nascita|}}} | [[Comune di nascita::{{{Comune di nascita|}}}]] }}
      |?Nome = nome
      |?Cognome = cognome
      |?Data di nascita = data di nascita
      |?Comune di nascita = comune di nascita
      |?Genere = genere
      |mainlabel = $1
      |format = table
      |class = datatable
}}
|-|
</tabber>

</div>

</includeonly>
