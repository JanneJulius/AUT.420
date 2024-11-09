// Petri Kannisto
// TTI/AUT
// Tampere University of Technology
// Created: 7/2017
// Modified: 7/2017

/**
 * Equipment server module
 */

// Creating a Simple Server
// https://github.com/node-opcua/node-opcua/blob/master/documentation/sample_server.js
// https://github.com/node-opcua/node-opcua/blob/master/documentation/creating_a_server.md
var opcuaModule = require("node-opcua");

var Constants = require("./Constants");
var EquipmentServer = require("./EquipmentServer");

/**
 * Implements an OPC UA server for the equipment.
 */
function UaServer(eqServer)
{
	var UA_SERVER_URN = "urn:CX-19788E:BeckhoffAutomation:TcOpcUaServer:1";
	var UA_PLC1_URN = "urn:CX-19788E:BeckhoffAutomation:Ua:PLC1";
	
	var self = this;
	var m_equipmentServer = eqServer;
	var m_addressSpace;
	
	var m_opcUaServer = new opcuaModule.OPCUAServer({
	    port: 8087, // the port of the listening socket of the server
	    //resourcePath: "UA/BatchSimulationServer", // this path will be added to the endpoint resource name
	    resourcePath: "", // this path will be added to the endpoint resource name
	    buildInfo : {
	        productName: "BatchSimulationServer",
	        buildNumber: "1",
	        buildDate: new Date(2017, 5, 17)
	    },
	    serverInfo : {
	    	applicationUri: UA_SERVER_URN // This will appear as the default namespace
	    }
	});
	
	this.private_addUaSimpleDevice = function(parent, name)
	{
        var value =
        {
            get: function ()
            {
            	return new opcuaModule.Variant({dataType: opcuaModule.DataType.Boolean, value: m_equipmentServer.getSimpleDeviceStatus(name) });
            },
    		set: function (variantIn)
    		{
    			m_equipmentServer.setSimpleDeviceStatus(name, variantIn.value);
            }
        };
        
        this.private_addUaVariable(parent, name, "Boolean", value);
	};
	
	this.private_addUaProportionalDevice = function(parent, name)
	{
        var value =
        {
            get: function ()
            {
            	return new opcuaModule.Variant({dataType: opcuaModule.DataType.UInt16, value: m_equipmentServer.getProportionalDeviceStatus(name) });
            },
    		set: function (variantIn)
    		{
    			m_equipmentServer.setProportionalDeviceStatus(name, variantIn.value);
            }
        };
        
        this.private_addUaVariable(parent, name, "UInt16", value);
	};
	
	this.private_addUaIntegerIndicator = function(parent, name)
	{
        var value =
        {
            get: function ()
            {
            	var retValue = m_equipmentServer.getSensorOrIndicatorValue(name);
            	return new opcuaModule.Variant({dataType: opcuaModule.DataType.UInt16, value: retValue });
            }
        };
        
        this.private_addUaVariable(parent, name, "UInt16", value);
	};
	
	this.private_addUaFloatIndicator = function(parent, name)
	{
        var value = {
            get: function ()
            {
            	var retValue = m_equipmentServer.getSensorOrIndicatorValue(name);
            	return new opcuaModule.Variant({dataType: opcuaModule.DataType.Double, value: retValue });
            }
        };
		
        this.private_addUaVariable(parent, name, "Double", value);
	};
	
	this.private_addUaSignal = function(parent, name)
	{
		var value = {
            get: function ()
            {
            	var retValue = m_equipmentServer.getSignalState(name);
            	return new opcuaModule.Variant({dataType: opcuaModule.DataType.Boolean, value: retValue });
            }
        };
		
		this.private_addUaVariable(parent, name, "Boolean", value);
	};
	
	this.private_addUaVariable = function(parent, name, dataType, value)
	{
		var namespaceIndex = m_addressSpace.getNamespaceIndex(UA_PLC1_URN);
		var displayName = "EQ_" + name;
		var nodeId = self.private_createNodeIdSerializedString(namespaceIndex, "eq_states." + displayName);
		var browseName = self.private_createBrowseNameSerializedString(namespaceIndex, displayName);
		
		m_addressSpace.addVariable({
            componentOf: parent,
            browseName: browseName,
            displayName: displayName,
            dataType: dataType,
            nodeId: nodeId,
            typeDefinition: "DataItemType",
            value: value
        });
	};
	
	this.private_createNodeIdSerializedString = function(namespaceIndex, browseName)
	{
		// Integer node ID: "ns=0;i=62"
		// String node ID: "ns=0;s=foo"
		return "ns=" + namespaceIndex + ";s=" + browseName;
	};
	
	this.private_createBrowseNameSerializedString = function(namespaceIndex, browseName)
	{
		return namespaceIndex + ":" + browseName;
	};
	
	this.private_postInitialize = function()
	{
	    console.log("OPC UA server initialized");
	    
    	m_addressSpace = m_opcUaServer.engine.addressSpace;
        
        // Registering PLC1 namespace
        m_addressSpace.registerNamespace(UA_PLC1_URN);
        
        // Resolving namespace indices
        var uaServerNamespaceIndex = m_addressSpace.getNamespaceIndex(UA_SERVER_URN); 
        var uaPlc1NamespaceIndex = m_addressSpace.getNamespaceIndex(UA_PLC1_URN);
        
        // Declaring PLC1
        var plc1Name = "PLC1";
        var plc1 = m_addressSpace.addObject({
            organizedBy: m_addressSpace.rootFolder.objects,
            browseName: self.private_createBrowseNameSerializedString(uaPlc1NamespaceIndex, plc1Name),
            nodeId: self.private_createNodeIdSerializedString(uaServerNamespaceIndex, plc1Name),
            displayName: plc1Name
        });
        
        // Declaring process parts
        var eqStatesName = "eq_states";
        var eqStates = m_addressSpace.addObject({
        	componentOf: plc1,
    	    browseName: self.private_createBrowseNameSerializedString(uaPlc1NamespaceIndex, eqStatesName),
    	    displayName: eqStatesName,
    	    nodeId: self.private_createNodeIdSerializedString(uaPlc1NamespaceIndex, eqStatesName),
    	    typeDefinition: "FolderType",
    	});
        
        // Declaring valves
        self.private_addUaSimpleDevice(eqStates, Constants.VALVE_101);
        self.private_addUaProportionalDevice(eqStates, Constants.VALVE_102);
        self.private_addUaSimpleDevice(eqStates, Constants.VALVE_103);
        self.private_addUaProportionalDevice(eqStates, Constants.VALVE_104);
        self.private_addUaSimpleDevice(eqStates, Constants.VALVE_201);
        self.private_addUaSimpleDevice(eqStates, Constants.VALVE_202);
        self.private_addUaSimpleDevice(eqStates, Constants.VALVE_203);
        self.private_addUaSimpleDevice(eqStates, Constants.VALVE_204);
        self.private_addUaSimpleDevice(eqStates, Constants.VALVE_301);
        self.private_addUaSimpleDevice(eqStates, Constants.VALVE_302);
        self.private_addUaSimpleDevice(eqStates, Constants.VALVE_303);
        self.private_addUaSimpleDevice(eqStates, Constants.VALVE_304);
        self.private_addUaSimpleDevice(eqStates, Constants.VALVE_401);
        self.private_addUaSimpleDevice(eqStates, Constants.VALVE_402);
        self.private_addUaSimpleDevice(eqStates, Constants.VALVE_403);
        self.private_addUaSimpleDevice(eqStates, Constants.VALVE_404);

        // Declaring heater
        self.private_addUaSimpleDevice(eqStates, Constants.E100);

        // Declaring pumps and pumps preset
        self.private_addUaProportionalDevice(eqStates, Constants.PUMP_1);
        self.private_addUaProportionalDevice(eqStates, Constants.PUMP_2);
        self.private_addUaSimpleDevice(eqStates, Constants.PUMPS_PRESET);

        // Declaring indicators
        self.private_addUaIntegerIndicator(eqStates, Constants.LI100);
        self.private_addUaIntegerIndicator(eqStates, Constants.LI200);
        self.private_addUaIntegerIndicator(eqStates, Constants.LI400);
        self.private_addUaIntegerIndicator(eqStates, Constants.PI300);
        self.private_addUaFloatIndicator(eqStates, Constants.TI100);
        self.private_addUaFloatIndicator(eqStates, Constants.TI300);
        self.private_addUaFloatIndicator(eqStates, Constants.FI100);

        // Declaring signals
        self.private_addUaSignal(eqStates, Constants.LA_PLUS_100);
        self.private_addUaSignal(eqStates, Constants.LS_MINUS_200);
        self.private_addUaSignal(eqStates, Constants.LS_MINUS_300);
        self.private_addUaSignal(eqStates, Constants.LS_PLUS_300);
	    
	    m_opcUaServer.start(function()
	    {
	        console.log("OPC UA server is now listening...");
	        console.log("OPC UA server port is ", m_opcUaServer.endpoints[0].port);
	        var endpointUrl = m_opcUaServer.endpoints[0].endpointDescriptions()[0].endpointUrl;
	        console.log("OPC UA server: the primary server endpoint url is ", endpointUrl );
	    });
	};

	m_opcUaServer.initialize(this.private_postInitialize);
}

module.exports = UaServer;
