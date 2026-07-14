using Raptor.Attributes;

namespace Raptor.StdLib;

[RaptorModule("math")]
public static class RaptorMath
{
    [RaptorMethod]
    [RaptorDescription("Calculates the sin of an angle in radians.")]
    [RaptorParam("angle", "Angle in radians")]
    public static void Sin(ref VMState state)
    {
        unsafe
        {
            state.RegPtr[0] = Math.Sin(state.RegPtr[1]);
        }
    }

    [RaptorMethod]
    [RaptorDescription("Calculates the cosine of an angle in radians.")]
    [RaptorParam("angle", "Angle in radians")]
    public static void Cos(ref VMState state)
    {
        unsafe
        {
            state.RegPtr[0] = Math.Cos(state.RegPtr[1]);
        }
    }

    [RaptorMethod]
    [RaptorDescription("Calculates the tangent of an angle in radians.")]
    [RaptorParam("angle", "Angle in radians")]
    public static void Tan(ref VMState state)
    {
        unsafe
        {
            state.RegPtr[0] = Math.Tan(state.RegPtr[1]);
        }
    }

    [RaptorDescription("Calculates the specified number to the specified power.")]
    [RaptorParam("base", "Number that specifies the base.")]
    [RaptorParam("power", "Number that specifies the power.")]
    public static void Pow(ref VMState state)
    {
        unsafe
        {
            state.RegPtr[0] = Math.Pow(state.RegPtr[1], state.RegPtr[2]);
        }
    }

    [RaptorMethod]
    [RaptorDescription("Returns the biggest integer smaller or equal than the specified number.")]
    [RaptorParam("x", "Specifies the number whose floor we will find.")]
    public static void Floor(ref VMState state)
    {
        unsafe
        {
            state.RegPtr[0] = Math.Floor(state.RegPtr[1]);
        }
    }

    [RaptorMethod]
    [RaptorDescription("Returns the smallest integer bigger or equal than the specified number.")]
    [RaptorParam("x", "Specifies the number whose ceiling we will find.")]
    public static void Ceiling(ref VMState state)
    {
        unsafe
        {
            state.RegPtr[0] = Math.Ceiling(state.RegPtr[1]);
        }
    }

    [RaptorMethod]
    [RaptorDescription("Returns the smaller number between two numbers.")]
    [RaptorParam("x", "First number.")]
    [RaptorParam("y", "Second number.")]
    public static void Min(ref VMState state)
    {
        unsafe
        {
            state.RegPtr[0] = Math.Min(state.RegPtr[1], state.RegPtr[2]);
        }
    }

    [RaptorMethod]
    [RaptorDescription("Returns the larger number between two numbers.")]
    [RaptorParam("x", "First number.")]
    [RaptorParam("y", "Second number.")]
    public static void Max(ref VMState state)
    {
        unsafe
        {
            state.RegPtr[0] = Math.Max(state.RegPtr[1], state.RegPtr[2]);
        }
    }

    [RaptorMethod]
    [RaptorDescription("Returns the square root of a number.")]
    [RaptorParam("x", "Specifies the number whose square root will find.")]
    public static void Sqrt(ref VMState state)
    {
        unsafe
        {
            state.RegPtr[0] = Math.Sqrt(state.RegPtr[1]);
        }
    }

    [RaptorMethod]
    [RaptorDescription("Returns the absolute value of a number.")]
    [RaptorParam("x", "Specifies the number whose absolute value we will find.")]
    public static void Abs(ref VMState state)
    {
        unsafe
        {
            state.RegPtr[0] = Math.Abs(state.RegPtr[1]);
        }
    }

    [RaptorMethod]
    [RaptorDescription("Returns the angle whose tangent is the quotient of two specified numbers")]
    [RaptorParam("x", "The x coordinate of a point.")]
    [RaptorParam("y", "The y coordinate of a point.")]
    public static void Atan2(ref VMState state)
    {
        unsafe
        {
            state.RegPtr[0] = Math.Atan2(state.RegPtr[1], state.RegPtr[2]);
        }
    }

    [RaptorMethod]
    [RaptorDescription("Returns a number clamped between a min and a max.")]
    [RaptorParam("number", "The number we will clamp.")]
    [RaptorParam("min", "The lower bound of the result.")]
    [RaptorParam("max", "The upper bound of the result.")]
    public static void Clamp(ref VMState state)
    {
        unsafe
        {
            state.RegPtr[0] = Math.Clamp(state.RegPtr[1], state.RegPtr[2], state.RegPtr[3]);
        }
    }

    [RaptorMethod]
    [RaptorDescription("Get the value of pi.")]
    public static void Pi(ref VMState state)
    {
        unsafe
        {
            state.RegPtr[0] = Math.PI;
        }
    }
}
