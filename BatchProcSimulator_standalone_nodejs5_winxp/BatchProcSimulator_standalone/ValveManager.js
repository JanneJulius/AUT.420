// Petri Kannisto
// TTI/AUT
// Tampere University of Technology
// Created: 6/2017
// Modified: 7/2017

var Constants = require("./Constants");
var MySet = require("./MySet");

/**
 * Holds the valves in the process.
 */
function ValveManager()
{
	// Used to tune throttling calculation.
	// If you change this, check its effects using "Tilavuusvirtamalli.xlsx"!
	// Also update unit tests PowerEquipmentManager and ValveManager accordingly.
	var THROTTLE_CONSTANT = 0.08;
	
	var self = this;
	
	var m_allOnOffValves = new MySet([
        Constants.VALVE_101,
        Constants.VALVE_103,
        Constants.VALVE_201,
        Constants.VALVE_202,
        Constants.VALVE_203,
        Constants.VALVE_204,
        Constants.VALVE_301,
        Constants.VALVE_302,
        Constants.VALVE_303,
        Constants.VALVE_304,
        Constants.VALVE_401,
        Constants.VALVE_402,
        Constants.VALVE_403,
        Constants.VALVE_404
    ]);
	
	// 0..100; 0 is full closed, 100 is full open
	var m_controlValvePositions = {};
	m_controlValvePositions[Constants.VALVE_102] = 0;
	m_controlValvePositions[Constants.VALVE_104] = 0;
	
	// Initially, there are no open valves so the set is empty.
	var m_currentlyOpenValves = new MySet();
	
	var m_currentValveCombination = Constants.VALVECOMBINATION_NO_FLOW; 
	
	// Creating sets to recognise valve combinations
	var set_T200_T300 = new MySet([
		Constants.VALVE_201,
		Constants.VALVE_204,
		Constants.VALVE_301,
		Constants.VALVE_303
	]);
	var set_T400_T300_T200 = new MySet([
   		Constants.VALVE_204,
   		Constants.VALVE_301,
   		Constants.VALVE_303,
   		Constants.VALVE_404
   	]);
	var set_T100_T300_T400 = new MySet([
  		Constants.VALVE_102,
  		Constants.VALVE_301,
  		Constants.VALVE_304,
  		Constants.VALVE_401
  	]);
	var set_T100_T300_CIRCULAR = new MySet([
  		Constants.VALVE_102,
  		Constants.VALVE_104,
  		Constants.VALVE_301,
  		Constants.VALVE_304
  	]);
	var set_T300_T100 = new MySet([
  		Constants.VALVE_103,
  		Constants.VALVE_204,
  		Constants.VALVE_302,
  		Constants.VALVE_303
  	]);
	
	var m_knownValveCombinations = {};
	m_knownValveCombinations[Constants.VALVECOMBINATION_T200_T300] = set_T200_T300;
	m_knownValveCombinations[Constants.VALVECOMBINATION_T400_T300_T200] = set_T400_T300_T200;
	m_knownValveCombinations[Constants.VALVECOMBINATION_T100_T300_T400] = set_T100_T300_T400;
	m_knownValveCombinations[Constants.VALVECOMBINATION_T100_T300_CIRCULAR] = set_T100_T300_CIRCULAR;
	m_knownValveCombinations[Constants.VALVECOMBINATION_T300_T100] = set_T300_T100;
	
	
	/**
	 * Returns the position of an on/off valve.
	 */
	this.getOnOffValvePosition = function(name)
	{
		if (!m_allOnOffValves.contains(name))
		{
			self.private_logAndReturnError("There is no on/off valve called \"" + name + "\"");
		}
		
		return m_currentlyOpenValves.contains(name);
	};
	
	/**
	 * Sets the position of an on/off valve.
	 */
	this.setOnOffValvePosition = function(name, value)
	{
		if (!m_allOnOffValves.contains(name)) 
		{
			self.private_logAndReturnError("There is no on/off valve called \"" + name + "\"");
		}
		
		if (typeof(value) !== "boolean")
		{
			self.private_logAndReturnError("On/off valve position must be of type boolean");
		}
		
		if (value)
		{
			m_currentlyOpenValves.add(name);
		}
		else
		{
			m_currentlyOpenValves.remove(name);
		}
		
		// Resolving valve combination
		self.private_resolveValveCombination();
	};
	
	/**
	 * Returns the position of a control valve.
	 */
	this.getControlValvePosition = function(name)
	{
		if (!m_controlValvePositions.hasOwnProperty(name))
		{
			self.private_logAndReturnError("There is no control valve called \"" + name + "\"");
		}
		
		return m_controlValvePositions[name];
	};
	
	/**
	 * Sets the position of a control valve.
	 */
	this.setControlValvePosition = function(name, value)
	{
		if (!m_controlValvePositions.hasOwnProperty(name))
		{
			self.private_logAndReturnError("There is no control valve called \"" + name + "\"");
		}
		
		if (typeof(value) !== "number" || value < 0 || value > 100)
		{
			self.private_logAndReturnError("Control valve position must be a number within [0, 100]");
		}
		
		if (name === Constants.VALVE_102)
		{
			// In simulation, V102 is on/off
			value = value > 0 ? 100 : 0;
		}
		
		m_controlValvePositions[name] = value;
		
		if (value > 0)
		{
			m_currentlyOpenValves.add(name);
		}
		else
		{
			m_currentlyOpenValves.remove(name);
		}
		
		// Resolving valve combination
		self.private_resolveValveCombination();
	};
	
	/**
	 * Calculates the Y factor regarding the current throttling position of a control valve.
	 */
	this.calculateYFactorForThrottling = function(name)
	{
		if (!m_controlValvePositions.hasOwnProperty(name))
		{
			self.private_logAndReturnError("There is no control valve called \"" + name + "\"");
		}
		
		// Throttle value is the complement of an opening value
		var throttleValue = 1 - self.getControlValvePosition(name) / 100;
		
		// Setting limits
		if (throttleValue < 0)
		{
			throttleValue = 0;
		}
		else if (throttleValue > 1)
		{
			throttleValue = 1;
		}
		
		//       a * k
		// y = ---------
		//       1 - k
		
		// There could be division by zero
		if (1 - throttleValue < 0.0001)
		{
			// "The largest y value"
			return 99999;
		}
		else
		{
			return THROTTLE_CONSTANT * throttleValue / (1 - throttleValue);
		}
	};
	
	/**
	 * Returns the name of the current valve opening combination.
	 */
	this.getValveCombination = function()
	{
		return m_currentValveCombination;
	};
	
	this.private_resolveValveCombination = function()
	{
		var currentValveCombinationTemp = Constants.VALVECOMBINATION_NO_FLOW;
		
		// Comparing current valve combination to all known combinations
		for (var key in m_knownValveCombinations)
		{
			if (m_knownValveCombinations.hasOwnProperty(key))
			{
				var set1 = m_knownValveCombinations[key];
				
				if (set1.equalsOtherSet(m_currentlyOpenValves))
				{
					// Assigning valve combination identifier
					currentValveCombinationTemp = key;
					break;
				}
			}
		}
		
		m_currentValveCombination = currentValveCombinationTemp;
	};
	
	this.private_logAndReturnError = function(err)
	{
		console.error(err);
		throw err;
	};
}

module.exports = ValveManager;
