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

using System.Runtime.InteropServices;
namespace Mooege.Net.GS.Message
{
    public enum GameAttributeEncoding
    {
        Int,
        IntMinMax,
        //FloatMinMax,
        Float16,
        Float16Or32,
        Float32,
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct GameAttributeValue
    {
        [FieldOffset(0)]
        public int Value;
        [FieldOffset(0)]
        public float ValueF;

        public GameAttributeValue(int value) { ValueF = 0f; Value = value; }
        public GameAttributeValue(float value) { Value = 0; ValueF = value; }
    }

    public partial class GameAttribute
    {
        public const float Float16Min = -65536.0f;
        public const float Float16Max = 65536.0f;

        public int Id;
        public GameAttributeValue _DefaultValue;
        public int U3;
        public int U4;
        public int U5;

        public string ScriptA;
        public string ScriptB;
        public string Name;

        public GameAttributeEncoding EncodingType;

        public byte U10;

        public GameAttributeValue Min;
        public GameAttributeValue Max;
        public int BitCount;

        public bool IsInteger { get { return EncodingType == GameAttributeEncoding.Int || EncodingType == GameAttributeEncoding.IntMinMax; } }

        public GameAttribute() { }

        public GameAttribute(int id, int defaultValue, int u3, int u4, int u5, string scriptA, string scriptB, string name, GameAttributeEncoding encodingType, byte u10, int min, int max, int bitCount)
        {
            Id = id;
            _DefaultValue.Value = defaultValue;
            U3 = u3;
            U4 = u4;
            U5 = u5;
            ScriptA = scriptA;
            ScriptB = scriptB;
            Name = name;
            EncodingType = encodingType;
            U10 = u10;

            Min = new GameAttributeValue(min);
            Max = new GameAttributeValue(max);
            BitCount = bitCount;
        }

        public GameAttribute(int id, float defaultValue, int u3, int u4, int u5, string scriptA, string scriptB, string name, GameAttributeEncoding encodingType, byte u10, float min, float max, int bitCount)
        {
            Id = id;
            _DefaultValue.ValueF = defaultValue;
            U3 = u3;
            U4 = u4;
            U5 = u5;
            ScriptA = scriptA;
            ScriptB = scriptB;
            Name = name;
            EncodingType = encodingType;
            U10 = u10;

            Min = new GameAttributeValue(min);
            Max = new GameAttributeValue(max);
            BitCount = bitCount;
        }
    }


    public class GameAttributeI : GameAttribute
    {
        public int DefaultValue { get { return _DefaultValue.Value; } }

        public GameAttributeI() { }

        public GameAttributeI(int id, int defaultValue, int u3, int u4, int u5, string scriptA, string scriptB, string name, GameAttributeEncoding encodingType, byte u10, int min, int max, int bitCount)
            : base(id, defaultValue, u3, u4, u5, scriptA, scriptB, name, encodingType, u10, min, max, bitCount)
        {

        }
    }

    public class GameAttributeF : GameAttribute
    {
        public float DefaultValue { get { return _DefaultValue.ValueF; } }

        public GameAttributeF() { }
        public GameAttributeF(int id, float defaultValue, int u3, int u4, int u5, string scriptA, string scriptB, string name, GameAttributeEncoding encodingType, byte u10, float min, float max, int bitCount)
            : base(id, defaultValue, u3, u4, u5, scriptA, scriptB, name, encodingType, u10, min, max, bitCount)
        {

        }
    }

    public class GameAttributeB : GameAttribute
    {
        public bool DefaultValue { get { return _DefaultValue.Value != 0; } }

        public GameAttributeB() { }
        public GameAttributeB(int id, int defaultValue, int u3, int u4, int u5, string scriptA, string scriptB, string name, GameAttributeEncoding encodingType, byte u10, int min, int max, int bitCount)
            : base(id, defaultValue, u3, u4, u5, scriptA, scriptB, name, encodingType, u10, min, max, bitCount)
        {

        }

    }


}