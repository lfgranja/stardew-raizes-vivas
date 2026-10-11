using System;
using FsCheck;

namespace LivingRoots.Tests
{
    /// <summary>
    /// Property-based generator for stack-size values covering max-stack-1
    /// and overflow cases (dislodged bin inventory/debris) per FR-005c.
    /// </summary>
    public class StackSizeGenerator : Arbitrary<int>
    {
        public override Gen<int> Generator
        {
            get
            {
                // Cover the valid max-stack-1 boundary and the violation (>1)
                return Gen.OneOf(
                    Gen.Constant(0),
                    Gen.Constant(1),
                    Gen.Choose(2, 20)  // violation domain
                );
            }
        }
    }
}
