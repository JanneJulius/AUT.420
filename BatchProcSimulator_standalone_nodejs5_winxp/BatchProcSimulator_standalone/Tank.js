// Petri Kannisto
// TTI/AUT
// Tampere University of Technology
// Created: 6/2017
// Modified: 7/2017

var PowerEquipmentManager = require("./PowerEquipmentManager");
var ValveManager = require("./ValveManager");
var TankFlowManager = require("./TankFlowManager");
var Constants = require("./Constants");

/**
 * Represents a tank.
 */
function Tank(name, flowM, valveM, powerEqM)
{
	// These are treated like constants
	var SIMULATION_CYCLE_LENGTH_ms = 100;
	var PRESSURE_TANK_VOLUME_l = 2;
	var BASIC_TANK_VOLUME_l = 11;
	var BASIC_TANK_HEIGHT_m = 0.36;
	var BASIC_TANK_BOTTOM_AREA_m2 = BASIC_TANK_VOLUME_l / 1000 / BASIC_TANK_HEIGHT_m;
	var BASIC_TANK_LI_SENSOR_FROM_TOP_m = 0.03;
	var HEATER_POWER_w = 1000;
	var SPECIFIC_THERMAL_CAPACITY_J_per_kg_per_C = 4190;
	var MAX_PRESSURE_WHEN_FULL_atm = 0.29;
	var MAX_PRESSURE_WHEN_NOT_FULL_atm = 0.12;
	
	var m_name = name;
	var self = this;
	
	this.private_isPressureTank = function()
	{
		return m_name === Constants.TANK_3;
	};
	this.private_getInitialLiquidLevel = function()
	{
		switch (m_name)
		{
		case Constants.TANK_1:
			return 0.6 * BASIC_TANK_VOLUME_l;
			
		case Constants.TANK_2:
			return 0.25 * BASIC_TANK_VOLUME_l;
			
		case Constants.TANK_3:
			return 0.06 * PRESSURE_TANK_VOLUME_l;
			
		case Constants.TANK_4:
			return 0.25 * BASIC_TANK_VOLUME_l;
			
		default:
			throw "Unknown tank name \"" + m_name + "\"";
		}
	};
	
	var m_tankFlowManager = flowM;
	var m_valveManager = valveM;
	var m_powerEquipmentManager = powerEqM;
	var m_persistedPressure_atm = 0;
	var m_liquidAmount_l = self.private_getInitialLiquidLevel();
	var m_temperature_C = Constants.DEFAULT_TEMPERATURE_C;
	var m_heaterState = false;
	
	
	/**
	 * Run a single simulation step.
	 */
	this.runSimulation = function()
	{
		// Calculating flow values; these will be zero if the valve combination is unknown
		var flowIn_l_min = m_tankFlowManager.calculateFlowIn_l_min(m_name);
		var flowOut_l_min = m_tankFlowManager.calculateFlowOut_l_min(m_name, flowIn_l_min, m_liquidAmount_l);
		
		// Running simuation
		self.private_simulatePressure(flowOut_l_min);
		self.private_simulateLiquidAmount(flowIn_l_min, flowOut_l_min);
		self.private_simulateTemperature(flowIn_l_min);
	};
	
	this.private_simulatePressure = function(flowOut_l_min)
	{
		// Scenario 1: not a pressure tank -> no pressure
		if (!self.private_isPressureTank())
		{
			m_persistedPressure_atm = 0;
			return;
		}
		
		// Scenario 2: pressure escapes due to open valves
		if (m_valveManager.getOnOffValvePosition(Constants.VALVE_204) ||
			m_valveManager.getOnOffValvePosition(Constants.VALVE_302) ||
			m_valveManager.getOnOffValvePosition(Constants.VALVE_401))
		{
			m_persistedPressure_atm = 0;
			return;
		}
		
		// Does the channel in have pressure from a pump?
		var pumpingIn_pump1 =
			m_valveManager.getControlValvePosition(Constants.VALVE_102) > 0 &&
			m_valveManager.getOnOffValvePosition(Constants.VALVE_301) &&
			m_valveManager.getOnOffValvePosition(Constants.VALVE_304) &&
			m_powerEquipmentManager.getEquipmentState(Constants.PUMP_1);
		var pumpingIn_pump2 =
			m_valveManager.getOnOffValvePosition(Constants.VALVE_201) &&
			m_valveManager.getOnOffValvePosition(Constants.VALVE_301) &&
			m_valveManager.getOnOffValvePosition(Constants.VALVE_303) &&
			m_powerEquipmentManager.getEquipmentState(Constants.PUMP_2);
		
		var throttleValvePosition = m_valveManager.getControlValvePosition(Constants.VALVE_104);
		
		if (pumpingIn_pump1 || pumpingIn_pump2)
		{
			if (throttleValvePosition < 1)
			{
				if (m_liquidAmount_l < PRESSURE_TANK_VOLUME_l)
				{
					// Scenario 3a: some pressure due to pumping into a non-full tank with full throttling
					m_persistedPressure_atm = MAX_PRESSURE_WHEN_NOT_FULL_atm;
					return;
				}
				else // Full tank
				{
					// Scenario 3b: full pressure due to pumping into a full tank with full throttling
					m_persistedPressure_atm = MAX_PRESSURE_WHEN_FULL_atm;
					return;
				}
			}
			else
			{
				// Scenario 4: there is (possibly throttled) flow through the pressure tank
				var yForThrottling = m_valveManager.calculateYFactorForThrottling(Constants.VALVE_104);
				
				// pd = y * Q
				m_persistedPressure_atm = yForThrottling * flowOut_l_min;
				return;
			}
		}
		else
		{
			// Scenario 5: the existing pressure persists due to closed valves although no pumping
			if (throttleValvePosition < 1)
			{
				return;
			}
			// Scenario 6: pressure escapes due to open throttling valve and no flow in
			else
			{
				m_persistedPressure_atm = 0;
				return;
			}
		}
	};
	
	this.private_simulateLiquidAmount = function(flowIn_l_min, flowOut_l_min)
	{
		var netFlowIn = flowIn_l_min - flowOut_l_min; // May be negative
		
		// Liquid amount simulation
		if (Math.abs(netFlowIn) > 0.001)
		{
			var netFlowIn_perSecond = netFlowIn / 60;
			var simulationCycle_seconds = SIMULATION_CYCLE_LENGTH_ms / 1000;
			var netFlowDuringCycle = netFlowIn_perSecond * simulationCycle_seconds;
			
			m_liquidAmount_l = m_liquidAmount_l + netFlowDuringCycle;
		}
		
		// Do not allow negative liquid amount; set "tank is empty" flag for flow calculation
		if (m_liquidAmount_l < 0.001)
		{
			m_liquidAmount_l = 0;
			m_tankFlowManager.setTankIsEmpty(m_name, true);
		}
		else
		{
			m_tankFlowManager.setTankIsEmpty(m_name, false);
		}
		
		// Liquid amount check for pressure tank
		if (self.private_isPressureTank())
		{
			// Do not allow a liquid amount above the volume
			if (m_liquidAmount_l >= PRESSURE_TANK_VOLUME_l)
			{
				m_liquidAmount_l = PRESSURE_TANK_VOLUME_l;
				m_tankFlowManager.setPressureTankIsFull(true);
			}
			else
			{
				m_tankFlowManager.setPressureTankIsFull(false);
			}
		}
	};
	
	this.private_simulateTemperature = function(flowIn_l_min)
	{
		// Temperature change with heating:
		// 
        //       P t
		// dT = -----
		//       c m
		//
		// P = 1000 W
		// t = simulation cycle length
		// c = 4190 J/kg/C
		// m ^ liquid volume in liters (water)
		
		// These are used to speed up heating simulation (for a faster client application development)
		var HEATING_MULTIPLIER = 10;
		var MIXING_MULTIPLIER = 10;
		var COOLING_MULTIPLIER = 25;
		
		if (m_name === Constants.TANK_1)
		{
			var dT = 0;
			
			if (m_heaterState)
			{
				// Heating is on
				dT = HEATER_POWER_w * (SIMULATION_CYCLE_LENGTH_ms / 1000) / SPECIFIC_THERMAL_CAPACITY_J_per_kg_per_C / m_liquidAmount_l;
				dT = dT * HEATING_MULTIPLIER;
			}
			
			m_temperature_C += dT;
		}
		
		// Calculating temperature change due to flow in. Mass and volume are assumed proportional.
	    //      m1*T1 + m2*T2
		// T = ---------------
		//         m1 + m2
		
		if (flowIn_l_min >= 0.001)
		{
			var flowIn_perSecond_l = flowIn_l_min / 60;
			var simulationCycle_seconds = SIMULATION_CYCLE_LENGTH_ms / 1000;
			var flowInDuringCycle_l = flowIn_perSecond_l * simulationCycle_seconds;
			flowInDuringCycle_l = flowInDuringCycle_l * MIXING_MULTIPLIER;
			var temperatureOfFlowIn_C = m_tankFlowManager.getTemperatureOfFlowIn_C(m_name);
			
			m_temperature_C =
				(m_liquidAmount_l * m_temperature_C + flowInDuringCycle_l * temperatureOfFlowIn_C) /
				(m_liquidAmount_l + flowInDuringCycle_l);
		}
		
		// Cooling is proportional to temperature difference.
		// T is temperature in, T0 is temperature out (20 C), a is constant.
		// 
		// dT = a(T-T0) = f(T)
		// 
		// 1) No change when no difference:
		//    f(0) = 0
		// 
		// 2) 30 C difference causes 1 C/min cooling speed:
		//    f(50) = 1/60
		// 
		// -> that makes a = 1/1800
		// 
		//          T - T0
		// -> dT = --------
		//           1800
		// (dT is per second!)
		
		// Calculating cooling effect
		var dT_cooling = -1 * (m_temperature_C - Constants.DEFAULT_TEMPERATURE_C) / 1800 / 10;
		dT_cooling = dT_cooling * COOLING_MULTIPLIER;
		m_temperature_C += dT_cooling;
		
		// Limiting maximal temperature
		if (m_temperature_C > 80)
		{
			m_temperature_C = 80;
		}
		
		// Setting temperature so others can use it
		m_tankFlowManager.setTemperature_C(m_name, m_temperature_C);
	};
	
	/**
	 * Gets the current liquid level in percents.
	 */
	this.getLiquidLevel_percent = function()
	{
		if (self.private_isPressureTank())
		{
			return m_liquidAmount_l / PRESSURE_TANK_VOLUME_l;
		}
		else
		{
			return m_liquidAmount_l / BASIC_TANK_VOLUME_l;
		}
	};
	
	/**
	 * Gets the state of a signal.
	 */
	this.getSignalState = function(name)
	{
		if (m_name === Constants.TANK_1 && name === Constants.LA_PLUS_100)
		{
			return self.getLiquidLevel_percent() < 0.95; // Level alarm 95%, false when surface is above
		}
		if (m_name === Constants.TANK_2 && name === Constants.LS_MINUS_200)
		{
			return self.getLiquidLevel_percent() >= 0.20; // Level signal 20%, true when surface is above
		}
		if (m_name === Constants.TANK_3 && name === Constants.LS_MINUS_300)
		{
			return self.getLiquidLevel_percent() >= 0.05; // Level signal 5%, true when surface is above
		}
		if (m_name === Constants.TANK_3 && name === Constants.LS_PLUS_300)
		{
			return self.getLiquidLevel_percent() >= 0.95; // Level signal 95%, true when surface is above
		}
		else
		{
			self.private_logAndReturnError("Tank \"" + m_name + "\" has no signal called \"" + name + "\"");
		}
	};
	
	/**
	 * Gets the value of a sensor.
	 */
	this.getSensorValue = function(name)
	{
		if (m_name === Constants.TANK_1 && name === Constants.FI100)
		{
			var flowIn_l_min = m_tankFlowManager.calculateFlowIn_l_min(m_name);
			return m_tankFlowManager.calculateFlowOut_l_min(m_name, flowIn_l_min, m_liquidAmount_l);
		}
		else if (m_name === Constants.TANK_1 && name === Constants.LI100 ||
				m_name === Constants.TANK_2 && name === Constants.LI200 ||
				m_name === Constants.TANK_4 && name === Constants.LI400)
		{
			return self.private_getLiquidLevel_mm();
		}
		else if (m_name === Constants.TANK_3 && name === Constants.PI300)
		{
			return m_persistedPressure_atm * 1000; // 1000 for hPa
		}
		else if (m_name === Constants.TANK_1 && name === Constants.TI100 ||
				m_name === Constants.TANK_3 && name === Constants.TI300)
		{
			return m_temperature_C;
		}
		else
		{
			self.private_logAndReturnError("Tank \"" + m_name + "\" has no sensor called \"" + name + "\"");
		}
	};
	
	/**
	 * Gets the state of the heater.
	 */
	this.getHeaterState = function()
	{
		if (m_name !== Constants.TANK_1)
		{
			self.private_logAndReturnError("This tank has no heater: " + m_name);
		}
		
		return m_heaterState;
	};
	
	/**
	 * Sets the state of the heater.
	 */
	this.setHeaterState = function(value)
	{
		if (m_name !== Constants.TANK_1)
		{
			self.private_logAndReturnError("This tank has no heater: " + m_name);
		}
		if (typeof(value) !== "boolean")
		{
			self.private_logAndReturnError("Heater state must be of type boolean");
		}
		
		m_heaterState = value;
	};
	
	this.private_getLiquidLevel_mm = function()
	{
		if (self.private_isPressureTank())
		{
			self.private_logAndReturnError("getLiquidLevel_m() not supported for pressure tank");
		}
		
		return self.getLiquidLevel_percent() * BASIC_TANK_HEIGHT_m * 1000; // 1000 for mm
	};
	
	this.private_logAndReturnError = function(err)
	{
		console.error(err);
		throw err;
	};
}

module.exports = Tank;
