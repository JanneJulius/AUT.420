// Petri Kannisto
// TTI/AUT
// Tampere University of Technology
// Created: 7/2017
// Modified: 7/2017
// 
// Some code taken from the Java version:
// Server Side Events (SSE) in Java
// http://eranmedan.com/server-side-events-sse-in-java
// 
// See also:
// http://stackoverflow.com/questions/11077857/what-are-long-polling-websockets-server-sent-events-sse-and-comet


// Listening to server side events
function enableEventStream()
{
    var source = new EventSource('stream');
    
    source.onopen = function(event)
    {
        console.log("Eventsource opened!");
    };

    source.onmessage = function(event)
    {
    	try
        {
            var receivedPayload = event.data;
            
            // Removing surrounding quotes
            if (receivedPayload.length >= 2)
            {
                receivedPayload = receivedPayload.substring(1, receivedPayload.length - 1);
            }
            
            // Unescaping double quotes
            receivedPayload = receivedPayload.replace(/\\"+/g, '"');
            
            // Showing raw JSON received from simulation
            //document.getElementById('raw_json').innerHTML = receivedPayload;
            
            // Parsing JSON
            var parsedSimulationData = JSON.parse(receivedPayload);
            
            // Set tank liquid levels
            setLiquidLevel("t100", parsedSimulationData.T100_LEVEL);
            setLiquidLevel("t200", parsedSimulationData.T200_LEVEL);
            setLiquidLevel("t300", parsedSimulationData.T300_LEVEL);
            setLiquidLevel("t400", parsedSimulationData.T400_LEVEL);
            
            // Set on/off valve status
            // V101 is irrelevant in the simulation
            setOnOffValveStatus("v103", parsedSimulationData.V103);
            setOnOffValveStatus("v201", parsedSimulationData.V201);
            // V202 is irrelevant in the simulation
            // V203 is irrelevant in the simulation
            setOnOffValveStatus("v204", parsedSimulationData.V204);
            setOnOffValveStatus("v301", parsedSimulationData.V301);
            setOnOffValveStatus("v302", parsedSimulationData.V302);
            setOnOffValveStatus("v303", parsedSimulationData.V303);
            setOnOffValveStatus("v304", parsedSimulationData.V304);
            setOnOffValveStatus("v401", parsedSimulationData.V401);
            // V402 is irrelevant in the simulation
            // V403 is irrelevant in the simulation
            setOnOffValveStatus("v404", parsedSimulationData.V404);
            
            // Set control valve status
            setControlValveStatus("v102", parsedSimulationData.V102);
            setControlValveStatus("v104", parsedSimulationData.V104);
            
            // Set power equipment status
            setPumpOrHeaterStatus("e100", parsedSimulationData.E100);
            setPumpOrHeaterStatus("p100", parsedSimulationData.P100);
            setPumpOrHeaterStatus("p200", parsedSimulationData.P200);
            
            // Set sensor values
            setSensorValue("fi100", parsedSimulationData.FI100);
            setSensorValue("li100", parsedSimulationData.LI100);
            setSensorValue("ti100", parsedSimulationData.TI100);
            setSensorValue("li200", parsedSimulationData.LI200);
            setSensorValue("pi300", parsedSimulationData.PI300);
            setSensorValue("ti300", parsedSimulationData.TI300);
            setSensorValue("li400", parsedSimulationData.LI400);
            
            // Set signals or alarms
            setSignalOrAlarm("la_p_100", parsedSimulationData.LA_PLUS_100);
            setSignalOrAlarm("ls_m_200", parsedSimulationData.LS_MINUS_200);
            setSignalOrAlarm("ls_m_300", parsedSimulationData.LS_MINUS_300);
            setSignalOrAlarm("ls_p_300", parsedSimulationData.LS_PLUS_300);
        }
        catch (err)
        {
            // Showing error message
            document.getElementById('errorMsg').innerHTML = err;
        }
    };
}

function setSignalOrAlarm(name, sigStatus)
{
    var style = "initial";
    
	if (sigStatus.trim().toLowerCase() == "true")
    {
        style = "font-weight: bold; text-decoration: underline;";
    }
    
    document.getElementById(name).style = style;
}

function setSensorValue(name, sensorValue)
{
    if (sensorValue.length > 5)
    {
        // Limiting value length
        sensorValue = sensorValue.substr(0, 5);
    }
    
    document.getElementById(name).innerHTML = name.toUpperCase() + ": " + sensorValue;
}

function setPumpOrHeaterStatus(name, status)
{
    var color = "#ffffff";
    
    if (status.trim().toLowerCase() == "true")
    {
        color = "#e0a000";
    }
    
    document.getElementById(name).style = "background-color: " + color + ";"
}    

function setControlValveStatus(name, status)
{
    var color = "#ffffff";
    var statusInt = parseInt(status);
    
    if (statusInt > 0 && status <= 50)
	{
        // Making it yellow
        
        var colorComponentY = 200 - 4 * statusInt; // This will become zero at 50
    	color = "rgb(255, 255, " + colorComponentY + ")";
	}
    
	if (statusInt > 50)
    {
        // Making it green
        
        var colorComponentG = 255 - 4 * (statusInt - 50);
		color = "rgb(" + colorComponentG + ", 255, 0)";
    }
    
    document.getElementById(name).style = "background-color: " + color + ";"
}

function setOnOffValveStatus(name, status)
{
    var color = "#ffffff";
    
    if (status.trim().toLowerCase() == "true")
        {
            color = "#60c060";
        }
    
    document.getElementById(name).style = "background-color: " + color + ";"
}

function setLiquidLevel(tank, level)
{
    var levelNum = (1 - parseFloat(level)) * 100;
    document.getElementById(tank + "_level").style = "transform: translateY(" + levelNum + "%);"
    
    // style="transform: translateY(50%);"
}

window.addEventListener("load", enableEventStream);
