<!-- Widget:Timeline -->


<noinclude>

Usage examples:

* <nowiki>{{#widget:Timeline
|prop1_label=Property|prop1_type=date|prop1_data=value1,value2,...
|prop2_label=|prop2_type=|prop2_data=value1,value2,...
|title=Distribution of the property}}
</nowiki>

<hr />

[[Visible to::whitelist|'''Visible to: ''']]
[[Visible to group::viewers|viewers]]
[[Visible to group::editors|editors]]

[[Editable by::whitelist|'''Editable by: ''']]
[[Editable by group::editors|editors]]

</noinclude>

<includeonly>
  <label for="prop1"><!--{$prop1_label}--> </label><select id="prop1"></select><br />
  
  <!--{if $prop2_label ne ''}--><label for="prop2"><!--{$prop2_label}--> </label><select id="prop2"></select><br /><!--{/if}-->
  
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

        var prop1 = document.getElementById('prop1').value;

        var prop2 = '';
        <!--{if $prop2_label ne ''}-->
        var prop2 = document.getElementById('prop2').value;
        <!--{/if}-->

        var params = '';

        mw.loader.using( 'ext.smw.api', function() {
            var smwApi = new smw.api();

            // Transform raw data into a query string
            // @param {array} printouts - example: ['?Capital of', '?Population']
            // @param {object} parameters - example: {'limit': 100}
            // @param {array|string} conditions - example: ['[[Category:Cities]]']
            var printouts = ['?' + prop1];
            if (prop2 !== '')
                printouts.push('?' + prop2);
            var parameters = {'limit': 500};
            var conditions = ['[[Category:$2]]'];

            document.getElementsByTagName("body")[0].style.cursor = "progress";
            var query1 = new smw.query( printouts, parameters, conditions ).toString();
            console.log('Query: ' + query1);

            // Create a link to the Special:Ask query
            var link1 = new smw.query( printouts, parameters, conditions ).getLink();
            console.log('Link 1: ' + link1);

            // Fetch data via Ajax/SMWAPI
            smwApi.fetch( query1 )
                .done( function ( result1 ) {
                    debugger;
                    console.log('Results: ' + JSON.stringify(result1));
                    document.getElementsByTagName("body")[0].style.cursor = "default";
                  var shinyInput = [];
                  var res1 = result1.query.results;
                  if (prop2 === '') { // no prop2: one data series
                      for (x in res1) {
                          var p1 = res1[x]['printouts'][prop1];
                          if (p1.length == undefined)
                              shinyInput.push(p1[0].<!--{$prop1_type|default: text}-->);
                          else
                              shinyInput.push('NA');
                      }
                      if (shinyInput[0].indexOf(',') >= 0) {
                          shinyInput = shinyInput[0].split(',').map(function (x) {
                              return (x != '') ? x : 'NA'
                          }).join(',');
                      }
                      console.log(shinyInput);

                      document.getElementById('plot').setAttribute('src', "http://$1:3838/timelines/?title=<!--{$title}--> '" + prop1 + "'" + params + "'&data=" + shinyInput);
                  }
                  else { // two data series
                    document.getElementsByTagName("body")[0].style.cursor = "default";
                        for(x in res1) {
                            var p1 = res1[x]['printouts'][prop1];
                            var p2 = res1[x]['printouts'][prop2];
                            var value1 = 'NA', value2 = 'NA';
                            if (p1.length == undefined)
                                value1 = p1[0].<!--{$prop1_type|default: text}-->;
                            if (p2.length == undefined)
                                value2 = p2[0].<!--{$prop2_type|default: text}-->;
                            shinyInput.push(value1 + ',' + value2);
                        }
                        shinyInput = shinyInput.join(';');
                        console.log(shinyInput);

                        document.getElementById('plot').setAttribute('src', "http://$1:3838/timelines/?title=<!--{$title}--> '" + prop1 + " vs. " + prop2 + "'" + params + "'&data=" + shinyInput);
                      }
                })
                .fail( function ( error ) {
                    // Do something
                    document.getElementsByTagName("body")[0].style.cursor = "default";
                });
        });

    }

    createSelect('prop1', '<!--{$prop1_data}-->');

    <!--{if $prop2_label ne ''}-->
    createSelect('prop2', '<!--{$prop2_data}-->');
    <!--{/if}-->

  </script>
</includeonly>
