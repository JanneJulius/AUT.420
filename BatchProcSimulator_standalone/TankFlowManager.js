// Petri Kannisto
// TTI/AUT
// Tampere University of Technology
// Created: 6/2017
// Modified: 6/2017

var Constants = require("./Constants");
var MySet = require("./MySet");

/**
 * Manages the flow from and to tanks.
 */
function TankFlowManager(pumpM, valveM)
{
	var self = this;
	var m_pumpManager = pumpM;
	var m_valveManager = valveM;
	var m_pressureTankIsFull = false;
	var m_pressureTankFlowIn_l_min = 0;
	var m_pressureTankFlowOut_l_min = 0;
	var m_temperatures = {};
	m_temperatures[Constants.TANK_1] = 20;
	m_temperatures[Constants.TANK_2] = 20;
	m_temperatures[Constants.TANK_3] = 20;
	m_temperatures[Constants.TANK_4] = 20;
	var m_emptyTanks = new MySet();
	
	
	/**
	 * Sets the flag whether the pressure tank is full.
	 */
	this.setPressureTankIsFull = function(full)
	{
		if (typeof(full) !== "boolean")
		{
			self.private_logAndReturnError("'Pressure tank full' flag must be of type boolean");
		}
		
		m_pressureTankIsFull = full;
	};
	
	/**
	 * Sets a flag whether a tank is empty.
	 */
	this.setTankIsEmpty = function(name, empty)
	{
		if (typeof(empty) !== "boolean")
		{
			self.private_logAndReturnError("'Tank is empty' flag must be of type boolean");
		}
		
		if (empty)
		{
			m_emptyTanks.add(name);
		}
		else
		{
			m_emptyTanks.remove(name);
		}
	};
	
	/**
	 * Gets the temperature of the liquid flow into a tank.
	 */
	this.getTemperatureOfFlowIn_C = function(tank)
	{
		var flowIn = self.calculateFlowIn_l_min(tank);
		
		if (flowIn <= 0.0001)
		{
			self.private_logAndReturnError("Cannot get temperature in: tank \"" + tank + "\" has no flow in");
		}
		
		var valveCombination = m_valveManager.getValveCombination();
		
		switch (tank)
		{
		case Constants.TANK_1:
		case Constants.TANK_2:
		case Constants.TANK_4:
			
			// Flow in can only be from T300; this has already been checked in calculateFlowIn_l_min
			return m_temperatures[Constants.TANK_3];
			
		case Constants.TANK_3:
			
			if (valveCombination === Constants.VALVECOMBINATION_T200_T300)
			{
				return m_temperatures[Constants.TANK_2];
			}
			else if (valveCombination === Constants.VALVECOMBINATION_T400_T300_T200)
			{
				return m_temperatures[Constants.TANK_4];
			}
			else if (valveCombination === Constants.VALVECOMBINATION_T100_T300_T400 ||
					valveCombination === Constants.VALVECOMBINATION_T100_T300_CIRCULAR)
			{
				return m_temperatures[Constants.TANK_1];
			}
			
			self.private_logAndReturnError("Unexpected valve combination \"" + valveCombination + "\"");
			break;
			
		default:
			
			self.private_logAndReturnError("Cannot calculate temperature flow into tank \"" + tank + "\"");
		}
	};
	
	/**
	 * Sets the liquid temperature in a tank.
	 */
	this.setTemperature_C = function(tank, temp)
	{
		if (!m_temperatures.hasOwnProperty(tank))
		{
			self.private_logAndReturnError("There is no tank called \"" + tank + "\"");
		}
		
		if (typeof(temp) !== "number")
		{
			self.private_logAndReturnError("Temperature must be of type number");
		}
		
		m_temperatures[tank] = temp;
	};
	
	/**
	 * Calculates the current flow into a tank.
	 */
	this.calculateFlowIn_l_min = function(tank)
	{
		switch (tank)
		{
		case Constants.TANK_1:
			
			return self.private_calcFlowInTank1();
			
		case Constants.TANK_2:
			
			if (m_valveManager.getValveCombination() !== Constants.VALVECOMBINATION_T400_T300_T200 ||
				!m_pressureTankIsFull || self.private_sourceTankIsEmpty())
			{
				// No flow in with the current valve combination, pressure tank is not full or source tank is empty
				return 0;
			}
			
			return m_pumpManager.calculateFlow_l_min(Constants.PUMP_2, 0);
			
		case Constants.TANK_3:
			
			return self.private_calcFlowInTank3();
			
		case Constants.TANK_4:
			
			if (m_valveManager.getValveCombination() !== Constants.VALVECOMBINATION_T100_T300_T400 ||
				!m_pressureTankIsFull || self.private_sourceTankIsEmpty())
			{
				// No flow in with the current valve combination, pressure tank is not full or source tank is empty
				return 0;
			}
			
			return m_pumpManager.calculateFlow_l_min(Constants.PUMP_1, 0);
			
		default:
			
			self.private_logAndReturnError("Cannot calculate flow in for tank \"" + tank + "\"");
		}
	};
	
	/**
	 * Calculates the current flow out of a tank.
	 */
	this.calculateFlowOut_l_min = function(tank, flowIn, liqAmount)
	{
		// Checking that the tank has something in it (or flow in)
		if (liqAmount < 0.001 && flowIn < 0.001)
		{
			return 0;
		}
		
		var valveCombination = m_valveManager.getValveCombination();
		
		switch (tank)
		{
		case Constants.TANK_1:
			
			if (valveCombination !== Constants.VALVECOMBINATION_T100_T300_T400 &&
				valveCombination !== Constants.VALVECOMBINATION_T100_T300_CIRCULAR)
			{
				// No flow out with the current valve combination
				return 0;
			}
			
			return self.private_calcFlowInTank3();
			
		case Constants.TANK_2:
			
			if (valveCombination !== Constants.VALVECOMBINATION_T200_T300)
			{
				// No flow out with the current valve combination
				return 0;
			}
			
			return self.private_calcFlowInTank3();
			
		case Constants.TANK_3:
			
			return self.private_calcFlowOutTank3(flowIn);
			
		case Constants.TANK_4:
			
			if (valveCombination !== Constants.VALVECOMBINATION_T400_T300_T200)
			{
				// No flow out with the current valve combination
				return 0;
			}
			
			return self.private_calcFlowInTank3();
			
		default:
			
			self.private_logAndReturnError("Cannot calculate flow out for tank \"" + tank + "\"");
		}
	};
	
	this.private_calcFlowInTank1 = function()
	{
		if (self.private_sourceTankIsEmpty())
		{
			// Empty source tank
			return 0;
		}
		
		// Recognising valve combination
		var valveCombination = m_valveManager.getValveCombination();
		
		if (valveCombination === Constants.VALVECOMBINATION_T100_T300_CIRCULAR)
		{
			var flowInTank3a = self.private_calcFlowInTank3();
			return self.private_calcFlowOutTank3(flowInTank3a);
		}
		else if (valveCombination === Constants.VALVECOMBINATION_T300_T100)
		{
			var flowInTank3b = self.private_calcFlowInTank3();
			return self.private_calcFlowOutTank3(flowInTank3b);
		}
		else
		{
			// No flow in with the current valve combination
			return 0;
		}
	};
	
	this.private_calcFlowInTank3 = function()
	{
		if (self.private_sourceTankIsEmpty())
		{
			// Empty source tank
			return 0;
		}
		
		// Recognising valve combination
		var valveCombination = m_valveManager.getValveCombination();
		var expectedPump = "";
		
		// There will be no throttling effect with zero
		var yForThrottling = 0;
		
		if (valveCombination === Constants.VALVECOMBINATION_T200_T300 ||
			valveCombination === Constants.VALVECOMBINATION_T400_T300_T200)
		{
			expectedPump = Constants.PUMP_2;
		}
		else if (valveCombination === Constants.VALVECOMBINATION_T100_T300_T400)
		{
			expectedPump = Constants.PUMP_1;
		}
		else if (valveCombination === Constants.VALVECOMBINATION_T100_T300_CIRCULAR)
		{
			expectedPump = Constants.PUMP_1;
			
			// There is no throttling if the tank is not full
			if (m_pressureTankIsFull)
			{
				yForThrottling = m_valveManager.calculateYFactorForThrottling(Constants.VALVE_104);
			}
		}
		else
		{
			// No flow in with the current valve combination
			return 0;
		}
		
		return m_pumpManager.calculateFlow_l_min(expectedPump, yForThrottling);
	};
	
	this.private_sourceTankIsEmpty = function()
	{
		var valveCombination = m_valveManager.getValveCombination();
		
		switch (valveCombination)
		{
		case Constants.VALVECOMBINATION_T200_T300:
			return m_emptyTanks.contains(Constants.TANK_2);
			
		case Constants.VALVECOMBINATION_T400_T300_T200:
			return m_emptyTanks.contains(Constants.TANK_4);
			
		case Constants.VALVECOMBINATION_T100_T300_T400:
			return m_emptyTanks.contains(Constants.TANK_1);
			
		case Constants.VALVECOMBINATION_T100_T300_CIRCULAR:
			return m_emptyTanks.contains(Constants.TANK_1);
			
		case Constants.VALVECOMBINATION_T300_T100:
			return m_emptyTanks.contains(Constants.TANK_3);
			
		default:
			return false;
		}
	};
	
	this.private_calcFlowOutTank3 = function(flowIn)
	{
		// Recognising valve combination
		var valveCombination = m_valveManager.getValveCombination();
		
		if (valveCombination !== Constants.VALVECOMBINATION_T400_T300_T200 &&
			valveCombination !== Constants.VALVECOMBINATION_T100_T300_T400 &&
			valveCombination !== Constants.VALVECOMBINATION_T100_T300_CIRCULAR &&
			valveCombination !== Constants.VALVECOMBINATION_T300_T100)
		{
			return 0;
		}
		
		// Liquid extraction from the bottom?
		if (valveCombination === Constants.VALVECOMBINATION_T300_T100)
		{
			// Getting pump flow with zero throttling
			return m_pumpManager.calculateFlow_l_min(Constants.PUMP_2, 0);
		}
		
		if (!m_pressureTankIsFull)
		{
			// Not full -> no flow out
			return 0;
		}
		
		// Flow through
		return flowIn;
	};
	
	this.private_logAndReturnError = function(err)
	{
		console.error(err);
		throw err;
	};
}

module.exports = TankFlowManager;
