<!-- Widget:ShinyPlotSrv -->


<noinclude>

Usage examples:

* '''Pie charts''': <nowiki>{{#widget:ShinyPlotSrv
|prop1_label=Property|prop1_data=value1,value2,...
|prop2_label=|prop2_data=
|prop3_label=Classes|prop3_data=
|plot=piesSrvAPI|title=Distribution of the property}}
</nowiki>

* '''Bar charts''': <nowiki>{{#widget:ShinyPlotSrv
|prop1_label=Property|prop1_data=value1,value2,...
|prop2_label=|prop2_data=
|prop3_label=Classes|prop3_data=
|plot=hbarsSrvAPI|title=Distribution of the property}}
</nowiki>

* '''Histograms''': <nowiki>{{#widget:ShinyPlotSrv
|prop1_label=Property|prop1_data=class1,class2,...
|prop2_label=Classes|prop2_data=value1,value2,...
|prop3_label=Classes|prop3_data=
|plot=barsSrvAPI|title=Distribution of the property}}
</nowiki>

* '''Histograms''': <nowiki>{{#widget:ShinyPlotSrv 
|prop1_label=Property|prop1_data=value1,value2,...
|prop2_label=Classes|prop2_data=class1,class2,...
|plot=wordcloudsSrvAPI|title=Words distribution}}
</nowiki>

* '''Word clouds''': <nowiki>{{#widget:ShinyPlotSrv
|prop1_label=Property|prop1_data=
|prop2_label=Classes|prop2_data=
|prop3_label=Classes|prop3_data=
|plot=wordcloudsSrvAPI|title=Words distribution}}
</nowiki>

* '''Boxplots''': <nowiki>{{#widget:ShinyPlotSrv
|prop1_label=Class|prop1_data=class1,class2,...
|prop2_label=Property|prop2_data=value1,value2,...
|plot=boxplotsSrvAPI|title=Distribution of the properties}}
</nowiki>

* '''Survival curves''': <nowiki>{{#widget:ShinyPlot
|prop1_label=Duration|prop1_type=number|prop1_data=status1,status2,...
|prop2_label=Censored|prop2_type=boolean|prop2_data=value1,value2,...
|prop3_label=By|prop3_type=text|prop3_data=class1,class2,...
|plot=scurvesSrvAPI|title=Survival curves}}
</nowiki>

* '''Scatterplots''': <nowiki>{{#widget:ShinyPlotSrv
|prop1_label=Property1|prop1_data=class1,class2,...
|prop2_label=Property2|prop2_data=value1,value2,...
|prop3_label=Classes|prop3_data=
|plot=scatterplotsSrvAPI|title=Correlation between the properties}}
</nowiki>

</noinclude>

<includeonly>
  <label for="prop1"><!--{$prop1_label}--> </label><select id="prop1"></select><br />
  
  <!--{if $prop2_label ne ''}--><label for="prop2"><!--{$prop2_label}--> </label><select id="prop2"></select><br /><!--{/if}-->
  
  <!--{if $prop3_label ne ''}--><label for="prop3"><!--{$prop3_label}--> </label><select id="prop3"></select><br /><!--{/if}-->

  <!--{if $plot eq 'audiograms' || $plot eq 'audioplots'}--><label for="patient">Paziente </label><input type="text" value="Paziente 00001" id="patient" /><br /><!--{/if}-->
  
  <!--{if isset($visitCat)}--><label for="visitNumber">Visita </label><input type="number" value="1" id="visitNumber" /><br /><!--{/if}-->
  
  <input type="button" value="Plot" onclick="postReq()" />
 
  <hr />

  <iframe id="plot" style="width: 800px; height: 600px; border:none"></iframe>

  <script type="text/javascript">
    function createSelect(id, inputData)
    {
        var split = inputData.split(',');
        var select = document.getElementById(id);
        split.forEach(function(value, index){
           var opt = document.createElement('option');
           opt.value = value;
           opt.innerHTML = value;
           select.appendChild(opt);
        });
    }
 
    function postReq()
    {
        debugger;

        var plot = '<!--{$plot}-->';

        var prop1 = document.getElementById('prop1').value;

        var prop2 = '';
        <!--{if $prop2_label ne ''}-->
        var prop2 = document.getElementById('prop2').value;
        <!--{/if}-->

        var prop3 = '';
        <!--{if $prop3_label ne ''}-->
        var prop3 = document.getElementById('prop3').value;
        <!--{/if}-->
        
        var patient = '';
        <!--{if $plot eq 'audiograms' || $plot eq 'audioplots'}-->
        patient = document.getElementById('patient').value;
        <!--{/if}-->

        var visitNumber = 0;
        var visitCat = '';
        var numberLabel = '';
        <!--{if isset($numberLabel)}-->
        visitNumber = document.getElementById('visitNumber').value;
        visitCat = '<!--{$visitCat}-->';
        numberLabel = '<!--{$numberLabel}-->';
        <!--{/if}-->

        var params = '';
        params = "&prop1=" + prop1;
        if (prop2 !== '')
            params += "&prop2=" + prop2;
        else if (visitNumber)
            params += "&prop2='visita'"

        if (prop3 !== '')
            params += "&prop3=" + prop3;
        params += '&labels=<!--{$labels|default: ''}-->';

        var path = mw.config.get('wgScriptPath');
        var sServer = mw.config.get('sServer').IP;
        document.getElementById('plot').setAttribute('src', sServer + "/<!--{$plot}-->/?title=<!--{$title}--> '" + prop1 + "'&cat=$1" + params + "&path=" + path);
    }

    createSelect('prop1', '<!--{$prop1_data}-->');

    <!--{if $prop2_label ne ''}-->
    createSelect('prop2', '<!--{$prop2_data}-->');
    <!--{/if}-->

    <!--{if $prop3_label ne ''}-->
    createSelect('prop3', '<!--{$prop3_data}-->');
    <!--{/if}-->
  </script>
</includeonly>
