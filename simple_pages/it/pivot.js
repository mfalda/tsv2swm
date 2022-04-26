// MediaWiki:Pivot.js

$('li#n-Cerca-una-voce a').prepend('<div id="drop-icon"><i class="fa fa-search"></i></div>');
$('li#n-Aggiungi-una-voce a').prepend('<div id="drop-icon"><i class="fa fa-plus-square"></i></div>');
$('li#n-Modifica-una-voce a').prepend('<div id="drop-icon"><i class="fa fa-edit"></i></div>');
$('li#n-Property-chains-helper a').prepend('<div id="drop-icon"><i class="fa fa-search-plus"></i></div>');
$('li#n-Ricerca-semantica a').prepend('<div id="drop-icon"><i class="fa fa-search-plus"></i></div>');
$('li#n-Esplora-i-dati a').prepend('<div id="drop-icon"><i class="fa fa-filter"></i></div>');
$('li#n-Tabelle-dei-dati a').prepend('<div id="drop-icon"><i class="fa fa-table"></i></div>');
$('li#n-Grafici a').prepend('<div id="drop-icon"><i class="fa fa-pie-chart"></i></div>');
$('li#n-Audiogrammi a').prepend('<div id="drop-icon"><i class="fa fa-deaf"></i></div>');
$('li#n-Mappe a').prepend('<div id="drop-icon"><i class="fa fa-map-marker"></i></div>');
$('li#n-Esporta-i-dati a').prepend('<div id="drop-icon"><i class="fa fa-download"></i></div>');
$('li#n-Citazioni a').prepend('<div id="drop-icon"><i class="fa fa-book"></i></div>');

$('#searchInput').focusin(function () {
    $(this).val('~*term*');
 });