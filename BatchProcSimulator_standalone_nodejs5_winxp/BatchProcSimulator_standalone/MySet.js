// Petri Kannisto
// TTI/AUT
// Tampere University of Technology
// Created: 6/2017
// Modified: 6/2017

/**
 * This class was created to wrap the ES set class as its delete keyword causes problems in Enide.
 * This class also provides the helper method equalsOtherSet().
 */
function MySet(initialItems)
{
	var self = this;
	var m_actualSet = new Set();
	
	// Adding initial items
	if (initialItems !== undefined)
	{
		initialItems.forEach(function(value)
		{
			m_actualSet.add(value);
		});
	}
	
	/**
	 * Adds an item.
	 */
	this.add = function(item)
	{
		m_actualSet.add(item);
	};
	
	/**
	 * Gets the size of the set.
	 */
	this.getSize = function()
	{
		return m_actualSet.size;
	};
	
	/**
	 * Whether the set contains given item.
	 */
	this.contains = function(item)
	{
		return m_actualSet.has(item);
	};
	
	/**
	 * Whether the set equals another set.
	 */
	this.equalsOtherSet = function(other)
	{
		if (self.getSize() !== other.getSize())
		{
			return false;
		}
		
		// This is required as the inline foreach function cannot end this function
		var atLeastOneValueNotFound = false;
		
		m_actualSet.forEach(function(value)
		{
			if (!other.contains(value))
			{
				atLeastOneValueNotFound = true;
				return false;
			}
		});
		
	    return !atLeastOneValueNotFound;
	};
	
	/**
	 * Removes an item from the set.
	 */
	this.remove = function(item)
	{
		m_actualSet.delete(item);
	};
}

module.exports = MySet;
