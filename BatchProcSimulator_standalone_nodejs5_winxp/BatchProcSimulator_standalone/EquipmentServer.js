// Petri Kannisto
// TTI/AUT
// Tampere University of Technology
// Created: 5/2017
// Modified: 7/2017

/**
 * Equipment server module
 */

var Constants = require("./Constants");
var PowerEquipmentManager = require("./PowerEquipmentManager");
var Tank = require("./Tank");
var TankFlowManager = require("./TankFlowManager");
var ValveManager = require("./ValveManager");

/**
 * Represents an equipment server.
 */
function EquipmentServer()
{
	var self = this;
	var m_powerEqManager;
	var m_valveManager;
	var m_tankFlowManager;
	var m_tank1;
	var m_tank2;
	var m_tank3;
	var m_tank4;
	
	this.private_instantiateObjects = function()
	{
		// This function is called in reset, thus a function
		
		// Instantiating simulation model classes
    	m_powerEqManager = new PowerEquipmentManager();
    	m_valveManager = new ValveManager();
    	m_tankFlowManager = new TankFlowManager(m_powerEqManager, m_valveManager);
    	m_tank1 = new Tank(Constants.TANK_1, m_tankFlowManager, m_valveManager, m_powerEqManager);
    	m_tank2 = new Tank(Constants.TANK_2, m_tankFlowManager, m_valveManager, m_powerEqManager);
    	m_tank3 = new Tank(Constants.TANK_3, m_tankFlowManager, m_valveManager, m_powerEqManager);
    	m_tank4 = new Tank(Constants.TANK_4, m_tankFlowManager, m_valveManager, m_powerEqManager);
	};
	
	self.private_instantiateObjects();
	
	/**
	 * Gets the value of a sensor or indicator.
	 */
	this.getSensorOrIndicatorValue = function(name)
	{
		switch (name)
    	{
    		case Constants.FI100:
    		case Constants.LI100:
        	case Constants.TI100:
        		return m_tank1.getSensorValue(name);
        		
        	case Constants.LI200:
        		return m_tank2.getSensorValue(name);
        	
        	case Constants.PI300:
        	case Constants.TI300:
        		return m_tank3.getSensorValue(name);
        		
        	case Constants.LI400:
        		return m_tank4.getSensorValue(name);
        		
    		default:
    			throw "Unknown sensor or indicator \"" + name + "\"";
    	}
	};
	
	/**
	 * Gets the state of a signal.
	 */
	this.getSignalState = function(name)
	{
		switch (name)
    	{
			case Constants.LA_PLUS_100:
				return m_tank1.getSignalState(name);
				
			case Constants.LS_MINUS_200:
				return m_tank2.getSignalState(name);
				
			case Constants.LS_MINUS_300:
			case Constants.LS_PLUS_300:
    			return m_tank3.getSignalState(name);
				
    		default:
    			throw "Unknown signal \"" + name + "\"";
    	}
	};
	
	/**
	 * Gets the status of a simple device.
	 */
	this.getSimpleDeviceStatus = function(name)
	{
		switch (name)
		{
			case Constants.E100:
				return m_tank1.getHeaterState();
				
			case Constants.PUMPS_PRESET:
				return m_powerEqManager.getEquipmentState(Constants.PUMPS_PRESET);
				
			default:
				break;
		}
		
		if (name.startsWith("V"))
		{
			return m_valveManager.getOnOffValvePosition(name);
		}
		else
		{
			throw "Unknown simple device \"" + name + "\"";
		}
		
		return false;
	};
	
	/**
	 * Sets the status of a simple device.
	 */
	this.setSimpleDeviceStatus = function(name, status)
	{
		switch (name)
		{
			case Constants.E100:
				m_tank1.setHeaterState(status);
				return;
				
			case Constants.PUMPS_PRESET:
				m_powerEqManager.setEquipmentState(Constants.PUMPS_PRESET, status);
				return;
				
			default:
				break;
		}
		
		if (name.startsWith("V"))
		{
			m_valveManager.setOnOffValvePosition(name, status);
			return;
		}
		else
		{
			throw "Unknown simple device \"" + name + "\"";
		}
	};
	
	/**
	 * Gets the status of a proportional device.
	 */
	this.getProportionalDeviceStatus = function(name)
	{
		if (name.startsWith("P")) // Pump?
		{
			return m_powerEqManager.getEquipmentState(name) ? 100 : 0;
		}
		if (name.startsWith("V")) // Valve?
		{
			return m_valveManager.getControlValvePosition(name);
		}
		else
		{
			throw "Unknown proportional device \"" + name + "\"";
		}
	};
	
	/**
	 * Sets the status of a proportional device.
	 */
	this.setProportionalDeviceStatus = function(name, status)
	{
		if (name.startsWith("P")) // Pump?
		{
			var valueIn = status > 30 ? true : false;
			m_powerEqManager.setEquipmentState(name, valueIn);
			
			return m_powerEqManager.getEquipmentState(name) ? 100 : 0;
		}
		if (name.startsWith("V")) // Valve?
		{
			var toBeAssigned = 0;
			
			// Checking input validity
			if (status > 100) { toBeAssigned = 0; }
			else if (status < 0) { toBeAssigned = 0; }
			else { toBeAssigned = status; }
			
			m_valveManager.setControlValvePosition(name, toBeAssigned);
		}
		else
		{
			throw "Unknown proportional device \"" + name + "\"";
		}
	};
	
	/**
	 * Serializes simulation state so it can be shown in the UI.
	 */
	this.serialize = function()
    {
		// Concatenating model state in a JSON string
    	var serialized = "{" +
			
    		//this.private_createSerializationItem("valve_comb", m_valveManager.getValveCombination(), ", ") + // This is just for debugging
			this.private_createSerializationItem("T100_LEVEL", m_tank1.getLiquidLevel_percent(), ", ") +
			this.private_createSerializationItem("T200_LEVEL", m_tank2.getLiquidLevel_percent(), ", ") +
			this.private_createSerializationItem("T300_LEVEL", m_tank3.getLiquidLevel_percent(), ", ") +
			this.private_createSerializationItem("T400_LEVEL", m_tank4.getLiquidLevel_percent(), ", ") +
			
			// Control valves
			this.private_createSerializationItem(Constants.VALVE_102, m_valveManager.getControlValvePosition(Constants.VALVE_102), ", ") +
			this.private_createSerializationItem(Constants.VALVE_104, m_valveManager.getControlValvePosition(Constants.VALVE_104), ", ") +
			
			// On/off valves
			// V101 is irrelevant in the simulation
			this.private_createOnOffValveSerializationString(Constants.VALVE_103) + 
			this.private_createOnOffValveSerializationString(Constants.VALVE_201) + 
			// V202 is irrelevant in the simulation
			// V203 is irrelevant in the simulation 
			this.private_createOnOffValveSerializationString(Constants.VALVE_204) + 
			this.private_createOnOffValveSerializationString(Constants.VALVE_301) + 
			this.private_createOnOffValveSerializationString(Constants.VALVE_302) + 
			this.private_createOnOffValveSerializationString(Constants.VALVE_303) + 
			this.private_createOnOffValveSerializationString(Constants.VALVE_304) + 
			this.private_createOnOffValveSerializationString(Constants.VALVE_401) + 
			// V402 is irrelevant in the simulation 
			// V403 is irrelevant in the simulation 
			this.private_createOnOffValveSerializationString(Constants.VALVE_404) +
			
			// Sensor values
			this.private_createSensorIntegerSerializationString(Constants.LI100) + 
			this.private_createSensorIntegerSerializationString(Constants.LI200) + 
			this.private_createSensorIntegerSerializationString(Constants.LI400) + 
			this.private_createSensorIntegerSerializationString(Constants.PI300) + 
			this.private_createSerializationItem(Constants.FI100, m_tank1.getSensorValue(Constants.FI100), ", ") +
			this.private_createSerializationItem(Constants.TI100, m_tank1.getSensorValue(Constants.TI100), ", ") +
			this.private_createSerializationItem(Constants.TI300, m_tank3.getSensorValue(Constants.TI300), ", ") +
			
			// Alarms and signals
			this.private_createSignalStateSerializationString(Constants.LA_PLUS_100) +
			this.private_createSignalStateSerializationString(Constants.LS_MINUS_200) +
			this.private_createSignalStateSerializationString(Constants.LS_MINUS_300) +
			this.private_createSignalStateSerializationString(Constants.LS_PLUS_300) +
			
			// Power equipment states
			this.private_createPumpSerializationString(Constants.PUMP_1) +
			this.private_createPumpSerializationString(Constants.PUMP_2) +
			this.private_createSerializationItem(Constants.E100, m_tank1.getHeaterState(), "") +
			
    	"}";

    	return serialized;
    };
    
    /**
     * Runs one simulation step.
     */
    this.runSimulation = function()
    {
    	m_tank1.runSimulation();
    	m_tank2.runSimulation();
    	m_tank3.runSimulation();
    	m_tank4.runSimulation();
    };
    
    /**
     * Reset the simulation.
     */
    this.reset = function()
    {
    	self.private_instantiateObjects();
    };
    
    this.private_createOnOffValveSerializationString = function(name)
    {
    	return this.private_createSerializationItem(name, m_valveManager.getOnOffValvePosition(name), ", ");
    };
    
    this.private_createPumpSerializationString = function(name)
    {
    	var pumpOn = m_powerEqManager.getEquipmentState(name);
    	var presetOn = m_powerEqManager.getEquipmentState(Constants.PUMPS_PRESET);
    	
    	return this.private_createSerializationItem(name, (pumpOn && presetOn), ", ");
    };
    
    this.private_createSensorIntegerSerializationString = function(name)
    {
    	var value = Math.round(this.getSensorOrIndicatorValue(name));
    	return this.private_createSerializationItem(name, value, ", ");
    };
    
    this.private_createSignalStateSerializationString = function(name)
    {
    	var value = this.getSignalState(name);
    	return this.private_createSerializationItem(name, value, ", ");
    };
    
    this.private_createSerializationItem = function(itemName, itemValue, endString)
    {
    	return '"' + itemName + '":"' + itemValue + '"' + endString;
    };
}

module.exports = EquipmentServer;
