﻿/*
 * Copyright (C) 2011 mooege project
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mooege.Common;
using System.Reflection;

namespace Mooege.Core.GS.Powers
{
    public class PowerLoader
    {
        static readonly Logger Logger = LogManager.CreateLogger();

        private static Dictionary<int, Type> _implementations = new Dictionary<int, Type>();

        public static PowerImplementation CreateImplementationForPowerSNO(int powerSNO)
        {
            if (_implementations.ContainsKey(powerSNO))
            {
                return (PowerImplementation)Activator.CreateInstance(_implementations[powerSNO]);
            }
            else
            {
                Logger.Debug("Unimplemented power: {0}", powerSNO);
                return null;
            }
        }

        static PowerLoader()
        {
            // Find all subclasses of PowerImplementation and index them by the PowerSNO they are attributed with.
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsSubclassOf(typeof(PowerImplementation)))
                {
                    var attributes = (ImplementsPowerSNO[])type.GetCustomAttributes(typeof(ImplementsPowerSNO), true);
                    foreach (var powerAttribute in attributes)
                    {
                        _implementations[powerAttribute.PowerSNO] = type;
                    }
                }
            }
        }
    }
}
