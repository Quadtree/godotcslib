/**
* This file is released under the MIT License: https://opensource.org/licenses/MIT
*/

using System;
using Godot;

[Serializable]
public struct IntVec2
{
    public int x;
    public int y;

    public IntVec2(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public static IntVec2 operator +(IntVec2 self, IntVec2 other)
    {
        return new IntVec2
        {
            x = self.x + other.x,
            y = self.y + other.y,
        };
    }

    public static bool operator ==(IntVec2 self, IntVec2 other)
    {
        return self.x == other.x && self.y == other.y;
    }

    public static bool operator !=(IntVec2 self, IntVec2 other)
    {
        return self.x != other.x || self.y != other.y;
    }

    public static IntVec2 operator /(IntVec2 self, int other)
    {
        return new IntVec2
        {
            x = self.x / other,
            y = self.y / other,
        };
    }

    public static IntVec2 operator -(IntVec2 self, IntVec2 other)
    {
        return new IntVec2
        {
            x = self.x - other.x,
            y = self.y - other.y,
        };
    }

    public float DistanceTo(IntVec2 other)
    {
        return Mathf.Sqrt(Util.Square(other.x - this.x) + Util.Square(other.y - this.y));
    }

    public override bool Equals(object obj)
    {
        if (obj is IntVec2) return this == (IntVec2)obj;
        return false;
    }

    public override int GetHashCode()
    {
        return (y << 15) + x;
    }

    public override string ToString()
    {
        return $"({x},{y})";
    }
}
