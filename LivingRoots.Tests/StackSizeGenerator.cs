using FsCheck;

namespace LivingRoots.Tests
{
    /// <summary>
    /// Property-based generator for stack-size values covering the max-stack-1
    /// boundary and the violation domain (>1) per FR-005c.
    /// </summary>
    /// <remarks>
    /// FsCheck.Xunit discovers custom arbitraries through public static members
    /// on the named type returning <see cref="Arbitrary{T}"/> — inheriting
    /// <see cref="Arbitrary{Int32}"/> is not discovered and fails with "No
    /// instances found on type".
    /// </remarks>
    public static class StackSizeGenerator
    {
        /// <summary>Generates stack sizes: the 0/1 boundary plus the 2-20 violation domain.</summary>
        public static Arbitrary<int> StackSize()
        {
            return Arb.From(
                Gen.OneOf(
                    Gen.Constant(0),
                    Gen.Constant(1),
                    Gen.Choose(2, 20)));
        }
    }
}
